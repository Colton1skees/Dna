using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemoryUseOrDef : MemoryAccess
    {
        public unsafe LLVMValueRef MemoryInst => NativeMemoryUseOrDefApi.GetMemoryInst(this);

        public unsafe MemoryAccess DefiningAccess => NativeMemoryUseOrDefApi.GetDefiningAccess(this);

        public MemoryUseOrDef(nint handle) : base(handle)
        {

        }

        public unsafe static implicit operator LLVMOpaqueMemoryUseOrDef*(MemoryUseOrDef memAccess)
        {
            return (LLVMOpaqueMemoryUseOrDef*)memAccess.Handle;
        }

        public unsafe static implicit operator MemoryUseOrDef(LLVMOpaqueMemoryUseOrDef* memAccess)
        {
            return new MemoryUseOrDef((nint)memAccess);
        }
    }
}
