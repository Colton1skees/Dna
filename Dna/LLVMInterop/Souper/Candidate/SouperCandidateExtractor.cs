using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Candidate
{
    public static class SouperCandidateExtractor
    {
        public static unsafe SouperFunctionCandidateSet ExtractCandidates(LLVMValueRef function, SouperInstContext instCtx, SouperExprBuilderContext exprBuilder, SouperExprBuilderOptions options)
            => NativeSouperCandidateExtractorApi.SouperExtractCandidates(function, instCtx, exprBuilder, options);
    }
}
