using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils
{
    public static class LLVMCloning
    {
        public unsafe static LLVMTypeRef GetFunctionPrototype(LLVMValueRef function)
        {
            return NativeCloningApi.GetFunctionType(function);
        }

        public static unsafe void InlineFunction(LLVMValueRef callInst)
        {
            var ptr = NativeCloningApi.InlineFunction(callInst);
            var errMsg = ptr == null ? null : StringMarshaler.AcquireString(ptr);
            if (errMsg != null)
                throw new InvalidOperationException($"Failed to inline {callInst}. Error: {errMsg}");
        }

        public static unsafe void AddParamAttr(LLVMValueRef function, uint paramIndex, AttrKind attrKind)
        {
            NativeCloningApi.AddParamAttr(function, paramIndex, attrKind);
        }

        public static unsafe void MakeMustTail(LLVMValueRef callInst)
        {
            NativeCloningApi.MakeMustTail(callInst);
        }

        public static unsafe void MakeDsoLocal(LLVMValueRef function, bool dsoLocal)
        {
            NativeCloningApi.MakeDsoLocal(function, dsoLocal);
        }

        public static unsafe void PrepareForCloning(LLVMValueRef function, bool jumpThreading)
        {
            NativeCloningApi.PrepareForCloning(function, jumpThreading);
        }

        public static unsafe LLVMBasicBlockRef CloneBasicBlock(LLVMBasicBlockRef block)
        {
            return NativeCloningApi.CloneBasicBlock(block);
        }

        public static unsafe bool MergeBlockIntoPredecessor(LLVMBasicBlockRef block)
        {
            return NativeCloningApi.MergeBlockIntoPredecessor(block);
        }
    }
}
