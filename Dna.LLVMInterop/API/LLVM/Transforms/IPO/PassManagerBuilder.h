#pragma once

#include <llvm/IR/LegacyPassManager.h>
#include <llvm/Transforms/IPO/PassManagerBuilder.h>

#include <API/ExportDef.h>

using namespace llvm;
using namespace llvm::legacy;

namespace Dna::API {
	DNA_EXPORT PassManagerBuilder* PassManagerBuilder_Constructor()
	{
		return new PassManagerBuilder();
	}

	DNA_EXPORT void PassManagerBuilder_PopulateFunctionPassManager(PassManagerBuilder* pmb, llvm::legacy::FunctionPassManager* fpm)
	{
		pmb->populateFunctionPassManager(*fpm);
	}

	DNA_EXPORT void PassManagerBuilder_PopulateModulePassManager(PassManagerBuilder* pmb, PassManagerBase* mpm)
	{
		pmb->populateModulePassManager(*mpm);
	}
}