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
        public unsafe static void Test(LLVMModuleRef module, IntPtr readBinaryContents)
        {
            NativeLLVMInterop.VerifyModule(module, readBinaryContents);
        }
    }
}
