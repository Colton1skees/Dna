using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.Manual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public class SouperInstMapping
    {
        public readonly nint handle;

        public unsafe SouperInst Lhs
        {
            get => NativeSouperInstMappingApi.InstMappingGetLhs(this);
            set => NativeSouperInstMappingApi.InstMappingSetLhs(this, value);
        }

        public unsafe SouperInst Rhs
        {
            get => NativeSouperInstMappingApi.InstMappingGetRhs(this);
            set => NativeSouperInstMappingApi.InstMappingSetRhs(this, value);
        }

        public unsafe SouperInstMapping(SouperInst lhs, SouperInst rhs)
        {
            handle = (nint)NativeSouperInstMappingApi.InstMappingConstructor(lhs, rhs);
        }

        public SouperInstMapping(nint handle)
        {
            this.handle = handle;
        }

        public unsafe static implicit operator SouperOpaqueInstMapping*(SouperInstMapping reg) => (SouperOpaqueInstMapping*)reg.handle;

        public unsafe static implicit operator SouperInstMapping(SouperOpaqueInstMapping* reg) => new SouperInstMapping((nint)reg);
    }
}
