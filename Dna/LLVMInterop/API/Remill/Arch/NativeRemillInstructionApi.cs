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
    public enum RemillInstructionCategoryId
    {
        kCategoryInvalid,
        kCategoryNormal,
        kCategoryNoOp,
        kCategoryError,
        kCategoryDirectJump,
        kCategoryIndirectJump,
        kCategoryConditionalIndirectJump,
        kCategoryDirectFunctionCall,
        kCategoryConditionalDirectFunctionCall,
        kCategoryIndirectFunctionCall,
        kCategoryConditionalIndirectFunctionCall,
        kCategoryFunctionReturn,
        kCategoryConditionalFunctionReturn,
        kCategoryConditionalBranch,
        kCategoryAsyncHyperCall,
        kCategoryConditionalAsyncHyperCall,
    }
    
    public static class NativeRemillInstructionApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueInstruction* Instruction_Constructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void Instruction_Reset(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Instruction_GetFunction(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Instruction_GetBytes(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_GetPc(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_GetNextPc(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_GetDelayedPc(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_GetBranchTakenPc(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_GetBranchNotTakenPc(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillArchId Instruction_GetArchName(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillArchId Instruction_GetSubArchName(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillArchId Instruction_GetBranchTakenArchName(RemillOpaqueInstruction* inst, bool* exits);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueArch* Instruction_GetArch(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_GetIsAtomicReadModifyWrite(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_GetHasBranchTakenDelaySlot(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_GetHasBranchNotTakenDelaySlot(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_GetInDelaySlot(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueRegister* Instruction_GetSegmentOverride(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillInstructionCategoryId Instruction_GetCategory(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<nint>* Instruction_GetOperands(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* Instruction_Serialize(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsControlFlow(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsDirectControlFlow(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsIndirectControlFlow(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsConditionalBranch(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsFunctionCall(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsFunctionReturn(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsValid(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsError(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Instruction_IsNoOp(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong Instruction_NumBytes(RemillOpaqueInstruction* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern RemillOpaqueInstructionLifterIntf* GetLifter(RemillOpaqueInstruction* inst);
    }
}
