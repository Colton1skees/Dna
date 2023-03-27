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
		bool runStructuring,
		bool justGVN = false)
	{

		const char* argv[12] = { "mesa", "-simplifycfg-sink-common=false",
				"-memdep-block-number-limit=10000000",
				"-dse-memoryssa-defs-per-block-limit=10000000",
				"-gvn-max-num-deps=100000000",
				"-dse-memoryssa-scanlimit=10000000",
				"-dse-memoryssa-partial-store-limit=10000000",
				"-gvn-max-block-speculations=10000000",
				"-memdep-block-scan-limit=10000000",
				"-unroll-count=1500",
				"-unroll-threshold=100000000",
				"-enable-store-refinement=1"
		};
		llvm::cl::ParseCommandLineOptions(12, argv);

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
		llvm::initializeGlobalsAAWrapperPassPass(Registry);
		llvm::initializeGVNLegacyPassPass(Registry);
		llvm::initializeDependenceAnalysisWrapperPassPass(Registry);
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


		/*
		const char* args[2] = { "-dse-memoryssa-defs-per-block-limit=1000000", "-earlycse-mssa-optimization-cap=1000000" };
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

		const char* args13[2] = { "test13", "-gvn-max-num-deps=100000000" };
		llvm::cl::ParseCommandLineOptions(2, args13);

		const char* args14[2] = { "test14", "-gvn-max-block-speculations=10000000" };
		llvm::cl::ParseCommandLineOptions(2, args14);

		const char* args15[2] = { "test15", "-gvn-max-num-visited-insts=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args15);
		*/
		//const char* args10[2] = { "test10", "memdep-block-number-limit=10000" };
		//llvm::cl::ParseCommandLineOptions(2, args10);


		/*
		const char* args99999[11] = {
			"foo"
			"-memdep-block-number-limit=10000",
			"-dse-memoryssa-defs-per-block-limit=1000000",
			"-gvn-max-num-deps=100000000",
			"-dse-memoryssa-scanlimit=1000000",
			"-dse-memoryssa-otherbb-cost=2",
			"-dse-memoryssa-partial-store-limit=1000000",
			"-gvn-max-block-speculations=1000000",
			"-memdep-block-scan-limit=1000000",
			"-unroll-count=1500",
			"-unroll-threshold=100000000"
		};
		llvm::cl::ParseCommandLineOptions(11, args99999);
		*/

		const char* argv67[12] = { "mesa", "-simplifycfg-sink-common=false",
				"-memdep-block-number-limit=10000000",
				"-dse-memoryssa-defs-per-block-limit=10000000",
				"-gvn-max-num-deps=100000000",
				"-dse-memoryssa-scanlimit=10000000",
				"-dse-memoryssa-partial-store-limit=10000000",
				"-gvn-max-block-speculations=10000000",
				"-memdep-block-scan-limit=10000000",
				"-unroll-count=1500",
				"-unroll-threshold=100000000",
				"-enable-store-refinement=1"
		};
		llvm::cl::ParseCommandLineOptions(12, argv67);


		if (justGVN)
		{
			printf("just gvn. \n");
			//FPM.add(llvm::createCFLSteensAAWrapperPass());
			//FPM.add(llvm::createGlobalsAAWrapperPass());
			//FPM.add(llvm::createSCEVAAWrapperPass());
			//FPM.add(llvm::createTypeBasedAAWrapperPass());
			//FPM.add(llvm::createScopedNoAliasAAWrapperPass());
			FPM.add(llvm::createBasicAAWrapperPass());
			//FPM.add(llvm::createLCSSAPass());

			// TODO: Properly pass the alias analysis func ptr.
			Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			FPM.add(Dna::Passes::createSegmentsAAWrapperPass());

			FPM.add(new Dna::Passes::SegmentsExternalAAWrapperPass());

			FPM.add(llvm::createGVNPass(false));

			//FPM.add(llvm::)

			PMB.populateFunctionPassManager(FPM);
			PMB.populateModulePassManager(module_manager);

			FPM.doInitialization();

			FPM.run(*f);

			FPM.doFinalization();

			module_manager.run(*f->getParent());

			return;
		}
		// Add the alias analysis passes.
		// Note: CSE i
		FPM.add(llvm::createEarlyCSEPass(true));
		FPM.add(llvm::createReassociatePass());
		FPM.add(llvm::createEarlyCSEPass(true));
		FPM.add(llvm::createCFLSteensAAWrapperPass());
		//FPM.add(llvm::createGlobalsAAWrapperPass());
		FPM.add(llvm::createSCEVAAWrapperPass());
		//FPM.add(llvm::createTypeBasedAAWrapperPass());
		//FPM.add(llvm::createScopedNoAliasAAWrapperPass());
		FPM.add(llvm::createBasicAAWrapperPass());
		if (runClassifyingAliasAnalysis)
		{
			// TODO: Properly pass the alias analysis func ptr.
			Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			FPM.add(Dna::Passes::createSegmentsAAWrapperPass());

			FPM.add(new Dna::Passes::SegmentsExternalAAWrapperPass());
		}
		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createEarlyCSEPass(true));
		FPM.add(llvm::createSpeculativeExecutionPass());
		FPM.add(llvm::createJumpThreadingPass(99999));
		FPM.add(llvm::createCorrelatedValuePropagationPass());
		FPM.add(llvm::createCFGSimplificationPass());


		// Add various optimization passes.
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createJumpThreadingPass());
		FPM.add(llvm::createCorrelatedValuePropagationPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createAggressiveInstCombinerPass());
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createReassociatePass());
		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createMergedLoadStoreMotionPass());
		FPM.add(llvm::createNewGVNPass());
		FPM.add(llvm::createSCCPPass());
		FPM.add(llvm::createBitTrackingDCEPass());
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createDeadStoreEliminationPass());

		FPM.add(llvm::createGVNHoistPass());
		FPM.add(llvm::createNewGVNPass());
		FPM.add(llvm::createGVNPass(false));
		// Note: This legacy GVN pass is necessary for DSE to work properly.


		FPM.add(llvm::createAggressiveDCEPass());
		FPM.add(llvm::createCFGSimplificationPass());
		FPM.add(llvm::createInstructionCombiningPass());
		FPM.add(llvm::createDeadStoreEliminationPass()); // added
		FPM.add(llvm::createCFGSimplificationPass());    // added
		FPM.add(llvm::createInstructionCombiningPass()); // added
		FPM.add(llvm::createCFGSimplificationPass());    // added
		FPM.add(llvm::createDeadStoreEliminationPass()); // added

		if (runConstantConcretization)
			FPM.add(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
		FPM.add(llvm::createDeadStoreEliminationPass()); // added


		if (aggressiveUnroll)
		{
			/*
				const char* args2[2] = { "testtwo", "-unroll-count=1500" };
				llvm::cl::ParseCommandLineOptions(2, args2);
				const char* args3[2] = { "testhree", "-unroll-threshold=100000000" };
				llvm::cl::ParseCommandLineOptions(2, args3);
				const char* args4[2] = { "testfiyr", "-memdep-block-scan-limit=1000000" };
				llvm::cl::ParseCommandLineOptions(2, args4);
				FPM.add(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));
				*/

			FPM.add(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));
		}

		//const char* args17[2] = { "testfiyr", "-memdep-block-scan-limit=1000000" };
		//llvm::cl::ParseCommandLineOptions(2, args17);

		/*
		else
		{
			const char* args11[2] = { "testeleven", "-memdep-block-scan-limit=1000000" };
			bool succ = llvm::cl::ParseCommandLineOptions(2, args11);
			if (!succ)
			{
				printf("oh no.");
			}
		}
		*/
		FPM.add(llvm::createIndVarSimplifyPass());
		FPM.add(llvm::createConstraintEliminationPass());

		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createEarlyCSEPass());
		FPM.add(llvm::createDeadStoreEliminationPass()); // added
		FPM.add(llvm::createSCCPPass());
		FPM.add(llvm::createSROAPass());
		FPM.add(llvm::createAggressiveDCEPass());
		//FPM.add(llvm::createReassociatePass());

		// Note: We should avoid pointer PHIs here.
		if (false)
		{
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createControlledNodeSplittingPass());
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createUnswitchPass());
		}

		FPM.add(llvm::createLoopRotatePass());
		bool cns = false;
		if (cns)
		{
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createControlledNodeSplittingPass());
			FPM.add(llvm::createCFGSimplificationPass());
			FPM.add(llvm::sl::createUnswitchPass());       // get rid of all switch instructions
			FPM.add(llvm::createLoopSimplifyCFGPass());    // ensure all exit blocks are dominated by
			// the loop header
			FPM.add(llvm::sl::createLoopExitEnumerationPass());  // ensure all loops have <= 1 exits
			FPM.add(llvm::sl::createUnswitchPass());       // get rid of all switch instructions
			// introduced by the loop exit enumeration
		}


		FPM.add(llvm::createFixIrreduciblePass());

		//FPM.add(llvm::createFixIrreduciblePass());
		//FPM.add(llvm::createGVNPass(false));
		//FPM.add(llvm::createNewGVNPass());
		//FPM.add(llvm::createStructurizeCFGPass());

		PMB.populateFunctionPassManager(FPM);
		PMB.populateModulePassManager(module_manager);

		if (false)
		{
			auto structuringPass = (llvm::sl::StructuredControlFlowPass*)llvm::sl::createASTComputePass();
			module_manager.add(structuringPass);
		}


		FPM.doInitialization();

		try
		{
			//f->dump();

			FPM.run(*f);
		}

		catch(...)
		{
			printf("excasdesption\n");
			f->dump();
			printf("brasdasduh.");
		}
		FPM.doFinalization();

		module_manager.run(*f->getParent());
	}
}