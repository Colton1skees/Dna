using Dna.BinaryTranslator.Lifting;
using Dna.BinaryTranslator.VMProtect;
using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Passes;
using Dna.SEH;
using Dna.Utilities;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Unsafe
{
    // Class for translating functions into a human readable IR / compiled representation
    public class BrighteningTranslator
    {
        private readonly IDna dna;

        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly BinaryFunction binaryFunction;

        public static void Run(IDna dna, RemillArch arch, LLVMContextRef ctx, BinaryFunction binaryFunction)
            => new BrighteningTranslator(dna, arch, ctx, binaryFunction).Run();

        private BrighteningTranslator(IDna dna, RemillArch arch, LLVMContextRef ctx, BinaryFunction binaryFunction)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.binaryFunction = binaryFunction;
        }

        private void Run()
        {
            var scopeTable = IterativeFunctionTranslator.GetScopeTable(dna.Binary, binaryFunction.Cfg.StartAddress);
            var scopeTableTree = new ScopeTableTree(scopeTable);
            (var cfg, var fallthroughFromIps) = IterativeFunctionTranslator.PreprocessCfg(binaryFunction.Cfg, scopeTable);
            var encodedCfg = X86CfgEncoder.EncodeCfg(dna.Binary, cfg);

            var (liftedFunction, blockMapping, filterFunctions) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, new BinaryFunction(encodedCfg, scopeTableTree, binaryFunction.JmpTables), fallthroughFromIps);
            liftedFunction = FunctionIsolator.IsolateFunctionIntoNewModuleWithSehSupport(arch, liftedFunction, filterFunctions.Select(x => x.LiftedFilterFunction).ToList().AsReadOnly()).function;


            liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");


            liftedFunction = StripRuntime(liftedFunction);
            liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");



            Debugger.Break();

            var compiledPath = ClangCompiler.Compile("translatedFunction.ll");
            var loaded = IDALoader.Load(compiledPath);

            Console.WriteLine(binaryFunction.Cfg.ToString());
            Debugger.Break();
        }

        // TODO: Refactor out code duplication!
        private LLVMValueRef StripRuntime(LLVMValueRef function)
        {
            // Apply concrete implementations to all simple.
            // E.g. __remill_memory_read() and __remill_memory_write().
            var runtime = UnsafeRuntimeImplementer.Implement(function.GlobalParent);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Create a new function which doesn't take a state structure pointer.
            // Instead it takes all root registers as `noalias ptr` arguments.
            var parameterizedStateStruct = ParameterizedStateStructure.CreateFromFunction(arch, function, true, true, justRAX: true);

            // Set the function variable to the output function. This is required,
            // since parameterization requires creating a completely new function
            // while inlining the original function.
            // Note: The old function is also destroyed(deleted) at this point.
            function = parameterizedStateStruct.OutputFunction;
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Modify the @__remill_function_call intrinsic to use the fastcall ABI.
            FastcallAbiInserter.Insert(arch, runtime, function, parameterizedStateStruct);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Replace the remill return and error intrinsics with
            // functions that allow more strong optimization.
            ErrorAndReturnImplementer.Implement(function);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Remove state ptr from @remill_jump intrinsic
            var jumpIntrinsic = function.GlobalParent.GetFunctions().FirstOrDefault(x => x.Name == "__remill_jump");
            List<LLVMValueRef> targets = jumpIntrinsic == default(LLVMValueRef) ? new() : RemillUtils.CallersOf(jumpIntrinsic).Where(x => x.InstructionParent.Parent == function).ToList();
            foreach (var target in targets)
            {
                Console.WriteLine(target.OperandCount);
                var operands = target.GetOperands().ToList();
                target.SetOperand(0, target.GetOperand(2));
                Console.WriteLine("");
            }

            // Delete all calls to the RETURN intrinsic.
            var retIntrinsic = function.GlobalParent.GetFunctions().FirstOrDefault(x => x.Name == "dna_return");
            List<LLVMValueRef> retTargets = retIntrinsic == default(LLVMValueRef) ? new() : RemillUtils.CallersOf(retIntrinsic).Where(x => x.InstructionParent.Parent == function).ToList();
            retTargets.ForEach(x => x.InstructionEraseFromParent());

            // Create a single @memory pointer.
            var memoryPtr = runtime.MemoryPointer;
            var builder = LLVMBuilderRef.Create(ctx);
            builder.Position(function.EntryBasicBlock, function.EntryBasicBlock.FirstInstruction);
            var dominatingLoad = builder.BuildLoad2(ctx.GetPtrType(), memoryPtr.Value, "mem");

            targets = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == memoryPtr && x != dominatingLoad).ToList();
            if (targets.Any())
            {
                foreach (var other in targets)
                {
                    other.ReplaceAllUsesWith(dominatingLoad);
                    other.InstructionEraseFromParent();
                }
            }

            for(int i = 0; i < 5; i++)
            {
                /*
                if(i == 2)
                {
                    function.GetParam(11).ReplaceAllUsesWith(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0));
                }
                */

                OptimizationApi.OptimizeModule(function.GlobalParent, function, false, false, 0, false, 0, false);
                PassPipeline.Run(dna.Binary, function, false, false);

                function.GlobalParent.PrintToFile("translatedFunction.ll");

                MbaDeobfuscationPass.Run(function);

            }

            return function;
        }
    }
}
