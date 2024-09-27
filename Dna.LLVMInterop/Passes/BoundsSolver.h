#pragma once

#include "souper/Parser/Parser.h"
#include <souper/Extractor/Candidates.h>
#include <souper/Extractor/ExprBuilder.h>
#include <souper/Extractor/Solver.h>
#include "Passes/JumpTableAnalysisPass.h"
#include "API/ExportDef.h"

namespace Dna::Passes 
{
	// Class for representing a slice of an LLVM IR variable.
	struct VariableSlice
	{
		// The bit width of the variable.
		uint32_t BitWidth;

		// The jump table index variable being solved for. E.g. if you have jmp(table[%100]), then this is the %100 variable.
		llvm::Value* Value;

		// The souper AST of the variable.
		souper::Inst* Ast;

		// An optional single unboundable input dependency.
		// E.g. if you have add(1111, mem[rax]), this would point to mem[rax].
		souper::Inst* OptionalDependency;

		VariableSlice(uint32_t bitWidth, llvm::Value* value, souper::Inst* ast, souper::Inst* optionalDependency) : BitWidth(bitWidth), Value(value), Ast(ast), OptionalDependency(optionalDependency) {}
	};

	struct SolvableLoadOrVariable
	{
		// The jump table index variable being solved for. E.g. if you have jmp(table[%100]), then this is the %100 variable.
		llvm::Value* Ptr;

		// The optional load instruction that dereferences the pointer. This is optional because not all solved variables get dereferenced.
		llvm::Value* LoadInst;

		SolvableLoadOrVariable(llvm::Value* ptr, llvm::Value* loadInst) : Ptr(ptr), LoadInst(loadInst) {}
	};

	struct ProvedConstant
	{
		souper::Inst* I;
		uint64_t V;

		ProvedConstant(souper::Inst* I, uint64_t V) : I(I), V(V) {}
	};

	struct SouperContext
	{
		souper::InstContext& InstCtx;
		souper::ReplacementContext& ReplacementCtx;
		souper::ExprBuilderContext& ExprBuilderCtx;
		souper::ExprBuilderOptions& ExprBuilderOptions;

		std::unique_ptr<souper::ExprBuilder> ExprBuilder;

		Dna::Passes::tTrySolveConstant TrySolveConstant;

		// Count to avoid creating duplicate variable names of different bit widths. 
		int VarCount = 0;

		souper::Inst* GetConstVar(uint32_t bitWidth)
		{
			auto constVar = InstCtx.createVar(bitWidth, "ConstVar" + std::to_string(VarCount));
			VarCount++;
			return constVar;
		}
	};

	// Given a variable and a single path constraint, try to evaluate all possible solutions up to N bound. If more than N values are discovered,
	// we return false to indicate that there are too many bounds to solve for.
	bool TrySolveBounds(std::vector<ProvedConstant>& solvedValues, souper::Inst& value, souper::Inst pathConstraint, SouperContext& ctx, int maxBounds = 1024)
	{
		// Create a variable to represent the value we are solving for.
		souper::Inst* toSolve = ctx.GetConstVar(value.Width);
		souper::InstMapping IM(&value, toSolve);

		int i = 0;
		do
		{
			// If we've reached the limit on the number of allowed solutions, bail. The variable is most likely unbounded.
			if (i > maxBounds)
			{
				printf("Failed to bound variable! Too many candidate values.");
				return false;
			}

			// Populate the path constraints.
			std::vector<souper::Inst*> ModelInstructions;
			std::vector<souper::Inst*> Preconditions;
			Preconditions.push_back(&pathConstraint);

			// Build the exclusions.
			for (const auto& RecoveredConstant : solvedValues) 
			{
				souper::Inst* NotConstant =
					ctx.InstCtx.getInst(souper::Inst::Ne, 64, { &value, RecoveredConstant.I });
				Preconditions.push_back(NotConstant);
			}

			// Add the preconditions
			souper::Inst* Precondition = nullptr;
			if (!Preconditions.empty())
				Precondition = ctx.InstCtx.getInst(souper::Inst::And, 1, Preconditions);

			// Build the query.
			souper::BlockPCs BPCs;
			std::vector<souper::InstMapping> PCs;
			auto query = ctx.ExprBuilder->BuildQuery(BPCs, PCs, IM, &ModelInstructions, Precondition);

			// Try to solve the query.
			auto name = _strdup(toSolve->Name.c_str());
			uint64_t constant;
			auto satisfiable = ctx.TrySolveConstant(_strdup(query.c_str()), name, &constant);

			// If the query is not satisfiable then we've solved all possible values. Note that TrySolveConstant
			// will probably? throw on `Unknown`(aka timeout) results.
			if (!satisfiable)
				break;

			// Otherwise we found one solution that we need to push back, but there are still more we may need to solve for.
			solvedValues.push_back(ProvedConstant(toSolve, constant));
			i++;
		}

		while (true);
		return true;
	}

	void CollectInputVariables(souper::Inst& inst, std::vector<souper::Inst*>& inputVariables)
	{
		switch (inst.K)
		{
		case souper::Inst::Var:
			inputVariables.push_back(&inst);
			break;

		case souper::Inst::Kind::Phi:
		case souper::Inst::Kind::Add:
		case souper::Inst::Kind::AddNSW:
		case souper::Inst::Kind::AddNUW:
		case souper::Inst::Kind::AddNW:
		case souper::Inst::Kind::Sub:
		case souper::Inst::Kind::SubNSW:
		case souper::Inst::Kind::SubNUW:
		case souper::Inst::Kind::SubNW:
		case souper::Inst::Kind::Mul:
		case souper::Inst::Kind::MulNSW:
		case souper::Inst::Kind::MulNUW:
		case souper::Inst::Kind::MulNW:
		case souper::Inst::Kind::UDiv:
		case souper::Inst::Kind::SDiv:
		case souper::Inst::Kind::UDivExact:
		case souper::Inst::Kind::SDivExact:
		case souper::Inst::Kind::URem:
		case souper::Inst::Kind::SRem:
		case souper::Inst::Kind::And:
		case souper::Inst::Kind::Or:
		case souper::Inst::Kind::Xor:
		case souper::Inst::Kind::Shl:
		case souper::Inst::Kind::ShlNSW:
		case souper::Inst::Kind::ShlNUW:
		case souper::Inst::Kind::ShlNW:
		case souper::Inst::Kind::LShr:
		case souper::Inst::Kind::LShrExact:
		case souper::Inst::Kind::AShr:
		case souper::Inst::Kind::AShrExact:
		case souper::Inst::Kind::Select:
		case souper::Inst::Kind::ZExt:
		case souper::Inst::Kind::SExt:
		case souper::Inst::Kind::Trunc:
		case souper::Inst::Kind::Eq:
		case souper::Inst::Kind::Ne:
		case souper::Inst::Kind::Ult:
		case souper::Inst::Kind::Slt:
		case souper::Inst::Kind::Ule:
		case souper::Inst::Kind::Sle:
		case souper::Inst::Kind::CtPop:
		case souper::Inst::Kind::BSwap:
		case souper::Inst::Kind::Cttz:
		case souper::Inst::Kind::Ctlz:
		case souper::Inst::Kind::BitReverse:
		case souper::Inst::Kind::FShl:
		case souper::Inst::Kind::FShr:
		case souper::Inst::Kind::ExtractValue:
		case souper::Inst::Kind::SAddWithOverflow:
		case souper::Inst::Kind::SAddO:
		case souper::Inst::Kind::UAddWithOverflow:
		case souper::Inst::Kind::UAddO:
		case souper::Inst::Kind::SSubWithOverflow:
		case souper::Inst::Kind::SSubO:
		case souper::Inst::Kind::USubWithOverflow:
		case souper::Inst::Kind::USubO:
		case souper::Inst::Kind::SMulWithOverflow:
		case souper::Inst::Kind::SMulO:
		case souper::Inst::Kind::UMulWithOverflow:
		case souper::Inst::Kind::UMulO:
		case souper::Inst::Kind::SAddSat:
		case souper::Inst::Kind::UAddSat:
		case souper::Inst::Kind::SSubSat:
		case souper::Inst::Kind::USubSat:
		case souper::Inst::Kind::Freeze:
		case souper::Inst::Kind::Const:
			for (auto& operand : inst.Ops)

				CollectInputVariables(*operand, inputVariables);
			break;

		default:
		{
			auto msg = "Unrecognized souper inst kind: " + std::string(inst.getKindName(inst.K));
			llvm::report_fatal_error(msg.c_str());
			throw new std::invalid_argument(msg);
		}
		}
	}

	DNA_EXPORT void Solve(llvm::Function* function, llvm::CallInst* remillJumpCall, llvm::Value* value, llvm::BasicBlock* indirectJump, llvm::LazyValueInfo* lvi, Dna::Passes::tTrySolveConstant trySolveConstant)
	{

	}
}