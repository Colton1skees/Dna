using Dna.LLVMInterop.API.LLVMBindings.IR;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public class FunctionPassManager : PassManagerBase
    {
        public unsafe FunctionPassManager() : base((nint)NativePassManagerApi.FunctionPassManagerConstructor())
        {

        }

        public FunctionPassManager(nint handle) : base(handle)
        {

        }

        public unsafe bool Run(LLVMValueRef function)
        {
            return NativePassManagerApi.RunOnFunction(this, function);
        }

        public unsafe bool DoInitialization()
        {
            return NativePassManagerApi.FunctionPassManagerDoInitialization(this);
        }

        public unsafe bool DoFinalization()
        {
            return NativePassManagerApi.FunctionPassManagerDoFinalization(this);
        }

        public unsafe static implicit operator LLVMOpaqueFunctionPassManager*(FunctionPassManager pass)
        {
            return (LLVMOpaqueFunctionPassManager*)pass.Handle;
        }

        public unsafe static implicit operator FunctionPassManager(LLVMOpaqueFunctionPassManager* pass)
        {
            return new FunctionPassManager((nint)pass);
        }
    }
}
