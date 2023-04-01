﻿using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemoryAccess
    {
        public readonly nint Handle;

        public LLVMValueRef AsValue => new LLVMValueRef(Handle);

        public MemoryAccess(nint handle)
        {
            this.Handle = handle;
        }

        public override string ToString()
        {
            return GetString();
        }

        private unsafe string GetString()
        {
            var pStr = NativeMemoryAccessApi.ToString(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public unsafe static implicit operator LLVMOpaqueMemoryAccess*(MemoryAccess memAccess)
        {
            return (LLVMOpaqueMemoryAccess*)memAccess.Handle;
        }

        public unsafe static implicit operator MemoryAccess(LLVMOpaqueMemoryAccess* memAccess)
        {
            return new MemoryAccess((nint)memAccess);
        }
    }
}
