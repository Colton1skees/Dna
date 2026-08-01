#pragma once

#include <llvm/IR/Module.h>
#include "llvm/ADT/SmallVector.h"
#include "llvm/TargetParser/Triple.h"
#include "llvm/Analysis/BasicAliasAnalysis.h"
#include "llvm/Analysis/ScalarEvolutionAliasAnalysis.h"
#include "llvm/Analysis/GlobalsModRef.h"
#include "llvm/Analysis/InlineCost.h"
#include "llvm/Analysis/Passes.h"
#include "llvm/Analysis/ScopedNoAliasAA.h"
#include "llvm/Analysis/TargetLibraryInfo.h"
#include "llvm/Analysis/TypeBasedAliasAnalysis.h"
#include "llvm/Analysis/MemoryDependenceAnalysis.h"
#include "llvm/Analysis/DemandedBits.h"
#include "llvm/IR/DataLayout.h"
#include "llvm/IR/Instructions.h"
#include "llvm/IR/LegacyPassManager.h"
#include "llvm/IR/Verifier.h"
#include "llvm/Support/CommandLine.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Transforms/AggressiveInstCombine/AggressiveInstCombine.h"
#include "llvm/Transforms/IPO.h"
#include "llvm/Transforms/IPO/ForceFunctionAttrs.h"
#include "llvm/Transforms/IPO/FunctionAttrs.h"
#include "llvm/Transforms/IPO/InferFunctionAttrs.h"
#include "llvm/Transforms/InstCombine/InstCombine.h"
#include "llvm/Transforms/Scalar.h"
#include "llvm/Transforms/Scalar/GVN.h"
#include "llvm/Transforms/Scalar/NewGVN.h"
#include "llvm/Transforms/Scalar/EarlyCSE.h"
#include "llvm/Transforms/Scalar/SCCP.h"
#include "llvm/Transforms/IPO/SCCP.h"
#include "llvm/Transforms/Utils/Mem2Reg.h"
#include "llvm/Transforms/Utils/LowerSwitch.h"
#include "llvm/Transforms/Utils/LoopSimplify.h"
#include "llvm/Transforms/Utils/LCSSA.h"
#include "llvm/Transforms/Utils/FixIrreducible.h"
#include "llvm/Transforms/Scalar/InstSimplifyPass.h"
#include "llvm/Transforms/Scalar/IndVarSimplify.h"
#include "llvm/Transforms/Scalar/SimpleLoopUnswitch.h"
#include "llvm/Transforms/Scalar/JumpThreading.h"
#include "llvm/Transforms/Scalar/CorrelatedValuePropagation.h"
#include "llvm/Transforms/Scalar/CallSiteSplitting.h"
#include "llvm/Transforms/Scalar/SpeculativeExecution.h"
#include "llvm/Transforms/Scalar/BDCE.h"
#include "llvm/Transforms/Scalar/ADCE.h"
#include "llvm/Transforms/Scalar/Reassociate.h"
#include "llvm/Transforms/Scalar/LoopSimplifyCFG.h"
#include "llvm/Transforms/Scalar/SimplifyCFG.h"
#include "llvm/Transforms/Scalar/LoopPassManager.h"
#include "llvm/Transforms/Scalar/LoopUnrollPass.h"
#include "llvm/Transforms/Scalar/SROA.h"
#include "llvm/Transforms/Utils.h"
#include "llvm/Transforms/Utils/Cloning.h"
#include "llvm/Transforms/Scalar/DeadStoreElimination.h"
#include <llvm/InitializePasses.h>
#include "llvm/Transforms/IPO/AlwaysInliner.h"
#include "llvm/Transforms/IPO/GlobalOpt.h"
#include "llvm/Transforms/IPO/FunctionAttrs.h"
#include "llvm/Transforms/Scalar/CorrelatedValuePropagation.h"
#include "llvm/Transforms/Scalar/TailRecursionElimination.h"

#include "llvm/Passes/PassBuilder.h"
#include "Passes/ClassifyingAliasAnalysisPass.h"
#include "Passes/ConstantConcretizationPass.h"
#include "Passes/ControlledNodeSplittingPass.h"
#include "Passes/generator_jit_sl_function.h"
#include "Passes/generator_jit_ast_compute.h"
#include "Passes/ControlFlowStructuringPass.h"
#include "Passes/JumpTableAnalysisPass.h"

#include "Utilities/magic_enum.hpp"

#include <API/RegionAPI/RegionAPI.h>
#include "API/ExportDef.h"
#include <tuple>
#include <chrono>
using namespace llvm::sl;


namespace Dna::Pipeline
{
	int constCount = 0;


	int count = 0;

	void InitializePasses()
	{
		// Initialize passes.
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		llvm::initializeCore(Registry);
		llvm::initializeCodeGen(Registry);
		initializeLoopStrengthReducePass(Registry);
		initializeLowerIntrinsicsPass(Registry);
		initializeUnreachableBlockElimLegacyPassPass(Registry);
		initializeConstantHoistingLegacyPassPass(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeVectorization(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
		llvm::initializeGlobalsAAWrapperPassPass(Registry);
		llvm::initializeGVNLegacyPassPass(Registry);
		llvm::initializeDependenceAnalysisWrapperPassPass(Registry);

		llvm::initializeLoopInfoWrapperPassPass(Registry);

		// For codegen passes, only passes that do IR to IR transformation are
		// supported.
		initializeScalarizeMaskedMemIntrinLegacyPassPass(Registry);
		initializeSelectOptimizePass(Registry);
		initializeCallBrPreparePass(Registry);
		initializeWinEHPreparePass(Registry);
		initializeDwarfEHPrepareLegacyPassPass(Registry);
		initializeSafeStackLegacyPassPass(Registry);
		initializeSjLjEHPreparePass(Registry);
		initializePreISelIntrinsicLoweringLegacyPassPass(Registry);
		initializeGlobalMergePass(Registry);
		initializeInterleavedLoadCombinePass(Registry);
		initializeInterleavedAccessPass(Registry);
		initializeUnreachableBlockElimLegacyPassPass(Registry);
		initializeExpandReductionsPass(Registry);
		initializeWasmEHPreparePass(Registry);
		initializeWriteBitcodePassPass(Registry);
		initializeReplaceWithVeclibLegacyPass(Registry);
		initializeJMCInstrumenterPass(Registry);


		initializeTarget(Registry);
	}

	void OptimizeModuleNew(llvm::Function* f)
	{
		InitializePasses();


		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		FPM.addPass(llvm::ReassociatePass());

		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::SpeculativeExecutionPass());
		FPM.addPass(llvm::JumpThreadingPass(99999));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());

		FPM.addPass(llvm::PromotePass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::SpeculativeExecutionPass());

		FPM.addPass(llvm::JumpThreadingPass(999999));
		FPM.addPass(llvm::JumpThreadingPass(-1));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		LPM.addPass(llvm::LoopSimplifyCFGPass());

		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());

		MPM.addPass(llvm::ModuleInlinerPass());

		// Add various optimization passes.
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::JumpThreadingPass());
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(llvm::createAggressiveInstCombinerPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::SROAPass({}));
		//FPM.addPass(llvm::createMergedLoadStoreMotionPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		// TODO: Insert custom passes for stack var elimination and load/store propagation
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		// TODO: Adhoc instruction combining

		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SCCPPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass());

		FPM.addPass(llvm::GVNHoistPass());

		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());

		FPM.addPass(llvm::SimplifyCFGPass());

		LPM.addPass(llvm::IndVarSimplifyPass());

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass());
		//FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());

		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

		FPM.run(*f, FAM);
	}

	void OptimizeModule(llvm::Module* module,
		llvm::Function* f,
		bool aggressiveUnroll,
		bool runClassifyingAliasAnalysis,
		Dna::Passes::tGetAliasResult getAliasResult,
		bool runConstantConcretization,
		Dna::Passes::tReadBinaryContents readBinaryContents,
		bool runStructuring,
		bool justGVN,
		Dna::Passes::tStructureFunction structureFunction)
	{
		// Run the "new" pipeline. The new pipeline is substantially more efficient, with a better order / selection of passes.
		// Though it is missing the passes intended for binary deobfuscation(namely binary load propagation, heuristic alias analysis, and enhanced store to load propagation).
		// Also note that the some of the passes are incorrectly named... (structure function is no longer a control flow structuring pass!, though it once was)
		OptimizeModuleNew(f);
		return;

		count++;
		/*
		const char* argv[7] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-dse-memoryssa-scanlimit=10000000",
		"-dse-memoryssa-partial-store-limit=10000000",
		"-memdep-block-scan-limit=500"
		};
		llvm::cl::ParseCommandLineOptions(7, argv);
		*/

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
			const char* argv[14] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-gvn-max-num-deps=25000000",
		"-dse-memoryssa-scanlimit=900000000",
		"-dse-memoryssa-partial-store-limit=90000000",
		"-gvn-max-block-speculations=90000000",
		"-memdep-block-scan-limit=1000000000",
		"-unroll-count=1500",
		"-unroll-threshold=100000000",
		"-enable-store-refinement=0",
		"-memssa-check-limit=99999999",
		"-memssa-check-limit=99999999"
			};
			llvm::cl::ParseCommandLineOptions(14, argv);
		}


		// Initialize passes.
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		llvm::initializeCore(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
		llvm::initializeGlobalsAAWrapperPassPass(Registry);
		llvm::initializeGVNLegacyPassPass(Registry);
		llvm::initializeDependenceAnalysisWrapperPassPass(Registry);
		//llvm::initializeIPSCCPLegacyPassPass(Registry);
		initializeTarget(Registry);

		// Create pass managers.
		llvm::FunctionPassManager FPM;
		//llvm::PassManagerBuilder PMB;
		llvm::legacy::PassManager module_manager;
		FunctionAnalysisManager FAM;
		ModulePassManager MPM;
		ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		// Configure pipeline.
	//	PMB.OptLevel = 3;
	//	PMB.SizeLevel = 2;
	//	PMB.DisableUnrollLoops = true; //!Guide.RunLoopPasses;
	//	PMB.SLPVectorize = false;
	//	PMB.LoopVectorize = false;


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
			//FPM.addPass(llvm::createGlobalsAAWrapperPass());
			//FPM.addPass(llvm::SCEVAAWrapperPass());
			//FPM.add(llvm::createTypeBasedAAWrapperPass());
			//MPM.addPass(llvm::ScopedNoAliasAAWrapperPass());
			//FPM.addPass(llvm::BasicAAWrapperPass());
			//FPM.addPass(llvm::createLCSSAPass());

			// TODO: Properly pass the alias analysis func ptr.
			Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			for (int i = 0; i < 100; i++)
			{
				printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
			}
			//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());

			//MPM.addPass(new Dna::Passes::SegmentsExternalAAWrapperPass());

			FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));

			//FPM.addPass(llvm::)

			//PMB.populateFunctionPassManager(FPM);
			//PMB.populateModulePassManager(module_manager);

			//FPM.doInitialization();

			//FPM.run(*f, FAM);

			MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));
			MPM.run(*f->getParent(), MAM);

			return;
		}
		// Add the alias analysis passes.
		// Note: CSE i
		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::EarlyCSEPass(true));
		//FPM.addPass(llvm::createGlobalsAAWrapperPass());
		//FPM.addPass(llvm::SCEVAAWrapperPass());
		//FPM.addPass(llvm::createTypeBasedAAWrapperPass());
		//FPM.addPass(llvm::createScopedNoAliasAAWrapperPass());
		//FPM.addPass(llvm::BasicAAWrapperPass());
		if (runClassifyingAliasAnalysis)
		{
			for (int i = 0; i < 100; i++)
			{
				printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
			}
			// TODO: Properly pass the alias analysis func ptr.
			//Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());

			//MPM.addPass(Dna::Passes::SegmentsExternalAAWrapperPass());
		}
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::SpeculativeExecutionPass());
		FPM.addPass(llvm::JumpThreadingPass(99999));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());


		//FPM.addPass(llvm::CallSiteSplittingPass());

		//FPM.addPass(llvm::createIPSCCPPass());

		//FPM.addPass(llvm::createCalledValuePropagationPass());
		FPM.addPass(llvm::PromotePass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::SpeculativeExecutionPass());

		//FPM.addPass(llvm::LazyValueInfo());
		FPM.addPass(llvm::JumpThreadingPass(999999));
		FPM.addPass(llvm::JumpThreadingPass(-1));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		LPM.addPass(llvm::LoopSimplifyCFGPass());


		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());


		// Add various optimization passes.
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::JumpThreadingPass());
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(llvm::createAggressiveInstCombinerPass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::SROAPass({}));
		//FPM.addPass(llvm::createMergedLoadStoreMotionPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass());

		FPM.addPass(llvm::GVNHoistPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		// Note: This legacy GVN pass is necessary for DSE to work properly.


		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SimplifyCFGPass());    // added
		FPM.addPass(llvm::InstCombinePass()); // added
		FPM.addPass(llvm::SimplifyCFGPass());    // added
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		//if (runConstantConcretization)
		//	FPM.addPass(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));

		if (true)
		{
			/*
				const char* args2[2] = { "testtwo", "-unroll-count=1500" };
				llvm::cl::ParseCommandLineOptions(2, args2);
				const char* args3[2] = { "testhree", "-unroll-threshold=100000000" };
				llvm::cl::ParseCommandLineOptions(2, args3);
				const char* args4[2] = { "testfiyr", "-memdep-block-scan-limit=1000000" };
				llvm::cl::ParseCommandLineOptions(2, args4);
				FPM.addPass(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));
				*/

				//LPM.addPass((llvm::LoopUnrollPass*)llvm::createLoopUnrollPass(3, false, false, 2000, -1, 1));
			FPM.addPass(llvm::LoopUnrollPass(llvm::LoopUnrollOptions(3, false, false)));
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
		LPM.addPass(llvm::IndVarSimplifyPass());
		//FPM.addPass(llvm::CreateConstraintEliminationPass());

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass());
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::ADCEPass());
		//FPM.addPass(llvm::createReassociatePass());

		// Note: We should avoid pointer PHIs here.
		if (false)
		{
			FPM.addPass(llvm::SimplifyCFGPass());
			//FPM.addPass(new llvm::sl::ControlledNodeSplittingPass());
			FPM.addPass(llvm::SimplifyCFGPass());
			//	FPM.addPass(llvm::sl::createUnswitchPass());
		}

		//FPM.addPass(llvm::createLoopRotatePass());


		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		//FPM.addPass(llvm::createFixIrreduciblePass());

		//FPM.addPass(llvm::createFixIrreduciblePass());
		//FPM.addPass(llvm::createGVNPass(false));
		//FPM.addPass(llvm::createNewGVNPass());
		//FPM.addPass(llvm::createStructurizeCFGPass());


		bool cns = false;
		if (cns)
		{
			FPM.addPass(llvm::SimplifyCFGPass());
			FPM.addPass(llvm::sl::ControlledNodeSplittingPass());
			FPM.addPass(llvm::sl::UnswitchPass());       // get rid of all switch instructions
			LPM.addPass(llvm::LoopSimplifyCFGPass());    // ensure all exit blocks are dominated by
			// the loop header
			//FPM.addPass(llvm::sl::LoopExitEnumerationPass());  // ensure all loops have <= 1 exits
			FPM.addPass(llvm::sl::UnswitchPass());       // get rid of all switch instructions
			// introduced by the loop exit enumeration

			FPM.addPass(llvm::sl::ControlledNodeSplittingPass());

			//	FPM.addPass(new Dna::Passes::ControlFlowStructuringPass());
		}

		printf("running.");
		//if (count == 13 || count == 14 || count == 15 || count == 16 || count == 17)
		if (structureFunction != nullptr)
		{
			printf("countcf.");
			//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
			FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		}

		//FPM.addPass(llvm::createFixIrreduciblePass());
		//FPM.addPass(llvm::createStructurizeCFGPass());

	//	PMB.populateFunctionPassManager(FPM);
	//	PMB.populateModulePassManager(module_manager);

		if (false)
		{
			//auto structuringPass = (llvm::sl::StructuredControlFlowPass*)llvm::sl::createASTComputePass();
			//MPM.addPass(llvm::sl::StructuredControlFlowPass());
		}



		try
		{
			MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));
			MPM.run(*f->getParent(), MAM);
			//f->dump();

			//FPM.run(*f, FAM);
		}

		catch (...)
		{
			printf("exception\n");
			//f->dump();
		}

		//module_manager.run(*f->getParent());
	}

	DNA_EXPORT void RunCfgCanonicalizationPipeline(llvm::Function* f)
	{
		// Run static pass initializers.
		InitializePasses();

		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		// Remove all switches. This simplifies analysis since we don't need to handle
		// cases where more than two case predecessors exist.
		FPM.addPass(llvm::LowerSwitchPass());
		// Remove irreducible control flow. Thus we only work with sane loops.
		// NOTE: It may be preferable to do controlled node splitting instead of FixIrreducible(which uses runtime indirection instead of node splitting).
		// The constraints would likely be easier to solve.
		FPM.addPass(llvm::FixIrreduciblePass());
		// Canonicalize the loop. Make sure all loops have dedicated exits(that is, no exit block for the loop has a predecessor
		// that is outside the loop. This implies that all exit blocks are dominated by the loop header.)
		FPM.addPass(llvm::LoopSimplifyPass());
		FPM.addPass(llvm::LCSSAPass());

		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM))); // Not needed anymore? 
		FPM.run(*f, FAM);
	}

	DNA_EXPORT void RunJumpTableSolvingPass(llvm::Function* f, Dna::Passes::tAnalyzeJumpTableBounds analyzeJumpTableBounds, Dna::Passes::tTrySolveConstant trySolveConstant)
	{
		// Run static pass initializers.
		InitializePasses();
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		//Registry.registerPass(Dna::Passes::JumpTableAnalysisPass);


		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		// Remove all switches. This simplifies analysis since we don't need to handle
		// cases where more than two case predecessors exist.
		FPM.addPass(Dna::Passes::JumpTableAnalysisPass(analyzeJumpTableBounds, trySolveConstant));

		//FAM.registerPass([analyzeJumpTableBounds, trySolveConstant] {return Dna::Passes::JumpTableAnalysisPass(analyzeJumpTableBounds, trySolveConstant); });
		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM))); // Not needed anymore? 
		FPM.run(*f, FAM);
	}
}

inline bool initialized = false;

void Initialize() {
	if (initialized)
		return;

	const char* argv[14] = { "mesa", "-simplifycfg-sink-common=false",
"-memdep-block-number-limit=10000000",
"-dse-memoryssa-defs-per-block-limit=10000000",
"-gvn-max-num-deps=25000000",
"-dse-memoryssa-scanlimit=900000000",
"-dse-memoryssa-partial-store-limit=90000000",
"-gvn-max-block-speculations=90000000",
"-memdep-block-scan-limit=1000000000",
"-unroll-count=3",
"-unroll-threshold=100000000",
"-enable-store-refinement=0",
"-memssa-check-limit=99999999",
"-memssa-check-limit=99999999"
	};
	llvm::cl::ParseCommandLineOptions(14, argv);

	// Initialize passes.
	llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
	llvm::initializeCore(Registry);
	llvm::initializeScalarOpts(Registry);
	llvm::initializeIPO(Registry);
	llvm::initializeAnalysis(Registry);
	llvm::initializeTransformUtils(Registry);
	llvm::initializeInstCombine(Registry);
	llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
	llvm::initializeGlobalsAAWrapperPassPass(Registry);
	llvm::initializeGVNLegacyPassPass(Registry);
	llvm::initializeDependenceAnalysisWrapperPassPass(Registry);
	//llvm::initializeIPSCCPLegacyPassPass(Registry);
	initializeTarget(Registry);


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


}


void OptimizeVmpModule(llvm::Module* module,
	llvm::Function* f,
	bool aggressiveUnroll,
	bool runClassifyingAliasAnalysis,
	Dna::Passes::tGetAliasResult getAliasResult,
	bool runConstantConcretization,
	Dna::Passes::tReadBinaryContents readBinaryContents,
	bool runStructuring,
	bool justGVN,
	Dna::Passes::tStructureFunction structureFunction,
	Dna::Passes::tEliminateStackVars eliminateStackVars,
	Dna::Passes::tStructureFunction adhocInstCombine,
	Dna::Passes::tEliminateStackVars multiUseCloning,
	bool fastPipeline)
{
	Initialize();



	// Create pass managers.
	llvm::FunctionPassManager FPM;
	//llvm::PassManagerBuilder PMB;
	llvm::legacy::PassManager module_manager;
	llvm::LoopAnalysisManager LAM;
	llvm::FunctionAnalysisManager FAM;
	llvm::CGSCCPassManager CGPM;
	llvm::CGSCCAnalysisManager CGAM;
	llvm::ModulePassManager MPM;
	llvm::ModuleAnalysisManager MAM;
	llvm::LoopPassManager LPM;
	llvm::PassBuilder PB;




	FPM.addPass(llvm::SROAPass({}));
	if (llvm::any_of(*f, [](const llvm::BasicBlock& block)
		{ return llvm::isa<llvm::SwitchInst>(block.getTerminator()); }))
	{
		FPM.addPass(llvm::LowerSwitchPass());
	}


	if (eliminateStackVars != nullptr)
	{
		FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));
	}

	FPM.addPass(llvm::SimplifyCFGPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	FPM.addPass(llvm::EarlyCSEPass(true));

	// Skip the remaining passes if we are
	if (fastPipeline)
		goto execute;

	MPM.addPass(llvm::IPSCCPPass());
	MPM.addPass(llvm::GlobalOptPass());
	CGPM.addPass(llvm::PostOrderFunctionAttrsPass());
	MPM.addPass(llvm::createModuleToPostOrderCGSCCPassAdaptor(std::move(CGPM)));

	FPM.addPass(llvm::DSEPass());
	FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::EarlyCSEPass(true));

	FPM.addPass(llvm::TailCallElimPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	FPM.addPass(llvm::ReassociatePass());
	FPM.addPass(llvm::DSEPass());

	FPM.addPass(llvm::NewGVNPass());

	FPM.addPass(llvm::SCCPPass());
	// Using this pass to add nsw/nuw annotations to instructions
	FPM.addPass(llvm::CorrelatedValuePropagationPass());

	FPM.addPass(llvm::JumpThreadingPass(99999));

	// Use multi-use cloning to disable InstCombine's single use checks
	if (multiUseCloning != nullptr)
	{
		FPM.addPass(Dna::Passes::MultiUseCloningPass(multiUseCloning));
	}

	FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::GVNPass());
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());

	FPM.addPass(llvm::LoopSimplifyPass());
	LPM.addPass(llvm::LoopSimplifyCFGPass());
	LPM.addPass(llvm::LoopRotatePass());
	LPM.addPass(llvm::LICMPass(500, 500, true));
	LPM.addPass(llvm::IndVarSimplifyPass());
	LPM.addPass(llvm::LoopDeletionPass());

	FPM.addPass(llvm::BDCEPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());


execute:

	try
	{
		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

		FPM.run(*f, FAM);
		MPM.run(*f->getParent(), MAM);
	}

	catch (...)
	{
		printf("Exception in pass pipeline!\n");

	}
}


void OptimizeVmpModuleOld(llvm::Module* module,
	llvm::Function* f,
	bool aggressiveUnroll,
	bool runClassifyingAliasAnalysis,
	Dna::Passes::tGetAliasResult getAliasResult,
	bool runConstantConcretization,
	Dna::Passes::tReadBinaryContents readBinaryContents,
	bool runStructuring,
	bool justGVN,
	Dna::Passes::tStructureFunction structureFunction,
	Dna::Passes::tEliminateStackVars eliminateStackVars,
	Dna::Passes::tStructureFunction adhocInstCombine,
	Dna::Passes::tEliminateStackVars multiUseCloning,
	bool fastPipeline)
{
	Initialize();



	// Create pass managers.
	llvm::FunctionPassManager FPM;
	//llvm::PassManagerBuilder PMB;
	llvm::legacy::PassManager module_manager;
	llvm::LoopAnalysisManager LAM;
	llvm::FunctionAnalysisManager FAM;
	llvm::CGSCCAnalysisManager CGAM;
	llvm::ModulePassManager MPM;
	llvm::ModuleAnalysisManager MAM;
	llvm::LoopPassManager LPM;
	llvm::PassBuilder PB;


	if (fastPipeline) {
		FPM.addPass(llvm::SROAPass({}));
		//FPM.addPass(llvm::EarlyCSEPass(true));

		if (eliminateStackVars != nullptr)
		{
			FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));
		}
		//FPM.addPass(llvm::SCCPPass());
		//FPM.addPass(llvm::InstSimplifyPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());

		try
		{
			FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
			PB.registerModuleAnalyses(MAM);
			PB.registerCGSCCAnalyses(CGAM);
			PB.registerFunctionAnalyses(FAM);
			PB.registerLoopAnalyses(LAM);
			PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

			//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

			FPM.run(*f, FAM);
		}

		catch (...)
		{
			printf("Exception in pass pipeline!\n");

		}

		return;
	}

	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::BDCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	//FPM.addPass(llvm::EarlyCSEPass(true));
	FPM.addPass(llvm::ReassociatePass());
	//FPM.addPass(llvm::EarlyCSEPass(true));

	if (structureFunction != nullptr)
	{
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (runClassifyingAliasAnalysis)
	{
		for (int i = 0; i < 10; i++)
		{
			printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
		}
		// TODO: Properly pass the alias analysis func ptr.
		//Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;
		//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());
		//MPM.addPass(Dna::Passes::SegmentsExternalAAWrapperPass());
	}
	FPM.addPass(llvm::EarlyCSEPass(true));
	FPM.addPass(llvm::SpeculativeExecutionPass());
	FPM.addPass(llvm::JumpThreadingPass(99999));
	FPM.addPass(llvm::CorrelatedValuePropagationPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	FPM.addPass(llvm::ReassociatePass());

	FPM.addPass(llvm::LoopSimplifyPass());
	LPM.addPass(llvm::LoopSimplifyCFGPass());
	LPM.addPass(llvm::LICMPass(500, 500, true)); // TODO: LICM
	LPM.addPass(llvm::LoopRotatePass());

	FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::LCSSAPass());
	LPM.addPass(llvm::IndVarSimplifyPass());
	LPM.addPass(llvm::LoopDeletionPass());

	FPM.addPass(llvm::PromotePass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::SpeculativeExecutionPass());

	//FPM.addPass(llvm::LazyValueInfo());
	FPM.addPass(llvm::JumpThreadingPass(999999));
	FPM.addPass(llvm::JumpThreadingPass(-1));
	FPM.addPass(llvm::CorrelatedValuePropagationPass());

	if (multiUseCloning != nullptr)
	{
		FPM.addPass(Dna::Passes::MultiUseCloningPass(multiUseCloning));
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::BDCEPass());
	}

	//FPM.addPass(FunctionDumpPass("test.ll"));
	FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::BDCEPass());



	MPM.addPass(llvm::ModuleInlinerPass());


	// Add various optimization passes.
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::JumpThreadingPass());
	FPM.addPass(llvm::CorrelatedValuePropagationPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	//FPM.addPass(llvm::createAggressiveInstCombinerPass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::ReassociatePass());
	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::NewGVNPass());
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());

	if (structureFunction != nullptr)
	{
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		FPM.addPass(llvm::ADCEPass());
	}

	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());
	if (eliminateStackVars != nullptr)
	{
		FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));
		FPM.addPass(llvm::ADCEPass());
	}

	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());
	if (adhocInstCombine != nullptr)
	{
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
	}

	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SCCPPass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::DSEPass());

	FPM.addPass(llvm::GVNHoistPass());
	//FPM.addPass(llvm::NewGVNPass());
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	// Note: This legacy GVN pass is necessary for DSE to work properly.


	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());

	if (adhocInstCombine != nullptr)
	{
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
	}

	//FPM.addPass(llvm::DSEPass()); // added
	FPM.addPass(llvm::SimplifyCFGPass());    // added
	//FPM.addPass(llvm::InstCombinePass()); // added
	//FPM.addPass(llvm::SimplifyCFGPass());    // added
	//FPM.addPass(llvm::DSEPass()); // added
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	//if (runConstantConcretization)
	//	FPM.addPass(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
	//FPM.addPass(llvm::DSEPass()); // added
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));


	LPM.addPass(llvm::IndVarSimplifyPass());
	//FPM.addPass(llvm::CreateConstraintEliminationPass());

	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::EarlyCSEPass());
	//FPM.addPass(llvm::DSEPass()); // added
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	//FPM.addPass(llvm::createReassociatePass());

	// Note: We should avoid pointer PHIs here.
	if (false)
	{
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(new llvm::sl::ControlledNodeSplittingPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//	FPM.addPass(llvm::sl::createUnswitchPass());
	}

	//FPM.addPass(llvm::createLoopRotatePass());
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	//FPM.addPass(llvm::createFixIrreduciblePass());
	//FPM.addPass(llvm::createFixIrreduciblePass());
	//FPM.addPass(llvm::createGVNPass(false));
	//FPM.addPass(llvm::createNewGVNPass());
	//FPM.addPass(llvm::createStructurizeCFGPass());

	if (structureFunction != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (eliminateStackVars != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (adhocInstCombine != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
		FPM.addPass(llvm::ADCEPass());
	}


	try
	{
		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

		FPM.run(*f, FAM);
	}

	catch (...)
	{
		printf("Exception in pass pipeline!\n");

	}

	//module_manager.run(*f->getParent());
}

DNA_EXPORT void OptimizeModuleVmp(llvm::Module* module,
	llvm::Function* f,
	bool aggressiveUnroll,
	bool runClassifyingAliasAnalysis,
	Dna::Passes::tGetAliasResult getAliasResult,
	bool runConstantConcretization,
	Dna::Passes::tReadBinaryContents readBinaryContents,
	bool runStructuring,
	bool justGVN,
	Dna::Passes::tStructureFunction structureFunction,
	Dna::Passes::tEliminateStackVars eliminateStackVars,
	Dna::Passes::tStructureFunction adhocInstCombine,
	Dna::Passes::tEliminateStackVars multiUseCloning,
	bool fastPipeline)
{
	OptimizeVmpModule(module, f, aggressiveUnroll, runClassifyingAliasAnalysis, getAliasResult, runConstantConcretization, readBinaryContents, runStructuring, justGVN, structureFunction, eliminateStackVars, adhocInstCombine, multiUseCloning, fastPipeline);
}
