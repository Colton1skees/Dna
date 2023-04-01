#pragma once

#include <API/ExportDef.h>
#include <API/ImmutableManagedVector.h>
#include <llvm/Analysis/LoopInfo.h>

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
}