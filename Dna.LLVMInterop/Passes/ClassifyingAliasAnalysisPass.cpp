#include <llvm/IR/PatternMatch.h>
#include <iostream>
#include <vector>
#include <stack>
#include <set>

#include "ClassifyingAliasAnalysisPass.h"

using namespace llvm::PatternMatch;

namespace Dna::Passes {
	ClassifyingAAResult::ClassifyingAAResult(const GlobalPointerContext& gpContext) : gpCtx(gpContext) 
	{
	
	}

	bool ClassifyingAAResult::invalidate(llvm::Function& F, const llvm::PreservedAnalyses& PA, llvm::FunctionAnalysisManager::Invalidator& Inv)
	{
		return false;
	}

	llvm::AliasResult ClassifyingAAResult::alias(const llvm::MemoryLocation& locA, const llvm::MemoryLocation& locB, llvm::AAQueryInfo& AAQI)
	{
		std::set<const llvm::Value*> visited;
		auto aTy = getPointerType(locA.Ptr, visited);
		auto bTy = getPointerType(locB.Ptr, visited);
		if ((aTy != PointerType::PtrTyUnk) && (bTy != PointerType::PtrTyUnk) && (aTy != bTy))
			return llvm::AliasResult::NoAlias;

		// If we cannot prove that the two memory locations are [NoAlias], then we fallback to the default alias analysis.
		return AAResultBase::alias(locA, locB, AAQI);
	}	

	PointerType ClassifyingAAResult::getPointerType(const llvm::Value* V, std::set<const llvm::Value*>& visited) const
	{
		if (isLocalStackAccess(V))
			return PointerType::PtrTyLocalStack;
		if (isBinarySectionAccess(V))
			return PointerType::PtrTyBinarySection;

		return PointerType::PtrTyUnk;
	}

	bool ClassifyingAAResult::isLocalStackAccess(const llvm::Value* V) const
	{
		// Compute a chain of elements, and return false if we encounter an unsupported element.
		std::vector<const llvm::Value*> chain;
		auto success = GetOperatorChain(V, &chain);
		if (!success)
			return false;

		for (auto value : chain)
		{
			if (value == gpCtx.rspPtr)
			{
				return true;
			}
		}

		return false;
	}

	bool ClassifyingAAResult::isBinarySectionAccess(const llvm::Value* V) const
	{
		// Compute a chain of elements, and return false if we encounter an unsupported element.
		std::vector<const llvm::Value*> chain;
		auto success = GetOperatorChain(V, &chain);
		if (!success)
			return false;

		for (auto value : chain)
		{
			if (const auto* constInt = llvm::dyn_cast<llvm::ConstantInt>(value))
			{
				auto intValue = constInt->getValue().getZExtValue();
				if (intValue >= 0x140009000 && intValue <= 0x14006C460)
					return true;
			}
		}

		return false;
	}

	bool ClassifyingAAResult::GetOperatorChain(const llvm::Value* V, std::vector<const llvm::Value*>* chain) const
	{
		if (const auto* gep = llvm::dyn_cast<llvm::GetElementPtrInst>(V))
		{
			chain->push_back(gep);
			return GetOperatorChain(gep->getOperand(1), chain);
		}

		else if (const auto* constInt = llvm::dyn_cast<llvm::ConstantInt>(V))
		{
			chain->push_back(constInt);
			return true;
		}

		else if (const auto* binop = llvm::dyn_cast<llvm::BinaryOperator>(V))
		{
			chain->push_back(binop);
			bool supportsOp0 = GetOperatorChain(binop->getOperand(0), chain);
			bool supportsOp1 = GetOperatorChain(binop->getOperand(1), chain);
			return supportsOp0 && supportsOp1;
		}

		else if (const auto& load = llvm::dyn_cast<llvm::LoadInst>(V))
		{
			// Push back the load operation to the chain.
			chain->push_back(load);

			// If this is the load from RSP, then we return `true` to indicate that it was a success,
			// and push back the rsp pointer.
			auto loadOp = load->getOperand(0);
			if (loadOp == gpCtx.rspPtr)
			{
				chain->push_back(loadOp);
				return true;
			}

			// Otherwise, we return false. This is likely a load to / from a register, or an unknown store.
			return false;
		}

		else
		{
			return false;
		}
	}

	char SegmentsAAWrapperPass::ID = 0;

	SegmentsAAWrapperPass::SegmentsAAWrapperPass() : llvm::ImmutablePass(ID) {}

	ClassifyingAAResult& SegmentsAAWrapperPass::getResult() { return *mResult; }

	const ClassifyingAAResult& SegmentsAAWrapperPass::getResult() const { return *mResult; }

	bool SegmentsAAWrapperPass::doInitialization(llvm::Module& M) {
		GlobalPointerContext GP{
			.rspPtr = M.getGlobalVariable("rsp")
		};
		mResult.reset(new ClassifyingAAResult(GP));
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