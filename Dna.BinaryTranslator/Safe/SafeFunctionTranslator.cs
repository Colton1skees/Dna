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

            ImplementSehStackPointerEscapes(executableRuntime.OutputFunction, updatedFilterFunctions);

            // Compile and load it into IDA.
            executableRuntime.OutputFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
            var opt = ClangCompiler.Optimize(executableRuntime.OutputFunction.GlobalParent, "translatedFunction.ll");
            var p = ClangCompiler.Compile("translatedFunction.ll");
            var ida = IDALoader.Load(p);

            return new SafelyTranslatedFunction(binaryFunction, executableRuntime, updatedFilterFunctions);

            executableRuntime.OutputFunction.GlobalParent.PrintToFile("translatedFunction.ll");
            string path = null;
            bool toDll = true;
            if (toDll)
                path = ClangCompiler.CompileToWindowsDll(executableRuntime.OutputFunction, "translatedFunction.ll", false);
            else
                path = ClangCompiler.Compile("translatedFunction.ll", false);

            IDALoader.Load(path, true);

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
    }
}
