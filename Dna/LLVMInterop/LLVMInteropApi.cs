using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativeLLVMInterop;

namespace Dna.LLVMInterop
{
    public static class LLVMInteropApi
    {
        public unsafe static Region Test(LLVMModuleRef module, IntPtr readBinaryContents)
        {
            nint ptr = NativeLLVMInterop.OptimizeModule(module, readBinaryContents);

            return Region.CreateRegion(ptr);
        }
    }
}
