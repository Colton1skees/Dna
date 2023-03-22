#pragma once

#include <llvm/IR/Value.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Instructions.h>
#include <llvm/IR/Constant.h>
#include <llvm/IR/Constants.h>
#include <set>

namespace Dna::Passes {
	bool GetPtrChain(const llvm::Value* V, std::set<const llvm::Value*>* chain, llvm::Value* rspPtr)
	{
		if (const auto* gep = llvm::dyn_cast<llvm::GetElementPtrInst>(V))
		{
			chain->insert(gep);
			return GetPtrChain(gep->getOperand(1), chain, rspPtr);
		}

		else if (const auto* constInt = llvm::dyn_cast<llvm::ConstantInt>(V))
		{
			chain->insert(constInt);
			return true;
		}

		else if (const auto* binop = llvm::dyn_cast<llvm::BinaryOperator>(V))
		{
			chain->insert(binop);
			bool supportsOp0 = GetPtrChain(binop->getOperand(0), chain, rspPtr);
			bool supportsOp1 = GetPtrChain(binop->getOperand(1), chain, rspPtr);
			return supportsOp0 && supportsOp1;
		}

		else if (const auto& load = llvm::dyn_cast<llvm::LoadInst>(V))
		{
			// Push back the load operation to the chain.
			chain->insert(load);

			// If this is the load from RSP, then we return `true` to indicate that it was a success,
			// and push back the rsp pointer.
			auto loadOp = load->getOperand(0);
			if (loadOp == rspPtr)
			{
				chain->insert(loadOp);
				return true;
			}

			// Otherwise, we return false. This is likely a load to / from a register, or an unknown store.
			return true;
		}

		else
		{
			return false;
		}
	}
}