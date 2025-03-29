using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    public class RemillInstruction
    {
        public readonly nint Handle;

        public unsafe string SemanticFunctionName => StringMarshaler.AcquireString(NativeRemillInstructionApi.Instruction_GetFunction(this));

        public unsafe string ByteStr => StringMarshaler.AcquireString(NativeRemillInstructionApi.Instruction_GetBytes(this));

        public unsafe ulong Pc => NativeRemillInstructionApi.Instruction_GetPc(this);

        public unsafe ulong NextPc => NativeRemillInstructionApi.Instruction_GetNextPc(this);

        public unsafe ulong DelayedPc => NativeRemillInstructionApi.Instruction_GetDelayedPc(this);

        public unsafe ulong BranchTakenPc => NativeRemillInstructionApi.Instruction_GetBranchTakenPc(this);

        public unsafe ulong BranchNotTakenPc => NativeRemillInstructionApi.Instruction_GetBranchNotTakenPc(this);

        public unsafe RemillArchId ArchId => NativeRemillInstructionApi.Instruction_GetArchName(this);

        public unsafe RemillArchId SubArchId => NativeRemillInstructionApi.Instruction_GetSubArchName(this);

        public unsafe RemillArchId? BranchTakenArchId => GetBranchTakenArchId();

        public unsafe RemillArch Arch => NativeRemillInstructionApi.Instruction_GetArch(this);

        public unsafe bool IsAtomicReadModifyWrite => NativeRemillInstructionApi.Instruction_GetIsAtomicReadModifyWrite(this);

        public unsafe bool HasBranchTakenDelaySlot => NativeRemillInstructionApi.Instruction_GetHasBranchTakenDelaySlot(this);

        public unsafe bool HasBranchNotTakenDelaySlot => NativeRemillInstructionApi.Instruction_GetHasBranchNotTakenDelaySlot(this);

        public unsafe bool InDelaySlot => NativeRemillInstructionApi.Instruction_GetInDelaySlot(this);

        public unsafe RemillRegister? SegmentOverride => GetSegmentOverride();

        public unsafe RemillInstructionCategoryId CategoryId => NativeRemillInstructionApi.Instruction_GetCategory(this);

        // TODO: Expose List<RemillOperand>.
        public unsafe string Text => ToString();

        public unsafe bool IsControlFlow => NativeRemillInstructionApi.Instruction_IsControlFlow(this);

        public unsafe bool IsDirectControlFlow => NativeRemillInstructionApi.Instruction_IsDirectControlFlow(this);

        public unsafe bool IsIndirectControlFlow => NativeRemillInstructionApi.Instruction_IsIndirectControlFlow(this);

        public unsafe bool IsConditionalBranch => NativeRemillInstructionApi.Instruction_IsConditionalBranch(this);

        public unsafe bool IsFunctionCall => NativeRemillInstructionApi.Instruction_IsFunctionCall(this);

        public unsafe bool IsFunctionReturn => NativeRemillInstructionApi.Instruction_IsFunctionReturn(this);

        public unsafe bool IsValid => NativeRemillInstructionApi.Instruction_IsValid(this);

        public unsafe bool IsError => NativeRemillInstructionApi.Instruction_IsError(this);

        public unsafe bool IsNoOp => NativeRemillInstructionApi.Instruction_IsNoOp(this);

        public unsafe ulong NumBytes => NativeRemillInstructionApi.Instruction_NumBytes(this);

        public unsafe RemillInstructionLifterIntf Lifter => NativeRemillInstructionApi.GetLifter(this);

        public RemillInstruction(nint handle)
        {
            Handle = handle;
        }

        public unsafe RemillInstruction()
        {
            Handle = (nint)NativeRemillInstructionApi.Instruction_Constructor();
        }

        public unsafe void Reset() => NativeRemillInstructionApi.Instruction_Reset(this);

        private unsafe RemillArchId? GetBranchTakenArchId()
        {
            bool exists = false;
            var id = NativeRemillInstructionApi.Instruction_GetBranchTakenArchName(this, &exists);
            return exists ? id : null;
        }

        private unsafe RemillRegister? GetSegmentOverride()
        {
            var ptr = NativeRemillInstructionApi.Instruction_GetSegmentOverride(this);
            return ptr == null ? (RemillRegister?)null : ptr;
        }

        public override unsafe string ToString()
        {
            return StringMarshaler.AcquireString(NativeRemillInstructionApi.Instruction_Serialize(this));
        }

        public unsafe static implicit operator RemillOpaqueInstruction*(RemillInstruction reg)
        {
            return (RemillOpaqueInstruction*)reg.Handle;
        }

        public unsafe static implicit operator RemillInstruction(RemillOpaqueInstruction* reg)
        {
            return new RemillInstruction((nint)reg);
        }
    }
}
