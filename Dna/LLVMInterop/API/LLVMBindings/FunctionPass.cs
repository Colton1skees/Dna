using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    public class FunctionPass : Pass
    {
        public FunctionPass(nint handle) : base(handle)
        {

        }

        public unsafe bool RunOnFunction(LLVMValueRef function)
        {
            return NativePassApi.RunOnFunction(this, function);
        }

        public unsafe static implicit operator LLVMOpaqueFunctionPass*(FunctionPass pass)
        {
            return (LLVMOpaqueFunctionPass*)pass.Handle;
        }

        public unsafe static implicit operator FunctionPass(LLVMOpaqueFunctionPass* pass)
        {
            return new FunctionPass((nint)pass);
        }
    }
}
