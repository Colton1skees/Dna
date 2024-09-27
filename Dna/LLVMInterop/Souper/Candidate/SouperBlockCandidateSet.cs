using Dna.LLVMInterop.API;
using Dna.LLVMInterop.Souper.Inst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public class SouperBlockCandidateSet
    {
        private readonly nint handle;

        public IReadOnlyList<SouperInstMapping> PCs => GetPCs();

        public IReadOnlyList<SouperBlockPCMapping> BPCs => GetBPCs();

        public IReadOnlyList<SouperCandidateReplacement> Replacements => GetReplacements();

        public SouperBlockCandidateSet(nint handle)
        {
            this.handle = handle;
        }

        private unsafe IReadOnlyList<SouperInstMapping> GetPCs()
        {
            var ptr = (nint)NativeSouperBlockCandidateSetApi.BlockCandidateSetGetPCs(this);

            var vec = new ManagedVector<SouperInstMapping>(ptr, x => new SouperInstMapping(x));

            return vec.Items;
        }

        private unsafe IReadOnlyList<SouperBlockPCMapping> GetBPCs()
        {
            var ptr = (nint)NativeSouperBlockCandidateSetApi.BlockCandidateSetGetBPCs(this);

            var vec = new ManagedVector<SouperBlockPCMapping>(ptr, x => new SouperBlockPCMapping(x));

            return vec.Items;
        }

        private unsafe IReadOnlyList<SouperCandidateReplacement> GetReplacements()
        {
            var ptr = (nint)NativeSouperBlockCandidateSetApi.BlockCandidateSetGetReplacements(this);

            var vec = new ManagedVector<SouperCandidateReplacement>(ptr, x => new SouperCandidateReplacement(x));

            return vec.Items;
        }

        public unsafe static implicit operator SouperOpaqueBlockCandidateSet*(SouperBlockCandidateSet block) => (SouperOpaqueBlockCandidateSet*)block.handle;

        public unsafe static implicit operator SouperBlockCandidateSet(SouperOpaqueBlockCandidateSet* block) => new SouperBlockCandidateSet((nint)block);
    }
}
