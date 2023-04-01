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
    public static class NativeMemoryLocationApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryLocation_GetOrNone")]
        public unsafe static extern LLVMOpaqueMemoryLocation* GetOrNone(LLVMOpaqueValue* instruction);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryLocation_ToString")]
        public unsafe static extern sbyte* ToString(LLVMOpaqueMemoryLocation* location);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryLocation_GetPtr")]
        public unsafe static extern LLVMOpaqueValue* GetPtr(LLVMOpaqueMemoryLocation* location);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryLocation_GetLocationSize")]
        public unsafe static extern LLVMOpaqueLocationSize* GetLocationSize(LLVMOpaqueMemoryLocation* location);
    }
}
