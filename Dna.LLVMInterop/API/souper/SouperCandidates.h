#include "souper/Parser/Parser.h"
#include <souper/Extractor/Candidates.h>
#include <souper/Extractor/ExprBuilder.h>
#include <souper/Extractor/Solver.h>
#include <API/ImmutableManagedVector.h>
#include "API/ExportDef.h"
#pragma once

// souper::CandidateReplacement
namespace Dna::API {
	DNA_EXPORT souper::CandidateReplacement* CandidateReplacementConstructor(llvm::Instruction* origin, souper::InstMapping* instMapping)
	{
		// Note that the InstMapping is cloned here.
		return new souper::CandidateReplacement(origin, *instMapping);
	}

	DNA_EXPORT llvm::Instruction* CandidateReplacementGetOrigin(souper::CandidateReplacement* cand)
	{
		return cand->Origin;
	}

	DNA_EXPORT souper::InstMapping* CandidateReplacementGetInstMapping(souper::CandidateReplacement* cand)
	{
		return &cand->Mapping;
	}

	// Get a managed vector<InstMapping*>
	DNA_EXPORT ImmutableManagedVector* CandidateReplacementGetPathConditions(souper::CandidateReplacement* cand)
	{
		auto vec = new std::vector<souper::InstMapping*>();
		for (auto pc : cand->PCs)
			vec->push_back(new souper::InstMapping(pc.LHS, pc.RHS));

		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	// Get a managed vector<BlockPCMapping*>
	DNA_EXPORT ImmutableManagedVector* CandidateReplacementGetBlockPathConditions(souper::CandidateReplacement* cand)
	{
		auto vec = new std::vector<souper::BlockPCMapping*>();
		for (auto pc : cand->BPCs)
			vec->push_back(new souper::BlockPCMapping(pc.B, pc.PredIdx, pc.PC));

		return ImmutableManagedVector::NonCopyingFrom(vec);
	}
}

// souper::BlockCandidateSet
namespace Dna::API {
	DNA_EXPORT ImmutableManagedVector* BlockCandidateSetGetPCs(souper::BlockCandidateSet* cand)
	{
		auto vec = new std::vector<souper::InstMapping*>();
		for (auto pc : cand->PCs)
			vec->push_back(new souper::InstMapping(pc.LHS, pc.RHS));
		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT ImmutableManagedVector* BlockCandidateSetGetBPCs(souper::BlockCandidateSet* cand)
	{
		auto vec = new std::vector<souper::BlockPCMapping*>();
		for (auto pc : cand->BPCs)
			vec->push_back(new souper::BlockPCMapping(pc.B, pc.PredIdx, pc.PC));
		return ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT ImmutableManagedVector* BlockCandidateSetGetReplacements(souper::BlockCandidateSet* cand)
	{
		auto vec = new std::vector<souper::CandidateReplacement*>();
		for (auto replacement : cand->Replacements)
		{
			auto cr = new souper::CandidateReplacement(replacement.Origin, replacement.Mapping);
			cr->PCs = replacement.PCs;
			cr->BPCs = replacement.BPCs;
			vec->push_back(cr);
		}

		return ImmutableManagedVector::NonCopyingFrom(vec);
	}
}

// souper::FunctionCandidateSet
namespace Dna::API {
	DNA_EXPORT ImmutableManagedVector* FunctionCandidateSetGetBlocks(souper::FunctionCandidateSet* fcs)
	{
		auto vec = new std::vector<souper::BlockCandidateSet*>();
		for (auto& block : fcs->Blocks)
		{
			// Clone everything by value. Note: A unique_ptr is used for `Blocks`, so this should prevent any issues w.r.t unexpected frees.
			auto clone = new souper::BlockCandidateSet();
			clone->PCs = block->PCs;
			clone->BPCs = block->BPCs;
			clone->Replacements = block->Replacements;
			vec->push_back(clone);
		}

		return ImmutableManagedVector::From(vec);
	}
}

// souper::ExprBuilderOptions
namespace Dna::API {
	DNA_EXPORT souper::ExprBuilderOptions* ExprBuilderOptionsConstructor(bool namedArrays, llvm::Value* candidateFilterInstruction)
	{
		auto options = new souper::ExprBuilderOptions();
		options->NamedArrays = namedArrays;
		options->CandidateFilterInstruction = candidateFilterInstruction;
		return options;
	}

	DNA_EXPORT llvm::Value* ExprBuilderOptionsGetCandidateFilterInstruction(souper::ExprBuilderOptions* options)
	{
		return options->CandidateFilterInstruction;
	}
}

// souper::BlockInfo
// TODO: We skip this struct for now since it's not really used 
namespace Dna::API {

}

// souper::ExprBuilderContext
// TODO: Export a managed wrapper for `InstMap` and `BlockMap`. They may be useful for debugging.
namespace Dna::API {
	DNA_EXPORT souper::ExprBuilderContext* ExprBuilderContextConstructor()
	{
		return new souper::ExprBuilderContext();
	}
}

// souper::ExtractCandidates()
namespace Dna::API {
	DNA_EXPORT souper::FunctionCandidateSet* SouperExtractCandidates(llvm::Function* function, souper::InstContext* instCtx, souper::ExprBuilderContext* ebc, souper::ExprBuilderOptions* opts)
	{
		auto fcs = souper::ExtractCandidates(function, *instCtx, *ebc, *opts);
		auto output = new souper::FunctionCandidateSet();
		for (auto& block : fcs.Blocks)
			output->Blocks.push_back(std::make_unique<souper::BlockCandidateSet>(*block));
		return output;
	}
}