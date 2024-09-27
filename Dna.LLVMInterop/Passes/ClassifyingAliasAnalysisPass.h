#pragma once

#include <llvm/Analysis/AliasAnalysis.h>
#include <unordered_map>
#include <memory>
#include <set>

#include <Arch/X86/X86Registers.h>

namespace Dna::Passes {
	// Typedef for a function pointer which returns whether two functions alias.
	typedef llvm::AliasResult::Kind(__cdecl* tGetAliasResult)(llvm::Value* ptrA, llvm::Value* ptrB);

	// Alias analysis pass that attempts to classify pointers based off of their access
	// patterns. For example, if you encounter [rsp - 0x10] and [.TEXT + 0x50],
	// this pass will return a [NoAlias] for these two memory accesses.
	// If two memory accesses cannot be proved to [NoAlias], then we
	// fall back to the results of the basic alias analysis pass.
	class ClassifyingAAResult : public llvm::AAResultBase
	{
		friend llvm::AAResultBase;

	public:
		static tGetAliasResult gGetAliasResult;

		ClassifyingAAResult();

		ClassifyingAAResult(tGetAliasResult);

		bool invalidate(llvm::Function& F, const llvm::PreservedAnalyses& PA, llvm::FunctionAnalysisManager::Invalidator& Inv);

		llvm::AliasResult alias(const llvm::MemoryLocation& locA, const llvm::MemoryLocation& locB, llvm::AAQueryInfo& AAQI, const llvm::Instruction* CtxI);

	private:
		tGetAliasResult pGetAliasResult;
	};

	class SegmentsAAWrapperPass : public llvm::ImmutablePass {
		std::unique_ptr<ClassifyingAAResult> mResult;

	public:
		static char ID;

		SegmentsAAWrapperPass();

		ClassifyingAAResult& getResult();
		const ClassifyingAAResult& getResult() const;

		virtual bool doInitialization(llvm::Module& M) override;
		virtual bool doFinalization(llvm::Module& M) override;

		virtual void getAnalysisUsage(llvm::AnalysisUsage& analysisUsage) const override;

	private:
		tGetAliasResult pGetAliasResult;
	};

	class SegmentsExternalAAWrapperPass : public llvm::ExternalAAWrapperPass {
	public:
		static char ID;

		SegmentsExternalAAWrapperPass() : llvm::ExternalAAWrapperPass(
			[](llvm::Pass& P, llvm::Function& F, llvm::AAResults& AAR) {
				if (auto* WrapperPass = P.getAnalysisIfAvailable<SegmentsAAWrapperPass>())
				AAR.addAAResult(WrapperPass->getResult());
			}) {}
	};

	llvm::ImmutablePass* createSegmentsAAWrapperPass();
}