using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public unsafe class DominatorTreeBase
    {
        public readonly nint Handle;

        public DominatorTreeBase(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe IReadOnlyList<LLVMBasicBlockRef> GetDescendants(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr.
            var vecPtr = NativeDominatorTreeBaseApi.DominatorTreeBase_GetDescendants(this, block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        /// <summary>
        /// Returns true iff A dominates B and A != B.
        /// </summary>
        public bool ProperlyDominates(LLVMBasicBlockRef a, LLVMBasicBlockRef b) => NativeDominatorTreeBaseApi.DominatorTreeBase_ProperlyDominates(this, a, b);

        /// <summary>
        /// Return true if A is dominated by the entry block of the function containing it.
        /// </summary>
        public bool IsReachableFromEntry(LLVMBasicBlockRef block) => NativeDominatorTreeBaseApi.DominatorTreeBase_IsReachableFromEntry(this, block);

        /// <summary>
        /// Returns true iff A dominates B.
        /// </summary>
        public bool Dominates(LLVMBasicBlockRef a, LLVMBasicBlockRef b) => NativeDominatorTreeBaseApi.DominatorTreeBase_Dominates(this, a, b);

        /// <summary>
        /// Find nearest common dominator basic block for basic block A and B.
        /// </summary>
        public LLVMBasicBlockRef FindNearestCommonDenominator(LLVMBasicBlockRef a, LLVMBasicBlockRef b) => NativeDominatorTreeBaseApi.DominatorTreeBase_FindNearestCommonDenominator(this, a, b);

        public unsafe static implicit operator LLVMOpaqueDominatorTreeBase*(DominatorTreeBase domTreeBase)
        {
            return (LLVMOpaqueDominatorTreeBase*)domTreeBase.Handle;
        }

        public unsafe static implicit operator DominatorTreeBase(LLVMOpaqueDominatorTreeBase* ptr)
        {
            return new DominatorTreeBase((nint)ptr);
        }
    }
}
