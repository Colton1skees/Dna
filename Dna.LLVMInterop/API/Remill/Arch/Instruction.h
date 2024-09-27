#pragma once 

#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/Arch.h"
#include <remill/Arch/Name.h>
#include "remill/BC/ABI.h"
#include "remill/BC/Util.h"
#include "remill/BC/Version.h"
#include <remill/BC/InstructionLifter.h>
#include "remill/OS/OS.h"

#include "API/ExportDef.h"
#include <API/ImmutableManagedVector.h>

// remill::Instruction.
namespace Dna::API {
	DNA_EXPORT remill::Instruction* Instruction_Constructor()
	{
		return new remill::Instruction();
	}

	DNA_EXPORT void Instruction_Reset(remill::Instruction* instruction)
	{
		instruction->Reset();
	}

	DNA_EXPORT char* Instruction_GetFunction(remill::Instruction* instruction)
	{
		return _strdup(instruction->function.c_str());
	}

	DNA_EXPORT char* Instruction_GetBytes(remill::Instruction* instruction)
	{
		return _strdup(instruction->bytes.c_str());
	}

	DNA_EXPORT uint64_t Instruction_GetPc(remill::Instruction* instruction)
	{
		return instruction->pc;
	}

	DNA_EXPORT uint64_t Instruction_GetNextPc(remill::Instruction* instruction)
	{
		return instruction->next_pc;
	}

	DNA_EXPORT uint64_t Instruction_GetDelayedPc(remill::Instruction* instruction)
	{
		return instruction->delayed_pc;
	}

	DNA_EXPORT uint64_t Instruction_GetBranchTakenPc(remill::Instruction* instruction)
	{
		return instruction->branch_taken_pc;
	}

	DNA_EXPORT uint64_t Instruction_GetBranchNotTakenPc(remill::Instruction* instruction)
	{
		return instruction->branch_not_taken_pc;
	}

	DNA_EXPORT remill::ArchName Instruction_GetArchName(remill::Instruction* instruction)
	{
		return instruction->arch_name;
	}

	DNA_EXPORT remill::ArchName Instruction_GetSubArchName(remill::Instruction* instruction)
	{
		return instruction->sub_arch_name;
	}

	DNA_EXPORT remill::ArchName Instruction_GetBranchTakenArchName(remill::Instruction* instruction, bool* exists)
	{
		auto val = instruction->branch_taken_arch_name;
		if (val.has_value())
		{
			*exists = true;
			return val.value();
		}

		*exists = false;
		return remill::kArchInvalid;
	}

	DNA_EXPORT const remill::Arch* Instruction_GetArch(remill::Instruction* instruction)
	{
		return instruction->arch;
	}

	DNA_EXPORT bool Instruction_GetIsAtomicReadModifyWrite(remill::Instruction* instruction)
	{
		return instruction->is_atomic_read_modify_write;
	}

	DNA_EXPORT bool Instruction_GetHasBranchTakenDelaySlot(remill::Instruction* instruction)
	{
		return instruction->has_branch_taken_delay_slot;
	}

	DNA_EXPORT bool Instruction_GetHasBranchNotTakenDelaySlot(remill::Instruction* instruction)
	{
		return instruction->has_branch_not_taken_delay_slot;
	}

	DNA_EXPORT bool Instruction_GetInDelaySlot(remill::Instruction* instruction)
	{
		return instruction->in_delay_slot;
	}

	DNA_EXPORT const remill::Register* Instruction_GetSegmentOverride(remill::Instruction* instruction)
	{
		return instruction->segment_override;
	}

	DNA_EXPORT const remill::Instruction::Category Instruction_GetCategory(remill::Instruction* instruction)
	{
		return instruction->category;
	}

	DNA_EXPORT ImmutableManagedVector* Instruction_GetOperands(remill::Instruction* instruction)
	{
		// TODO: Handle ownership.
		auto output = new std::vector<remill::Operand*>();
		for (auto& op : instruction->operands)
		{
			output->push_back(new remill::Operand(op));
		}

		return ImmutableManagedVector::NonCopyingFrom(output);
	}

	DNA_EXPORT char* Instruction_Serialize(remill::Instruction* instruction)
	{
		return _strdup(instruction->Serialize().c_str());
	}

	DNA_EXPORT bool Instruction_IsControlFlow(remill::Instruction* instruction)
	{
		return instruction->IsControlFlow();
	}

	DNA_EXPORT bool Instruction_IsDirectControlFlow(remill::Instruction* instruction)
	{
		return instruction->IsDirectControlFlow();
	}

	DNA_EXPORT bool Instruction_IsIndirectControlFlow(remill::Instruction* instruction)
	{
		return instruction->IsIndirectControlFlow();
	}

	DNA_EXPORT bool Instruction_IsConditionalBranch(remill::Instruction* instruction)
	{
		return instruction->IsConditionalBranch();
	}

	DNA_EXPORT bool Instruction_IsFunctionCall(remill::Instruction* instruction)
	{
		return instruction->IsFunctionCall();
	}

	DNA_EXPORT bool Instruction_IsFunctionReturn(remill::Instruction* instruction)
	{
		return instruction->IsFunctionReturn();
	}

	DNA_EXPORT bool Instruction_IsValid(remill::Instruction* instruction)
	{
		return instruction->IsValid();
	}

	DNA_EXPORT bool Instruction_IsError(remill::Instruction* instruction)
	{
		return instruction->IsError();
	}

	DNA_EXPORT bool Instruction_IsNoOp(remill::Instruction* instruction)
	{
		return instruction->IsNoOp();
	}

	DNA_EXPORT uint64_t Instruction_NumBytes(remill::Instruction* instruction)
	{
		return instruction->NumBytes();
	}

	DNA_EXPORT remill::InstructionLifterIntf* GetLifter(remill::Instruction* instruction)
	{
		auto ptr = instruction->GetLifter();

		return ptr.get();
	}
}