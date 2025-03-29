#pragma once
#include <llvm/Analysis/ConstantFolding.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Module.h>

#include <API/ExportDef.h>

namespace DNA::API {
	DNA_EXPORT llvm::Constant* TryConstantFold(llvm::Instruction* instruction) {
		return llvm::ConstantFoldInstruction(instruction, instruction->getModule()->getDataLayout());
	}
}