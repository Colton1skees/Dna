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
    public static class NativeMemoryUseOrDefApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryUseOrDef_GetMemoryInst")]
        public unsafe static extern LLVMOpaqueValue* GetMemoryInst(LLVMOpaqueMemoryUseOrDef* useOrDef);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryUseOrDef_GetDefiningAccess")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetDefiningAccess(LLVMOpaqueMemoryUseOrDef* useOrDef);
    }
}
