using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms
{
    public static unsafe class UtilsPasses
    {
        public unsafe static FunctionPass CreateLowerSwitchPass()
        {
            return NativeUtilsApi.CreateLowerSwitchPass();
        }

        public unsafe static FunctionPass CreateLCSSAPass()
        {
            return NativeUtilsApi.CreateLCSSAPass();
        }

        public unsafe static FunctionPass CreateLoopSimplifyPass()
        {
            return NativeUtilsApi.CreateLoopSimplifyPass();
        }

        public unsafe static FunctionPass CreateFixIrreduciblePass()
        {
            return NativeUtilsApi.CreateFixIrreduciblePass();
        }
    }
}
