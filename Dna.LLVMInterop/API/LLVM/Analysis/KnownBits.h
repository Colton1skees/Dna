#pragma once

#include <llvm/Analysis/ValueTracking.h>
#include <llvm/IR/Instruction.h>
#include <llvm/IR/Module.h>
#include <llvm/Support/KnownBits.h>

#include <API/ExportDef.h>

namespace DNA::API {
	struct RestrictedKnownBits {
		uint64_t Zero;
		uint64_t One;
	};

	DNA_EXPORT void KnownBits_Get(llvm::Instruction* instruction, llvm::DataLayout *dataLayout, RestrictedKnownBits *out) {
		auto KB = llvm::computeKnownBits(instruction, *dataLayout);
		out->Zero = KB.Zero.getZExtValue();
		out->One = KB.One.getZExtValue();
	}
}