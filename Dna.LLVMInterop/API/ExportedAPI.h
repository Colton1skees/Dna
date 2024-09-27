#pragma once

#include <llvm/IR/Module.h>
#include "llvm-c/Transforms/PassManagerBuilder.h"
#include "llvm/ADT/SmallVector.h"
#include "llvm/ADT/Triple.h"
#include "llvm/Analysis/BasicAliasAnalysis.h"
#include "llvm/Analysis/ScalarEvolutionAliasAnalysis.h"
#include "llvm/Analysis/GlobalsModRef.h"
#include "llvm/Analysis/InlineCost.h"
#include "llvm/Analysis/Passes.h"
#include "llvm/Analysis/ScopedNoAliasAA.h"
#include "llvm/Analysis/TargetLibraryInfo.h"
#include "llvm/Analysis/TypeBasedAliasAnalysis.h"
#include "llvm/Analysis/MemoryDependenceAnalysis.h"
#include "llvm/IR/DataLayout.h"
#include "llvm/IR/LegacyPassManager.h"
#include "llvm/IR/CFG.h"
#include "llvm/IR/Dominators.h"
#include "llvm/IR/Verifier.h"
#include "llvm/Support/CommandLine.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Transforms/AggressiveInstCombine/AggressiveInstCombine.h"
#include "llvm/Transforms/IPO.h"
#include "llvm/Transforms/IPO/ForceFunctionAttrs.h"
#include "llvm/Transforms/IPO/FunctionAttrs.h"
#include "llvm/Transforms/IPO/InferFunctionAttrs.h"
#include "llvm/Transforms/IPO/PassManagerBuilder.h"
#include "llvm/Transforms/InstCombine/InstCombine.h"
#include "llvm/Transforms/Instrumentation.h"
#include "llvm/Transforms/Scalar.h"
#include "llvm/Transforms/Scalar/GVN.h"
#include "llvm/Transforms/Scalar/InstSimplifyPass.h"
#include "llvm/Transforms/Scalar/SimpleLoopUnswitch.h"
#include "llvm/Transforms/Utils.h"
#include "llvm/Transforms/Utils/Cloning.h"
#include "llvm/Transforms/Vectorize.h"
#include <llvm/InitializePasses.h>

#include "llvm/Transforms/IPO/AlwaysInliner.h"

#include "Passes/ClassifyingAliasAnalysisPass.h"
#include "Passes/ConstantConcretizationPass.h"
#include "Passes/ControlledNodeSplittingPass.h"
#include "Passes/generator_jit_sl_function.h"
#include "Passes/generator_jit_ast_compute.h"

#include "Utilities/magic_enum.hpp"

#include "remill/BC/Util.h"
#include "remill/BC/Optimizer.h"
#include <API/Remill/BC/Optimizer.h>
#include "remill/BC/Optimizer.h"
#include <API/RegionAPI/RegionAPI.h>
#include <API/OptimizationAPI/OptimizationAPI.h>
#include <API/OptimizationAPI/OptimizationUtils.h>
#include <API/Passes/Passes.h>
#include <API/LLVM/Analysis/LoopInfo.h>
#include <API/LLVM/Analysis/MemorySSA.h>
#include <API/LLVM/Transforms/Utils/Cloning.h>
#include "remill/Arch/Arch.h"
#include "remill/BC/ABI.h"
#include "remill/BC/Version.h"
#include "remill/OS/OS.h"
#include <API/Remill/Arch/Arch.h>
#include <API/Remill/Arch/Instruction.h>
#include <API/Remill/BC/IntrinsicTable.h>
#include <API/Remill/BC/InstructionLifter.h>
#include <API/Remill/BC/Util.h>
#include <API/Remill/Arch/Context.h>

#include "souper/SouperInst.h"
#include "souper/SouperCandidates.h"
#include "souper/SouperExprBuilder.h"

using namespace llvm::sl;

/*
namespace Dna::API {
	DNA_EXPORT PassManagerBuilder* PassManagerBuilder_Constructor()
	{
		printf("assadsddsasddads");
		return new PassManagerBuilder();
	}

	DNA_EXPORT void PassManagerBuilder_PopulateFunctionPassManager(PassManagerBuilder* pmb, llvm::legacy::FunctionPassManager* fpm)
	{
		pmb->populateFunctionPassManager(*fpm);
	}

	DNA_EXPORT void PassManagerBuilder_PopulateModulePassManager(PassManagerBuilder* pmb, PassManagerBase* mpm)
	{
		pmb->populateModulePassManager(*mpm);
	}
}
*/