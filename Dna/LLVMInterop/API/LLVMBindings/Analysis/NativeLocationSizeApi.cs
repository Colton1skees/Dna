using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public static class NativeLocationSizeApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetHasValue")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool GetHasValue(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetValue")]
        public unsafe static extern ulong GetValue(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetIsPrecise")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool GetIsPrecise(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetIsZero")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool GetIsZero(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetMayBeBeforePointer")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool GetMayBeBeforePointer(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetIsEqual")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool GetIsEqual(LLVMOpaqueLocationSize* locationSize, LLVMOpaqueLocationSize* other);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_GetRaw")]
        public unsafe static extern ulong GetRaw(LLVMOpaqueLocationSize* locationSize);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LocationSize_ToString")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern sbyte* ToString(LLVMOpaqueLocationSize* locationSize);

    }
}
