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
    public static class NativePassApi
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate bool dgStructureFunction(LLVMOpaqueValue* function, nint loopInfo, nint mssa);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateControlledNodeSplittingPass();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateUnSwitchPass();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopExitEnumerationPass();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateControlFlowStructuringPass(nint structureFunction);
    }
}
