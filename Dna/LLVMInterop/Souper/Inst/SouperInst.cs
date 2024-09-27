using Dna.LLVMInterop.API;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperInst
    {
        public readonly nint handle;

        public unsafe SouperInstKind Kind => NativeSouperInstApi.InstGetKind(this);

        public unsafe uint Width => NativeSouperInstApi.InstGetWidth(this);

        public unsafe SouperBlock Block => NativeSouperInstApi.InstGetBlock(this);

        public unsafe ulong Value => NativeSouperInstApi.InstGetVal(this);

        public unsafe ulong Min => NativeSouperInstApi.InstGetRangeMin(this);

        public unsafe ulong Max => NativeSouperInstApi.InstGetRangeMax(this);

        public unsafe string Name => StringMarshaler.AcquireString(NativeSouperInstApi.InstGetName(this));

        public unsafe string Text => StringMarshaler.AcquireString(NativeSouperInstApi.InstGetString(this));

        public IReadOnlyList<SouperInst> Operands => GetOperands();

        public IReadOnlyList<LLVMValueRef> Origins => GetOrigins();

        public SouperInst(nint handle)
        {
            this.handle = handle;
        }

        private unsafe IReadOnlyList<SouperInst> GetOperands()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeSouperInstApi.InstGetOrderedOps(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<SouperInst>((nint)vecPtr,
                (nint ptr) => new SouperInst(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<LLVMValueRef> GetOrigins()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeSouperInstApi.InstGetOrigins(this);

            // Convert the ptr to a typed managed LLVMValueRef.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)vecPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public override string ToString() => Text;

        public unsafe static implicit operator SouperOpaqueInst*(SouperInst reg) => (SouperOpaqueInst*)reg?.handle;

        public unsafe static implicit operator SouperInst(SouperOpaqueInst* reg) => reg == null ? null : new SouperInst((nint)reg);

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (obj is SouperInst souperInst)
                return this.handle == souperInst.handle;
            return false;
        }

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }
    }
}
