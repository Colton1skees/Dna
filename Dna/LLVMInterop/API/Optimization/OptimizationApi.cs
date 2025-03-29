using Dna.LLVMInterop.API.LLVMBindings;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.LLVMInterop.API.Optimization
{
    public static class OptimizationApi
    {
        public static unsafe void OptimizeModuleVmp(LLVMModuleRef module,
            LLVMValueRef function,
            bool aggressiveUnroll,
            bool runClassifyingAliasAnalysis,
            nint ptrGetAliasResult,
            bool runConstantConcretization,
            nint ptrReadBinaryContents,
            bool runStructuring,
            bool justGVN = false,
            nint ptrStructureFunction = 0,
            nint ptrEliminateStackVars = 0,
            nint adhocInstCombine = 0,
            nint multiUseCloning = 0)
        {
            NativeOptimizationApi.OptimizeModuleVmp(module,
                function,
                aggressiveUnroll,
                runClassifyingAliasAnalysis,
                ptrGetAliasResult,
                runConstantConcretization,
                ptrReadBinaryContents,
                runStructuring,
                justGVN,
                ptrStructureFunction,
                ptrEliminateStackVars,
                adhocInstCombine,
                multiUseCloning);
        }

        public static unsafe void OptimizeModule(LLVMModuleRef module,
            LLVMValueRef function,
            bool aggressiveUnroll,
            bool runClassifyingAliasAnalysis,
            nint ptrGetAliasResult,
            bool runConstantConcretization,
            nint ptrReadBinaryContents,
            bool runStructuring,
            bool justGVN = false,
            nint ptrStructureFunction = 0)
        {
            NativeOptimizationApi.OptimizeLLVMModule(module,
                function,
                aggressiveUnroll, 
                runClassifyingAliasAnalysis,
                ptrGetAliasResult, 
                runConstantConcretization, 
                ptrReadBinaryContents,
                runStructuring,
                justGVN,
                ptrStructureFunction);
        }

        public static unsafe void RunCfgCanonicalizationPipeline(LLVMValueRef function)
        {
            NativeOptimizationApi.RunCfgCanonicalizationPipeline(function);
        }

        public unsafe static void RunJumpTableSolvingPass(LLVMValueRef function, dgSolveJumpTableBounds structureFunction, dgTrySolveConstant trySolveConstant)
        {
            NativeOptimizationApi.RunJumpTableSolvingPass(function, Marshal.GetFunctionPointerForDelegate(structureFunction), Marshal.GetFunctionPointerForDelegate(trySolveConstant));
        }
    }
}
