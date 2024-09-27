using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    public static class NativeRemillRegisterApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Register_GetName(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Register_GetOffset(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Register_GetSize(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Register_GetType(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* Register_GetConstantName(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueValue>* Register_GetGepIndexList(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Register_GetGepOffset(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Register_GetGepTypeAtOffset(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Register_EnclosingRegisterOfSize(RemillOpaqueRegister* reg, ulong size);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Register_EnclosingRegister(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<RemillOpaqueRegister>* Register_EnclosedRegisters(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* Register_AddressOf(RemillOpaqueRegister* reg, LLVMOpaqueValue* statePtr, LLVMOpaqueBasicBlock* addToEnd);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* Register_AddressOfUsingBuilder(RemillOpaqueRegister* reg, LLVMOpaqueValue* statePtr, LLVMOpaqueBuilder* builder);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Register_GetParent(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueArch* Register_GetArch(RemillOpaqueRegister* reg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<RemillOpaqueRegister>* Register_GetChildren(RemillOpaqueRegister* reg);
    }
}
