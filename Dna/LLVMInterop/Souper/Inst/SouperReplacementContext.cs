using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperReplacementContext
    {
        private readonly nint handle;

        public SouperReplacementContext(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperReplacementContext()
        {
            handle = (nint)NativeSouperReplacementContextApi.ReplacementContextConstructor();
        }

        public unsafe static implicit operator SouperOpaqueReplacementContext*(SouperReplacementContext pcMapping) => (SouperOpaqueReplacementContext*)pcMapping.handle;

        public unsafe static implicit operator SouperReplacementContext(SouperOpaqueReplacementContext* pcMapping) => new SouperReplacementContext((nint)pcMapping);
    }
}
