using Dna.LLVMInterop.API;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperInstContext
    {
        private readonly nint handle;

        public SouperInstContext(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperInstContext()
        {
            this.handle = (nint)NativeSouperInstContextApi.InstContextConstructor();
        }

        public unsafe SouperInst GetConst(uint width, ulong value) => NativeSouperInstContextApi.InstContextGetConst(this, width, value);

        public unsafe SouperInst CreateVar(uint width, string name) => NativeSouperInstContextApi.InstContextCreateVar(this, width, new MarshaledString(name));

        public unsafe SouperInst GetInst(SouperInstKind kind, uint width, SouperInst[] operands, bool available = true)
        {
            // The lack of type safety here is definitely not ideal because the type system does not support generic pointer arguments.
            // Maybe TODO: Fix.
            var casted = operands.Select(x => x.handle).ToArray();
            var ops = ManagedVector<nint>.From(casted, x => x);
            return NativeSouperInstContextApi.InstContextGetInst(this, kind, width, (OpaqueManagedVector<SouperOpaqueInst>*)ops.Handle, available);
        }

        public unsafe SouperInst GetInst(SouperInstKind kind, uint width, SouperInst op1, bool available = true)
        {
            return NativeSouperInstContextApi.InstContextGetInstWithSingleArg(this, kind, width, op1, available);
        }

        public unsafe SouperInst GetInst(SouperInstKind kind, uint width, SouperInst op1, SouperInst op2, bool available = true)
        {
            return NativeSouperInstContextApi.InstContextGetInstWithDoubleArg(this, kind, width, op1, op2, available);
        }

        public unsafe static implicit operator SouperOpaqueInstContext*(SouperInstContext pcMapping) => (SouperOpaqueInstContext*)pcMapping.handle;

        public unsafe static implicit operator SouperInstContext(SouperOpaqueInstContext* pcMapping) => new SouperInstContext((nint)pcMapping);
    }
}
