using Dna.LLVMInterop.API.RegionAnalysis.Native;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public unsafe abstract class PassManagerBase
    {
        public readonly nint Handle;

        public PassManagerBase(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe void Add(Pass pass)
        {
            NativePassManagerApi.AddPass(this, pass);
        }

        public unsafe static implicit operator LLVMOpaquePassManagerBase*(PassManagerBase pass)
        {
            return (LLVMOpaquePassManagerBase*)pass.Handle;
        }
    }
}
