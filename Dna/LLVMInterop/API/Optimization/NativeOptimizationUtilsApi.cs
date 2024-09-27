using Dna.LLVMInterop.API;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public static class NativeOptimizationUtilsApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* FindReachableNodes(LLVMOpaqueBasicBlock* source, LLVMOpaqueBasicBlock* target);
    }
}
