using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemorySSAWalker
    {
        public readonly nint Handle;

        public MemorySSAWalker(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe MemoryAccess? GetClobberingMemoryAccess(LLVMValueRef instruction)
        {
            var result = NativeMemorySSAWalkerApi.GetClobberingMemoryAccess(this, instruction);
            return result == null ? null : MemoryAccess.From(result);
        }

        public unsafe MemoryAccess? GetClobberingMemoryAccess(MemoryAccess memAccess)
        {
            var result = NativeMemorySSAWalkerApi.GetClobberingMemoryAccess(this, memAccess);
            return result == null ? null : result;
        }

        // TODO: Get rid of the opaque memory location pointer.
        public unsafe MemoryAccess? GetClobberingMemoryAccess(MemoryAccess memAccess, MemoryLocation location)
        {
            var result = NativeMemorySSAWalkerApi.GetClobberingMemoryAccess(this, memAccess, location);
            return result == null ? null : result;
        }

        public unsafe static implicit operator LLVMOpaqueMemorySSAWalker*(MemorySSAWalker memAccess)
        {
            return (LLVMOpaqueMemorySSAWalker*)memAccess.Handle;
        }

        public unsafe static implicit operator MemorySSAWalker(LLVMOpaqueMemorySSAWalker* memAccess)
        {
            return new MemorySSAWalker((nint)memAccess);
        }
    }
}
