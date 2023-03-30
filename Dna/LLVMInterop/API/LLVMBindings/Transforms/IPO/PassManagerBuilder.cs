using Dna.LLVMInterop.API.LLVMBindings.IR;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO
{
    public class PassManagerBuilder
    {
        private readonly nint Handle;

        public unsafe PassManagerBuilder() : this((nint)NativePassManagerBuilderApi.PassManagerBuilderConstructor())
        {
            
        }

        public PassManagerBuilder(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe void PopulateFunctionPassManager(FunctionPassManager fpm)
        {
            NativePassManagerBuilderApi.PopulateFunctionPassManager(this, fpm);
        }

        public unsafe void PopulateModulePassManager(PassManager mpm)
        {
            NativePassManagerBuilderApi.PopulateModulePassManager(this, mpm);
        }

        public unsafe static implicit operator LLVMOpaquePassManagerBuilder*(PassManagerBuilder pass)
        {
            return (LLVMOpaquePassManagerBuilder*)pass.Handle;
        }

        public unsafe static implicit operator PassManagerBuilder(LLVMOpaquePassManagerBuilder* pass)
        {
            return new PassManagerBuilder((nint)pass);
        }
    }
}
