#pragma once

#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/Arch.h"
#include "remill/BC/ABI.h"
#include "remill/BC/Util.h"
#include "remill/BC/Version.h"
#include "remill/BC/InstructionLifter.h"
#include "remill/OS/OS.h"

#include "API/ExportDef.h"

// remill::OperandLifter
namespace Dna::API {
	DNA_EXPORT llvm::Value* OperandLifter_LoadRegAddress(remill::OperandLifter* lifter, llvm::BasicBlock* block, llvm::Value* statePtr, char* regName)
	{
		// Note: While remill returns a pair of <llvm::Value*, llvm::Type*>,
		// we only return the value.
		return lifter->LoadRegAddress(block, statePtr, regName).first;
	}

	DNA_EXPORT llvm::Value* OperandLifter_LoadRegValue(remill::OperandLifter* lifter, llvm::BasicBlock* block, llvm::Value* statePtr, char* regName)
	{
		// Note: While remill returns a pair of <llvm::Value*, llvm::Type*>,
		// we only return the value.
		return lifter->LoadRegValue(block, statePtr, regName);
	}

	DNA_EXPORT llvm::Type* OperandLifter_GetMemoryType(remill::OperandLifter* lifter)
	{
		return lifter->GetMemoryType();
	}

	DNA_EXPORT void OperandLifter_ClearCache(remill::OperandLifter* lifter)
	{
		return lifter->ClearCache();
	}
}

// remill::InstructionLifterIntf
namespace Dna::API {
	DNA_EXPORT remill::LiftStatus InstructionLifterIntf_LiftIntoBlockWithStatePtr(remill::InstructionLifterIntf* lifter, remill::Instruction* inst,
		llvm::BasicBlock* block, llvm::Value* statePtr, bool isDelayed)
	{
		return lifter->LiftIntoBlock(*inst, block, statePtr, isDelayed);
	}

	DNA_EXPORT remill::LiftStatus InstructionLifterIntf_LiftIntoBlock(remill::InstructionLifterIntf* lifter, remill::Instruction* inst,
		llvm::BasicBlock* block, bool isDelayed)
	{
		return lifter->LiftIntoBlock(*inst, block, isDelayed);
	}
}