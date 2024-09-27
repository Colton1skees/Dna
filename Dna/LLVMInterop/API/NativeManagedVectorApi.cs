using Dna.LLVMInterop.API.LLVMBindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API
{
    public static class NativeManagedVectorApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImmutableManagedVector_FromManagedArray")]
        public unsafe static extern nint FromManagedArray(nint inputArray, int count);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImmutableManagedVector_GetCount")]
        public unsafe static extern int GetCount(nint managedVector);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImmutableManagedVector_GetElementAt")]
        public unsafe static extern nint GetElementAt(nint managedVector, int index);
    }
}
