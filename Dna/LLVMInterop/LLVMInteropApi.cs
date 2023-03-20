using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public static class LLVMInteropApi
    {
        public unsafe static void Test(LLVMModuleRef module)
        {
            NativeLLVMInterop.VerifyModule(module);
        }
    }
}
