using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemoryLocation
    {
        public readonly nint Handle;

        public unsafe LLVMValueRef Ptr => NativeMemoryLocationApi.GetPtr(this);

        public unsafe LocationSize Size => NativeMemoryLocationApi.GetLocationSize(this);

        public MemoryLocation(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe override string ToString()
        {
            var pStr = NativeMemoryLocationApi.ToString(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public static unsafe MemoryLocation? Get(LLVMValueRef instruction)
        {
            var ptr = NativeMemoryLocationApi.GetOrNone(instruction);
            return ptr == null ? null : ptr;
        }

        public unsafe static implicit operator LLVMOpaqueMemoryLocation*(MemoryLocation memAccess)
        {
            return (LLVMOpaqueMemoryLocation*)memAccess.Handle;
        }

        public unsafe static implicit operator MemoryLocation(LLVMOpaqueMemoryLocation* memAccess)
        {
            return new MemoryLocation((nint)memAccess);
        }
    }
}
