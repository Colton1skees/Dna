using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.Utilities;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;
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

        private readonly VmHandlerCache handlerCache;

        private readonly Dictionary<ulong, ulong> bytecodeAddrToRip = new();

        private OrderedSet<VmHandler> handlers = new OrderedSet<VmHandler>();

        public IterativeVmpExplorer(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.funcRip = funcRip;
            this.handlerCache = new VmHandlerCache(ctx);
        }

        public LLVMValueRef Run()
        {
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
            while (true)
            {
                // Lift all native handlers to LLVM IR and cache them
                IterativeVmpTranslator.LiftHandlersIntoCache(handlerCache, dna, handlersRipsToLift);
                handlersRipsToLift.Clear();

                // Lift the partial CFG
                var stateStruct = handlerCache.GetLiftedHandler(handlers.First().NativeRip).ParameterizedStateStructure;
                var liftedFunction = new IterativeCfgBuilder(outModule, arch, stateStruct, vCfg, handlerCache, vmexitHandlerRips).Run(default, handlers.First());


                IterativeVmpTranslator.CanonicalizeMemoryPtr(liftedFunction);

                // Run our optimization pipeline
                PassPipeline.Run(dna.Binary, liftedFunction, false);

                // Solve for any unknown indirect jumps in the control flow graph.
                var solver = new VmpJmpTableSolver(liftedFunction);
                var (newTables, bytecodePtrToRips) = solver.Solve();
                foreach (var entry in bytecodePtrToRips)
                {
                    bytecodeAddrToRip.TryAdd(entry.Key, entry.Value);
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

                WorkList<VmHandler> changedNodes = new WorkList<VmHandler>();
                foreach (var (handler, info) in newCfg.Instructions)
                {
                    // All new nodes get marked as changed
                    if (!vCfg.Contains(handler))
                    {
                        changedNodes.AddToBack(handler);
                        continue;
                    }

                    // If a node has new predecessors, it should be marked as changed.
                    var oldInfo = vCfg.Instructions[handler];
                    if (!oldInfo.Predecessors.SetEquals(info.Predecessors))
                    {
                        changedNodes.AddToBack(handler);
                        continue;
                    }
                }

                // Get all reachable nodes starting from the list of changed nodes
                // (this is includes the changed nodes themselves)
                var reachableNodes = GetReachableNodes(newCfg, changedNodes);
                foreach(var (handler, info) in newCfg.Instructions)
                    info.Metadata.IsComplete = !reachableNodes.Contains(handler);

                // Replace the CFG
                vCfg = newCfg;
                Debugger.Break();
            }


            Debugger.Break();
            return default;
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


    }

    public class IterativeCfgBuilder
    {
        private readonly LLVMModuleRef module;

        private readonly RemillArch arch;
        private readonly VmpParameterizedStateStructure stateStruct;

        private readonly VmCfg vCfg;

        private readonly VmHandlerCache handlerCache;

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;

        private LLVMBuilderRef builder;

        public IterativeCfgBuilder(LLVMModuleRef module, RemillArch arch, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, VmHandlerCache handlerCache, IReadOnlySet<ulong> vmexitHandlerRips)
        {
            this.module = module;
            this.arch = arch;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.handlerCache = handlerCache;
            this.vmexitHandlerRips = vmexitHandlerRips;
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

            
            // Stack allocate a local state structure and copy all registers into it
            builder.Position(translatedFunction.EntryBasicBlock, translatedFunction.EntryBasicBlock.FirstInstruction);
            var registerAllocaMapping = VmPartialBlockLifter.CreateLocalStateStruct(builder, stateStruct, translatedFunction);

            // Lift all handlers into their own basic block
            var exitBlock = translatedFunction.AppendBasicBlock("exit");
            builder.Position(exitBlock, exitBlock.FirstInstruction);
            builder.BuildRetVoid();
            var blockMapping = LiftInsts(incremental, translatedFunction, exitBlock, registerAllocaMapping);

            if (!incremental)
            {
                builder.PositionAtEnd(translatedFunction.EntryBasicBlock);
                builder.BuildBr(blockMapping[entryHandler]);
            }

            // If we cant incremental build, need to do everything from scatch. Put each inst in basic block, hook them up. Maybe split into SESE regions just to not be insanely unreadable. Insert hooks on exit
            // If incremental, we are just wiring into an existing
            // TODO: If incremental build, wire up state structure and outgoing edges.
            module.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
            //Debugger.Break();
            return translatedFunction;
        }

        private Dictionary<VmHandler, LLVMBasicBlockRef> LiftInsts(bool incremental, LLVMValueRef function, LLVMBasicBlockRef exitBlock, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
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
                var liftedHandler = handlerCache.CloneLiftedHandlerIntoModule(handler.NativeRip, module);
                var call = VmPartialBlockLifter.CallVmHandler(builder, function, liftedHandler, registerAllocaMapping, stateStruct);
                LLVMCloning.InlineFunction(liftedHandler);
                liftedHandler.DeleteFunction();
                LiftInstEdges(handler, function, exitBlock, blockMapping, registerAllocaMapping);
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
            var indirectPc = VmCfgLifter.LoadBytecodePointer(builder, registerAllocaMapping);

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
                VmCfgLifter.AddCallToIndirectBranchIntrinsic(module, builder, llvmBlock, registerAllocaMapping, exitBlock, handler.BytecodeRip);
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
}
