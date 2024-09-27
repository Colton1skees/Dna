using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public class DominatorTree : DominatorTreeBase
    {
        public DominatorTree(nint handle) : base(handle)
        {
        }

        /// <summary>
        /// Return true if the (end of the) basic block BB dominates the use U.
        /// </summary>
        public unsafe bool Dominates(LLVMBasicBlockRef block, LLVMUseRef use) => NativeDominatorTreeApi.DominatorTree_BlockDominatesUse(this, block, use);

        /// <summary>
        /// Return true if value Def dominates use U, in the sense that Def is available at U, and could be substituted as the used value without violating the SSA dominance requirement.
        /// </summary>
        public unsafe bool Dominates(LLVMValueRef def, LLVMUseRef use) => NativeDominatorTreeApi.DominatorTree_DefDominatesUse(this, def, use);

        /// <summary>
        /// Return true if value Def dominates all possible uses inside instruction User.
        /// </summary>
        public unsafe bool Dominates(LLVMValueRef def, LLVMValueRef userInst) => NativeDominatorTreeApi.DominatorTree_DefDominatesUser(this, def, userInst);

        /// <summary>
        /// Returns true if Def would dominate a use in any instruction in BB.
        /// </summary>
        public unsafe bool Dominates(LLVMValueRef def, LLVMBasicBlockRef block) => NativeDominatorTreeApi.DominatorTree_InstDominatesBlock(this, def, block);

        /// <summary>
        /// Find the nearest instruction I that dominates both I1 and I2, in the sense that a result produced before I will be available at both I1 and I2.
        /// </summary>
        public unsafe LLVMValueRef FindNearestCommonDominator(LLVMValueRef inst1, LLVMValueRef inst2) => NativeDominatorTreeApi.DominatorTree_FindNearestCommonDominator(this, inst1, inst2);

        public unsafe static implicit operator LLVMOpaqueDominatorTree*(DominatorTree domTree)
        {
            return (LLVMOpaqueDominatorTree*)domTree.Handle;
        }

        public unsafe static implicit operator DominatorTree(LLVMOpaqueDominatorTree* domTree)
        {
            return new DominatorTree((nint)domTree);
        }
    }
}
