#include <llvm/IR/Module.h>
#include <llvm/IR/Value.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Instructions.h>
#include <llvm/IR/Constant.h>
#include <llvm/IR/Constants.h>
#include <llvm/IR/InstIterator.h>
#include <llvm/Support/Casting.h>

#include "ConstantConcretizationPass.h"
#include "PassUtilities.h"

namespace Dna::Passes {
	char ConstantConcretizationPass::ID = 0;

	bool ConstantConcretizationPass::runOnFunction(llvm::Function& F)
	{
		// Get the entry basic block.
		auto block = &F.getEntryBlock();

		auto rspPtr = F.getParent()->getGlobalVariable("rsp");

		for (auto &instruction : *block)
		{
			if (auto load = llvm::dyn_cast<llvm::LoadInst>(&instruction))
			{
				std::set<const llvm::Value*> operands;
				bool success = GetPtrChain(instruction.getOperand(0), &operands, rspPtr);

				bool dumpLoad = false;
				if (dumpLoad)
				{
					printf("Dumping load chain for: ");
					//instruction.dump();
					for (auto operand : operands)
					{
						printf("input: ");
						//operand->dump();
					}
				}

				if (!success)
				{
					printf("Skipping load chain since it reads from stack.");
					continue;
				}


				// Skip if the store utilizes RSP.
				if (operands.count(rspPtr))
				{
					printf("Ignoring memory access since it touches the stack.");
					continue;
				}

				// Now we are pattern matching for cases like:
				// input: i64 5369024343
				// input: % 222 = getelementptr inbounds i8, ptr % 0, i64 5369024343
				if (operands.size() != 2)
				{
					continue;
				}

				const llvm::ConstantInt* constant = nullptr;
				for (auto potentialConstant : operands)
				{
					if (auto constInt = llvm::dyn_cast<llvm::ConstantInt>(potentialConstant))
					{
						constant = constInt;
					}
				}

				if (constant == nullptr)
				{
					printf("Breaking since we could not find a constant within the binary section.");
					break;
				}

				auto intValue = constant->getValue().getZExtValue();
				bool valid = intValue >= 0x140009000 && intValue <= 0x14006C460;
				if (!valid)
				{
					printf("Breaking due to integer range being invalid.");
					break;
				}

				auto bitWidth = load->getType()->getIntegerBitWidth();
				auto byteWidth = (bitWidth / 8);
				bool known = true;
				for (unsigned long long i = 0; i < byteWidth; i++)
				{
					if (writtenMemory.count(i + intValue))
					{
						known = false;
					}
				}

				if (!known)
				{
					continue;
				}

				auto memValue = readBinaryContents(intValue, byteWidth);
				loadMapping.emplace(load, memValue);
			}

			else if (auto store = llvm::dyn_cast<llvm::StoreInst>(&instruction))
			{
				std::set<const llvm::Value*> operands;
				auto success = GetPtrChain(instruction.getOperand(1), &operands, rspPtr);

				bool dumpStore = false;
				if (dumpStore)
				{
					printf("Dumping store chain for: ");
					//instruction.dump();
					for (auto operand : operands)
					{
						printf("input: ");
						//operand->dump();
					}
				}

				// Terminate if we encountered an unsupported memory address computation?
				if (!success)
				{
					printf("Permamently exiting due to unsupported operation.");
					printf("Dumping store chain for: ");
					//instruction.dump();
					for (auto operand : operands)
					{
						printf("input: ");
						//operand->dump();
					}
					break;
				}

				// Skip if the store utilizes RSP.
				if (operands.count(rspPtr))
				{
					printf("Ignoring memory access since it touches the stack.");
					continue;
				}

				// Now we are pattern matching for cases like:
				// input: i64 5369024343
				// input: % 222 = getelementptr inbounds i8, ptr % 0, i64 5369024343
				if (operands.size() != 2)
				{
					continue;
				}

				const llvm::ConstantInt* constant = nullptr;
				for (auto potentialConstant : operands)
				{
					if (auto constInt = llvm::dyn_cast<llvm::ConstantInt>(potentialConstant))
					{
						constant = constInt;
					}
				}

				if (constant == nullptr)
				{
					printf("Breaking since we could not find a constant within the binary section.");
					break;
				}

				auto intValue = constant->getValue().getZExtValue();
				bool valid = intValue >= 0x140009000 && intValue <= 0x14006C460;
				if (!valid)
				{
					printf("Breaking due to integer range being invalid.");
				}

				auto bitWidth = store->getOperand(0)->getType()->getIntegerBitWidth();
				for (unsigned long long i = 0; i < (bitWidth / 8); i++)
				{
					writtenMemory.insert(intValue + i);
				}
			}
		}
		
		if (loadMapping.size() == 0)
		{
			printf("\nfailed to locate any concretizeable loads.");
			return false;
		}

		for (auto pair : loadMapping)
		{
			auto bitWidth = pair.first->getType()->getIntegerBitWidth();
			auto byteWidth = (bitWidth / 8);
			auto intTy = llvm::IntegerType::get(F.getContext(), bitWidth);
			auto constInt = llvm::ConstantInt::get(intTy, pair.second);
			pair.first->replaceAllUsesWith(constInt);
		}

		return true;
	}

	static llvm::RegisterPass<ConstantConcretizationPass> X("ConstantConcretizationPass",
		"Concretize the load instructions reading from the binary",
		false /* Only looks at CFG */,
		false /* Analysis Pass */);

	llvm::FunctionPass* getConstantConcretizationPassPass(tReadBinaryContents readBinaryContents) {
		return new ConstantConcretizationPass(readBinaryContents);
	}
}