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
using static Dna.BinaryTranslator.VMProtect.VmpJmpTableSolver;
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
            var stateStruct2 = new ParameterizedStateStructure(arch, ctx, false, true, false);
            RemillRegister bytecodeRegister =  handlerLifter.GetVmenterBytecodeRegister(stateStruct2, handlerLifter.LiftHandler(funcRip, true));
            Dictionary<VmHandler, RemillRegister> handlerVips = new();
            handlerVips[handlers.First()] = bytecodeRegister;

            //handlerLifter.LiftHandler(0x140048BBD, false);
            while (true)
            {


                Console.WriteLine($"Lifting iteration {ii++}");

                /*
                // see VIPs.txt
                if (false && ii == 551)
                {
                    Console.WriteLine($"VIPs: ");
                    foreach(var inst in vCfg.Instructions)
                    {
                        var incomingVip = inst.Key.BytecodeRip == funcRip ? bytecodeRegister : inst.Value.Predecessors.Select(x => handlerVips[x]).DistinctBy(x => x.Name).Single();
                        var outgoingVip = handlerVips[inst.Key];

                        Console.WriteLine($"{inst.Key.BytecodeRip.ToString("X")} {incomingVip} {outgoingVip}");
                    }

                    Debugger.Break();
                }
                */

                //var join = String.Join(", ", handlerLifter.handlerRipToLlvmFunction.Keys.Select(x => "0x" + x.ToString("X")));
                //Console.WriteLine(join);
       

                //// Identify all VmExit handlers.
                //foreach (var (rip, isVmEnter) in handlersRipsToLift)
                //{
                //    //handlerLifter.LiftHandler(rip, handlers.Count == 1);
                //    var cfg = HandlerLifter.DisHandler(dna, rip);
                //    if (HandlerLifter.IsVmexit(cfg))
                //        vmexitHandlerRips.Add(rip);
                //}


                // Lift all native handlers to LLVM IR and cache them
                //IterativeVmpTranslator.LiftHandlersIntoCache(arch, handlerCache, dna, handlersRipsToLift);
                handlersRipsToLift.Clear();

                // Lift the partial CFG
                //var stateStruct = handlerCache.GetLiftedHandler(handlers.First().NativeRip).ParameterizedStateStructure;


                liftedFunction = new IterativeCfgBuilder(outModule, arch, stateStruct, vCfg, handlerLifter, vmexitHandlerRips, handlerVips).Run(liftedFunction, handlers.First());
                FixMemPtr(liftedFunction.GlobalParent);

                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                liftedFunction.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

                IterativeVmpTranslator.CanonicalizeMemoryPtr(liftedFunction);

                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                // Run our optimization pipeline
                PassPipeline.Run(dna.Binary, liftedFunction, false);


                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                // Solve for any unknown indirect jumps in the control flow graph.
                //var solver = new VmpJmpTableSolver(liftedFunction);
                //var (newTables, bytecodePtrToRips) = solver.Solve();

                var solver = new VmpSolver(arch, liftedFunction);

                var dests = solver.SolveRIPs(stateStruct);

                var (newTables, bytecodePtrToRips) = solver.Solve();

                if (!newTables.Any())
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
                    var cfg = HandlerLifter.DisHandler(dna, entry.Value);
                    if (HandlerLifter.IsVmexit(cfg))
                        vmexitHandlerRips.Add(entry.Value);

                    if (bytecodeAddrToRip.TryAdd(entry.Key, entry.Value) && !handlerLifter.ContainsHandler(entry.Value))
                    {
                        //handlerLifter.LiftHandler(rip, handlers.Count == 1);
                       

                    }
                    

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

                        var incomingVip = info.Predecessors.Select(x => handlerVips[x]).DistinctBy(x => x.Name).Single();
                        handlerVips[handler] = handlerLifter.GetBytecodeRegister(HandlerLifter.DisHandler(dna, handler.NativeRip), stateStruct2, handlerLifter.LiftHandler(handler.NativeRip, false), false, incomingVip);

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

                var tcfg = GetCfg(vCfg, handlers.First());

                //Console.WriteLine("\n\n" + GraphFormatter.FormatGraph(tcfg));

                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                if (false && ii == 500)
                {
                    FixMemPtr(handlerLifter.cacheModule);
                    handlerLifter.cacheModule.PrintToFile("cacheModule.ll");

                    /*
                    foreach(var h in handlerLifter.handlerRipToLlvmFunction.Keys)
                    {
                        handlerLifter.Lift(liftedFunction.GlobalParent, h, h == funcRip);
                    }

                    liftedFunction.GlobalParent.PrintToFile("allHandlers.ll");

                    Console.WriteLine("Done");
                    */
                    Debugger.Break();
                }

                //Debugger.Break();
            }

            // TODO tomorrow: Hook up incremental algorithm
            Debugger.Break();
            return default;
        }

        // Identify the VIP by a load of some constant address, followed by a store of that address to some register
  
        private RemillRegister GetBytecodeRegister(VmpParameterizedStateStructure stateStruct, HandlerLifter lifter, ulong vmenterRip)
        {
            // RBP is the right one
            //return arch.GetRegisterByName("RBP");
            // Lift the VMEnter
            var lifted = lifter.LiftHandler(vmenterRip, true);

            //PassPipeline.Run(dna.Binary, lifted);



            lifted.GlobalParent.PrintToFile("translatedFunction.ll");

            // Get all constant addresses being used as pointers
            var gepConstants = lifted.GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && x.OperandCount == 2 && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(1).ConstIntZExt))
                .Select(x => x.GetOperand(1));

            var constStores = lifted.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(0).ConstIntZExt)).ToList();

            // %24 = getelementptr inbounds i8, ptr %mem, i64 5368736796
            // store i64 5368736796, ptr %out_RSI, align 8
            LLVMValueRef targetStore = constStores
                .SingleOrDefault(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && gepConstants.Contains(x.GetOperand(0)));

            // Otherwise look for a unique bytecode address that is not stored anywhere else
            if (targetStore.Handle == 0)
                targetStore = constStores.Single(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && constStores.Count(y => x.GetOperand(0) == y.GetOperand(0)) == 1);
            

            var destRegArg = targetStore.GetOperand(1);

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

        public static ControlFlowGraph<VmHandler> GetCfg(VmCfg vCfg, VmHandler entry)
        {
            var cfg = new ControlFlowGraph<VmHandler>(entry.BytecodeRip);

            Dictionary<VmHandler, BasicBlock<VmHandler>> labels = new();
            foreach (var (handler, info) in vCfg.Instructions)
            {
                var isEntrypoint = handler == entry;
                var hasMultiplePredecessors = info.Predecessors.Count > 1;
                var isCyclic = info.Predecessors.Contains(handler);
                var isCondTarget = info.Predecessors.Any(x => vCfg.Instructions[x].Successors.Count > 1);

                var isLabel = isEntrypoint || hasMultiplePredecessors || isCyclic || isCondTarget;
                if (!isLabel)
                    continue;

                labels.Add(handler, cfg.CreateBlock(handler.BytecodeRip));
            }

            foreach (var label in labels.Keys)
                VisitLabel(vCfg, labels, label);

            return cfg;
        }

        public static void VisitLabel(VmCfg vCfg, Dictionary<VmHandler, BasicBlock<VmHandler>> labels, VmHandler handler)
        {
            var block = labels[handler];
            while (true)
            {
                block.Instructions.Add(handler);

                var succs = vCfg.Instructions[handler].Successors;
                if (!succs.Any())
                    break;

                // Add an outgoing edges if any targets are labels
                if (succs.Any(x => labels.ContainsKey(x)))
                {
                    block.AddOutgoingEdges(succs.Select(x => new BlockEdge<VmHandler>(block, labels[x])));
                    return;
                }

                handler = succs.Single();
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

        private readonly Dictionary<VmHandler, RemillRegister> handlerVips;

        private LLVMBuilderRef builder;

        public IterativeCfgBuilder(LLVMModuleRef module, RemillArch arch, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, HandlerLifter handlerCache, IReadOnlySet<ulong> vmexitHandlerRips, Dictionary<VmHandler, RemillRegister> handlerVips)
        {
            this.module = module;
            this.arch = arch;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.handlerCache = handlerCache;
            this.vmexitHandlerRips = vmexitHandlerRips;
            this.handlerVips = handlerVips;
            builder = LLVMBuilderRef.Create(module.Context);
        }

        public LLVMValueRef Run(LLVMValueRef translatedFunction, VmHandler entryHandler)
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

            var blockMapping = LiftInsts(incremental, translatedFunction, exitBlock, entryHandler, registerAllocaMapping, toDelete);

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


                //var srcIp = caller.GetOperand(2).ConstIntZExt;

                var srcIp = caller.GetOperand((uint)caller.OperandCount - 2).ConstIntZExt;

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

        private Dictionary<VmHandler, LLVMBasicBlockRef> LiftInsts(bool incremental, LLVMValueRef function, LLVMBasicBlockRef exitBlock, VmHandler entryHandler, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, HashSet<LLVMValueRef> toDelete)
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

                // Concretize the VIP register!
                if (handler != entryHandler)
                {
                    var incomingVipRegister = vCfg.Instructions[handler].Predecessors.Select(x => handlerVips[x]).DistinctBy(x => x.Name).Single();
                    builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, handler.BytecodeRip), registerAllocaMapping[incomingVipRegister]);
                }

                var liftedHandler = handlerCache.Lift(module, handler.NativeRip, handler == entryHandler);
                var call = VmPartialBlockLifter.CallVmHandler(builder, function, liftedHandler, registerAllocaMapping, stateStruct);
             


                LiftInstEdges(handler, function, exitBlock, blockMapping, registerAllocaMapping);
                //module.PrintToFile("translatedFunction.ll");

                IterativeVmpExplorer.FixMemPtr(module);
             

                LLVMCloning.InlineFunction(liftedHandler);
                //module.PrintToFile("translatedFunction.ll");
                liftedHandler.DeleteFunction();
                //toDelete.Add(liftedHandler);
            }

            return blockMapping;
        }

        private void LiftInstEdges(VmHandler handler, LLVMValueRef function, LLVMBasicBlockRef exitBlock, Dictionary<VmHandler, LLVMBasicBlockRef> blockMapping, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
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
            var bytecodeRegister = handlerVips[handler];
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
                //VmCfgLifter.AddCallToIndirectBranchIntrinsic(module, builder, llvmBlock, bytecodeRegister, registerAllocaMapping, exitBlock, handler.BytecodeRip);
                AddCallToIndirectBranchIntrinsic(module, builder, registerAllocaMapping, handler.BytecodeRip, exitBlock);
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


        public LLVMValueRef AddCallToIndirectBranchIntrinsic(LLVMModuleRef module, LLVMBuilderRef builder, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, ulong exitFromRip, LLVMBasicBlockRef exitBlock)
        {
            var values = stateStruct.OrderedRegisterArguments.Select(x => builder.BuildLoad2(LLVMTypeRef.Int64, registerAllocaMapping[x]))
                .Append(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, exitFromRip))
                .ToArray();
            var func = GetOrCreateJmpIntrinsic(module, stateStruct.OrderedRegisterArguments);
            var call = builder.BuildCall2(func.GetFunctionPrototype(), func, values);
            builder.BuildBr(exitBlock);
            return call;
        }

        public static LLVMValueRef GetOrCreateJmpIntrinsic(LLVMModuleRef module, IReadOnlyList<RemillRegister> registers)
        {
            // The intrinsic accepts a list of all registers, and an additional i64 argument containing the bytecode pointer we jumped from.
            // call(rax, rcx, ..., BYTECODE_PTR)
            var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, registers.Select(x => LLVMTypeRef.Int64).Append(LLVMTypeRef.Int64).ToArray());
            var func = module.GetFunctions().SingleOrDefault(x => x.Name == "vmp_branch");
            if (func.Handle != 0)
                return func;

            func = module.AddFunction("vmp_branch", prototype);
            return func;
        }

    }

    public class HandlerLifter
    {
        private readonly IDna dna;

        private readonly LLVMContextRef ctx;

        private readonly RemillArch arch;

        public readonly LLVMModuleRef cacheModule;

        public readonly Dictionary<ulong, LLVMValueRef> handlerRipToLlvmFunction = new();

        public HandlerLifter(IDna dna, LLVMContextRef ctx, RemillArch arch)
        {
            this.dna = dna;
            this.ctx = ctx;
            this.arch = arch;

            if (File.Exists("cacheModule.ll"))
            {
                cacheModule = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, "cacheModule.ll").Value;
                foreach (var f in cacheModule.GetFunctions().Where(x => x.Name.StartsWith("Parameterized_TranslatedFrom") && !x.Name.Contains("from_cache")))
                {
                    var split = f.Name.Split(new string[] { "Parameterized_TranslatedFrom", "_" }, StringSplitOptions.RemoveEmptyEntries);
                    var parsed = ulong.Parse(split[0], System.Globalization.NumberStyles.HexNumber);
                    handlerRipToLlvmFunction[parsed] = f;
                }

                return;
            }

            cacheModule = ctx.CreateModuleWithName("HandlerCache");
        }

        public static ControlFlowGraph<Instruction> DisHandler(IDna dna, ulong ip)
        {
            return dna.RecursiveDescent.ReconstructCfg(ip, IterativeVmpExplorer.DescentCallback(dna), null, IterativeVmpExplorer.ShouldContinueCallback(dna));
        }

        public static bool IsVmexit(ControlFlowGraph<Instruction> cfg)
        {
            return cfg.GetInstructions().Count(x => x.Mnemonic == Iced.Intel.Mnemonic.Pop) > 10;
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
            var (liftedFunction, blockMapping, filterFunctions) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, new BinaryFunction(encodedCfg, scopeTableTree, new List<JmpTable>()), fallthroughFromIps, CallHandlingKind.VMProtect);
            liftedFunction = FunctionIsolator.IsolateFunctionIntoNewModule(arch, liftedFunction);


            //liftedFunction.Handle = 0;
    
     
            var (stripped, stateStruct) = IterativeFunctionTranslator.StripRuntimeVmp(dna, ctx, arch, liftedFunction);
            liftedFunction.Handle = 0;

            stripped.GlobalParent.PrintToFile("translatedFunction.ll");
            // liftedFunction.Handle = 0;


            // This is where the VIP gets swapped from RSI to r9
            // Notably RSI is set to some BS + a binary offset

            if (handlerRip == 0x140048BBD)
            {
                //GetBytecodeRegister(cfg, stateStruct, stripped, false, arch.GetRegisterByName("RSI"));
                Debugger.Break();
            }

            // Eliminate any stack expansion in the IR
            EliminateStackExpansionLoop(stripped, handlerRip);

            EliminateStackAlignment(stripped, stateStruct);


            // Optimize one last time
            OptimizationApi.OptimizeModule(stripped.GlobalParent, stripped, false, false, 0, false, 0, false);


            stripped.GlobalParent.PrintToFile("translatedFunction.ll");
           
            // Move the newly created function into the target module.
            var newHandler = FunctionIsolator.IsolateFunctionInto(cacheModule, stripped);
            handlerRipToLlvmFunction[handlerRip] = newHandler;
            return newHandler;

            //File.WriteAllText("binja.py", new LLVMToBinjaGraph(stripped).Process());

        }


        public RemillRegister GetVmenterBytecodeRegister(ParameterizedStateStructure stateStruct, LLVMValueRef lifted)
        {
            // Get all constant addresses being used as pointers
            var gepConstants = lifted.GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && x.OperandCount == 2 && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(1).ConstIntZExt))
                .Select(x => x.GetOperand(1));

            var constStores = lifted.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(0).ConstIntZExt)).ToList();

            // %24 = getelementptr inbounds i8, ptr %mem, i64 5368736796
            // store i64 5368736796, ptr %out_RSI, align 8
            LLVMValueRef targetStore = constStores
                .SingleOrDefault(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && gepConstants.Contains(x.GetOperand(0)));

            // Otherwise look for a unique bytecode address that is not stored anywhere else
            if (targetStore.Handle == 0)
                targetStore = constStores.Single(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && constStores.Count(y => x.GetOperand(0) == y.GetOperand(0)) == 1);


            var destRegArg = targetStore.GetOperand(1);

            var destIdx = Array.IndexOf(lifted.GetParams(), destRegArg);

            var reg = stateStruct.RegisterOutputArgumentIndices.Single(x => x.Value == destIdx).Key;
            return reg;
        }

        public RemillRegister GetBytecodeRegister(ControlFlowGraph<Instruction> cfg, ParameterizedStateStructure stateStruct, LLVMValueRef stripped, bool isVmEnter, RemillRegister existingRegister)
        {
            if (isVmEnter)
                return GetVmenterBytecodeRegister(stateStruct, stripped);

            var inReg = stateStruct.GetRegInputParam(existingRegister, stripped);
            var outReg = stateStruct.GetRegOutputParam(existingRegister, stripped);

            var store = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(1) == outReg);

            var add = store.GetOperand(0);
            var isVmExit = IsVmexit(cfg);
            var matches = Matches(inReg, add);
            if (!matches && !isVmEnter && !isVmExit)
            {
                var instructions = stripped.EntryBasicBlock.GetInstructions();
                var firstLoad = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0).Kind == LLVMValueKind.LLVMInstructionValueKind && x.GetOperand(0).InstructionOpcode == LLVMOpcode.LLVMGetElementPtr);
                var firstAdd = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMAdd && x.GetOperand(0) == firstLoad && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind);
                var destRegParam = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0) == firstAdd && x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind).GetOperand(1);
                var destReg = stateStruct.RegisterOutputArgumentIndices.Single(x => stripped.GetParam((uint)x.Value) == destRegParam);
                return destReg.Key;

                /*
                var instructions = stripped.EntryBasicBlock.GetInstructions();
                var firstAdd = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMAdd && x.GetOperand(0).Kind == LLVMValueKind.LLVMArgumentValueKind && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind);
                var destRegParam = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0) == firstAdd && x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind).GetOperand(1);
                var destReg = stateStruct.RegisterOutputArgumentIndices.Single(x => stripped.GetParam((uint)x.Value) == destRegParam);
                return destReg.Key;
                */
            }

            return existingRegister;

        }


        private bool Matches(LLVMValueRef inReg, LLVMValueRef add)
        {
            if (add.Kind != LLVMValueKind.LLVMInstructionValueKind || add.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;
            if (IsIncrement(inReg, add))
                return true;
            if (IsIncrementLoadReg(add))
                return true;

            return false;
        }

        private static bool IsIncrement(LLVMValueRef inReg, LLVMValueRef add)
        {
            if (add.GetOperand(0) != inReg)
                return false;
            if (add.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        //  %0 = getelementptr inbounds i8, ptr %mem, i64 %R11
        //  %1 = load i64, ptr %0, align 8
        //  %add.i.i108.i = add i64 %1, 4
        //  store i64 %add.i.i108.i, ptr %out_RSI, align 8
        private static bool IsIncrementLoadReg(LLVMValueRef add)
        {
            if (add.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            var load = add.GetOperand(0);
            if (load.Kind != LLVMValueKind.LLVMInstructionValueKind || load.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return false;

            var gep = load.GetOperand(0);
            if (gep.Kind != LLVMValueKind.LLVMInstructionValueKind || gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr || gep.OperandCount != 2)
                return false;

            if (gep.GetOperand(1).Kind != LLVMValueKind.LLVMArgumentValueKind)
                return false;

            return true;
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
            if (handlerRip == 0x140090F0D)
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();
            }
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

            
            // Get a bitmask indicating which branches lead to a cycle
            var getCycles = (LLVMBasicBlockRef b0, LLVMBasicBlockRef b1) =>
            {
 
                var b0Cyclic = ReachesCycle(b0, new(), new());
                var b1Cyclic = ReachesCycle(b1, new(), new());

                uint r = 0;
                r |= b0Cyclic ? 1u : 0;
                r |= b1Cyclic ? 2u : 0;
                return r;
            };

            /*
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
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMBr && x.OperandCount == 3 && IsStackExpansionPredicate(x) && getCycles(x.GetOperand(1).AsBasicBlock(), x.GetOperand(2).AsBasicBlock()) == 1)
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
            if (!IsAddImm(cond.GetOperand(0)) && !IsAddImm(cond.GetOperand(1)))
                return false;
            
            return true;
        }

        private static bool IsAddImm(LLVMValueRef x)
        {
            if (x.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            if (x.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;
            if (x.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
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

    public class VmpSolver
    {
        private readonly RemillArch arch;

        private readonly LLVMValueRef function;

        public VmpSolver(RemillArch arch, LLVMValueRef function)
        {
            this.arch = arch;
            this.function = function;
        }

        // For each bytecode RIP, collect a set of unique handler RIPs it can branch to
        public Dictionary<ulong, HashSet<ulong>> SolveRIPs(VmpParameterizedStateStructure stateStruct)
        {
            var target = GetBranchIntrinsic();
            if (target.Handle == 0)
                throw new InvalidOperationException("No jump tables to solve");

           
            var calls = RemillUtils.CallersOf(target);
            var ripIndex = (uint)stateStruct.RegisterArgumentIndices[arch.GetRegisterByName(arch.ProgramCounterRegisterName)];

            var output = new Dictionary<ulong, HashSet<ulong>>();
            foreach (var call in calls)
            {
                var jmpFromVip = call.GetOperand((uint)call.OperandCount - 2);
                if (jmpFromVip.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    throw new InvalidOperationException($"Jumping from unknown VIP {jmpFromVip}");

                var rips = new HashSet<ulong>();
                var rip = call.GetOperand(ripIndex);
                if (rip.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                {
                    rips.Add(rip.ConstIntZExt);
                }

                else if (rip.Kind == LLVMValueKind.LLVMInstructionValueKind && rip.InstructionOpcode == LLVMOpcode.LLVMSelect)
                {
                    var constants = new LLVMValueRef[] { rip.GetOperand(1), rip.GetOperand(2) };
                    if (!constants.All(x => x.Kind == LLVMValueKind.LLVMConstantIntValueKind))
                        throw new InvalidOperationException($"Cannot solve RIP for {rip}");
                    rips.AddRange(constants.Select(x => x.ConstIntZExt));
                }

                else
                {
                    throw new InvalidOperationException($"Cannot solve RIP for {rip}");
                }

                output.TryAdd(jmpFromVip.ConstIntZExt, new());
                output[jmpFromVip.ConstIntZExt].AddRange(rips);

            }

            return output;
        }

        public JmpTablesWithHandlerRips Solve()
        {
            var targetFunc = GetBranchIntrinsic();
            if (targetFunc.Handle == 0)
                throw new InvalidOperationException("No jump tables to solve!");

            return null;
        }

        private LLVMValueRef GetBranchIntrinsic()
            => function.GlobalParent.GetFunctions().SingleOrDefault(x => x.Name.Contains("vmp_branch"));
    }
}
