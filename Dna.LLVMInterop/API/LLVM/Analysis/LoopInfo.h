#pragma once

#include <API/ExportDef.h>
#include <API/ImmutableManagedVector.h>
#include <llvm/Analysis/LoopInfo.h>
#include <unordered_map>
#include <unordered_set>
// Loop
namespace Dna::API {
	DNA_EXPORT char* Loop_GetName(llvm::Loop* loop)
	{
		return _strdup(loop->getName().str().c_str());
	}

	DNA_EXPORT unsigned int Loop_GetLoopDepth(llvm::Loop* loop)
	{
		return loop->getLoopDepth();
	}

	DNA_EXPORT llvm::BasicBlock* Loop_GetHeader(llvm::Loop* loop)
	{
		return loop->getHeader();
	}

	DNA_EXPORT llvm::Loop* Loop_GetParentLoop(llvm::Loop* loop)
	{
		return loop->getParentLoop();
	}

	DNA_EXPORT llvm::Loop* Loop_GetOutermostLoop(llvm::Loop* loop)
	{
		return loop->getOutermostLoop();
	}

	DNA_EXPORT bool Loop_ContainsLoop(llvm::Loop* src, llvm::Loop* l)
	{
		return src->contains(l);
	}

	DNA_EXPORT bool Loop_ContainsBlock(llvm::Loop* src, llvm::BasicBlock* b)
	{
		return src->contains(b);
	}

	DNA_EXPORT bool Loop_ContainsInstruction(llvm::Loop* src, llvm::Instruction* i)
	{
		return src->contains(i);
	}

	DNA_EXPORT ImmutableManagedVector* Loop_GetSubLoops(llvm::Loop* loop)
	{
		return ImmutableManagedVector::From(&loop->getSubLoops());
	}

	DNA_EXPORT bool Loop_IsInnermost(llvm::Loop* loop)
	{
		return loop->isInnermost();
	}

	DNA_EXPORT bool Loop_IsOutermost(llvm::Loop* loop)
	{
		return loop->isOutermost();
	}

	DNA_EXPORT ImmutableManagedVector* Loop_GetBlocks(llvm::Loop* loop)
	{
		return ImmutableManagedVector::From(&loop->getBlocks());
	}

	DNA_EXPORT bool Loop_IsLoopExiting(llvm::Loop* loop, llvm::BasicBlock* block)
	{
		return loop->isLoopExiting(block);
	}

	DNA_EXPORT unsigned int Loop_GetNumBackEdges(llvm::Loop* loop)
	{
		return loop->getNumBackEdges();
	}

	DNA_EXPORT ImmutableManagedVector* Loop_GetExitingBlocks(llvm::Loop* loop)
	{
		llvm::SmallVector<llvm::BasicBlock*> exitingBlocks;
		loop->getExitingBlocks(exitingBlocks);
		return ImmutableManagedVector::From(&exitingBlocks);
	}

	DNA_EXPORT ImmutableManagedVector* Loop_GetExitBlocks(llvm::Loop* loop)
	{
		llvm::SmallVector<llvm::BasicBlock*> exitingBlocks;
		loop->getExitBlocks(exitingBlocks);
		return ImmutableManagedVector::From(&exitingBlocks);
	}
}

// LoopInfo
namespace Dna::API {
	DNA_EXPORT llvm::LoopInfo* LoopInfo_Constructor(llvm::Function* func)
	{
		llvm::DominatorTree DT(*func);
		llvm::LoopInfo* LI = new llvm::LoopInfo();
		LI->analyze(DT);

		return LI;
	}

	DNA_EXPORT ImmutableManagedVector* LoopInfo_GetLoopsInPreorder(llvm::LoopInfo* loopInfo)
	{
		return ImmutableManagedVector::From(&loopInfo->getLoopsInPreorder());
	}

	DNA_EXPORT ImmutableManagedVector* LoopInfo_GetLoopsInReverseSiblingPreorder(llvm::LoopInfo* loopInfo)
	{
		return ImmutableManagedVector::From(&loopInfo->getLoopsInReverseSiblingPreorder());
	}

	DNA_EXPORT llvm::Loop* LoopInfo_GetLoopFor(llvm::LoopInfo* loopInfo, llvm::BasicBlock* block)
	{
		return loopInfo->getLoopFor(block);
	}

	DNA_EXPORT unsigned int LoopInfo_GetLoopDepth(llvm::LoopInfo* loopInfo, llvm::BasicBlock* block)
	{
		return loopInfo->getLoopDepth(block);
	}

	DNA_EXPORT bool LoopInfo_IsLoopheader(llvm::LoopInfo* loopInfo, llvm::BasicBlock* block)
	{
		return loopInfo->isLoopHeader(block);
	}

	DNA_EXPORT ImmutableManagedVector* LoopInfo_GetTopLevelLoops(llvm::LoopInfo* loopInfo)
	{
		return ImmutableManagedVector::From(&loopInfo->getTopLevelLoops());
	}


	// Use DFS to detect a possible loop, most likely an loop in an irreducible
	// CFG. One of the headers of the loop is the FirstBB.
	// Loop is set to true upon successfully detecting such a loop.
	void checkIrreducibleCFG(llvm::BasicBlock* BB,
		llvm::BasicBlock* FirstBB,
		std::unordered_set<const llvm::BasicBlock*>& VisitedBBs,
		bool& Loop) {
		VisitedBBs.insert(BB);
		for (llvm::succ_iterator PI = succ_begin(BB), E = succ_end(BB);
			PI != E; ++PI) {
			llvm::BasicBlock* Succ = *PI;
			if (Succ == FirstBB) {
				Loop = true;
			}
			else if (VisitedBBs.count(Succ)) {
				continue;
			}
			else {
				checkIrreducibleCFG(Succ, FirstBB, VisitedBBs, Loop);
			}
			if (Loop)
				return;
		}
	}

	// Return true if the Basic Block containing the passed Phi node is
	// a loop entry point, either the loop header for a natural loop or
	// a entry point for an irreducible CFG.
	DNA_EXPORT bool IsLoopEntryPoint(llvm::LoopInfo* li, llvm::PHINode* Phi) {
		BasicBlock* BB = Phi->getParent();
		// If LLVM can determine if BB is a loop header, simply return true.
		// Presumably, this should handle structured loops.
		if (li->isLoopHeader(BB))
			return true;
		if (Phi->getNumIncomingValues() <= 1)
		return false;

		bool Loop = false;
		std::unordered_set<const llvm::BasicBlock*> VisitedBBs;
		checkIrreducibleCFG(BB, BB, VisitedBBs, Loop);
		return Loop;
	}


}
