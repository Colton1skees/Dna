using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public class SouperExprBuilderContext
    {
        private readonly nint handle;

        public SouperExprBuilderContext(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperExprBuilderContext()
        {
            handle = (nint)NativeSouperExprBuilderContextApi.ExprBuilderContextConstructor();
        }

        public unsafe static implicit operator SouperOpaqueExprBuilderContext*(SouperExprBuilderContext block) => (SouperOpaqueExprBuilderContext*)block.handle;

        public unsafe static implicit operator SouperExprBuilderContext(SouperOpaqueExprBuilderContext* block) => new SouperExprBuilderContext((nint)block);
    }
}
