#include "souper/Parser/Parser.h"
#include <souper/Extractor/Candidates.h>
#include <souper/Extractor/ExprBuilder.h>
#include <souper/Extractor/Solver.h>
#include <API/ImmutableManagedVector.h>
#include "API/ExportDef.h"
#pragma once

// souper::ExprBuilder
namespace Dna::API {
	DNA_EXPORT souper::ExprBuilder* ExprBuilderConstructorKlee(souper::InstContext* ctx)
	{
		auto uniquePtr = souper::createKLEEBuilder(*ctx);

		auto ptr = uniquePtr.get();

		// Relinquish ownership to the managed caller.
		uniquePtr.release();

		// Since we're passing ownership of this object to managed code,
		// we need to relinquish ownership.
		return ptr;
	}

	// Similar to the way we collect UB constraints. We could combine it with
	// getUBInstCondition, because the workflow is quite similar.
	// However, mixing two parts (one for UB constraints, one for BlockPCs)
	// may make the code less structured. If we see big performance overhead,
	// we may consider to combine these two parts together.
	DNA_EXPORT souper::Inst* ExprBuilderGetBlockPCs(souper::ExprBuilder* exprBuilder, souper::Inst* root)
	{
		return exprBuilder->getBlockPCs(root);
	}

	//  std::string BuildQuery(InstContext &IC, const BlockPCs &BPCs,
	//		const std::vector<InstMapping>& PCs, InstMapping Mapping,
	//		std::vector<Inst*>* ModelVars, Inst* Precondition, bool Negate = false,
	//		bool DropUB = false) = 0;
	DNA_EXPORT char* SouperBuildQuery(souper::InstContext* ctx, ImmutableManagedVector* blocksPcs, ImmutableManagedVector* pcs, souper::InstMapping* mapping, ImmutableManagedVector* modelVars, souper::Inst* precondition, bool negate, bool dropUb)
	{
		// BlockPC = std::vector<BlockPCMapping>
		// Cast the managed vector to a vector of <BlockPCMapping> values.
		std::vector<souper::BlockPCMapping> BPCs;
		auto bpcPtrs = (std::vector<souper::BlockPCMapping*>*)blocksPcs->items;
		for (auto blockPc : *bpcPtrs)
			BPCs.push_back(*blockPc);

		// Cast the managed vector to a vector of <InstMapping>
		std::vector<souper::InstMapping> PCs;
		auto pcPtrs = (std::vector<souper::InstMapping*>*)pcs->items;
		for (auto pc : *pcPtrs)
			PCs.push_back(*pc);

		// Before we needed to perform clones since souper::BuildQuery expected vectors of value types.
		// But here no cloning is needed since ModelVars is a vec of reference types.
		auto ModelVars = (std::vector<souper::Inst*>*)modelVars->items;

		// Build the query.
		auto query = souper::BuildQuery(*ctx, BPCs, PCs, *mapping, ModelVars, precondition, negate, dropUb);

		// Return a heap allocated string clone.
		return _strdup(query.c_str());
	}
}