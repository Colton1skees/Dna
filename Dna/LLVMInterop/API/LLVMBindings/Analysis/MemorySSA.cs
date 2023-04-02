using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemorySSA
    {
        public readonly nint Handle;

        public unsafe MemorySSAWalker Walker => NativeMemorySSAApi.GetWalker(this);

        public unsafe MemorySSAWalker SkipWalker => NativeMemorySSAApi.GetSkipSelfWalker(this);

        public MemorySSA(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe MemoryUseOrDef GetMemoryAccess(LLVMValueRef instruction) => NativeMemorySSAApi.GetMemoryAccess(this, instruction);

        public unsafe MemoryPhi GetMemoryAccessFromBlock(LLVMBasicBlockRef instruction) => NativeMemorySSAApi.GetMemoryAccessFromBlock(this, instruction);

        public unsafe bool IsLiveOnEntryDef(MemoryAccess memAccess) => NativeMemorySSAApi.IsLiveOnEntryDef(this, memAccess);

        public unsafe bool GetLiveOnEntryDef() => NativeMemorySSAApi.GetLiveOnEntryDef(this);

        public unsafe IReadOnlyList<MemoryAccess> GetBlockAccesses(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeMemorySSAApi.GetBlockAccesses(this, block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<MemoryAccess>((nint)vecPtr,
                (nint ptr) => MemoryAccess.From(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe IReadOnlyList<MemoryAccess> GetBlockDefs(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeMemorySSAApi.GetBlockDefs(this, block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<MemoryAccess>((nint)vecPtr,
                (nint ptr) => MemoryAccess.From(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe bool LocallyDominates(MemoryAccess a, MemoryAccess b) => NativeMemorySSAApi.LocallyDominates(this, a, b);

        public unsafe bool Dominates(MemoryAccess a, MemoryAccess b) => NativeMemorySSAApi.Dominates(this, a, b);

        public unsafe bool Dominates(MemoryAccess a, LLVMUseRef b) => NativeMemorySSAApi.Dominates(this, a, b);

        public unsafe static implicit operator LLVMOpaqueMemorySSA*(MemorySSA memAccess)
        {
            return (LLVMOpaqueMemorySSA*)memAccess.Handle;
        }

        public unsafe static implicit operator MemorySSA(LLVMOpaqueMemorySSA* memAccess)
        {
            return new MemorySSA((nint)memAccess);
        }
    }
}
