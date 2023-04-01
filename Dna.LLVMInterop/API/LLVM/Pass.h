#pragma once

#include <llvm/Pass.h>
#include <llvm/Analysis/LoopPass.h>
#include <API/ExportDef.h>

using namespace llvm;

namespace Dna::API {
	DNA_EXPORT llvm::PassKind Pass_GetPassKind(llvm::Pass* pass)
	{
		return pass->getPassKind();
	}

	DNA_EXPORT char* Pass_GetPassName(llvm::Pass* pass)
	{
		return _strdup(pass->getPassName().str().c_str());
	}

	DNA_EXPORT bool ModulePass_RunOnModule(llvm::ModulePass* pass, llvm::Module* module)
	{
		return pass->runOnModule(*module);
	}

	DNA_EXPORT bool FunctionPass_RunOnFunction(llvm::FunctionPass* pass, llvm::Function* function)
	{
		return pass->runOnFunction(*function);
	}
}