using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    // Misc LLVM wrappers 
    public static class LLVMUtil
    {
        public static unsafe IReadOnlyList<LLVMValueRef> GetRpoInstructions(LLVMValueRef func)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = LLVMUtilApi.Function_GetRpoInstructions(func);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)vecPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe uint GetBlockPredessorsCount(LLVMBasicBlockRef block) => LLVMUtilApi.BasicBlock_GetPredSize(block);

        public static unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlockPredecessors(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = LLVMUtilApi.BasicBlock_GetPredecessors(block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe uint GetBlockSuccessorsCount(LLVMBasicBlockRef block) => LLVMUtilApi.BasicBlock_GetSuccSize(block);

        public static unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlockSuccessors(LLVMBasicBlockRef block)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = LLVMUtilApi.BasicBlock_GetSuccessors(block);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe IReadOnlyList<LLVMValueRef> GetValueUsers(LLVMValueRef value)
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = LLVMUtilApi.Value_GetUsers(value);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)vecPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public static unsafe LLVMBasicBlockRef SplitBlockAt(LLVMBasicBlockRef block, LLVMValueRef inst, string name, bool before = false)
        {
            return LLVMUtilApi.SplitBasicBlockAt(block, inst, new MarshaledString(name), before);
        }

        public static unsafe void MakeFunctionDsoLocal(LLVMValueRef function, bool dsoLocal)
        {
            LLVMUtilApi.MakeDsoLocal(function, dsoLocal);
        }

        public static unsafe void AddNoSideEffectFunctionAttrs(LLVMValueRef function)
        {
            LLVMUtilApi.AddNoSideEffectAttributes(function);
        }

        public static unsafe void MakeParamNoAlias(LLVMValueRef arg)
        {
            LLVMUtilApi.MakeArgNoAlias(arg);
        }
    }
}
