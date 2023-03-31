#pragma once 

#include <Passes/ControlFlowStructuringPass.h>
#include <Passes/ControlledNodeSplittingPass.h>

#include "API/ExportDef.h"

namespace Dna::API {
	DNA_EXPORT llvm::FunctionPass* CreateControlledNodeSplittingPass()
	{
		return new llvm::sl::ControlledNodeSplittingPass();
	}

	DNA_EXPORT llvm::FunctionPass* CreateUnSwitchPass()
	{
		return new llvm::sl::UnswitchPass();
	}

	DNA_EXPORT llvm::FunctionPass* CreateLoopExitEnumerationPass()
	{
		return new llvm::sl::LoopExitEnumerationPass();
	}


	DNA_EXPORT llvm::FunctionPass* CreateControlFlowStructuringPass(Dna::Passes::tStructureFunction structureFunction)
	{
		return new Dna::Passes::ControlFlowStructuringPass(structureFunction);
	}
}