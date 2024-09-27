using Dna.LLVMInterop.API;
using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public class SouperCandidateReplacement
    {
        private readonly nint handle;

        public unsafe LLVMValueRef Origin => NativeSouperCandidateReplacementApi.CandidateReplacementGetOrigin(this);

        public unsafe SouperInstMapping InstMapping => NativeSouperCandidateReplacementApi.CandidateReplacementGetInstMapping(this);

        public IReadOnlyList<SouperInstMapping> PathConditions => GetPathConditions();

        public IReadOnlyList<SouperBlockPCMapping> BlockPathConditions => GetBlockPathConditions();

        public SouperCandidateReplacement(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperCandidateReplacement(LLVMValueRef origin, SouperInstMapping instMapping)
        {
            handle = (nint)NativeSouperCandidateReplacementApi.CandidateReplacementConstructor(origin, instMapping);
        }

        private unsafe IReadOnlyList<SouperInstMapping> GetPathConditions()
        {
            var ptr = (nint)NativeSouperCandidateReplacementApi.CandidateReplacementGetPathConditions(this);

            var vec = new ManagedVector<SouperInstMapping>(ptr, x => new SouperInstMapping(x));

            return vec.Items;
        }

        private unsafe IReadOnlyList<SouperBlockPCMapping> GetBlockPathConditions()
        {
            var ptr = (nint)NativeSouperCandidateReplacementApi.CandidateReplacementGetBlockPathConditions(this);

            var vec = new ManagedVector<SouperBlockPCMapping>(ptr, x => new SouperBlockPCMapping(x));

            return vec.Items;
        }

        public unsafe static implicit operator SouperOpaqueCandidateReplacement*(SouperCandidateReplacement block) => (SouperOpaqueCandidateReplacement*)block.handle;

        public unsafe static implicit operator SouperCandidateReplacement(SouperOpaqueCandidateReplacement* block) => new SouperCandidateReplacement((nint)block);
    }
}
