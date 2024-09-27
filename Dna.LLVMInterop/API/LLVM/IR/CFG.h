#pragma once

#include <llvm/IR/CFG.h>
#include <API/ImmutableManagedVector.h>
#include <API/ExportDef.h>

using namespace llvm;

namespace Dna::API {
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
}