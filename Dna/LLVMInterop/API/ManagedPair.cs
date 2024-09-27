using Dna.LLVMInterop.API.Remill.BC;
using Dna.LLVMInterop.API.Remill.Manual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API
{
    public class ManagedPair<T1, T2>
    {
        public readonly nint Handle;

        public unsafe T1* First => (T1*)NativeManagedPairApi.GetFirst(this);

        public unsafe T2* Second => (T2*)NativeManagedPairApi.GetSecond(this);

        public unsafe ManagedPair(OpaqueManagedPair* handle)
        {
            this.Handle = (nint)handle;
        }

        public ManagedPair(nint handle)
        {
            this.Handle = handle;
        }

        public unsafe static implicit operator OpaqueManagedPair*(ManagedPair<T1, T2> reg) => (OpaqueManagedPair*)reg.Handle;

        public unsafe static implicit operator ManagedPair<T1, T2>(OpaqueManagedPair* reg) => new ManagedPair<T1, T2>((nint)reg);
    }
}
