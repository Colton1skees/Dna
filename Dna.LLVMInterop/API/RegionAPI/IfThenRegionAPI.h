#pragma once

#include "Passes/generator_jit_sl_function.h"
#include "API/ExportDef.h"
using namespace llvm::sl;

namespace Dna::API {
	DNA_EXPORT Region* IfThenRegionGetThen(RegionIfThen* region)
	{
		return region->getThen();
	}

	DNA_EXPORT bool IfThenRegionGetIsNegated(RegionIfThen* region)
	{
		return region->isNegated();
	}

	DNA_EXPORT llvm::Instruction* IfThenRegionGetLLVMTerminatorInstruction(RegionIfThen* region)
	{
		return region->get_terminator_inst();
	}
}