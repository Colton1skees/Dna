using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API
{
    public static class StringMarshaler
    {
        public static unsafe string AcquireString(sbyte* pStr)
        {
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }
    }
}
