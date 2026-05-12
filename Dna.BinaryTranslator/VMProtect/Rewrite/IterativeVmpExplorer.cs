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

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;

        public IterativeCfgBuilder(LLVMModuleRef module, RemillArch arch, LLVMValueRef? existingFunction, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, IReadOnlySet<ulong> vmexitHandlerRips)
        {
            this.module = module;
            this.arch = arch;
            this.existingFunction = existingFunction;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.vmexitHandlerRips = vmexitHandlerRips;
        }

        public void Run()
        {

        }
    }
}
