#pragma once

#include <llvm/IR/Instruction.h>
#include "llvm/IR/Attributes.h"
#include "llvm/IR/Function.h"
#include <llvm/Transforms/Utils/Cloning.h>
#include <remill/BC/Util.h>
#include <API/ExportDef.h>

namespace Dna::API {
	DNA_EXPORT llvm::Type* GetFunctionType(llvm::Function* func)
	{
		return func->getFunctionType();
	}

	// Invokes llvm::InlineFunction().
	// Returns nullptr if successful, error message ptr otherwise.
	DNA_EXPORT char* InlineFunction(llvm::Function* call)
	{
		call->removeFnAttr(llvm::Attribute::OptimizeNone);
		call->removeFnAttr(llvm::Attribute::NoInline);
		call->removeFnAttr(llvm::Attribute::ReadNone);
		call->removeFnAttr(llvm::Attribute::ReadOnly);
		call->removeFnAttr(llvm::Attribute::NoDuplicate);
		call->addFnAttr(llvm::Attribute::AlwaysInline);
		//return nullptr;

		for (auto caller : remill::CallersOf(call))
		{
			llvm::InlineFunctionInfo ifi;
			auto result = llvm::InlineFunction(*caller, ifi);
		}

		return nullptr;
		//return result.isSuccess() ? nullptr : _strdup(result.getFailureReason());
	}

	// This should probably not be here.
	DNA_EXPORT void AddParamAttr(llvm::Function* func, unsigned int index, unsigned int attr)
	{
		func->addParamAttr(index, (llvm::Attribute::AttrKind)attr);
	}

	// This should probably not be here.
	DNA_EXPORT void MakeMustTail(llvm::CallInst* callInst)
	{
		callInst->setTailCallKind(llvm::CallInst::TCK_MustTail);
	}

	
	// This should probably not be here.
	DNA_EXPORT void MakeDllImport(llvm::Function* function)
	{
		auto attrList = function->getAttributes();
		//attrList = attrList.addFnAttribute(function->getContext(), AttributeList::FunctionIndex, Attribute::AlwaysInline);
	}

	DNA_EXPORT void MakeDsoLocal(llvm::Function* function, bool dsoLocal)
	{
		function->setDSOLocal(dsoLocal);
	}


}