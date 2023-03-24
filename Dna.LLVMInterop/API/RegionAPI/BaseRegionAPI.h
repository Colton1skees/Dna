#pragma once

#include "Passes/generator_jit_sl_function.h"
#include "API/ExportDef.h"
#include "Utilities/magic_enum.hpp"
using namespace llvm::sl;

namespace Dna::API {
	extern "C" __declspec(dllexport) uint32_t RegionGetKind(Region* region)
	{
		return region->get_kind();
	}

	DNA_EXPORT Region* RegionGetOwnerRegion(Region* region)
	{
		return region->getOwnerRegion();
	}

	DNA_EXPORT const Region* RegionGetEntryRegionBlock(Region* region)
	{
		return region->getEntryRegionBlock();
	}

	DNA_EXPORT size_t RegionGetId(Region* region)
	{
		return region->get_id();
	}

	DNA_EXPORT size_t RegionGetPredCount(Region* region)
	{
		return region->pred_size();
	}

	DNA_EXPORT Region* RegionGetPred(Region* region, size_t index)
	{
		return region->get_pred(index);
	}

	DNA_EXPORT size_t RegionGetSuccCount(Region* region)
	{
		return region->succ_size();
	}

	DNA_EXPORT Region* RegionGetSucc(Region* region, size_t index)
	{
		return region->get_succ(index);
	}

	DNA_EXPORT llvm::BasicBlock* RegionGetLLVMBasicBlock(Region* region)
	{
		return region->get_bb();
	}

	DNA_EXPORT llvm::BasicBlock* RegionGetHeadLLVMBasicBlock(Region* region)
	{
		return region->get_head_bb();
	}

	DNA_EXPORT size_t RegionGetChildCount(Region* region)
	{
		region->getChildSize();
	}

	DNA_EXPORT Region* RegionGetChild(Region* region, size_t index)
	{
		return region->getChild(index);
	}
}