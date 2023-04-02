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
}