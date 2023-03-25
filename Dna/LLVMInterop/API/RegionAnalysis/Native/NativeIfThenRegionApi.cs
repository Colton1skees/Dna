using ELFSharp.MachO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Native
{
    public class NativeIfThenRegionApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IfThenRegionGetThen")]
        public unsafe static extern nint IfThenRegionGetThen(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IfThenRegionGetIsNegated")]
        public unsafe static extern bool IfThenRegionGetIsNegated(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IfThenRegionGetLLVMTerminatorInstruction")]
        public unsafe static extern nint IfThenRegionGetLLVMTerminatorInstruction(nint region);
    }
}
