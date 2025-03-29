#pragma once 

#include <llvm/IR/CFG.h>
#include <llvm/IR/Function.h>
#include <llvm/IR/Instructions.h>
#include <llvm/Pass.h>

#include <set>
#include <stack>
#include <vector>

namespace Dna::Passes {
	typedef unsigned long long(__cdecl* tReadBinaryContents)(unsigned long long rva, unsigned int byteSize);

	struct ConstantConcretizationPass : llvm::FunctionPass {
		static char ID;

		tReadBinaryContents readBinaryContents;

		std::set<unsigned long long> writtenMemory;

		std::unordered_map<llvm::Instruction*, unsigned long long> loadMapping;

		ConstantConcretizationPass() : FunctionPass(ID)
		{

		}

		ConstantConcretizationPass(tReadBinaryContents readBinaryContents) : FunctionPass(ID) 
		{
			this->readBinaryContents = readBinaryContents;
		}

		bool runOnFunction(llvm::Function& F) override;
	};

	llvm::FunctionPass* getConstantConcretizationPassPass(tReadBinaryContents readBinaryContents);
 }