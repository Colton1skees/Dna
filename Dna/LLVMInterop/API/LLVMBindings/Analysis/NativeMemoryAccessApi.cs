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
    public static class NativeMemoryAccessApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryAccess_GetBlock")]
        public unsafe static extern LLVMOpaqueBasicBlock* GetBlock(LLVMOpaqueMemoryAccess* memoryAccess);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryAccess_ToString")]
        public unsafe static extern sbyte* ToString(LLVMOpaqueMemoryAccess* memoryAccess);
    }
}
