#pragma once

#include "llvm/Pass.h"
#include "llvm/ADT/APInt.h"
#include "llvm/ADT/ArrayRef.h"
#include "llvm/ADT/DenseMap.h"
#include "llvm/ADT/None.h"
#include "llvm/ADT/SmallPtrSet.h"
#include "llvm/ADT/SmallVector.h"
#include "llvm/ADT/Statistic.h"
#include "llvm/Analysis/AliasAnalysis.h"
#include "llvm/Analysis/AssumptionCache.h"
#include "llvm/Analysis/BasicAliasAnalysis.h"
#include "llvm/Analysis/BlockFrequencyInfo.h"
#include "llvm/Analysis/CFG.h"
#include "llvm/Analysis/ConstantFolding.h"
#include "llvm/Analysis/GlobalsModRef.h"
#include "llvm/Analysis/InstructionSimplify.h"
#include "llvm/Analysis/LazyBlockFrequencyInfo.h"
#include "llvm/Analysis/LoopInfo.h"
#include "llvm/Analysis/MemoryBuiltins.h"
#include "llvm/Analysis/OptimizationRemarkEmitter.h"
#include "llvm/Analysis/ProfileSummaryInfo.h"
#include "llvm/Analysis/TargetFolder.h"
#include "llvm/Analysis/TargetLibraryInfo.h"
#include "llvm/Analysis/TargetTransformInfo.h"
#include "llvm/Analysis/Utils/Local.h"
#include "llvm/Analysis/ValueTracking.h"
#include "llvm/Analysis/VectorUtils.h"
#include "llvm/IR/BasicBlock.h"
#include "llvm/IR/CFG.h"
#include "llvm/IR/Constant.h"
#include "llvm/IR/Constants.h"
#include "llvm/IR/DIBuilder.h"
#include "llvm/IR/DataLayout.h"
#include "llvm/IR/DebugInfo.h"
#include "llvm/IR/DerivedTypes.h"
#include "llvm/IR/Dominators.h"
#include "llvm/IR/Function.h"
#include "llvm/IR/GetElementPtrTypeIterator.h"
#include "llvm/IR/IRBuilder.h"
#include "llvm/IR/InstrTypes.h"
#include "llvm/IR/Instruction.h"
#include "llvm/IR/Instructions.h"
#include "llvm/IR/IntrinsicInst.h"
#include "llvm/IR/Intrinsics.h"
#include "llvm/IR/LegacyPassManager.h"
#include "llvm/IR/Metadata.h"
#include "llvm/IR/Operator.h"
#include "llvm/IR/PassManager.h"
#include "llvm/IR/PatternMatch.h"
#include "llvm/IR/Type.h"
#include "llvm/IR/Use.h"
#include "llvm/IR/User.h"
#include "llvm/IR/Value.h"
#include "llvm/IR/ValueHandle.h"
#include "llvm/InitializePasses.h"
#include "llvm/Support/Casting.h"
#include "llvm/Support/CommandLine.h"
#include "llvm/Support/Compiler.h"
#include "llvm/Support/Debug.h"
#include "llvm/Support/DebugCounter.h"
#include "llvm/Support/ErrorHandling.h"
#include "llvm/Support/KnownBits.h"
#include "llvm/Support/raw_ostream.h"
#include "llvm/Transforms/InstCombine/InstCombine.h"
#include "llvm/Transforms/Utils/Local.h"
#include <llvm/Analysis/MemorySSA.h>
#include <Passes/ClassifyingAliasAnalysisPass.h>
namespace Dna::Passes {
	// Helper function to be used by native code for attempting to solve constant jump table values.
	// Given a string representation of a model, and a target variable name to extract from the (possibly) solved model, we try to extract out a constant solution.
	typedef bool(__cdecl* tTrySolveConstant)(char* model, char* targetVariable, uint64_t* solvedConstant);

	typedef void(__cdecl* tAnalyzeJumpTableBounds)(llvm::Function* func, llvm::LoopInfo* loopInfo, llvm::MemorySSA* memSsa, llvm::LazyValueInfo* lvi, tTrySolveConstant trySolveConstant);

	struct JumpTableAnalysisPass : public llvm::PassInfoMixin<JumpTableAnalysisPass>
	{
		static char ID;

		tAnalyzeJumpTableBounds analyzeBounds;

		tTrySolveConstant trySolveConstant;

		JumpTableAnalysisPass(tAnalyzeJumpTableBounds structureFunction, tTrySolveConstant trySolveConstant)
		{
			this->analyzeBounds = structureFunction;
			this->trySolveConstant = trySolveConstant;
		}


		llvm::PreservedAnalyses run(llvm::Function& F, llvm::FunctionAnalysisManager& fam)
		{
			llvm::LoopInfo& LI = fam.getResult<llvm::LoopAnalysis>(F);
			llvm::MemorySSA& mssa = fam.getResult<llvm::MemorySSAAnalysis>(F).getMSSA();
			llvm::LazyValueInfo& lvi = fam.getResult<llvm::LazyValueAnalysis>(F);
			mssa.ensureOptimizedUses();
			analyzeBounds(&F, &LI, &mssa, &lvi, trySolveConstant);

			// This pass does not mutate the control flow graph, so all analyses should be preserved.
			return llvm::PreservedAnalyses::all();
		}
	};

	char JumpTableAnalysisPass::ID = 0;

	static llvm::RegisterPass<JumpTableAnalysisPass> JumpTableAnalysis("JumpTableAnalysisPass", "Recover accurate jump table bounds from a function slice.");
}