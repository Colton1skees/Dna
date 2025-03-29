#pragma once 

#include "API/ExportDef.h"
#include "Pipeline/Pipeline.h" 

namespace Dna::API {
	DNA_EXPORT void OptimizeLLVMModule(llvm::Module* module,
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
		Dna::Pipeline::OptimizeModule(module, f, aggressiveUnroll, runClassifyingAliasAnalysis, getAliasResult, runConstantConcretization, readBinaryContents, runStructuring, justGVN, structureFunction);
	}

	DNA_EXPORT void PrepareForCloning(llvm::Function* function, bool jumpThreading)
	{
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
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		// Demote ssa variables to stack vars
		FPM.addPass(llvm::RegToMemPass());
		// Apply jump threading to undo some of the critical edge splittingf
		if(jumpThreading)
			FPM.addPass(llvm::JumpThreadingPass(99999));

		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		// Invoke the passes
		FPM.run(*function, FAM);
	}

	DNA_EXPORT llvm::BasicBlock* CloneBasicBlock(llvm::BasicBlock* block)
	{
		llvm::ValueToValueMapTy VMap;
		auto clone = llvm::CloneBasicBlock(block, VMap, ".clone", block->getParent());

		/*
		for (auto& inst : *clone)
		{
			for (int i = 0; i < inst.getNumOperands(); i++)
			{
				if (VMap.find(inst.getOperand(i)) == VMap.end())
				{
					continue;
				}

				inst.setOperand(i, VMap[inst.getOperand(i)]);
			}
		}
		*/
		llvm::ValueToValueMapTy invMap;
		for (auto key : VMap)
		{
			llvm::outs() << "mapping: " << *key->first << " to: " << *key->second << "\n";
			//llvm::outs() << "mapping instruction from " << key->first->
			//invMap[key.second] = invMap[key.first];
		}

		


		printf("no remap");
		llvm::remapInstructionsInBlocks({clone}, VMap);

		return clone;
	}

	DNA_EXPORT bool MergeBlockIntoPredecessor(llvm::BasicBlock* block)
	{
		return llvm::MergeBlockIntoPredecessor(block);
	}
}