#pragma once

#include "API/ExportDef.h"
#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/Arch.h"
#include "remill/BC/Optimizer.h"
#include <remill/BC/Optimizer.h>


// Remill Optimizer 
namespace Dna::API {
	DNA_EXPORT void OptimizeModule(const remill::Arch* arch, llvm::Module* module, llvm::Function** funcArray, int functionsCount)
	{
		std::vector<llvm::Function*> functions;
		for (int i = 0; i < functionsCount; i++)
			functions.push_back(funcArray[i]);

		remill::OptimizeModule(arch, module, functions);
	}

	DNA_EXPORT void OptimizeBareModule(llvm::Module* module)
	{
		remill::OptimizeBareModule(module);
	}

}
