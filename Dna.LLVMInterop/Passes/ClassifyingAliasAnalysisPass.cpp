#include <llvm/IR/PatternMatch.h>
#include <iostream>
#include <vector>
#include <stack>
#include <set>
#include <llvm/Support/raw_ostream.h>
#include "ClassifyingAliasAnalysisPass.h"

using namespace llvm::PatternMatch;

namespace Dna::Passes {
	tGetAliasResult ClassifyingAAResult::gGetAliasResult = nullptr;

	ClassifyingAAResult::ClassifyingAAResult() {}

	ClassifyingAAResult::ClassifyingAAResult(tGetAliasResult ptrGetAliasResult) : pGetAliasResult(ptrGetAliasResult) {}

	bool ClassifyingAAResult::invalidate(llvm::Function& F, const llvm::PreservedAnalyses& PA, llvm::FunctionAnalysisManager::Invalidator& Inv)
	{
		return false;
	}

	llvm::AliasResult ClassifyingAAResult::alias(const llvm::MemoryLocation& locA, const llvm::MemoryLocation& locB, llvm::AAQueryInfo& AAQI, const llvm::Instruction* CtxI)
	{
		// If our managed implementation of alias analysis returns a valid result, then we use it.
		// Otherwise, UINT8_MAX was returned, meaning that we must use the value obtained via LLVM's default alias analysis.
		auto result = pGetAliasResult((llvm::Value*)locA.Ptr, (llvm::Value*)locB.Ptr);
		if (result == llvm::AliasResult::NoAlias
			|| result == llvm::AliasResult::MayAlias
			|| result == llvm::AliasResult::PartialAlias
			|| result == llvm::AliasResult::MustAlias)
		{
			return result;
		}

		// If we cannot prove that the two memory locations are [NoAlias], then we fallback to the default alias analysis.
		return AAResultBase::alias(locA, locB, AAQI, CtxI);
	}	

	char SegmentsAAWrapperPass::ID = 0;

	SegmentsAAWrapperPass::SegmentsAAWrapperPass() : llvm::ImmutablePass(ID) 
	{

	}

	ClassifyingAAResult& SegmentsAAWrapperPass::getResult() { return *mResult; }

	const ClassifyingAAResult& SegmentsAAWrapperPass::getResult() const { return *mResult; }

	bool SegmentsAAWrapperPass::doInitialization(llvm::Module& M) {
		mResult.reset(new ClassifyingAAResult(ClassifyingAAResult::gGetAliasResult));
		return false;
	}

	bool SegmentsAAWrapperPass::doFinalization(llvm::Module& M) {
		mResult.reset();
		return false;
	}

	void SegmentsAAWrapperPass::getAnalysisUsage(llvm::AnalysisUsage& AU) const {
		AU.setPreservesAll();
	}

	llvm::ImmutablePass* createSegmentsAAWrapperPass() { return new SegmentsAAWrapperPass(); }

	static llvm::RegisterPass<SegmentsAAWrapperPass> SegmentsAliasAnalysis("-saa", "NoAlias for pointers in different segments", false, true);
}