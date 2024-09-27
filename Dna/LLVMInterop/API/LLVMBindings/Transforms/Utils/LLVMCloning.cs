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
    }
}
