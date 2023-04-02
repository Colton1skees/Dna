using LLVMSharp.Interop;
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

        public unsafe static MemoryAccess From(LLVMOpaqueMemoryAccess* access)
        {
            return From((nint)access);
        }

        public unsafe static MemoryAccess From(nint handle)
        {
            var kind = new LLVMValueRef(handle).Kind;
            return kind switch
            {
                LLVMValueKind.LLVMMemoryDefValueKind => new MemoryUseOrDef(handle),
                LLVMValueKind.LLVMMemoryUseValueKind => new MemoryUseOrDef(handle),
                LLVMValueKind.LLVMMemoryPhiValueKind => new MemoryPhi(handle),
                _ => throw new InvalidOperationException($"Cannot translate kind {kind} to memory access.")
            };
        }

        public unsafe static implicit operator LLVMOpaqueMemoryAccess*(MemoryAccess memAccess)
        {
            return (LLVMOpaqueMemoryAccess*)memAccess.Handle;
        }

        public unsafe static implicit operator MemoryAccess(LLVMOpaqueMemoryAccess* memAccess)
        {
            return From(memAccess);
        }
    }
}
