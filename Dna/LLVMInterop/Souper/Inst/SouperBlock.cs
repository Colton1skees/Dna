using Dna.LLVMInterop.API;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.Manual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperBlock
    {
        private readonly nint handle;

        public unsafe string Name
        {
            get
            {
                var ptr = NativeSouperBlockApi.BlockGetName(this);
                return ptr == null ? null : StringMarshaler.AcquireString(ptr);
            }
        }

        public unsafe uint PredCount => NativeSouperBlockApi.BlockGetPreds(this);

        public unsafe uint Number => NativeSouperBlockApi.BlockGetNumber(this);

        public unsafe uint ConcretePred => NativeSouperBlockApi.BlockGetConcretePred(this);

        public SouperBlock(nint handle)
        {
            this.handle = handle;
        }

        public unsafe static implicit operator SouperOpaqueBlock*(SouperBlock block) => (SouperOpaqueBlock*)block.handle;

        public unsafe static implicit operator SouperBlock(SouperOpaqueBlock* block) => block == null ? null : new SouperBlock((nint)block);
    }
}
