#pragma once
#include <llvm/Analysis/ConstantFolding.h>
#include <llvm/Analysis/InstructionSimplify.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Module.h>
#include <llvm/Transforms/Utils/Local.h>

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
		query.CanUseUndef = false;

		return llvm::simplifyInstruction(instruction, query);
	}

	DNA_EXPORT bool IsInstructionTriviallyDead(llvm::Instruction* instruction) {

		return llvm::isInstructionTriviallyDead(instruction);
	}


	DNA_EXPORT void DropPoisonGeneratingFlags(llvm::Instruction* instruction) {

		instruction->dropPoisonGeneratingFlagsAndMetadata();

		if (auto* Call = dyn_cast<CallBase>(instruction)) {

			// Strip attributes from the return value
			Call->removeRetAttr(Attribute::NoUndef);
			Call->removeRetAttr(Attribute::NonNull);
			Call->removeRetAttr(Attribute::Dereferenceable);

			// Strip attributes from every argument
			for (unsigned ArgIdx = 0; ArgIdx < Call->arg_size(); ++ArgIdx) {
				Call->removeParamAttr(ArgIdx, Attribute::NoUndef);
				Call->removeParamAttr(ArgIdx, Attribute::NonNull);
				Call->removeParamAttr(ArgIdx, Attribute::Dereferenceable);
			}
		}
	}
}