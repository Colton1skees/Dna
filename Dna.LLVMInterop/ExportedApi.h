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

extern "C" __declspec(dllexport) void OptimizeModule(llvm::Module * module)
{
	printf("received llvm module.");

	auto target = module->getFunction("SampleFunc");

	printf("got target.\n");
	auto name = target->getName().str().c_str();
	printf("printed target.");


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


	llvm::legacy::FunctionPassManager FPM(module);

	llvm::PassManagerBuilder PMB;
	llvm::legacy::PassManager module_manager;

	PMB.OptLevel = 3;
	PMB.SizeLevel = 2;
	PMB.DisableUnrollLoops = true; //!Guide.RunLoopPasses;
	PMB.RerollLoops = false;
	PMB.SLPVectorize = false;
	PMB.LoopVectorize = false;

	// Add the alias analysis passes.
	FPM.add(llvm::createCFLSteensAAWrapperPass());
	// FPM.add(llvm::createCFLAndersAAWrapperPass());
	FPM.add(llvm::createTypeBasedAAWrapperPass());
	FPM.add(llvm::createScopedNoAliasAAWrapperPass());
	FPM.add(Dna::Passes::createSegmentsAAWrapperPass());
	FPM.add(new Dna::Passes::SegmentsExternalAAWrapperPass());
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

	PMB.populateFunctionPassManager(FPM);
	PMB.populateModulePassManager(module_manager);


	auto f = (module->getFunction("SampleFunc"));

	FPM.doInitialization();
	FPM.run(*f);
	FPM.doFinalization();

	module_manager.run(*f->getParent());
	printf("done");
}