#pragma once

#include <llvm/IR/CFG.h>
#include <llvm/IR/Dominators.h>
#include <API/ImmutableManagedVector.h>
#include <API/ExportDef.h>

using namespace llvm;

using TreeBase = llvm::DominatorTreeBase<llvm::BasicBlock, false>*;
using DomTree = llvm::DominatorTree*;
using Block = llvm::BasicBlock*;

// llvm::DominatorTreeBase
namespace Dna::API {
	DNA_EXPORT ImmutableManagedVector* DominatorTreeBase_GetDescendants(TreeBase treeBase, Block block)
	{
		llvm::SmallVector<Block> exitingBlocks;
		treeBase->getDescendants(block, exitingBlocks);

		// Copy the predecessors to a vector..
		auto vec = new std::vector<llvm::BasicBlock*>();
		for (llvm::BasicBlock* block : exitingBlocks)
			vec->push_back(block);

		// Construct and return an immutable managed vector
		return ImmutableManagedVector::From(vec);
	}

	DNA_EXPORT bool DominatorTreeBase_ProperlyDominates(TreeBase treeBase, Block a, Block b)
	{
		return treeBase->properlyDominates(a, b);
	}

	DNA_EXPORT bool DominatorTreeBase_IsReachableFromEntry(TreeBase treeBase, Block a)
	{
		return treeBase->isReachableFromEntry(a);
	}

	DNA_EXPORT bool DominatorTreeBase_Dominates(TreeBase treeBase, Block a, Block b)
	{
		return treeBase->dominates(a, b);
	}

	DNA_EXPORT Block DominatorTreeBase_FindNearestCommonDenominator(TreeBase treeBase, Block a, Block b)
	{
		return treeBase->findNearestCommonDominator(a, b);
	}
}

// llvm::DominatorTree
namespace Dna::API {
	DNA_EXPORT bool DominatorTree_BlockDominatesUse(DomTree domTree, const Block block, const llvm::Use* use)
	{
		return domTree->dominates(block, *use);
	}

	DNA_EXPORT bool DominatorTree_DefDominatesUse(DomTree domTree, const llvm::Value* def, const llvm::Use* use)
	{
		return domTree->dominates(def, *use);
	}

	DNA_EXPORT bool DominatorTree_DefDominatesUser(DomTree domTree, const llvm::Value* def, const llvm::Instruction* user)
	{
		return domTree->dominates(def, user);
	}

	DNA_EXPORT bool DominatorTree_InstDominatesBlock(DomTree domTree, const llvm::Instruction* def, const Block block)
	{
		return domTree->dominates(def, block);
	}

	DNA_EXPORT llvm::Instruction* DominatorTree_FindNearestCommonDominator(DomTree domTree, llvm::Instruction* i1, llvm::Instruction* i2)
	{
		Block bb1 = i1->getParent();
		Block bb2 = i2->getParent();
		if (bb1 == bb2)
			return i1->comesBefore(i2) ? i1 : i2;
		if (!domTree->isReachableFromEntry(bb2))
			return i1;
		if (!domTree->isReachableFromEntry(bb1))
			return i2;
		Block domBB = domTree->findNearestCommonDominator(bb1, bb2);
		if (bb1 == domBB)
			return i1;
		if (bb2 == domBB)
			return i2;
		return domBB->getTerminator();
	}
}