using Dna.LLVMInterop.API.LLVMBindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API
{
    public static class NativeManagedPairApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImmutableManagedPair_GetFirst")]
        public unsafe static extern nint GetFirst(OpaqueManagedPair* managedPair);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImmutableManagedPair_GetSecond")]
        public unsafe static extern nint GetSecond(OpaqueManagedPair* managedPair);
    }
}
