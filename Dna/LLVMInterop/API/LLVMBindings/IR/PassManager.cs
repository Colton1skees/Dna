using Dna.LLVMInterop.API.LLVMBindings.IR;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public class PassManager : PassManagerBase
    {
        public unsafe PassManager() : base((nint)NativePassManagerApi.PassManagerConstructor())
        {
            
        }

        public PassManager(nint handle) : base(handle)
        {

        }

        public unsafe bool Run(LLVMModuleRef module)
        {
            return NativePassManagerApi.RunOnModule(this, module);
        }

        public unsafe static implicit operator LLVMOpaquePassManager*(PassManager pass)
        {
            return (LLVMOpaquePassManager*)pass.Handle;
        }

        public unsafe static implicit operator PassManager(LLVMOpaquePassManager* pass)
        {
            return new PassManager((nint)pass);
        }
    }
}
