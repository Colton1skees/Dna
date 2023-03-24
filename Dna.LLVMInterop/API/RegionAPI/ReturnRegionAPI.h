#pragma once

#include "Passes/generator_jit_sl_function.h"
#include "API/ExportDef.h"
using namespace llvm::sl;

namespace Dna::API {
	DNA_EXPORT llvm::ReturnInst* ReturnRegionGetLLVMReturnInstruction(RegionReturn* region)
	{
		return region->get_return_inst();
	}
}