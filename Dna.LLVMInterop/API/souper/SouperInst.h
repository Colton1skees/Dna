#include "souper/Parser/Parser.h"
#include <souper/Extractor/Candidates.h>
#include <souper/Extractor/ExprBuilder.h>
#include <souper/Extractor/Solver.h>
#include <API/ImmutableManagedVector.h>
#include "API/ExportDef.h"
#pragma once

// souper::Block
namespace Dna::API {
	DNA_EXPORT char* BlockGetName(souper::Block* block)
	{
		return _strdup(block->Name.c_str());
	}

	DNA_EXPORT uint32_t BlockGetPreds(souper::Block* block)
	{
		return block->Preds;
	}

	DNA_EXPORT uint32_t BlockGetNumber(souper::Block* block)
	{
		return block->Number;
	}

	DNA_EXPORT uint32_t BlockGetConcretePred(souper::Block* block)
	{
		return block->ConcretePred;
	}

	DNA_EXPORT ImmutableManagedVector* BlockGetPredVars(souper::Block* block)
	{
		//  std::vector<Inst *> PredVars;
		return ImmutableManagedVector::From(&block->PredVars);
	}
}

// souper::Inst
namespace Dna::API {
	DNA_EXPORT souper::Inst::Kind InstGetKind(souper::Inst* inst)
	{
		return inst->K;
	}

	DNA_EXPORT uint32_t InstGetWidth(souper::Inst* inst)
	{
		return inst->Width;
	}

	DNA_EXPORT souper::Block* InstGetBlock(souper::Inst* inst)
	{
		return inst->B;
	}

	DNA_EXPORT uint64_t InstGetVal(souper::Inst* inst)
	{
		return inst->Val.getZExtValue();
	}

	DNA_EXPORT char* InstGetName(souper::Inst* inst)
	{
		return _strdup(inst->Name.c_str());
	}

	DNA_EXPORT char* InstGetString(souper::Inst* inst)
	{
		return _strdup(inst->Print());
	}

	DNA_EXPORT ImmutableManagedVector* InstGetOrderedOps(souper::Inst* inst)
	{
		// std::vector<Inst *> Ops;
		return ImmutableManagedVector::From(&inst->orderedOps());
	}

	DNA_EXPORT ImmutableManagedVector* InstGetOrigins(souper::Inst* inst)
	{
		//   std::vector<llvm::Value *> Origins;
		return ImmutableManagedVector::From(&inst->Origins);
	}

	DNA_EXPORT uint64_t InstGetRangeMin(souper::Inst* inst)
	{
		//   std::vector<llvm::Value *> Origins;
		return inst->Range.getUnsignedMin().getZExtValue();
	}

	DNA_EXPORT uint64_t InstGetRangeMax(souper::Inst* inst)
	{
		//   std::vector<llvm::Value *> Origins;
		return inst->Range.getUnsignedMax().getZExtValue();
	}
}

// souper::InstMapping
namespace Dna::API {
	DNA_EXPORT souper::InstMapping* InstMappingConstructor(souper::Inst* lhs, souper::Inst* rhs)
	{
		return new souper::InstMapping(lhs, rhs);
	}

	DNA_EXPORT souper::Inst* InstMappingGetLhs(souper::InstMapping* instMapping)
	{
		return instMapping->LHS;
	}

	DNA_EXPORT void InstMappingSetLhs(souper::InstMapping* instMapping, souper::Inst* lhs)
	{
		instMapping->LHS = lhs;
	}

	DNA_EXPORT souper::Inst* InstMappingGetRhs(souper::InstMapping* instMapping)
	{
		return instMapping->RHS;
	}

	DNA_EXPORT void InstMappingSetRhs(souper::InstMapping* instMapping, souper::Inst* rhs)
	{
		instMapping->RHS = rhs;
	}
}

// souper::BlockPCMapping
namespace Dna::API {
	souper::BlockPCMapping* BlockPcMappingConstructor(souper::Block* block, uint32_t index, souper::InstMapping* PC)
	{
		return new souper::BlockPCMapping(block, index, *PC);
	}

	souper::Block* BlockPcMappingGetBlock(souper::BlockPCMapping* mapping)
	{
		return mapping->B;
	}

	uint32_t BlockPcMappingGetPrexIdx(souper::BlockPCMapping* mapping)
	{
		return mapping->PredIdx;
	}

	souper::InstMapping* BlockPcMappingGetPc(souper::BlockPCMapping* mapping)
	{
		return &mapping->PC;
	}
}

// souper::ReplacementContext
namespace Dna::API {
	DNA_EXPORT souper::ReplacementContext* ReplacementContextConstructor()
	{
		return new souper::ReplacementContext();
	}
}

// souper::InstContext
namespace Dna::API {
	DNA_EXPORT souper::InstContext* InstContextConstructor()
	{
		return new souper::InstContext();
	}

	DNA_EXPORT souper::Inst* InstContextGetConst(souper::InstContext* ctx, uint32_t width, uint64_t value)
	{
		auto apInt = llvm::APInt(width, value);
		return ctx->getConst(apInt);
	}

	DNA_EXPORT souper::Inst* InstContextCreateVar(souper::InstContext* ctx, uint32_t width, char* name)
	{
		return ctx->createVar(width, name);
	}

	DNA_EXPORT souper::Inst* InstContextGetInst(souper::InstContext* ctx, souper::Inst::Kind kind, uint32_t width, ImmutableManagedVector* operands, bool available)
	{
		// Copy the operands into a heap allocated vector. Note that we leak memory here,
		// but it's ok unless we're processing literally millions of instructions.
		auto ops = (std::vector<souper::Inst*>*)operands->items;

		// Create the instruction.
		return ctx->getInst(kind, width, *ops, available);
	}

	DNA_EXPORT souper::Inst* InstContextGetInstWithSingleArg(souper::InstContext* ctx, souper::Inst::Kind kind, uint32_t width, souper::Inst* op1, bool available)
	{
		return ctx->getInst(kind, width, { op1 }, available);
	}

	DNA_EXPORT souper::Inst* InstContextGetInstWithDoubleArg(souper::InstContext* ctx, souper::Inst::Kind kind, uint32_t width, souper::Inst* op1, souper::Inst* op2, bool available)
	{
		return ctx->getInst(kind, width, { op1, op2 }, available);
	}
}

