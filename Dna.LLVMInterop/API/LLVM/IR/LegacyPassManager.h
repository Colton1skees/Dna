#pragma once

#include <llvm/Pass.h>
#include <llvm/IR/LegacyPassManager.h> 
#include <API/ExportDef.h>

using namespace llvm::legacy;

namespace Dna::API {
	DNA_EXPORT PassManager* PassManager_Constructor()
	{
		return new PassManager();
	}

	DNA_EXPORT void PassManagerBase_Add(PassManagerBase* passManager, llvm::Pass* pass)
	{
		passManager->add(pass);
	}

	DNA_EXPORT void PassManager_Run(PassManager* passManager, llvm::Module* mod)
	{
		passManager->run(*mod);
	}

	DNA_EXPORT FunctionPassManager* FunctionPassManager_Constructor(llvm::Module* mod)
	{
		return new FunctionPassManager(mod);
	}

	DNA_EXPORT void FunctionPassManager_DoInitialization(FunctionPassManager* passManager)
	{
		passManager->doInitialization();
	}

	DNA_EXPORT void FunctionPassManager_DoFinalization(FunctionPassManager* passManager)
	{
		passManager->doFinalization();
	}
}