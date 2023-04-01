using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class LocationSize
    {
        public readonly nint Handle;

        public unsafe bool HasValue => NativeLocationSizeApi.GetHasValue(this);

        public unsafe ulong? Value => NativeLocationSizeApi.GetHasValue(this) ? NativeLocationSizeApi.GetValue(this) : null;

        public unsafe bool IsPrecise => NativeLocationSizeApi.GetIsPrecise(this);

        public unsafe bool IsZero => NativeLocationSizeApi.GetIsZero(this);

        public unsafe bool MayBeBeforePointer => NativeLocationSizeApi.GetMayBeBeforePointer(this);

        public unsafe ulong Raw => NativeLocationSizeApi.GetRaw(this);

        public LocationSize(nint handle)
        {
            this.Handle = handle;
        }

        // TODO: Override equality operators.
        public unsafe bool IsEqual(LocationSize other)
        {
            return NativeLocationSizeApi.GetIsEqual(this, other);
        }

        public unsafe override string ToString()
        {
            var pStr = NativeLocationSizeApi.ToString(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public unsafe static implicit operator LLVMOpaqueLocationSize*(LocationSize memAccess)
        {
            return (LLVMOpaqueLocationSize*)memAccess.Handle;
        }

        public unsafe static implicit operator LocationSize(LLVMOpaqueLocationSize* memAccess)
        {
            return new LocationSize((nint)memAccess);
        }
    }
}
