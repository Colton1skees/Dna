using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativeLLVMInterop;

namespace Dna.LLVMInterop.Passes
{
    public enum AliasResult : byte
    {
        /// The two locations do not alias at all.
        ///
        /// This value is arranged to convert to false, while all other values
        /// convert to true. This allows a boolean context to convert the result to
        /// a binary flag indicating whether there is the possibility of aliasing.
        NoAlias = 0,
        /// The two locations may or may not alias. This is the least precise
        /// result.
        MayAlias,
        /// The two locations alias, but only due to a partial overlap.
        PartialAlias,
        /// The two locations precisely alias each other.
        MustAlias,
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate AliasResult dgGetAliasResult(LLVMOpaqueValue* ptrA, LLVMOpaqueValue* ptrB);

    public static unsafe class ClassifyingAliasAnalysisPass
    {
        /// <summary>
        /// Unmanaged pointer to the `GetAliasKind` method, which is invoked by native code.
        /// </summary>
        public static nint PtrGetAliasResult { get;} = Marshal.GetFunctionPointerForDelegate(new dgGetAliasResult(GetAliasResult));

        public static unsafe AliasResult GetAliasResult(LLVMOpaqueValue* opaquePtrA, LLVMOpaqueValue* opaquePtrB)
        {
            return GetAliasResult(opaquePtrA, opaquePtrB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe AliasResult GetAliasResult(LLVMValueRef ptrA, LLVMValueRef ptrB)
        {
            return (AliasResult)byte.MaxValue;
        }
    }
}
