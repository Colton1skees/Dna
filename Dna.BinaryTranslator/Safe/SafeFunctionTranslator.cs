using AsmResolver;
using AsmResolver.PE;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using Dna.Binary;
using Dna.Binary.Windows;
using Dna.BinaryTranslator.Unsafe;
using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Dna.Extensions;
using Dna.LLVMInterop.API;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.Relocation;
using Dna.Utilities;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebAssembly.Instructions;
using X86Block = Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>;
using static Iced.Intel.AssemblerRegisters;
using System.Reflection;
using Dna.BinaryTranslator.Runtime;
using ELFSharp.Utilities;
using Dna.Reconstruction;
using AsmResolver.PE.Exceptions.X64;
using Dna.SEH;
using System.Reflection.PortableExecutable;
using Dna.BinaryTranslator.Lifting;
using System.Reflection.Metadata;

namespace Dna.BinaryTranslator.Safe
{
    public record SafelyTranslatedFunction(BinaryFunction BinaryFunction, SafeNontrivialExecutableRuntimeImplementer Runtime, IReadOnlyList<LiftedFilterFunction> LiftedFilterFunction);

    /// <summary>
    /// Class for statically translating a complete control flow graph to LLVM IR.
    /// This function produces executable LLVM IR for a given function,
    /// while making no unsafe assumptions.
    /// </summary>
    public class SafeFunctionTranslator
    {
        private readonly IDna dna;

        private RemillArch arch;

        private readonly LLVMContextRef ctx;

        private BinaryFunction binaryFunction;

        private ControlFlowGraph<Instruction> cfg;

        public static SafelyTranslatedFunction Translate(IDna dna, RemillArch arch, LLVMContextRef ctx, BinaryFunction binaryFunction)
            => new SafeFunctionTranslator(dna, arch, ctx, binaryFunction).Translate();

        private SafeFunctionTranslator(IDna dna, RemillArch arch, LLVMContextRef ctx, BinaryFunction binaryFunction)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.binaryFunction = binaryFunction;
            cfg = binaryFunction.Cfg;
        }

        private unsafe SafelyTranslatedFunction Translate()
        {
            // Preprocess the CFG to make it liftable.
            (binaryFunction, var fallthroughFromIps, var splitTargets) = PreprocessCfg();

            Console.WriteLine("\n\n\n\n\n\n");
            Console.WriteLine($"{GraphFormatter.FormatGraph(binaryFunction.Cfg)}");

            // Translate the control flow graph to LLVM IR.
            bool isolateFunctionIntoNewModule = false;
            var (translatedFunction, blockMapping, liftedSehEntries) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, binaryFunction, fallthroughFromIps, CallHandlingKind.Vmexit);

            // Insert the virtual dispatcher.
            VirtualDispatcher.CreateInFunction(translatedFunction, cfg, binaryFunction.ScopeTableTree, liftedSehEntries, blockMapping, splitTargets);

            // Isolate the lifted function into it's own module. This removes the ~50mb of unused remill semantics bloat.1
            (translatedFunction, var newEntries) = FunctionIsolator.IsolateFunctionIntoNewModuleWithSehSupport(arch, translatedFunction, liftedSehEntries.Select(x => x.LiftedFilterFunction).ToList().AsReadOnly());
            translatedFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Implement all of the trivial remill runtime components. E.g. memory read, write, flag calculations.
            var parameterizedStateStruct = ImplementTrivialRuntimeComponents(translatedFunction);

            // Implement all of the nontrivial runtime components(vmcall, vmreturn).
            var executableRuntime = ImplementNontrivialRuntimeComponents(parameterizedStateStruct, splitTargets);

            // Update the "LiftedFilterFunction" data structures to map to the newly isolated module.
            var updatedFilterFunctions = GetUpdatedFilterFunctionsForIsolatedModule(executableRuntime.OutputFunction.GlobalParent, liftedSehEntries.Select(x => x.LiftedFilterFunction).ToList());

            // Implement the SEH stack pointer escape logic.
            ImplementSehStackPointerEscapes(executableRuntime.OutputFunction, updatedFilterFunctions);

            // Enter recompilation code, which is in dire need of a refctor
            var scratchAddresses = GetScratchSectionRva((dna.Binary as WindowsBinary));

            HashSet<Instruction> x86CallInstructions = new();
            x86CallInstructions.AddRange(cfg.GetInstructions().Where(x => x.Mnemonic == Mnemonic.Call));
            var vmCallStubs = new Dictionary<ulong, ulong>();
            ulong i = 0;
            // foreach (var splitTarget in splitTargets.Where(x => x != cfg.Nodes.First()))

            Dictionary<X86Block, ulong> blockToEnterRva = new();
            foreach (var splitTarget in splitTargets)
            {
                var rva = scratchAddresses.firstSectionRva + (i * 8);
                vmCallStubs.Add(splitTarget.Address, rva);
                i++;
                blockToEnterRva.Add(splitTarget, rva);
            }

            executableRuntime.OutputFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
            // Replace the RSP global variables with their corresponding rva.
            var builder = LLVMBuilderRef.Create(ctx);
            var imgBasePtr = executableRuntime.OutputFunction.LastParam;
            foreach(var (block, enterRva) in blockToEnterRva)
            {
                var glob = executableRuntime.CallKeyToStubVmEnterGlobalPtrs[block.Address];

                var newValue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, enterRva);

                /*
                glob.Initializer = newValue;
                //glob.ReplaceAllUsesWith(newValue);
                glob.Linkage = LLVMLinkage.LLVMPrivateLinkage;
                //glob.DeleteGlobal();
                */
                foreach (var user in glob.GetUsers())
                {
                    Debug.Assert(user.Kind == LLVMValueKind.LLVMInstructionValueKind && user.InstructionOpcode == LLVMOpcode.LLVMLoad);
                    Console.WriteLine($"User: {user}");
                    builder.PositionBefore(user);
                    var sum = builder.BuildAdd(imgBasePtr, newValue);

                    user.ReplaceAllUsesWith(sum);
                    user.InstructionEraseFromParent();
                }
                
            }

            foreach(var glob in executableRuntime.CallKeyToStubVmEnterGlobalPtrs.Values)
                glob.DeleteGlobal();

            // Delete the ret stub global variable.
            var retRva = scratchAddresses.firstSectionRva + (i * 8);
            var retStubGlobal = executableRuntime.RetStubOffsetPtrGlobal;
            // retStubGlobal.Initializer = (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, retRva));
            //retStubGlobal.Linkage = LLVMLinkage.LLVMPrivateLinkage;
            foreach (var user in retStubGlobal.GetUsers())
            {
                Debug.Assert(user.Kind == LLVMValueKind.LLVMInstructionValueKind && user.InstructionOpcode == LLVMOpcode.LLVMLoad);
                builder.PositionBefore(user);
                var sum = builder.BuildAdd(imgBasePtr, (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, retRva)));

                user.ReplaceAllUsesWith(sum);
                user.InstructionEraseFromParent();
            }
            retStubGlobal.DeleteGlobal();
            //retStubGlobal.DeleteGlobal();


            // Then go through and re

            var peImage = (SerializedPEImage)PEImage.FromBytes(dna.Binary.Bytes);
            var exceptions = peImage.Exceptions.GetEntries().ToList();
            var runtimeFunction = exceptions.Single(x => (ulong)x.Begin.Rva + dna.Binary.BaseAddress == binaryFunction.Cfg.StartAddress) as X64RuntimeFunction;
            var unwindInfo = runtimeFunction.UnwindInfo;

            // Compute the stack height from the unwind info.
            var uwcAddr = dna.Binary.BaseAddress + unwindInfo.Rva + 0x4;
            var codes = UnwindCodeParser.ParseUnwindCode(dna.Binary, uwcAddr, unwindInfo.UnwindCodes.Length * 2, unwindInfo.Version);
            var stackHeight = StackHeightCalculator.Get(codes);

            var assembler = new Assembler(64);
            var label = assembler.CreateLabel("referencePoint");
            assembler.Label(ref label);

            var liftedFuncAddr = scratchAddresses.firstSectionRva + (i * 8);

            var shouldAllocateStackFrame = (ulong vmKey) =>
            {
                // For now we only allocate the stack frame in the entry basic block.
                // TODO: In the future there may be cases where the stack frame is allocated
                // outside of the basic block.
                if (vmKey == cfg.GetBlocks().First().Address)
                    return true;

                return false;
            };

            executableRuntime.OutputFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
            //Console.WriteLine("\ndone done done");
            //Console.ReadLine();
            string path = null;
            bool toDll = true;
            if (toDll)
            {
                path = ClangCompiler.CompileToWindowsDll(executableRuntime.OutputFunction, "translatedFunction.ll", false);
            }

            else
            {
                path = ClangCompiler.Compile("translatedFunction.ll", false);
            }

            //var otherDiff =
            var stubBuilder = new VmStubBuilder(binaryFunction.Cfg.StartAddress, assembler, liftedFuncAddr, stackHeight, parameterizedStateStruct.OrderedRegisterArguments, label, scratchAddresses.secondSectionRva, dna.Binary.BaseAddress, shouldAllocateStackFrame, dna.Binary.BaseAddress + scratchAddresses.thirdSectionRva);
            stubBuilder.EncodeVmEnter(binaryFunction.Cfg.StartAddress);

            MergeExecutable(assembler, stubBuilder, executableRuntime.OutputFunction.Name, parameterizedStateStruct, WindowsBinary.From(path).PEFile, scratchAddresses, vmCallStubs.AsReadOnly(), liftedFuncAddr, path);


            // Compile and load it into IDA.
            executableRuntime.OutputFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
            var opt = ClangCompiler.Optimize(executableRuntime.OutputFunction.GlobalParent, "translatedFunction.ll");
            var p = ClangCompiler.Compile("translatedFunction.ll");
            var ida = IDALoader.Load(p);



            return new SafelyTranslatedFunction(binaryFunction, executableRuntime, updatedFilterFunctions);

            /*
            executableRuntime.OutputFunction.GlobalParent.PrintToFile("translatedFunction.ll");
            string path = null;
            bool toDll = true;
            if (toDll)
                path = ClangCompiler.CompileToWindowsDll(executableRuntime.OutputFunction, "translatedFunction.ll", false);
            else
                path = ClangCompiler.Compile("translatedFunction.ll", false);

            IDALoader.Load(path, true);
            */

        }

        private (BinaryFunction binaryFunction, HashSet<ulong> fallthroughFromIps, IReadOnlySet<X86Block> splitTargets) PreprocessCfg()
        {
            var scopeTableTree = binaryFunction.ScopeTableTree;

            // Deduplicate all fallthrough edges to remove all duplicated
            // code.
            (cfg, var fallthroughFromIps) = FallthroughDeduplicator.DeduplicateFallthroughEdges(cfg, Enumerable.Empty<ulong>());

            // At each CALL instruction within a basic block, split the basic block into two
            // separate basic blocks. Note that the BinaryFunction object does not need to be updated
            // because we don't actually insert or modify any existing instructions.
            var sehPoints = scopeTableTree.ScopeTable.Entries.SelectMany(x => new List<ulong>() { x.BeginAddr, x.EndAddr, x.HandlerAddr }).ToList();
            var (splitTargets, newFallthroughFromIps) = X86CfgSplitter.SplitBlocksAtSeh(cfg, sehPoints.ToHashSet());
            fallthroughFromIps.AddRange(newFallthroughFromIps);

            // Split blocks at calls.
            (splitTargets, newFallthroughFromIps) = X86CfgSplitter.SplitBlocksAtCalls(cfg);
            fallthroughFromIps.AddRange(newFallthroughFromIps);

            // Recreate the binary function, this time with fallthrough edges deduplicated.
            binaryFunction = new BinaryFunction(X86CfgEncoder.EncodeCfg(dna.Binary, cfg), binaryFunction.ScopeTableTree, binaryFunction.JmpTables);

            // Append the entry basic block to the set of split targets.
            // Note: This is because VirtualDispatcher.CreateInFunction expects a set of dispatcher targets.
            // One of which must be the entry basic block.
            var clone = splitTargets.ToHashSet();
            clone.Add(cfg.GetBlocks().First());
            splitTargets = clone;

            return (binaryFunction, fallthroughFromIps, splitTargets);
        }

        private ParameterizedStateStructure ImplementTrivialRuntimeComponents(LLVMValueRef translatedFunction)
        {
            // Insert a state structure. Note that we explicitly disable the memory pointer here.
            // TODO: *Stop inlining inside of this code*.
            // For whatever reason the inlining logic ends up converting musttails to non tail calls
            // which screws up everything.
            // However for now, we temporarily fix this by converting calls to RET and CALL to 'musttail' calls.
            var parameterizedStateStruct = ParameterizedStateStructure.CreateFromFunction(arch, translatedFunction, false);

            // Implement all of the "easy"(stateless) runtime methods, e.g. memory_write, memory_read.
            SafeTrivialRuntimeImplementer.Implement(parameterizedStateStruct.OutputFunction.GlobalParent);

            return parameterizedStateStruct;
        }

        private SafeNontrivialExecutableRuntimeImplementer ImplementNontrivialRuntimeComponents(ParameterizedStateStructure parameterizedStateStruct, IReadOnlySet<X86Block> vmReentryPoints)
        {
            // Collect all 'call' instructions in the control flow graph.
            List<Instruction> x86CallInstructions = new();
            x86CallInstructions.AddRange(cfg.GetInstructions().Where(x => x.Mnemonic == Mnemonic.Call));
            var callInsts = x86CallInstructions.ToDictionary(x => x.IP, x => x);

            // Create a global variable which the offset(relative to imgbase) of the vmexit native stub.
            var module = parameterizedStateStruct.OutputFunction.GlobalParent;
            var retStubGlobalOffset = module.AddGlobal(ctx.Int64Type, $"ptr_vm_exit_for_func_{cfg.StartAddress.ToString("X")}");

            // For each function call(which is a vmexit), create a global variable which contains the offset to the vmcall stub.
            var vmCallStubs = new Dictionary<ulong, LLVMValueRef>();
            foreach (var splitTarget in vmReentryPoints)
            {
                var global = module.AddGlobal(ctx.Int64Type, $"ptr_vm_reenter_at_{splitTarget.Address.ToString("X")}");
                vmCallStubs.Add(splitTarget.Address, global);
            }

            // Get a delegate which tells you whether a specific address is within any try statement.
            dgIsInstructionInsideOfTryStatement isInTry = binaryFunction.ScopeTableTree.ScopeTable.IsAddressInsideTryStatement;

            // Implement the remill_function_call and remill_return intrinsics.
            var executableRuntime = SafeNontrivialExecutableRuntimeImplementer.Implement(arch, parameterizedStateStruct.OutputFunction, parameterizedStateStruct, retStubGlobalOffset, vmCallStubs.AsReadOnly(), callInsts, isInTry);
            return executableRuntime;
        }

        private IReadOnlyList<LiftedFilterFunction> GetUpdatedFilterFunctionsForIsolatedModule(LLVMModuleRef newModule, IReadOnlyList<LiftedFilterFunction> originalFilters)
        {
            // public record LiftedFilterFunction(ulong Address, LLVMValueRef LlvmFunction, LLVMValueRef RspGlobal, LLVMValueRef ImagebaseGlobal);
            var output = new List<LiftedFilterFunction>();
            foreach(var filter in originalFilters) 
            {
                var function = newModule.GetNamedFunction(filter.LlvmFunction.Name);
                var rspGlobal = newModule.GetNamedGlobal(filter.RspGlobal.Name);
                var imagebaseGlobal = newModule.GetNamedGlobal(filter.ImagebaseGlobal.Name);
                output.Add(new LiftedFilterFunction(filter.Address, function, rspGlobal, imagebaseGlobal));
            }

            return output;
        }

        private void ImplementSehStackPointerEscapes(LLVMValueRef translatedFunction, IReadOnlyList<LiftedFilterFunction> liftedFilterFunctions)
        {
            SehLocalEscapeImplementer.Implement(translatedFunction, liftedFilterFunctions);
        }

        // TODO: Refactor all of the recompilation code below.
        private (ulong firstSectionRva, ulong secondSectionRva, ulong thirdSectionRva) GetScratchSectionRva(WindowsBinary bin)
        {
            var path = @"foobar.dll";
            bin.PEFile.Write(path);
            var peFile = PEFile.FromBytes(File.ReadAllBytes(path));

            // Section to store vmcall stub pointers.
            var firstSec = SectionManager.AllocateNewSection(peFile, ".vcPtrs", 4096 * 100, SectionFlags.MemoryRead);
            // Section for the other stuff.
            var secondSec = SectionManager.AllocateNewSection(peFile, ".st", 4096 * 100, SectionFlags.MemoryRead);
            // First section where new binary code is emplaced
            var thirdSec = SectionManager.AllocateNewSection(peFile, ".SEG0", 4096 * 100, SectionFlags.MemoryRead | SectionFlags.MemoryExecute);
            return (firstSec.Rva, secondSec.Rva, thirdSec.Rva);
        }

        private void MergeExecutable(Assembler assembler, VmStubBuilder builder, string compiledFuncName, ParameterizedStateStructure parameterizedStateStructure, PEFile src, (ulong firstSectionRva, ulong secondSectionRva, ulong thirdSectionRva) scratchSectionRvas, IReadOnlyDictionary<ulong, ulong> vmCallStubRvas, ulong funcPtrRva, string path)
        {
            var destWinBin = (WindowsBinary)dna.Binary;
            var dest = destWinBin.PEFile;

            // Construct images.
            var srcImage = PEImage.FromFile(src);
            var dstImage = PEImage.FromFile(dest);

            // Section to store vmcall stub pointers.
            var firstSec = SectionManager.AllocateNewSection(dest, ".vcPtrs", 4096 * 100, SectionFlags.MemoryRead);
            // Section for the other stuff.
            var secondSec = SectionManager.AllocateNewSection(dest, ".st", 4096 * 100, SectionFlags.MemoryRead | SectionFlags.MemoryExecute);

            var targetFunc = srcImage.Exports.Entries.Single(x => x.Name == compiledFuncName);
            Dictionary<PESection, PESection> oldToNew = new();
            int x = 0;
            if (src.Sections.First().Name != ".text")
                throw new InvalidOperationException($"TODO: Dynamically fetch .text section start");
            foreach (var section in src.Sections)
            {
                string name = ".SEG" + x.ToString();
                // Copy the segment contents over.
                var contents = section.Contents.WriteIntoArray();
                var newSec = SectionManager.AllocateNewSection(dest, name, contents, section.GetVirtualSize(), section.Characteristics);
                oldToNew.Add(section, newSec);
                x += 1;
            }

            /*
            var newFuncAddr = oldToNew.Single(x => x.Key.Name.Contains(".text")).Value.Rva + destWinBin.BaseAddress;
            var jumpSeg = SectionManager.AllocateNewSection(dest, ".vmentry", 4096, SectionFlags.MemoryExecute | SectionFlags.MemoryRead);
            */

            ulong newFuncAddr = oldToNew.Single(x => x.Key.Name.Contains(".text")).Value.Rva;

            ulong endRip = 0;
            var encodedBytes = InstructionEncoder.EncodeInstructions(assembler.Instructions.ToList(), secondSec.Rva + destWinBin.BaseAddress, out endRip);
            destWinBin.WriteMutableBytes(secondSec.Rva + destWinBin.BaseAddress, encodedBytes.ToArray());

            ulong srcRip = 0;
            foreach (var vmCallStub in vmCallStubRvas)
            {
                builder.assembler = new Assembler(64);
                builder.initialReferencePoint = builder.assembler.CreateLabel();
                builder.assembler.Label(ref builder.initialReferencePoint);
                srcRip = endRip;
                builder.initialRva = srcRip - dna.Binary.BaseAddress;
                builder.EncodeVmCall(vmCallStub.Key);
                encodedBytes = InstructionEncoder.EncodeInstructions(builder.assembler.Instructions.ToList(), srcRip, out endRip);
                destWinBin.WriteMutableBytes(srcRip, encodedBytes.ToArray());

                destWinBin.WriteMutableBytes(destWinBin.BaseAddress + vmCallStub.Value, BitConverter.GetBytes(srcRip - destWinBin.BaseAddress));
            }


            destWinBin.WriteMutableBytes(funcPtrRva + destWinBin.BaseAddress, BitConverter.GetBytes(newFuncAddr));


            // Emit the ret
            builder.assembler = new Assembler(64);
            srcRip = endRip;
            builder.initialRva = srcRip - dna.Binary.BaseAddress;
            builder.initialReferencePoint = builder.assembler.CreateLabel();
            builder.assembler.Label(ref builder.initialReferencePoint);
            builder.EncodeVmRet();
            encodedBytes = InstructionEncoder.EncodeInstructions(builder.assembler.Instructions.ToList(), srcRip, out endRip);
            destWinBin.WriteMutableBytes(srcRip, encodedBytes.ToArray());
            destWinBin.WriteMutableBytes(destWinBin.BaseAddress + funcPtrRva, BitConverter.GetBytes(srcRip - destWinBin.BaseAddress));

            // Nop out the old function with a jmp to the start of the new section.
            foreach (var inst in binaryFunction.Cfg.GetInstructions())
            {
                for (int i = 0; i < inst.Length; i++)
                    destWinBin.WriteMutableByte(inst.IP + (ulong)i, 0x90);
            }


            // Modify the old func to jump to our new vmenter.
            var startIp = binaryFunction.Cfg.StartAddress;
            assembler = new Assembler(64);
            assembler.jmp(secondSec.Rva + dna.Binary.BaseAddress);
            var encoding = InstructionEncoder.EncodeInstruction(assembler.Instructions.Single(), startIp);
            destWinBin.WriteMutableBytes(startIp, encoding);



            dest.Write("recompiled.exe");
            dest.Write("recompiled2.exe");
            IDALoader.Load("recompiled2.exe");
            Console.WriteLine("");
        }

    }
}
