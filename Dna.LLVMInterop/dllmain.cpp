// dllmain.cpp : Defines the entry point for the DLL application.
#include <llvm/Support/JSON.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/IR/LegacyPassManager.h>
#include <llvm/Pass.h>
#include <llvm/Transforms/Scalar.h>
#include "llvm/Transforms/Scalar/SimpleLoopUnswitch.h"
#include "llvm/Transforms/Utils/LowerSwitch.h"
#include <API/ExportedApi.h>
#include "Passes/ClassifyingAliasAnalysisPass.h"
#include <Windows.h>

struct RestrictedKnownBits {
	uint64_t Zero;
	uint64_t One;
};

DNA_EXPORT void GetKnownBits(llvm::Instruction* instruction, RestrictedKnownBits* out, llvm::SimplifyQuery* sq) {
	auto KB = llvm::computeKnownBits(instruction, sq->DL, 0, sq->AC, sq->CxtI, sq->DT);
	out->Zero = KB.Zero.getZExtValue();
	out->One = KB.One.getZExtValue();
}