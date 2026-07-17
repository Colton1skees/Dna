#pragma once

#include "llvm/ADT/PostOrderIterator.h"
#include <llvm/IR/CFG.h>
#include <API/ImmutableManagedVector.h>
#include <API/ExportDef.h>

using namespace llvm;

namespace Dna::API {

	DNA_EXPORT ImmutableManagedVector* Function_GetRpoInstructions(llvm::Function* function, llvm::Instruction** outInstructions)
	{
		auto rpoInstructions = new std::vector<llvm::Instruction*>();

		rpoInstructions->reserve(function->getInstructionCount());

		llvm::ReversePostOrderTraversal<llvm::Function*> RPOT(function);

		for (llvm::BasicBlock* BB : RPOT) 
		{
			for (llvm::Instruction& I : *BB) 
			{
				rpoInstructions->push_back(&I);
			}
		}

		return ImmutableManagedVector::NonCopyingFrom(rpoInstructions);
	}


	DNA_EXPORT unsigned int BasicBlock_GetPredSize(llvm::BasicBlock* block)
	{
		return llvm::pred_size(block);
	}

	DNA_EXPORT ImmutableManagedVector* BasicBlock_GetPredecessors(llvm::BasicBlock* block)
	{
		// Copy the predecessors to a vector..
		auto vec = new std::vector<llvm::BasicBlock*>();
		for (llvm::BasicBlock* pred : llvm::predecessors(block))
			vec->push_back(pred);

		// Construct and return an immutable managed vector
		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT unsigned int BasicBlock_GetSuccSize(llvm::BasicBlock* block)
	{
		return llvm::succ_size(block);
	}

	DNA_EXPORT ImmutableManagedVector* BasicBlock_GetSuccessors(llvm::BasicBlock* block)
	{
		// Copy the predecessors to a vector..
		auto vec = new std::vector<llvm::BasicBlock*>();
		for (llvm::BasicBlock* succ : llvm::successors(block))
			vec->push_back(succ);

		// Construct and return an immutable managed vector
		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT ImmutableManagedVector* Value_GetUsers(llvm::Value* value)
	{
		// Copy the predecessors to a vector..
		auto vec = new std::vector<llvm::Value*>();
		for (llvm::Value* succ : value->users())
			vec->push_back(succ);

		// Construct and return an immutable managed vector
		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT llvm::BasicBlock* SplitBasicBlockAt(llvm::BasicBlock* block, llvm::Instruction* inst, char* name, bool before)
	{
		return block->splitBasicBlock(inst, name, before);
	}

	// Add a set of side effects(taken by analyzing function-attrs),
	// that allow DCE / other passes to delete unnecessary calls to this function.
	// attributes #8 = { mustprogress nofree noinline norecurse nosync nounwind willreturn memory(none) }
	DNA_EXPORT void AddNoSideEffectAttributes(llvm::Function* function)
	{
		function->addFnAttr(llvm::Attribute::MustProgress);
		function->addFnAttr(llvm::Attribute::NoFree);
		function->addFnAttr(llvm::Attribute::NoRecurse);
		function->addFnAttr(llvm::Attribute::NoSync);
		function->addFnAttr(llvm::Attribute::NoUnwind);
		function->addFnAttr(llvm::Attribute::WillReturn);
		//function->setMemoryEffects(MemoryEffects::none());
		function->setMemoryEffects(llvm::MemoryEffects::inaccessibleMemOnly(llvm::ModRefInfo::Mod));
		return;
	}

	DNA_EXPORT void MakeArgNoAlias(llvm::Argument* arg)
	{
		arg->addAttr(llvm::Attribute::NoAlias);
	}

	DNA_EXPORT void MakeDsoLocal(llvm::GlobalValue* function, bool dsoLocal)
	{
		function->setDSOLocal(dsoLocal);
	}

}