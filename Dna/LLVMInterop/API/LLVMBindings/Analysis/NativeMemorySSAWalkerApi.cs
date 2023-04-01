using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public static class NativeMemorySSAWalkerApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAWalker_GetInstClobberingMemoryAccess")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetClobberingMemoryAccess(LLVMOpaqueMemorySSAWalker* ssaWalker, LLVMOpaqueValue* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAWalker_GetAccessClobberingMemoryAccess1")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetClobberingMemoryAccess(LLVMOpaqueMemorySSAWalker* ssaWalker, LLVMOpaqueMemoryAccess* memAccess);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAWalker_GetLocClobberingMemoryAccess")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetClobberingMemoryAccess(LLVMOpaqueMemorySSAWalker* ssaWalker, LLVMOpaqueMemoryAccess* memAccess, LLVMOpaqueMemoryLocation* location);
    }
}
