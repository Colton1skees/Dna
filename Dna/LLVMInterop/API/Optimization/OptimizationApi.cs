using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Optimization
{
    public static class OptimizationApi
    {
        public static unsafe void OptimizeModule(LLVMModuleRef module,
            LLVMValueRef function,
            bool aggressiveUnroll,
            bool runClassifyingAliasAnalysis,
            nint ptrGetAliasResult,
            bool runConstantConcretization,
            nint ptrReadBinaryContents,
            bool runStructuring,
            bool justGVN = false)
        {
            NativeOptimizationApi.OptimizeLLVMModule(module,
                function,
                aggressiveUnroll, 
                runClassifyingAliasAnalysis,
                ptrGetAliasResult, 
                runConstantConcretization, 
                ptrReadBinaryContents,
                runStructuring,
                justGVN);
        }
    }
}
