using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public static class NativeDominatorTreeBaseApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* DominatorTreeBase_GetDescendants(LLVMOpaqueDominatorTreeBase* treeBase, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTreeBase_ProperlyDominates(LLVMOpaqueDominatorTreeBase* treeBase, LLVMOpaqueBasicBlock* blockA, LLVMOpaqueBasicBlock* blockB);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTreeBase_IsReachableFromEntry(LLVMOpaqueDominatorTreeBase* treeBase, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTreeBase_Dominates(LLVMOpaqueDominatorTreeBase* treeBase, LLVMOpaqueBasicBlock* blockA, LLVMOpaqueBasicBlock* blockB);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueBasicBlock* DominatorTreeBase_FindNearestCommonDenominator(LLVMOpaqueDominatorTreeBase* treeBase, LLVMOpaqueBasicBlock* blockA, LLVMOpaqueBasicBlock* blockB);
    }

    public static class NativeDominatorTreeApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTree_BlockDominatesUse(LLVMOpaqueDominatorTree* treeBase, LLVMOpaqueBasicBlock* block, LLVMOpaqueUse* use);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTree_DefDominatesUse(LLVMOpaqueDominatorTree* treeBase, LLVMOpaqueValue* def, LLVMOpaqueUse* use);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTree_DefDominatesUser(LLVMOpaqueDominatorTree* treeBase, LLVMOpaqueValue* def, LLVMOpaqueValue* userInst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern bool DominatorTree_InstDominatesBlock(LLVMOpaqueDominatorTree* treeBase, LLVMOpaqueValue* def, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* DominatorTree_FindNearestCommonDominator(LLVMOpaqueDominatorTree* treeBase, LLVMOpaqueValue* inst1, LLVMOpaqueValue* inst2);
    }
}
