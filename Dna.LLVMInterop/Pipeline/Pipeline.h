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
#include "Passes/ControlFlowStructuringPass.h"

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

		/*
		const char* argv[7] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-dse-memoryssa-scanlimit=10000000",
		"-dse-memoryssa-partial-store-limit=10000000",
		"-memdep-block-scan-limit=500"
		};
		llvm::cl::ParseCommandLineOptions(7, argv);


		justGVN = false;
		if (justGVN)
		{
			const char* argv[13] = { "mesa", "-simplifycfg-sink-common=false",
					"-memdep-block-number-limit=10000000",
					"-dse-memoryssa-defs-per-block-limit=10000000",
					"-gvn-max-num-deps=100000000",
					"-dse-memoryssa-scanlimit=10000000",
					"-dse-memoryssa-partial-store-limit=10000000",
					"-gvn-max-block-speculations=10000000",
					"-memdep-block-scan-limit=28000",
					"-unroll-count=1500",
					"-unroll-threshold=100000000",
					"-enable-store-refinement=1"
			};
			llvm::cl::ParseCommandLineOptions(12, argv);
		}

		else

		{
			const char* argv[12] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-gvn-max-num-deps=25000000",
		"-dse-memoryssa-scanlimit=900000000",
		"-dse-memoryssa-partial-store-limit=90000000",
		"-gvn-max-block-speculations=90000000",
		"-memdep-block-scan-limit=1000000000",
		"-unroll-count=1500",
		"-unroll-threshold=100000000",
		"-enable-store-refinement=0"
			};
			llvm::cl::ParseCommandLineOptions(12, argv);
		}
				*/

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

		FPM.add(llvm::createAggressiveDCEPass());

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

		catch (...)
		{
			printf("excasdesption\n");
			//f->dump();
			printf("brasdasduh.");
		}
		FPM.doFinalization();

		module_manager.run(*f->getParent());
	}
}