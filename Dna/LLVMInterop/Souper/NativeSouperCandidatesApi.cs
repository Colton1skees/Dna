using Dna.LLVMInterop.API;
using Dna.LLVMInterop.Souper.Candidate;
using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper
{
    public static class NativeSouperCandidateReplacementApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueCandidateReplacement* CandidateReplacementConstructor(LLVMOpaqueValue* origin, SouperOpaqueInstMapping* instMapping);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* CandidateReplacementGetOrigin(SouperOpaqueCandidateReplacement* cand);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInstMapping* CandidateReplacementGetInstMapping(SouperOpaqueCandidateReplacement* cand);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueInstMapping>* CandidateReplacementGetPathConditions(SouperOpaqueCandidateReplacement* cand);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueBlockPCMapping>* CandidateReplacementGetBlockPathConditions(SouperOpaqueCandidateReplacement* cand);
    }

    public static class NativeSouperBlockCandidateSetApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueInstMapping>* BlockCandidateSetGetPCs(SouperOpaqueBlockCandidateSet* cand);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueBlockPCMapping>* BlockCandidateSetGetBPCs(SouperOpaqueBlockCandidateSet* cand);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueCandidateReplacement>* BlockCandidateSetGetReplacements(SouperOpaqueBlockCandidateSet* cand);
    }

    public static class NativeSouperFunctionCandidateSetApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueBlockCandidateSet>* FunctionCandidateSetGetBlocks(SouperOpaqueFunctionCandidateSet* fcs);
    }

    public static class NativeSouperExprBuilderOptionsApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueExprBuilderOptions* ExprBuilderOptionsConstructor(bool namedArrays, LLVMOpaqueValue* candidateFilterInstruction);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* ExprBuilderOptionsGetCandidateFilterInstruction(SouperOpaqueExprBuilderOptions* options);
    }

    public static class NativeSouperExprBuilderContextApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueExprBuilderContext* ExprBuilderContextConstructor();
    }

    public static class NativeSouperCandidateExtractorApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueFunctionCandidateSet* SouperExtractCandidates(LLVMOpaqueValue* function, SouperOpaqueInstContext* instCtx, SouperOpaqueExprBuilderContext* ebc, SouperOpaqueExprBuilderOptions* ebo);
    }
}
