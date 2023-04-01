using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class MemoryPhi : MemoryAccess
    {
        public IReadOnlyList<LLVMBasicBlockRef> Blocks => GetBlocks();

        public IReadOnlyList<LLVMUseRef> IncomingValues => GetIncomingValues();

        public unsafe uint IncomingValueCount => NativeMemoryPhiApi.GetNumIncomingValues(this);

        public MemoryPhi(nint handle) : base(handle)
        {

        }

        private unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlocks()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeMemoryPhiApi.GetBlocks(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<LLVMUseRef> GetIncomingValues()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeMemoryPhiApi.GetIncomingValues(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMUseRef>((nint)vecPtr,
                (nint ptr) => new LLVMUseRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe MemoryAccess GetIncomingValue(uint index)
        {
            return NativeMemoryPhiApi.GetIncomingValue(this, index);
        }

        public unsafe MemoryAccess GetIncomingValue(LLVMBasicBlockRef block)
        {
            return NativeMemoryPhiApi.GetIncomingValueForBlock(this, block);
        }

        public unsafe static implicit operator LLVMOpaqueMemoryPhi*(MemoryPhi memAccess)
        {
            return (LLVMOpaqueMemoryPhi*)memAccess.Handle;
        }

        public unsafe static implicit operator MemoryPhi(LLVMOpaqueMemoryPhi* memAccess)
        {
            return new MemoryPhi((nint)memAccess);
        }
    }
}
