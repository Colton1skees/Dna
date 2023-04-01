using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    public class LoopPass : Pass
    {
        public LoopPass(nint handle) : base(handle)
        {

        }

        public unsafe static implicit operator LLVMOpaqueLoopPass*(LoopPass pass)
        {
            return (LLVMOpaqueLoopPass*)pass.Handle;
        }

        public unsafe static implicit operator LoopPass(LLVMOpaqueLoopPass* pass)
        {
            return new LoopPass((nint)pass);
        }
    }
}
