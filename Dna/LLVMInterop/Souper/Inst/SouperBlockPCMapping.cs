using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.Manual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperBlockPCMapping
    {
        public readonly nint handle;

        public unsafe SouperBlock Block => NativeSouperBlockPCMappingApi.BlockPcMappingGetBlock(this);

        public unsafe uint PredIdx => NativeSouperBlockPCMappingApi.BlockPcMappingGetPrexIdx(this);

        public unsafe SouperInstMapping Pc => NativeSouperBlockPCMappingApi.BlockPcMappingGetPc(this);

        public SouperBlockPCMapping(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperBlockPCMapping(SouperBlock block, uint index, SouperInstMapping pc)
        {
            handle = (nint)NativeSouperBlockPCMappingApi.BlockPcMappingConstructor(block, index, pc);
        }

        public unsafe static implicit operator SouperOpaqueBlockPCMapping*(SouperBlockPCMapping pcMapping) => (SouperOpaqueBlockPCMapping*)pcMapping.handle;

        public unsafe static implicit operator SouperBlockPCMapping(SouperOpaqueBlockPCMapping* pcMapping) => new SouperBlockPCMapping((nint)pcMapping);
    }
}
