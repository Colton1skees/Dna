using Dna.LLVMInterop.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public class SouperFunctionCandidateSet
    {
        private readonly nint handle;

        public IReadOnlyList<SouperBlockCandidateSet> Blocks => GetBlocks();

        public SouperFunctionCandidateSet(nint handle)
        {
            this.handle = handle;
        }

        private unsafe IReadOnlyList<SouperBlockCandidateSet> GetBlocks()
        {
            var ptr = (nint)NativeSouperFunctionCandidateSetApi.FunctionCandidateSetGetBlocks(this);

            var vec = new ManagedVector<SouperBlockCandidateSet>(ptr, x => new SouperBlockCandidateSet(x));

            return vec.Items;
        }

        public unsafe static implicit operator SouperOpaqueFunctionCandidateSet*(SouperFunctionCandidateSet block) => (SouperOpaqueFunctionCandidateSet*)block.handle;

        public unsafe static implicit operator SouperFunctionCandidateSet(SouperOpaqueFunctionCandidateSet* block) => new SouperFunctionCandidateSet((nint)block);
    }
}
