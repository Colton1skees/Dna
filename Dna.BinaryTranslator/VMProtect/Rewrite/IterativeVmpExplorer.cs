using Dna.DataStructures;
using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                Debugger.Break();
            }


            Debugger.Break();
            return default;
        }
    }

    public class IterativeCfgBuilder
    {
        private readonly LLVMModuleRef module;

        private readonly RemillArch arch;
        private readonly LLVMValueRef? existingFunction;
        private readonly VmpParameterizedStateStructure stateStruct;

        private readonly VmCfg vCfg;

        private readonly VmHandlerCache handlerCache;

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;

        private LLVMBuilderRef builder;

        public IterativeCfgBuilder(LLVMModuleRef module, RemillArch arch, LLVMValueRef? existingFunction, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, VmHandlerCache handlerCache, IReadOnlySet<ulong> vmexitHandlerRips)
        {
            this.module = module;
            this.arch = arch;
            this.existingFunction = existingFunction;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.handlerCache = handlerCache;
            this.vmexitHandlerRips = vmexitHandlerRips;
            builder = LLVMBuilderRef.Create(module.Context);
        }

        public void Run(LLVMValueRef translatedFunction)
        {
            bool incremental = translatedFunction.Handle != 0;
            if (!incremental)
            {
                translatedFunction = module.AddFunction($"PartialCfg", stateStruct.ParameterizedFunctionPrototype);
                translatedFunction.AppendBasicBlock("entry");
            }
            
            // Stack allocate a local state structure and copy all registers into it
            builder.PositionBefore(translatedFunction.EntryBasicBlock.FirstInstruction);
            var registerAllocaMapping = VmPartialBlockLifter.CreateLocalStateStruct(builder, stateStruct, translatedFunction);

            // Lift all handlers into their own basic block
            LiftInsts(incremental, translatedFunction, registerAllocaMapping);

            // If we cant incremental build, need to do everything from scatch. Put each inst in basic block, hook them up. Maybe split into SESE regions just to not be insanely unreadable. Insert hooks on exit
            // If incremental, we are just wiring into an existing

        }

        private Dictionary<VmHandler, LLVMBasicBlockRef> LiftInsts(bool incremental, LLVMValueRef function, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
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
                VmPartialBlockLifter.CallVmHandler(builder, function, liftedHandler, registerAllocaMapping, stateStruct);
                // TODO: Inline^
                LiftInstEdges(handler, function, blockMapping, registerAllocaMapping);

            }

           

            return blockMapping;
        }

        private void LiftInstEdges(VmHandler handler, LLVMValueRef function, Dictionary<VmHandler, LLVMBasicBlockRef> blockMapping, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
        {
            var llvmBlock = blockMapping[handler];
            bool isVmExit = vmexitHandlerRips.Contains(handler.NativeRip);
            var info = vCfg.Instructions[handler];
            bool isComplete = info.Metadata.IsComplete;
            if ((isComplete && info.Successors.Count == 0) || isVmExit)
            {
                VmPartialBlockLifter.UpdateOutputRegisters(builder, function, registerAllocaMapping, stateStruct);
            }
        }
    }
}
