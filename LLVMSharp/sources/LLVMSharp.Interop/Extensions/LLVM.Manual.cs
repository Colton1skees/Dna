// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from https://github.com/llvm/llvm-project/tree/llvmorg-18.1.3/llvm/include/llvm-c
// Original source is Copyright (c) the LLVM Project and Contributors. Licensed under the Apache License v2.0 with LLVM Exceptions. See NOTICE.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace LLVMSharp.Interop;

public static unsafe partial class LLVM
{
    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64TargetInfo", ExactSpelling = true)]
    public static extern void InitializeAArch64TargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUTargetInfo", ExactSpelling = true)]
    public static extern void InitializeAMDGPUTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMTargetInfo", ExactSpelling = true)]
    public static extern void InitializeARMTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRTargetInfo", ExactSpelling = true)]
    public static extern void InitializeAVRTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFTargetInfo", ExactSpelling = true)]
    public static extern void InitializeBPFTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonTargetInfo", ExactSpelling = true)]
    public static extern void InitializeHexagonTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiTargetInfo", ExactSpelling = true)]
    public static extern void InitializeLanaiTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchTargetInfo", ExactSpelling = true)]
    public static extern void InitializeLoongArchTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsTargetInfo", ExactSpelling = true)]
    public static extern void InitializeMipsTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430TargetInfo", ExactSpelling = true)]
    public static extern void InitializeMSP430TargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeNVPTXTargetInfo", ExactSpelling = true)]
    public static extern void InitializeNVPTXTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCTargetInfo", ExactSpelling = true)]
    public static extern void InitializePowerPCTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVTargetInfo", ExactSpelling = true)]
    public static extern void InitializeRISCVTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcTargetInfo", ExactSpelling = true)]
    public static extern void InitializeSparcTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZTargetInfo", ExactSpelling = true)]
    public static extern void InitializeSystemZTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVETargetInfo", ExactSpelling = true)]
    public static extern void InitializeVETargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyTargetInfo", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86TargetInfo", ExactSpelling = true)]
    public static extern void InitializeX86TargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeXCoreTargetInfo", ExactSpelling = true)]
    public static extern void InitializeXCoreTargetInfo();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64Target", ExactSpelling = true)]
    public static extern void InitializeAArch64Target();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUTarget", ExactSpelling = true)]
    public static extern void InitializeAMDGPUTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMTarget", ExactSpelling = true)]
    public static extern void InitializeARMTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRTarget", ExactSpelling = true)]
    public static extern void InitializeAVRTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFTarget", ExactSpelling = true)]
    public static extern void InitializeBPFTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonTarget", ExactSpelling = true)]
    public static extern void InitializeHexagonTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiTarget", ExactSpelling = true)]
    public static extern void InitializeLanaiTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchTarget", ExactSpelling = true)]
    public static extern void InitializeLoongArchTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsTarget", ExactSpelling = true)]
    public static extern void InitializeMipsTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430Target", ExactSpelling = true)]
    public static extern void InitializeMSP430Target();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeNVPTXTarget", ExactSpelling = true)]
    public static extern void InitializeNVPTXTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCTarget", ExactSpelling = true)]
    public static extern void InitializePowerPCTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVTarget", ExactSpelling = true)]
    public static extern void InitializeRISCVTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcTarget", ExactSpelling = true)]
    public static extern void InitializeSparcTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZTarget", ExactSpelling = true)]
    public static extern void InitializeSystemZTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVETarget", ExactSpelling = true)]
    public static extern void InitializeVETarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyTarget", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86Target", ExactSpelling = true)]
    public static extern void InitializeX86Target();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeXCoreTarget", ExactSpelling = true)]
    public static extern void InitializeXCoreTarget();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64TargetMC", ExactSpelling = true)]
    public static extern void InitializeAArch64TargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUTargetMC", ExactSpelling = true)]
    public static extern void InitializeAMDGPUTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMTargetMC", ExactSpelling = true)]
    public static extern void InitializeARMTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRTargetMC", ExactSpelling = true)]
    public static extern void InitializeAVRTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFTargetMC", ExactSpelling = true)]
    public static extern void InitializeBPFTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonTargetMC", ExactSpelling = true)]
    public static extern void InitializeHexagonTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiTargetMC", ExactSpelling = true)]
    public static extern void InitializeLanaiTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchTargetMC", ExactSpelling = true)]
    public static extern void InitializeLoongArchTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsTargetMC", ExactSpelling = true)]
    public static extern void InitializeMipsTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430TargetMC", ExactSpelling = true)]
    public static extern void InitializeMSP430TargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeNVPTXTargetMC", ExactSpelling = true)]
    public static extern void InitializeNVPTXTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCTargetMC", ExactSpelling = true)]
    public static extern void InitializePowerPCTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVTargetMC", ExactSpelling = true)]
    public static extern void InitializeRISCVTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcTargetMC", ExactSpelling = true)]
    public static extern void InitializeSparcTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZTargetMC", ExactSpelling = true)]
    public static extern void InitializeSystemZTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVETargetMC", ExactSpelling = true)]
    public static extern void InitializeVETargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyTargetMC", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86TargetMC", ExactSpelling = true)]
    public static extern void InitializeX86TargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeXCoreTargetMC", ExactSpelling = true)]
    public static extern void InitializeXCoreTargetMC();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64AsmPrinter", ExactSpelling = true)]
    public static extern void InitializeAArch64AsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeAMDGPUAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeARMAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeAVRAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeBPFAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeHexagonAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeLanaiAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeLoongArchAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeMipsAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430AsmPrinter", ExactSpelling = true)]
    public static extern void InitializeMSP430AsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeNVPTXAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeNVPTXAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCAsmPrinter", ExactSpelling = true)]
    public static extern void InitializePowerPCAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeRISCVAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeSparcAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeSystemZAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVEAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeVEAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86AsmPrinter", ExactSpelling = true)]
    public static extern void InitializeX86AsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeXCoreAsmPrinter", ExactSpelling = true)]
    public static extern void InitializeXCoreAsmPrinter();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64AsmParser", ExactSpelling = true)]
    public static extern void InitializeAArch64AsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUAsmParser", ExactSpelling = true)]
    public static extern void InitializeAMDGPUAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMAsmParser", ExactSpelling = true)]
    public static extern void InitializeARMAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRAsmParser", ExactSpelling = true)]
    public static extern void InitializeAVRAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFAsmParser", ExactSpelling = true)]
    public static extern void InitializeBPFAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonAsmParser", ExactSpelling = true)]
    public static extern void InitializeHexagonAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiAsmParser", ExactSpelling = true)]
    public static extern void InitializeLanaiAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchAsmParser", ExactSpelling = true)]
    public static extern void InitializeLoongArchAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsAsmParser", ExactSpelling = true)]
    public static extern void InitializeMipsAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430AsmParser", ExactSpelling = true)]
    public static extern void InitializeMSP430AsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCAsmParser", ExactSpelling = true)]
    public static extern void InitializePowerPCAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVAsmParser", ExactSpelling = true)]
    public static extern void InitializeRISCVAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcAsmParser", ExactSpelling = true)]
    public static extern void InitializeSparcAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZAsmParser", ExactSpelling = true)]
    public static extern void InitializeSystemZAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVEAsmParser", ExactSpelling = true)]
    public static extern void InitializeVEAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyAsmParser", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyAsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86AsmParser", ExactSpelling = true)]
    public static extern void InitializeX86AsmParser();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAArch64Disassembler", ExactSpelling = true)]
    public static extern void InitializeAArch64Disassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAMDGPUDisassembler", ExactSpelling = true)]
    public static extern void InitializeAMDGPUDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeARMDisassembler", ExactSpelling = true)]
    public static extern void InitializeARMDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeAVRDisassembler", ExactSpelling = true)]
    public static extern void InitializeAVRDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeBPFDisassembler", ExactSpelling = true)]
    public static extern void InitializeBPFDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeHexagonDisassembler", ExactSpelling = true)]
    public static extern void InitializeHexagonDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLanaiDisassembler", ExactSpelling = true)]
    public static extern void InitializeLanaiDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeLoongArchDisassembler", ExactSpelling = true)]
    public static extern void InitializeLoongArchDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMipsDisassembler", ExactSpelling = true)]
    public static extern void InitializeMipsDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeMSP430Disassembler", ExactSpelling = true)]
    public static extern void InitializeMSP430Disassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializePowerPCDisassembler", ExactSpelling = true)]
    public static extern void InitializePowerPCDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeRISCVDisassembler", ExactSpelling = true)]
    public static extern void InitializeRISCVDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSparcDisassembler", ExactSpelling = true)]
    public static extern void InitializeSparcDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeSystemZDisassembler", ExactSpelling = true)]
    public static extern void InitializeSystemZDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeVEDisassembler", ExactSpelling = true)]
    public static extern void InitializeVEDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeWebAssemblyDisassembler", ExactSpelling = true)]
    public static extern void InitializeWebAssemblyDisassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeX86Disassembler", ExactSpelling = true)]
    public static extern void InitializeX86Disassembler();

    [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FfiLLVMInitializeXCoreDisassembler", ExactSpelling = true)]
    public static extern void InitializeXCoreDisassembler();

    [return: NativeTypeName("LLVMBool")]
    public static int InitializeNativeTarget()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            {
                InitializeX86TargetInfo();
                InitializeX86Target();
                InitializeX86TargetMC();
                return 0;
            }

            case Architecture.Arm:
            case Architecture.Armv6:
            {
                InitializeARMTargetInfo();
                InitializeARMTarget();
                InitializeARMTargetMC();
                return 0;
            }

            case Architecture.Arm64:
            {
                InitializeAArch64TargetInfo();
                InitializeAArch64Target();
                InitializeAArch64TargetMC();
                return 0;
            }

            case Architecture.Wasm:
            {
                InitializeWebAssemblyTargetInfo();
                InitializeWebAssemblyTarget();
                InitializeWebAssemblyTargetMC();
                return 0;
            }

            case Architecture.S390x:
            {
                InitializeSystemZTargetInfo();
                InitializeSystemZTarget();
                InitializeSystemZTargetMC();
                return 0;
            }

            case Architecture.LoongArch64:
            {
                InitializeLoongArchTargetInfo();
                InitializeLoongArchTarget();
                InitializeLoongArchTargetMC();
                return 0;
            }

            case Architecture.Ppc64le:
            {
                InitializePowerPCTargetInfo();
                InitializePowerPCTarget();
                InitializePowerPCTargetMC();
                return 0;
            }


            default:
            {
                return 1;
            }
        }
    }

    [return: NativeTypeName("LLVMBool")]
    public static int InitializeNativeAsmParser()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            {
                InitializeX86AsmParser();
                return 0;
            }

            case Architecture.Arm:
            case Architecture.Armv6:
            {
                InitializeARMAsmParser();
                return 0;
            }

            case Architecture.Arm64:
            {
                InitializeAArch64AsmParser();
                return 0;
            }

            case Architecture.Wasm:
            {
                InitializeWebAssemblyAsmParser();
                return 0;
            }

            case Architecture.S390x:
            {
                InitializeSystemZAsmParser();
                return 0;
            }

            case Architecture.LoongArch64:
            {
                InitializeLoongArchAsmParser();
                return 0;
            }

            case Architecture.Ppc64le:
            {
                InitializePowerPCAsmParser();
                return 0;
            }


            default:
            {
                return 1;
            }
        }
    }

    [return: NativeTypeName("LLVMBool")]
    public static int InitializeNativeAsmPrinter()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            {
                InitializeX86AsmPrinter();
                return 0;
            }

            case Architecture.Arm:
            case Architecture.Armv6:
            {
                InitializeARMAsmPrinter();
                return 0;
            }

            case Architecture.Arm64:
            {
                InitializeAArch64AsmPrinter();
                return 0;
            }

            case Architecture.Wasm:
            {
                InitializeWebAssemblyAsmPrinter();
                return 0;
            }

            case Architecture.S390x:
            {
                InitializeSystemZAsmPrinter();
                return 0;
            }

            case Architecture.LoongArch64:
            {
                InitializeLoongArchAsmPrinter();
                return 0;
            }

            case Architecture.Ppc64le:
            {
                InitializePowerPCAsmPrinter();
                return 0;
            }


            default:
            {
                return 1;
            }
        }
    }

    [return: NativeTypeName("LLVMBool")]
    public static int InitializeNativeDisassembler()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            {
                InitializeX86Disassembler();
                return 0;
            }

            case Architecture.Arm:
            case Architecture.Armv6:
            {
                InitializeARMDisassembler();
                return 0;
            }

            case Architecture.Arm64:
            {
                InitializeAArch64Disassembler();
                return 0;
            }

            case Architecture.Wasm:
            {
                InitializeWebAssemblyDisassembler();
                return 0;
            }

            case Architecture.S390x:
            {
                InitializeSystemZDisassembler();
                return 0;
            }

            case Architecture.LoongArch64:
            {
                InitializeLoongArchDisassembler();
                return 0;
            }

            case Architecture.Ppc64le:
            {
                InitializePowerPCDisassembler();
                return 0;
            }


            default:
            {
                return 1;
            }
        }
    }
}
