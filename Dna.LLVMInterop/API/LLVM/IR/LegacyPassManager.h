#pragma once

#include <llvm/Pass.h>
#include <llvm/IR/LegacyPassManager.h> 
#include <API/ExportDef.h>

namespace Dna::API {
	DNA_EXPORT llvm::legacy::PassManager* PassManager_Constructor()
	{
		return new llvm::legacy::PassManager();
	}

	DNA_EXPORT void PassManagerBase_Add(llvm::legacy::PassManagerBase* passManager, llvm::Pass* pass)
	{
		passManager->add(pass);
	}

	DNA_EXPORT bool PassManager_Run(llvm::legacy::PassManager* passManager, llvm::Module* mod)
	{
		return passManager->run(*mod);
	}

	DNA_EXPORT llvm::legacy::FunctionPassManager* FunctionPassManager_Constructor(llvm::Module* mod)
	{
		return new llvm::legacy::FunctionPassManager(mod);
	}

	DNA_EXPORT bool FunctionPassManager_Run(llvm::legacy::FunctionPassManager* passManager, llvm::Function* func)
	{
		return passManager->run(*func);
	}


	DNA_EXPORT bool FunctionPassManager_DoInitialization(llvm::legacy::FunctionPassManager* passManager)
	{
		return passManager->doInitialization();
	}

	DNA_EXPORT bool FunctionPassManager_DoFinalization(llvm::legacy::FunctionPassManager* passManager)
	{
		return passManager->doFinalization();
	}
}