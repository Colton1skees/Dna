using Dna.LLVMInterop.API.LLVMBindings;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Remill.Manual;
using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    public enum RemillOsId : uint
    {
        kOSInvalid,
        kOSmacOS,
        kOSLinux,
        kOSWindows,
        kOSSolaris,
    }

    public enum RemillArchId : uint
    {
        kArchInvalid = 0,

        kArchX86,
        kArchX86_AVX,
        kArchX86_AVX512,
        kArchX86_SLEIGH,

        kArchAMD64,
        kArchAMD64_AVX,
        kArchAMD64_AVX512,
        kArchAMD64_SLEIGH,

        kArchAArch32LittleEndian,
        kArchAArch64LittleEndian,

        kArchSparc32,
        kArchSparc64,

        kArchThumb2LittleEndian,

        kArchPPC,
    };

    public static class NativeRemillArchApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueArch* Arch_Constructor(LLVMOpaqueContext* context, RemillOsId os, RemillArchId archId);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueDecodingContext* Arch_CreateInitialContext(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueArch* Arch_GetModuleArch(LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_AddressType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_StateStructType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_StatePointerType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_MemoryPointerType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_LiftedFunctionType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* Arch_RegisterWindowType(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueIntrinsicTable* Arch_GetIntrinsicTable(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void Arch_ForEachRegister(RemillOpaqueArch* arch, nint callback);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Arch_RegisterAtStateOffset(RemillOpaqueArch* arch, ulong offset);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Arch_RegisterByName(RemillOpaqueArch* arch, sbyte* name);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Arch_StackPointerRegisterName(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Arch_ProgramCounterRegisterName(RemillOpaqueArch* arch);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* Arch_DeclareLiftedFunction(RemillOpaqueArch* arch, sbyte* name, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* Arch_DefineLiftedFunction(RemillOpaqueArch* arch, sbyte* name, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void* Arch_InitializeEmptyLiftedFunction(RemillOpaqueArch* arch, LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void* Arch_PrepareModule(RemillOpaqueArch* arch, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void* Arch_PrepareModuleDataLayout(RemillOpaqueArch* arch, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueOperandLifter* Arch_DefaultLifter(RemillOpaqueArch* arch, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Arch_DecodeInstruction(RemillOpaqueArch* arch, ulong address, byte* instrBytes, int byteCount, RemillOpaqueInstruction* inst, RemillOpaqueDecodingContext* decodingContext);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Arch_DecodeDelayedInstruction(RemillOpaqueArch* arch, ulong address, sbyte* instrBytes, RemillOpaqueInstruction* inst, RemillOpaqueDecodingContext* decodingContexth);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint Arch_DefaultCallingConv(RemillOpaqueArch* arch);
    }
}
