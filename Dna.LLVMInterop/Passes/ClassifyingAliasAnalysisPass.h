#pragma once

#include <llvm/Analysis/AliasAnalysis.h>
#include <llvm/Pass.h>
#include <unordered_map>
#include <memory>
#include <set>

#include <Arch/X86/X86Registers.h>

namespace Dna::Passes {
	// Typedef for a function pointer which returns whether two functions alias.
	typedef llvm::AliasResult(__cdecl* tGetAliasResult)(llvm::Value* ptrA, llvm::Value* ptrB);

	// Classification of the type of a pointer.
	enum PointerType 
	{
		// Unknown pointer type
		PtrTyUnk,
		// Section within the binary(e.g. .TEXT)
		PtrTyBinarySection,
		// Local stack access(e.g. [rsp - 0x10] or [rsp - 0x10 + index)
		PtrTyLocalStack,
	};

	// Class containing information about certain global variable pointers.
	struct GlobalPointerContext
	{
		// Global variable for the @memory ptr.
		llvm::GlobalVariable* memoryPtr;

		llvm::GlobalVariable* rspPtr;

		// Mapping between <llvm::Global, register_id>
		std::unordered_map<llvm::GlobalValue*, X86RegisterId>* registerPtrMapping;
	};

	// Alias analysis pass that attempts to classify pointers based off of their access
	// patterns. For example, if you encounter [rsp - 0x10] and [.TEXT + 0x50],
	// this pass will return a [NoAlias] for these two memory accesses.
	// If two memory accesses cannot be proved to [NoAlias], then we
	// fall back to the results of the basic alias analysis pass.
	class ClassifyingAAResult : public llvm::AAResultBase<ClassifyingAAResult> 
	{
		friend llvm::AAResultBase<ClassifyingAAResult>;

	public:
		ClassifyingAAResult(const GlobalPointerContext& gpContext);

		bool invalidate(llvm::Function& F, const llvm::PreservedAnalyses& PA, llvm::FunctionAnalysisManager::Invalidator& Inv);

		llvm::AliasResult alias(const llvm::MemoryLocation& locA, const llvm::MemoryLocation& locB, llvm::AAQueryInfo& AAQI);

		PointerType getPointerType(const llvm::Value* V, std::set<const llvm::Value*>& visited) const;

		GlobalPointerContext gpCtx;

		std::unordered_map<const llvm::Value*, PointerType> ptrTypeCache;

		const llvm::Value* rspPtr;

		const llvm::Value* memoryPtr;

	private:
		bool isLocalStackAccess(const llvm::Value* V) const;

		bool isBinarySectionAccess(const llvm::Value* V) const;

		bool GetOperatorChain(const llvm::Value* V, std::vector<const llvm::Value*>* chain) const;
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