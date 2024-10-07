using Dna.LLVMInterop.API.LLVMBindings;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public static class NativeOptimizationApi
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong dgReadBinaryContents(ulong rva, uint size);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern IntPtr OptimizeLLVMModule(LLVMOpaqueModule* module,
            LLVMOpaqueValue* function,
            bool aggressiveUnroll,
            bool runClassifyingAliasAnalysis,
            nint ptrGetAliasResult,
            bool runConstantConcretization,
            nint ptrReadBinaryContents,
            bool runStructuring,
            bool justGVN,
            nint ptrStructureFunction);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern IntPtr RunCfgCanonicalizationPipeline(LLVMOpaqueValue* function);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void RunJumpTableSolvingPass(LLVMOpaqueValue* function, nint solveJumpTableBounds, nint trySolveConstant);
    }
}
