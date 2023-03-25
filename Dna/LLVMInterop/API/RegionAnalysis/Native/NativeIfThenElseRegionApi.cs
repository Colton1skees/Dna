using ELFSharp.MachO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Native
{
    public static class NativeIfThenElseRegionApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IfThenElseRegionGetElse")]
        public unsafe static extern nint IfThenElseRegionGetElse(nint region);
    }
}
