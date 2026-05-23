#pragma once
#include <llvm/Analysis/ConstantFolding.h>
#include <llvm/Analysis/InstructionSimplify.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Module.h>

#include <API/ExportDef.h>

namespace DNA::API {
	DNA_EXPORT llvm::Constant* TryConstantFold(llvm::Instruction* instruction) {
		return llvm::ConstantFoldInstruction(instruction, instruction->getModule()->getDataLayout());
	}

	DNA_EXPORT llvm::Value* TrySimplify(llvm::Instruction* instruction) {

		llvm::SimplifyQuery query(instruction->getModule()->getDataLayout());
		query.DT = nullptr;
		query.AC = nullptr;
		query.CxtI = nullptr;


		return llvm::simplifyInstruction(instruction, query);
	}
}