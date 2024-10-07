using Dna.Extensions;
using Dna.LLVMInterop.API.Optimization;
using Dna.Utilities;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    public record CompileableFunctionMetadata(SafelyTranslatedFunction TranslatedFunction, IReadOnlyDictionary<ulong, ulong> VmcallPtrs, ulong VmRetPtr);

    public class FunctionCompiler
    {
        private readonly IDna dna;

        private readonly LLVMValueRef llvmFunction;

        private readonly SafelyTranslatedFunction translatedFunction;

        private ulong ptrReadonlyData;

        public static CompileableFunctionMetadata Compile(IDna dna, SafelyTranslatedFunction function, ulong ptrReadonlyData)
        {
            return new FunctionCompiler(dna, function, ptrReadonlyData).Compile();
        }

        private FunctionCompiler(IDna dna, SafelyTranslatedFunction translatedFunction, ulong ptrReadonlyData)
        {
            this.dna = dna;
            this.llvmFunction = translatedFunction.Runtime.OutputFunction;
            this.translatedFunction = translatedFunction;
            this.ptrReadonlyData = ptrReadonlyData;
        }

        private CompileableFunctionMetadata Compile()
        {
            var vcallPtrs = InitializeVmcallPtrs();
            var vexitPtr = InitializeVexitPtr();

            ImplementCSpecificHandler();

            //OptimizationApi.OptimizeModule(llvmFunction.GlobalParent, llvmFunction);

            llvmFunction.GlobalParent.PrintToFile("translatedFunction.ll");

            var path = ClangCompiler.CompileToWindowsDll(llvmFunction, "translatedFunction.ll", false);
            IDALoader.Load(path, true);

            return new CompileableFunctionMetadata(translatedFunction, vcallPtrs, vexitPtr);
        }

        private IReadOnlyDictionary<ulong, ulong> InitializeVmcallPtrs()
        {
            // Mapping of <vcall address, vcallptr>
            var output = new Dictionary<ulong, ulong>();
            foreach(var vcall in translatedFunction.Runtime.CallKeyToStubVmEnterGlobalPtrs)
            {
                var ptrAddress = AllocUlong();
                var offset = ptrAddress - dna.Binary.BaseAddress;
                output.Add(vcall.Key, ptrAddress);

                // Collect all loads to the current global vmenter pointer variable.
                //Console.WriteLine(vcall.Value);
                var globalLoads = vcall.Value
                    .GetUsers()
                    .Where(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionOpcode == LLVMOpcode.LLVMLoad)
                    .ToList();

                // Replace all uses of the global variable (e.g. ptr_vm_reenter_at_1400036DA) with a constant representing the offset(relative to imgbase)
                // of a readonly ptr to the vcall stub.
                var constOffset = LLVMValueRef.CreateConstInt(llvmFunction.GetFunctionCtx().Int64Type, offset);
                foreach (var load in globalLoads)
                {
                    load.ReplaceAllUsesWith(constOffset);
                    load.InstructionEraseFromParent();
                }
            }

            return output.AsReadOnly();
        }

        private ulong InitializeVexitPtr()
        {
            var vexitPtr = translatedFunction.Runtime.RetStubOffsetPtrGlobal;
            var globalLoads = vexitPtr
                    .GetUsers()
                    .Where(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionOpcode == LLVMOpcode.LLVMLoad)
                    .ToList();

            var ptrAddress = AllocUlong();
            var offset = ptrAddress - dna.Binary.BaseAddress;
            var constOffset = LLVMValueRef.CreateConstInt(llvmFunction.GetFunctionCtx().Int64Type, offset);
            foreach (var load in globalLoads)
            {
                load.ReplaceAllUsesWith(constOffset);
                load.InstructionEraseFromParent();
            }

            return ptrAddress;
        }

        private ulong AllocUlong()
        {
            var result = ptrReadonlyData;
            ptrReadonlyData += 8;
            return result;
        }

        /// <summary>
        /// Provides a sample implementation for C_specific_handler that will later be replaced.
        /// </summary>
        private void ImplementCSpecificHandler()
        {
            var handler = llvmFunction.GlobalParent.GetNamedFunction("__C_specific_handler");
            if (handler.Handle == nint.Zero)
                return;

            var block = handler.AppendBasicBlock("entry");
            var ctx = handler.GetFunctionCtx();
            var builder = LLVMBuilderRef.Create(ctx);
            builder.PositionAtEnd(block);
            builder.BuildRet(LLVMValueRef.CreateConstInt(ctx.GetInt32Ty(), 11111111));
        }
    }
}
