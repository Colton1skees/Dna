using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public class SouperExprBuilderOptions
    {
        private readonly nint handle;

        // TODO: Because of how nullability works with structs, this won't actually return null if the candidate filter instruction is null.
        // CandidateFilterInstruction tells souper to essentially only slice this exact instruction.
        public unsafe LLVMValueRef? CandidateFilterInstruction => NativeSouperExprBuilderOptionsApi.ExprBuilderOptionsGetCandidateFilterInstruction(this);

        public unsafe SouperExprBuilderOptions(bool namedArrays, LLVMValueRef candidateFilterInstruction)
        {
            handle = (nint)NativeSouperExprBuilderOptionsApi.ExprBuilderOptionsConstructor(namedArrays, candidateFilterInstruction);
        }

        public SouperExprBuilderOptions(nint handle)
        {
            this.handle = handle;
        }

        public unsafe static implicit operator SouperOpaqueExprBuilderOptions*(SouperExprBuilderOptions block) => (SouperOpaqueExprBuilderOptions*)block.handle;

        public unsafe static implicit operator SouperExprBuilderOptions(SouperOpaqueExprBuilderOptions* block) => new SouperExprBuilderOptions((nint)block);
    }
}
