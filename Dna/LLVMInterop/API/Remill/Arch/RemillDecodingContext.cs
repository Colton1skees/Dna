using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    public class RemillDecodingContext
    {
        public readonly nint Handle;

        public RemillDecodingContext(nint handle)
        {
            Handle = handle;
        }

        public unsafe RemillDecodingContext()
        {
            Handle = (nint)NativeRemillDecodingContext.DecodingContext_Constructor();
        }

        public unsafe static implicit operator RemillOpaqueDecodingContext*(RemillDecodingContext reg)
        {
            return (RemillOpaqueDecodingContext*)reg.Handle;
        }

        public unsafe static implicit operator RemillDecodingContext(RemillOpaqueDecodingContext* reg)
        {
            return new RemillDecodingContext((nint)reg);
        }
    }
}
