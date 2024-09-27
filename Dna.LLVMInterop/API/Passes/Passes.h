#pragma once 

#include <Passes/ControlFlowStructuringPass.h>
#include <Passes/ControlledNodeSplittingPass.h>
#include <Passes/JumpTableAnalysisPass.h>

#include "API/ExportDef.h"

namespace Dna::API {
	DNA_EXPORT llvm::sl::ControlledNodeSplittingPass* CreateControlledNodeSplittingPass()
	{
		return new llvm::sl::ControlledNodeSplittingPass();
	}

	DNA_EXPORT llvm::FunctionPass* CreateUnSwitchPass()
	{
		return llvm::createLowerSwitchPass();
	}

	DNA_EXPORT llvm::sl::LoopExitEnumerationPass* CreateLoopExitEnumerationPass()
	{
		return new llvm::sl::LoopExitEnumerationPass();
	}

	DNA_EXPORT Dna::Passes::ControlFlowStructuringPass* CreateControlFlowStructuringPass(Dna::Passes::tStructureFunction structureFunction)
	{
		return new Dna::Passes::ControlFlowStructuringPass(structureFunction);
	}

	DNA_EXPORT llvm::FunctionPass* CreateJumpTableAnalysisPass(Dna::Passes::tAnalyzeJumpTableBounds analyzeJumpTableBounds, Dna::Passes::tTrySolveConstant trySolveConstant)
	{
		return new Dna::Passes::JumpTableAnalysisPass(analyzeJumpTableBounds, trySolveConstant);
	}
}