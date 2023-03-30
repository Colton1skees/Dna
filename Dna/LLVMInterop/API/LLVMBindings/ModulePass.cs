using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    public class ModulePass : Pass
    {
        public ModulePass(nint handle) : base(handle)
        {

        }

        public unsafe bool RunOnModule(LLVMModuleRef module)
        {
            return NativePassApi.RunOnModule(this, module);
        }

        public unsafe static implicit operator LLVMOpaqueModulePass*(ModulePass pass)
        {
            return (LLVMOpaqueModulePass*)pass.Handle;
        }

        public unsafe static implicit operator ModulePass(LLVMOpaqueModulePass* pass)
        {
            return new ModulePass((nint)pass);
        }
    }
}
