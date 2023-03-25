#pragma once

#include <llvm/IR/Module.h>
#include "llvm-c/Transforms/PassManagerBuilder.h"
#include "llvm/ADT/SmallVector.h"
#include "llvm/ADT/Triple.h"
#include "llvm/Analysis/BasicAliasAnalysis.h"
#include "llvm/Analysis/CFLAndersAliasAnalysis.h"
#include "llvm/Analysis/CFLSteensAliasAnalysis.h"
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

#include <API/RegionAPI/RegionAPI.h>

using namespace llvm::sl;

namespace Dna::Pipeline
{
	void OptimizeModule(llvm::Module* module,
		llvm::Function* f,
		bool aggressiveUnroll,
		bool runClassifyingAliasAnalysis,
		Dna::Passes::tGetAliasResult getAliasResult,
		bool runConstantConcretization, 
		Dna::Passes::tReadBinaryContents readBinaryContents,
		bool runStructuring)
	{
		// Initialize passes.
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		llvm::initializeCore(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeInstrumentation(Registry);
		llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
		initializeTarget(Registry);

		// Create pass managers.
		llvm::legacy::FunctionPassManager FPM(module);
		llvm::PassManagerBuilder PMB;
		llvm::legacy::PassManager module_manager;

		// Configure pipeline.
		PMB.OptLevel = 3;
		PMB.SizeLevel = 2;
		PMB.DisableUnrollLoops = true; //!Guide.RunLoopPasses;
		PMB.RerollLoops = false;
		PMB.SLPVectorize = false;
		PMB.LoopVectorize = false;

		const char* args[2] = { "test", "-earlycse-mssa-optimization-cap=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args);

		const char* args4[2] = { "test4", "-dse-memoryssa-defs-per-block-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args4);

		const char* args5[2] = { "test5", "-dse-memoryssa-partial-store-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args5);

		const char* args6[2] = { "test6", "-dse-memoryssa-path-check-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args6);

		const char* args7[2] = { "test7", "-dse-memoryssa-scanlimit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args7);

		const char* args8[2] = { "test8", "-dse-memoryssa-walklimit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args8);

		const char* args9[2] = { "test9", "-dse-memoryssa-otherbb-cost=2" };
		llvm::cl::ParseCommandLineOptions(2, args9);

		//const char* args10[2] = { "test10", "memdep-block-number-limit=10000" };
		//llvm::cl::ParseCommandLineOptions(2, args10);

		const char* args11[2] = { "test11", "-memdep-block-scan-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args11);

		// Add the alias analysis passes.
		FPM.add(llvm::createReassociatePass());
		FPM.add(llvm::createCFLSteensAAWrapperPass());
		FPM.add(llvm::createTypeBasedAAWrapperPass());
		FPM.add(llvm::createScopedNoAliasAAWrapperPass());
		if (runClassifyingAliasAnalysis)
		{
			// TODO: Properly pass the alias analysis func ptr.
			Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			FPM.add(Dna::Passes::createSegmentsAAWrapperPass());

			FPM.add(new Dna::Passes::SegmentsExternalAAWrapperPass());
		}
		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createEarlyCSEPass());

		// Add various optimization passes.
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createEarlyCSEPass(true));
		FPM.add(llvm::createGVNHoistPass());
		FPM.add(llvm::createGVNSinkPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createJumpThreadingPass());
		FPM.add(llvm::createCorrelatedValuePropagationPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createAggressiveInstCombinerPass());
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createReassociatePass());

		FPM.add(llvm::createMergedLoadStoreMotionPass());
		FPM.add(llvm::createGVNPass(false));
		FPM.add(llvm::createBitTrackingDCEPass());
		FPM.add(llvm::createAggressiveDCEPass());
		FPM.add(llvm::createDeadStoreEliminationPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createDeadStoreEliminationPass()); // added
		FPM.add(llvm::createCFGSimplificationPass());    // added
		FPM.add(llvm::createInstructionCombiningPass()); // added
		FPM.add(llvm::createCFGSimplificationPass());    // added
		FPM.add(llvm::createDeadStoreEliminationPass()); // added

		if(runConstantConcretization)
			FPM.add(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
		FPM.add(llvm::createDeadStoreEliminationPass()); // added

		if (aggressiveUnroll)
		{
			const char* args2[2] = { "testtwo", "-unroll-count=1500" };
			llvm::cl::ParseCommandLineOptions(2, args2);
			const char* args3[2] = { "testhree", "-unroll-threshold=100000000" };
			llvm::cl::ParseCommandLineOptions(2, args3);
			FPM.add(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));
		}


		FPM.add(llvm::createIndVarSimplifyPass());
		FPM.add(llvm::createConstraintEliminationPass());

		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createEarlyCSEPass());
		FPM.add(llvm::createDeadStoreEliminationPass()); // added
		FPM.add(llvm::createSCCPPass());
		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createAggressiveDCEPass());
		FPM.add(llvm::createReassociatePass());

		// Note: We should avoid pointer PHIs here.
		if (runStructuring)
		{
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createControlledNodeSplittingPass());
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createUnswitchPass());
		}


		PMB.populateFunctionPassManager(FPM);
		PMB.populateModulePassManager(module_manager);

		if (runStructuring)
		{
			auto structuringPass = (llvm::sl::StructuredControlFlowPass*)llvm::sl::createASTComputePass();
			module_manager.add(structuringPass);
		}


		FPM.doInitialization();
		FPM.run(*f);
		FPM.doFinalization();

		module_manager.run(*f->getParent());
	}
}