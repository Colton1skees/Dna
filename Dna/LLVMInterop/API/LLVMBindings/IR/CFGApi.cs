using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public static class CFGApi
    {
        public static unsafe uint GetBlockPredessorsCount(LLVMBasicBlockRef block) => NativeCFGApi.BasicBlock_GetPredSize(block);

        public static unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlockPredecessors(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeCFGApi.BasicBlock_GetPredecessors(block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe uint GetBlockSuccessorsCount(LLVMBasicBlockRef block) => NativeCFGApi.BasicBlock_GetSuccSize(block);

        public static unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlockSuccessors(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeCFGApi.BasicBlock_GetSuccessors(block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe IReadOnlyList<LLVMValueRef> GetValueUsers(LLVMValueRef value)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeCFGApi.Value_GetUsers(value);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)vecPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }
    }
}
