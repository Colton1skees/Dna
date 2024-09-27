using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public enum RemillLiftStatus
    {
        kLiftedInvalidInstruction,
        kLiftedUnsupportedInstruction,
        kLiftedLifterError,
        kLiftedUnknownISEL,
        kLiftedMismatchedISEL,
        kLiftedInstruction
    };

    public static class NativeRemillLifterApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* OperandLifter_LoadRegAddress(RemillOpaqueOperandLifter* lifter, LLVMOpaqueBasicBlock* block,
            LLVMOpaqueValue* statePtr, sbyte* regName);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* OperandLifter_LoadRegValue(RemillOpaqueOperandLifter* lifter, LLVMOpaqueBasicBlock* block,
            LLVMOpaqueValue* statePtr, sbyte* regName);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* OperandLifter_GetMemoryType(RemillOpaqueOperandLifter* lifter);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void OperandLifter_ClearCache(RemillOpaqueOperandLifter* lifter);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillLiftStatus InstructionLifterIntf_LiftIntoBlockWithStatePtr(RemillOpaqueOperandLifter* lifter, RemillOpaqueInstruction* inst,
            LLVMOpaqueBasicBlock* block, LLVMOpaqueValue* statePtr, bool isDelay);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillLiftStatus InstructionLifterIntf_LiftIntoBlock(RemillOpaqueOperandLifter* lifter, RemillOpaqueInstruction* inst,
            LLVMOpaqueBasicBlock* block, bool isDelay);
    }
}
