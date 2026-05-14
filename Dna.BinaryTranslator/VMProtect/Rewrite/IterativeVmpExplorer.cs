using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.Lifting;
using Dna.BinaryTranslator.Unsafe;
using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Relocation;
using Dna.SEH;
using Dna.Utilities;
using FASTER.core;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;

using static Iced.Intel.AssemblerRegisters;


using VmCfg = Dna.ControlFlow.InstGraph<Dna.BinaryTranslator.VMProtect.VmHandler, Dna.BinaryTranslator.VMProtect.Rewrite.HandlerMetadata>;

namespace Dna.BinaryTranslator.VMProtect.Rewrite
{
    public struct HandlerMetadata
    {
        public bool IsComplete;
    }

    public class IterativeVmpExplorer
    {
        private readonly IDna dna;

        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly ulong funcRip;

        //private readonly VmHandlerCache handlerCache;

        private readonly Dictionary<ulong, ulong> bytecodeAddrToRip = new();

        private OrderedSet<VmHandler> handlers = new OrderedSet<VmHandler>();

        public IterativeVmpExplorer(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.funcRip = funcRip;
            //this.handlerCache = new VmHandlerCache(ctx);
        }

        public static Func<BasicBlock<Instruction>, IEnumerable<ulong>> DescentCallback(IDna dna)
        {
            return (BasicBlock<Instruction> x) => { return DescentCallback(dna, x); };
        }

        public static IEnumerable<ulong> DescentCallback(IDna dna, BasicBlock<Instruction> block)
        {
            var inst = block.ExitInstruction;
            if (inst.Mnemonic != Mnemonic.Call)
                return null;
            if (!inst.HasImmediateBranchTarget())
                return null;
            var target = inst.GetImmediateBranchTarget();
            return new ulong[] { target};
        }

        public static Func<ulong, bool> ShouldContinueCallback(IDna dna)
        {
            return (ulong x) => { return ShouldContinueCallback(dna, x); };
        }

        public static bool ShouldContinueCallback(IDna dna, ulong ip)
        {
            var inst = dna.BinaryDisassembler.GetInstructionAt(ip);
            if (inst.Mnemonic == Mnemonic.Call)
                return false;

            return true;
        }

        public LLVMValueRef Run()
        {
            arch.GetOrLoadSemantics();
            var outModule = IterativeVmpTranslator.CreateOutputModule(ctx, arch);

            // Append the first handler to the handler set.
            // Note that we use the random garbage for the bytecode pointer.
            // This is fine because the bytecode pointer of the first handler does not really matter.
            handlers.Add(new VmHandler(funcRip, funcRip));
            bytecodeAddrToRip[funcRip] = funcRip;

            HashSet<ulong> vmexitHandlerRips = new();

            // Mark the first handler for lifting.
            var handlersRipsToLift = new OrderedSet<(ulong nativeRip, bool isVmEnter)>();
            handlersRipsToLift.Add((funcRip, true));

            // Append the handler to the VM cfg
            var vCfg = new VmCfg();
            vCfg.GetOrAdd(handlers.First());
            vCfg.Instructions[handlers.First()].Metadata.IsComplete = false;

            var blockCache = new VmBlockCache(ctx);
            LLVMValueRef liftedFunction = default;
            int ii = 0;
            var handlerLifter = new HandlerLifter(dna, ctx, arch);

            var stateStruct = new VmpParameterizedStateStructure(arch, ctx, false);
            RemillRegister bytecodeRegister = GetBytecodeRegister(stateStruct, handlerLifter, funcRip);

            while (true)
            {
                Console.WriteLine($"Lifting iteration {ii++}");

                // Identify all VmExit handlers.
                foreach (var (rip, isVmEnter) in handlersRipsToLift)
                {
                    //handlerLifter.LiftHandler(rip, handlers.Count == 1);
                    var cfg = HandlerLifter.DisHandler(dna, rip);
                    var insts = cfg.GetInstructions();
                    var count = insts.Count(x => x.Mnemonic == Iced.Intel.Mnemonic.Pop);
                    if (count > 10)
                        vmexitHandlerRips.Add(rip);
                }


                // Lift all native handlers to LLVM IR and cache them
                //IterativeVmpTranslator.LiftHandlersIntoCache(arch, handlerCache, dna, handlersRipsToLift);
                handlersRipsToLift.Clear();

                // Lift the partial CFG
                //var stateStruct = handlerCache.GetLiftedHandler(handlers.First().NativeRip).ParameterizedStateStructure;


                liftedFunction = new IterativeCfgBuilder(outModule, arch, stateStruct, vCfg, handlerLifter, vmexitHandlerRips).Run(liftedFunction, handlers.First(), bytecodeRegister);
                FixMemPtr(liftedFunction.GlobalParent);


                IterativeVmpTranslator.CanonicalizeMemoryPtr(liftedFunction);

                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                // Run our optimization pipeline
                PassPipeline.Run(dna.Binary, liftedFunction, false);

                // Solve for any unknown indirect jumps in the control flow graph.
                var solver = new VmpJmpTableSolver(liftedFunction);
                var (newTables, bytecodePtrToRips) = solver.Solve();

                if(!newTables.Any())
                {
                    Console.WriteLine($"Finished devirt");
                    outModule.PrintToFile("translatedFunction.ll");
                    IDALoader.Load(ClangCompiler.Compile("translatedFunction.ll"));
                    Console.WriteLine("Finished devirt");

                    Debugger.Break();
                    Console.ReadLine();
                }

                foreach (var entry in bytecodePtrToRips)
                {
                    
                    if (bytecodeAddrToRip.TryAdd(entry.Key, entry.Value) && !handlerLifter.ContainsHandler(entry.Value))
                        handlersRipsToLift.Add((entry.Value, false));
                    

                    if (bytecodeAddrToRip[entry.Key] != entry.Value)
                        throw new InvalidOperationException($"Multiple native addresses assigned to the same bytecode ptr!");
                }

                var cloneAddr = (VmHandler x) => x;
                var cloneMetadata = (HandlerMetadata x) => x;
                var newCfg = vCfg.Clone(cloneAddr, cloneMetadata);

                var getHandler = (ulong x) => new VmHandler(x, bytecodeAddrToRip[x]);

                foreach(var table in newTables)
                {
                    // Update the CFG with new edge info
                    var handler = getHandler(table.JmpFromAddr);
                    foreach (var pred in table.KnownPredecessorAddresses)
                        newCfg.AddEdge(getHandler(pred), handler);
                    foreach (var succ in table.KnownOutgoingAddresses)
                        newCfg.AddEdge(handler, getHandler(succ));
                }

                // Get all newly added nodes & nodes with new predecessors
                WorkList<VmHandler> worklist = new WorkList<VmHandler>();
                foreach (var (handler, info) in newCfg.Instructions)
                {
                    // All new nodes get marked as changed
                    if (!vCfg.Contains(handler))
                    {
                        worklist.AddToBack(handler);
                        continue;
                    }

                    // If a node has new predecessors, it should be marked as changed.
                    var oldInfo = vCfg.Instructions[handler];
                    if (!oldInfo.Predecessors.SetEquals(info.Predecessors))
                    {
                        worklist.AddToBack(handler);
                        continue;
                    }
                }

                // Get all reachable nodes starting from the worklist
                // (this is includes the changed worklist members themselves)
                var reachableNodes = GetReachableNodes(newCfg, worklist);
                foreach(var (handler, info) in newCfg.Instructions)
                    info.Metadata.IsComplete = !reachableNodes.Contains(handler);

                // Replace the CFG
                vCfg = newCfg;

                liftedFunction.GlobalParent.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));

                //Debugger.Break();
            }

            // TODO tomorrow: Hook up incremental algorithm
            Debugger.Break();
            return default;
        }

        // Identify the VIP by a load of some constant address, followed by a store of that address to some register
        // %24 = getelementptr inbounds i8, ptr %mem, i64 5368736796
        // store i64 5368736796, ptr %out_RSI, align 8
        private RemillRegister GetBytecodeRegister(VmpParameterizedStateStructure stateStruct, HandlerLifter lifter, ulong vmenterRip)
        {
            // Lift the VMEnter
            var lifted = lifter.LiftHandler(vmenterRip, true);

            lifted.GlobalParent.PrintToFile("translatedFunction.ll");

            // Get all constant addresses being used as pointers
            var gepConstants = lifted.GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && x.OperandCount == 2 && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind)
                .Select(x => x.GetOperand(1));

            // Get all stores 
            var destRegArg = lifted.GetInstructions()
                .Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && gepConstants.Contains(x.GetOperand(0)))
                .GetOperand(1);

            var destIdx = Array.IndexOf(lifted.GetParams(), destRegArg);

            var reg = stateStruct.RegisterOutputArgumentIndices.Single(x => x.Value == destIdx).Key;
            return reg;

        }

        private HashSet<VmHandler> GetReachableNodes(VmCfg vCfg, WorkList<VmHandler> worklist)
        {
            var seen = new HashSet<VmHandler>();
            while (worklist.Count != 0)
            {
                var pop = worklist.PopBack();
                if (!seen.Add(pop))
                    continue;

                worklist.AddRangeToBack(vCfg.Instructions[pop].Successors);
            }

            return seen;
        }

        public static void FixMemPtr(LLVMModuleRef module)
        {
            var memPtr = module.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(module.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }
        }
    }

    public class IterativeCfgBuilder
    {
        private readonly LLVMModuleRef module;

        private readonly RemillArch arch;

        private readonly VmpParameterizedStateStructure stateStruct;

        private readonly VmCfg vCfg;

        private readonly HandlerLifter handlerCache;

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;

        private LLVMBuilderRef builder;

        public IterativeCfgBuilder(LLVMModuleRef module, RemillArch arch, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, HandlerLifter handlerCache, IReadOnlySet<ulong> vmexitHandlerRips)
        {
            this.module = module;
            this.arch = arch;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.handlerCache = handlerCache;
            this.vmexitHandlerRips = vmexitHandlerRips;
            builder = LLVMBuilderRef.Create(module.Context);
        }

        public LLVMValueRef Run(LLVMValueRef translatedFunction, VmHandler entryHandler, RemillRegister bytecodeRegister)
        {
            bool incremental = translatedFunction.Handle != 0;
            if (!incremental)
            {
                translatedFunction = module.AddFunction($"PartialCfg", stateStruct.ParameterizedFunctionPrototype);
                translatedFunction.AppendBasicBlock("entry");
            }

            foreach (var outReg in stateStruct.RegisterOutputArgumentIndices.Values)
                LLVMUtil.MakeParamNoAlias(translatedFunction.GetParam((uint)outReg));

            var jmpHandler = module.GetNamedFunction("vmp_maybe_unsolved_jump");
            var callers = jmpHandler.Handle == 0 ? new List<LLVMValueRef>() : RemillUtils.CallersOf(jmpHandler);

            // Stack allocate a local state structure and copy all registers into it
            builder.Position(translatedFunction.EntryBasicBlock, translatedFunction.EntryBasicBlock.FirstInstruction);
            var registerAllocaMapping = VmPartialBlockLifter.CreateLocalStateStruct(builder, stateStruct, translatedFunction);

            // Lift all handlers into their own basic block
            var exitBlock = translatedFunction.AppendBasicBlock("exit");
            builder.Position(exitBlock, exitBlock.FirstInstruction);
            builder.BuildRetVoid();

            var toDelete = new HashSet<LLVMValueRef>();

            var blockMapping = LiftInsts(incremental, translatedFunction, exitBlock, entryHandler, bytecodeRegister, registerAllocaMapping, toDelete);

            // If rebuilding cfg from scratch, insert jump from entry block to first VM instruction
            if (!incremental)
            {
                builder.PositionAtEnd(translatedFunction.EntryBasicBlock);
                builder.BuildBr(blockMapping[entryHandler]);
                //module.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

                /*
                module.PrintToFile(("translatedFunction.ll"));
                foreach (var f in toDelete)
                {
                    var any = translatedFunction.GetInstructions().First(x => x.InstructionOpcode == LLVMOpcode.LLVMCall && x.GetCallInstTarget() == f);

                        

                    var callers2 = RemillUtils.CallersOf(f);
                    LLVMCloning.InlineFunction(f);
                    module.PrintToFile(("translatedFunction.ll"));
                    f.DeleteFunction();
                }
                */


                module.PrintToFile(("translatedFunction.ll"));
                return translatedFunction;
            }

            // Wire new handlers into CFG
            // We need to identify the vmp_maybe_unsolved_jmp calls and hook them up
            foreach (var caller in callers)
            {
                // Fetch register values from the local state structure
                builder.PositionBefore(caller);
                VmPartialBlockLifter.LoadOutputRegisters(builder, translatedFunction, registerAllocaMapping, stateStruct);


                var destIp = caller.GetOperand(0);
                //var destBlock = blockMapping.Single(x => x.Key.BytecodeRip ==  destIp);


                var srcIp = caller.GetOperand(2).ConstIntZExt;
                var handler = vCfg.Instructions.Single(x => x.Key.BytecodeRip == srcIp).Key;
                var vNode = vCfg.Instructions[handler];
 

                var outgoingAddresses = vNode.Successors.OrderBy(x => x).ToList();
                // If this lookup fails, an incremental rebuild was not possible. TODO: Set `incremental` to false and clear CFG in this case
                var defaultBlock = blockMapping[outgoingAddresses.First()];
                var liftedCases = new HashSet<VmHandler>() { outgoingAddresses.First() };


            
                var swtch = builder.BuildSwitch(destIp, defaultBlock, (uint)outgoingAddresses.Count);

                foreach (var target in outgoingAddresses)
                {
                    if (liftedCases.Contains(target))
                        continue;

                    liftedCases.Add(target);
                    var targetBlock = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Value;

                    swtch.AddCase(LLVMValueRef.CreateConstInt(module.Context.Int64Type, target.BytecodeRip), targetBlock);
                }

                LLVMUtil.SplitBlockAt(caller.InstructionParent, caller, "split", true);
                swtch.InstructionParent.LastInstruction.InstructionEraseFromParent();

    
                // TODO: Split basic block before lifting edges..
                // copy from args to local state structure.. make them no alias
                module.PrintToFile(("translatedFunction.ll"));
                //Debugger.Break();
            }

            // If we cant incremental build, need to do everything from scatch. Put each inst in basic block, hook them up. Maybe split into SESE regions just to not be insanely unreadable. Insert hooks on exit
            // If incremental, we are just wiring into an existing
            // TODO: If incremental build, wire up state structure and outgoing edges.
            // vmp_unsolved_jump(target ptr, rip, jmp from bytecode ptr)
            module.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
            //Debugger.Break();
            return translatedFunction;
        }

        private Dictionary<VmHandler, LLVMBasicBlockRef> LiftInsts(bool incremental, LLVMValueRef function, LLVMBasicBlockRef exitBlock, VmHandler entryHandler, RemillRegister bytecodeRegister, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, HashSet<LLVMValueRef> toDelete)
        {
            // Create blocks for each lifted instructions
            var blockMapping = new Dictionary<VmHandler, LLVMBasicBlockRef>();
            foreach(var (handler, info) in vCfg.Instructions)
            {
                // Skip if we are doing incremental building and already have this inst in the IR
                if (incremental && info.Metadata.IsComplete)
                    continue;

                blockMapping[handler] = function.AppendBasicBlock($"bb_{handler.BytecodeRip.ToString("X")}");
            }

            foreach (var (handler, block) in blockMapping)
            {
                builder.PositionAtEnd(block);
                var liftedHandler = handlerCache.Lift(module, handler.NativeRip, handler == entryHandler);
                var call = VmPartialBlockLifter.CallVmHandler(builder, function, liftedHandler, registerAllocaMapping, stateStruct);
             
                LiftInstEdges(handler, function, exitBlock, blockMapping, bytecodeRegister, registerAllocaMapping);
                module.PrintToFile("translatedFunction.ll");

                IterativeVmpExplorer.FixMemPtr(module);
             

                LLVMCloning.InlineFunction(liftedHandler);
                module.PrintToFile("translatedFunction.ll");
                liftedHandler.DeleteFunction();
                //toDelete.Add(liftedHandler);
            }

            return blockMapping;
        }

        private void LiftInstEdges(VmHandler handler, LLVMValueRef function, LLVMBasicBlockRef exitBlock, Dictionary<VmHandler, LLVMBasicBlockRef> blockMapping, RemillRegister bytecodeRegister, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
        {
            var llvmBlock = blockMapping[handler];
            bool isVmExit = vmexitHandlerRips.Contains(handler.NativeRip);
            var info = vCfg.Instructions[handler];
            bool isComplete = info.Metadata.IsComplete;

            // If this is an exit node, store all register values and exit.
            if ((isComplete && info.Successors.Count == 0) || isVmExit)
            {
                VmPartialBlockLifter.UpdateOutputRegisters(builder, function, registerAllocaMapping, stateStruct);
                builder.BuildRetVoid();
                return;
            }

            // Load the indirect jump value.
            var int64Ty = module.Context.GetInt64Ty();
            var indirectPc = VmCfgLifter.LoadBytecodePointer(builder, bytecodeRegister, registerAllocaMapping);

            var liftedCases = new HashSet<VmHandler>();
            LLVMBasicBlockRef defaultBlock = null;
            var outgoingAddresses = info.Successors.OrderBy(x => x).ToList();
            // If the jump table is considered incomplete(we know some edges but potentially not all), then we lift the jump table as a switch statement
            // where the known values get their own 'case', and the default case points to an remill_jump intrinsic.
            if (!isComplete)
            {
                defaultBlock = function.AppendBasicBlock($"reprove_new_edge_for_jmp_table_{handler.BytecodeRip.ToString("X")}");
                builder.PositionAtEnd(defaultBlock);
                VmPartialBlockLifter.UpdateOutputRegisters(builder, function, registerAllocaMapping, stateStruct);
                VmCfgLifter.AddCallToIndirectBranchIntrinsic(module, builder, llvmBlock, bytecodeRegister, registerAllocaMapping, exitBlock, handler.BytecodeRip);
                builder.PositionAtEnd(llvmBlock);
            }

            // If the jump table is considered complete(i.e. we are 100% confident that we know all possible outgoing values),
            // then we make the default case for the jump table point to a randomly selected(first) jump table outgoing block.
            else
            {
                defaultBlock = blockMapping[outgoingAddresses.First()];
                liftedCases.Add(outgoingAddresses.First());
            }

            builder.PositionAtEnd(llvmBlock);
            var swtch = builder.BuildSwitch(indirectPc, defaultBlock, (uint)outgoingAddresses.Count);

            foreach (var target in outgoingAddresses)
            {
                if (liftedCases.Contains(target))
                    continue;

                liftedCases.Add(target);
                var targetBlock = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Value;
 
                swtch.AddCase(LLVMValueRef.CreateConstInt(module.Context.Int64Type, target.BytecodeRip), targetBlock);
            }
        }
    }

    public class HandlerLifter
    {
        private readonly IDna dna;

        private readonly LLVMContextRef ctx;

        private readonly RemillArch arch;

        private readonly LLVMModuleRef cacheModule;

        private readonly Dictionary<ulong, LLVMValueRef> handlerRipToLlvmFunction = new();

        public HandlerLifter(IDna dna, LLVMContextRef ctx, RemillArch arch)
        {
            this.dna = dna;
            this.ctx = ctx;
            this.arch = arch;
            cacheModule = ctx.CreateModuleWithName("HandlerCache");
        }

        public static ControlFlowGraph<Instruction> DisHandler(IDna dna, ulong ip)
        {
            return dna.RecursiveDescent.ReconstructCfg(ip, null, null, IterativeVmpExplorer.ShouldContinueCallback(dna));
        }

        public bool ContainsHandler(ulong rip)
            => handlerRipToLlvmFunction.ContainsKey(rip);

        public LLVMValueRef Lift(LLVMModuleRef module, ulong handlerRip, bool isVmEnter)
        {
            var handler = LiftHandler(handlerRip, isVmEnter);
            var wrapperName = handler.Name + "_from_cache";

            var existingInDest = module.GetNamedFunction(wrapperName);
            if (existingInDest.Handle != 0)
                return existingInDest;

            // Add a new function with the exact same prototype as the handler function.
            var newHandler = cacheModule.AddFunction(wrapperName, handler.GetFunctionPrototype());

            // Build a call to the original handler function, followed by a RET.
            var builder = LLVMBuilderRef.Create(ctx);
            var entryBb = newHandler.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBb);
            var call = builder.BuildCall2(handler.GetFunctionPrototype(), handler, newHandler.GetParams());


            builder.BuildRetVoid();

            // Inlines ALL calls to the handler function.
            // TODO: Inline only the call we just created. We just need to add a pinvoke import for this.
            LLVMCloning.InlineFunction(handler);

            //cacheModule.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // Move the newly created function into the target module.
            newHandler = FunctionIsolator.IsolateFunctionInto(module, newHandler);
            return newHandler;
        }

        public LLVMValueRef LiftHandler(ulong handlerRip, bool isVmEnter)
        {
            if (handlerRipToLlvmFunction.TryGetValue(handlerRip, out var existing))
                return existing;

            Console.WriteLine($"New handler: 0x{handlerRip.ToString("X")}");

            var cfg = HandlerLifter.DisHandler(dna, handlerRip);

            var scopeTable = IterativeFunctionTranslator.GetScopeTable(dna.Binary, handlerRip);
            var scopeTableTree = new ScopeTableTree(scopeTable);
            (cfg, var fallthroughFromIps) = IterativeFunctionTranslator.PreprocessCfg(cfg, scopeTable);

            // TODO: The sub rsp rewriting might emit an instruction with a different length. I might need to concretize RIPs in the CFG translator.
            if (isVmEnter)
                ReplaceStackExpansion(cfg);

            // Lift the function using remill.
            var encodedCfg = X86CfgEncoder.EncodeCfg(dna.Binary, cfg);
            var (liftedFunction, blockMapping, filterFunctions) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, new BinaryFunction(encodedCfg, scopeTableTree, new List<JmpTable>()), fallthroughFromIps, CallHandlingKind.Jmp);
            liftedFunction = FunctionIsolator.IsolateFunctionIntoNewModule(arch, liftedFunction);

            liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

            liftedFunction.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
     
            var (stripped, stateStruct) = IterativeFunctionTranslator.StripRuntimeVmp(dna, ctx, arch, liftedFunction);
            // liftedFunction.Handle = 0;

            if (handlerRip == 0x1400060EF)
            {
                Console.WriteLine(GraphFormatter.FormatGraph(cfg));
            }

            // Eliminate any stack expansion in the IR
            EliminateStackExpansionLoop(stripped, handlerRip);

            EliminateStackAlignment(stripped, stateStruct);

            // Optimize one last time
            OptimizationApi.OptimizeModule(stripped.GlobalParent, stripped, false, false, 0, false, 0, false);

            // Move the newly created function into the target module.
            var newHandler = FunctionIsolator.IsolateFunctionInto(cacheModule, stripped);
            handlerRipToLlvmFunction[handlerRip] = newHandler;
            return newHandler;

            //File.WriteAllText("binja.py", new LLVMToBinjaGraph(stripped).Process());

        }

        private void ReplaceStackExpansion(ControlFlowGraph<Instruction> cfg)
        {
            foreach(var block in cfg.GetBlocks())
            {
                for(int i = 0; i < block.Instructions.Count; i++)
                {
                    if (!IsSubRspImm(block.Instructions[i]))
                        continue;

                    var assembler = new Assembler(64);
                    assembler.sub(rsp, 12582912);
                    var encodedInst = InstructionEncoder.RelocateInstructions(assembler.Instructions.ToList(), block.Instructions[i].IP).Single();
                    block.Instructions[i] = encodedInst;
                }
            }
        }

        private bool IsAndRspImm(Instruction x)
            => x.Mnemonic == Mnemonic.And && x.Op0Kind == OpKind.Register && x.Op0Register == Register.RSP && x.Op1Kind.IsImmediate() && x.GetImmediate(1) == (ulong)0xFFFFFFFFFFFFFFF0;

        private bool IsSubRspImm(Instruction x)
            => x.Mnemonic == Mnemonic.Sub && x.Op0Kind == OpKind.Register && x.Op0Register == Register.RSP && x.Op1Kind.IsImmediate();

        private void EliminateStackAlignment(LLVMValueRef function, ParameterizedStateStructure stateStruct)
        {
            foreach(var inst in function.GetInstructions())
            {
                if (inst.InstructionOpcode != LLVMOpcode.LLVMAnd)
                    continue;

                if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    continue;

                var imm = inst.GetOperand(1).ConstIntZExt;
                if (imm is not (unchecked((ulong)-16) or unchecked((ulong)-256) or 0xF or 0xFF))
                    continue;

                var rsp = stateStruct.GetRegInputParam(arch.GetRegisterByName(arch.StackPointerRegisterName), function);
                var lhs = inst.GetOperand(0);

                var matches = lhs == rsp || (lhs.InstructionOpcode == LLVMOpcode.LLVMAdd && lhs.GetOperand(0) == rsp);
                if (!matches)
                    continue;
                inst.ReplaceAllUsesWith(lhs);

                //function.GlobalParent.PrintToFile("translatedFunction.ll");

                //Debugger.Break();
            }
        }

        // VMProtect handlers will relocate the stack to a new location if they run out of space.
        // To make optimization a bit easier we modify the VMEnter to allocate a huge amount of stack space and delete all of the stack expansion loops.
        private void EliminateStackExpansionLoop(LLVMValueRef function, ulong handlerRip)
        {
        
            var hasCycles = () =>
            {
                var entryBlock = function.EntryBasicBlock;
                if (entryBlock.LastInstruction.InstructionOpcode != LLVMOpcode.LLVMBr || entryBlock.LastInstruction.OperandCount <= 1)
                    return false;
                var b0 = entryBlock.LastInstruction.GetOperand(1).AsBasicBlock();
                var b0Cyclic = ReachesCycle(b0, new(), new());
                var b1 = entryBlock.LastInstruction.GetOperand(2).AsBasicBlock();
                var b1Cyclic = ReachesCycle(b1, new(), new());
                return b0Cyclic || b1Cyclic;
            };

            /*
            // Get a bitmask indicating which branches lead to a cycle
            var getCycles = () =>
            {
                var exitInsts = function.GetBlocks()
                .Select(x => x.Terminator)
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMBr && x.OperandCount == 3 && IsStackExpansionPredicate(x))
                .ToList();

                if (exitInsts.Count != 1)
                {
                    if (hasCycles())
                        Debugger.Break();
                    return 0u;
                }

                var entryBlock = exitInsts.Single().InstructionParent;

                var b0 = entryBlock.LastInstruction.GetOperand(1).AsBasicBlock();
                var b0Cyclic = ReachesCycle(b0, new(), new());
                var b1 = entryBlock.LastInstruction.GetOperand(2).AsBasicBlock();
                var b1Cyclic = ReachesCycle(b1, new(), new());

                uint r = 0;
                r |= b0Cyclic ? 1u : 0;
                r |= b1Cyclic ? 2u : 0;
                return r;
            };

            // If neither has a cycle, there is no stack expansion.
            if (getCycles() == 0)
                return;

            // Run the full pass pipeline hoping that any spurious cycles get eliminated
            PassPipeline.Run(dna.Binary, function);
            var cycles = getCycles();
            if (cycles == 0)
                return;

            // This should never happen. There should be only one path leading to the virtual stack expansion loop
            if (cycles == 3)
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                throw new InvalidOperationException($"Cyclic handler at 0x{handlerRip.ToString("X")}");
            }

            // Update the branch instruction accordingly.
            var terminator = function.EntryBasicBlock.LastInstruction;
            var cond = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, cycles == 1 ? 1u : 0);
            terminator.SetOperand(0, cond);

            if (handlerRip == 0x140003EC2)
            {
                PassPipeline.Run(dna.Binary, function);
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();
            }
            */

            var exitInsts = function.GetBlocks()
                .Select(x => x.Terminator)
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMBr && x.OperandCount == 3 && IsStackExpansionPredicate(x))
                .ToList();

            if (exitInsts.Count == 0)
                return;

            exitInsts.Single().SetOperand(0, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1));


        }

        //   %6 = add i64 %RDI, 224
        // %.not = icmp ugt i64 % 3, %6
        private static bool IsStackExpansionPredicate(LLVMValueRef x)
        {
            var cond = x.GetOperand(0);
            if (cond.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            if (cond.ICmpPredicate != LLVMIntPredicate.LLVMIntUGT)
                return false;
            if (!IsAddImm(cond.GetOperand(0), 224) && !IsAddImm(cond.GetOperand(1), 224))
                return false;
            
            return true;
        }

        private static bool IsAddImm(LLVMValueRef x, ulong imm)
        {
            if (x.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            if (x.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;
            if (x.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;
            if (x.GetOperand(1).ConstIntZExt != imm)
                return false;

            return true;
        }

        private bool ReachesCycle(LLVMBasicBlockRef curr, HashSet<LLVMBasicBlockRef> visited, HashSet<LLVMBasicBlockRef> stack)
        {
            if (stack.Contains(curr))
                return true;
            if (visited.Contains(curr))
                return false;

            stack.Add(curr);
            visited.Add(curr);
            var terminator = curr.Terminator;
            if (terminator != null)
            {
                for(var i = 0; i < terminator.SuccessorsCount; i++)
                {
                    if (ReachesCycle(terminator.GetSuccessor((uint)i), visited, stack))
                        return true;
                }
            }

            stack.Remove(curr);
            return false;
        }
    }
}
