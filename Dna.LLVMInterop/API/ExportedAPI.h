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

void DumpRegion(llvm::sl::Region* region)
{

	switch (region->get_kind())
	{
	case Region::SK_SEQUENCE:
		auto sequence = static_cast<RegionSequence*>(region);

		for (auto i = 0; i < sequence->getChildSize(); i++)
		{
			auto child = sequence->getChild(i);

			auto str = magic_enum::enum_name(child->get_kind());

			std::printf(
				"%.*s",
				static_cast<int>(str.length()),
				str.data());

			printf("\n");
		}
		break;
	}
}

void DumpStructuredFunction(const llvm::sl::StructuredFunction& sfunc)
{
	auto rootRegion = sfunc.getBody();

	auto str = magic_enum::enum_name(rootRegion->get_kind());

	std::printf(
		"%.*s",
		static_cast<int>(str.length()),
		str.data());

	printf("\n");

	DumpRegion(rootRegion);
}

extern "C" __declspec(dllexport) llvm::sl::Region * OptimizeModule(llvm::Module * module, Dna::Passes::tReadBinaryContents readBinaryContents)
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

	FPM.add(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
	FPM.add(llvm::createDeadStoreEliminationPass()); // added

	FPM.add(llvm::createSCEVAAWrapperPass());
	FPM.add(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));


	FPM.add(new Dna::Passes::SegmentsExternalAAWrapperPass());
	FPM.add(llvm::createSROAPass());
	FPM.add(llvm::createEarlyCSEPass());
	FPM.add(llvm::createDeadStoreEliminationPass()); // added
	FPM.add(llvm::createSCCPPass());
	FPM.add(llvm::createSROAPass());
	FPM.add(llvm::createAggressiveDCEPass());

	// Note: We should avoid pointer PHIs here.
	FPM.add(llvm::createCFGSimplificationPass());
	FPM.add(llvm::sl::createControlledNodeSplittingPass());
	FPM.add(llvm::createCFGSimplificationPass());
	FPM.add(llvm::sl::createUnswitchPass());

	PMB.populateFunctionPassManager(FPM);
	PMB.populateModulePassManager(module_manager);

	auto structuringPass = (llvm::sl::StructuredControlFlowPass*)llvm::sl::createASTComputePass();
	module_manager.add(structuringPass);

	auto f = (module->getFunction("SampleFunc"));

	const char* args[2] = { "test", "-memdep-block-scan-limit=10000000" };
	llvm::cl::ParseCommandLineOptions(2, args);

	const char* args2[2] = { "testtwo", "-unroll-count=1500" };
	llvm::cl::ParseCommandLineOptions(2, args2);

	const char* args3[2] = { "testhree", "-unroll-threshold=100000000" };
	llvm::cl::ParseCommandLineOptions(2, args3);

	//const char* args2[2] = { "testt", "-unroll-count=10000" };
	//llvm::cl::ParseCommandLineOptions(2, args2);

	FPM.doInitialization();
	FPM.run(*f);
	FPM.doFinalization();

	module_manager.run(*f->getParent());
	printf("done");


	auto structuredFunction = structuringPass->getStructuredFunction(f);

	printf("got structured function.");
	DumpStructuredFunction(*structuredFunction);

	auto root = structuredFunction->getBody();

	auto kind = root->get_kind();

	auto str = magic_enum::enum_name(kind);

	printf("foo \n");
	std::printf(
		"%.*s",
		static_cast<int>(str.length()),
		str.data());


	auto childCt = root->getChildSize();

	printf("The value of a : %zu", childCt);

	return root;
}