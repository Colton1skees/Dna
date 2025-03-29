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
    public static class NativeConstantFoldingAPI
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "TryConstantFold")]
        public unsafe static extern LLVMOpaqueValue* TryConstantFold(LLVMOpaqueValue* inst);
    }
}
