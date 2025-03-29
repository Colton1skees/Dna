#pragma once

#include <llvm/IR/Module.h>
#include "llvm-c/Transforms/PassManagerBuilder.h"
#include "llvm/ADT/SmallVector.h"
#include "llvm/ADT/Triple.h"
#include "llvm/Analysis/BasicAliasAnalysis.h"
#include "llvm/Analysis/ScalarEvolutionAliasAnalysis.h"
#include "llvm/Analysis/GlobalsModRef.h"
#include "llvm/Analysis/InlineCost.h"
#include "llvm/Analysis/Passes.h"
#include "llvm/Analysis/ScopedNoAliasAA.h"
#include "llvm/Analysis/TargetLibraryInfo.h"
#include "llvm/Analysis/TypeBasedAliasAnalysis.h"
#include "llvm/Analysis/MemoryDependenceAnalysis.h"
#include "llvm/Analysis/DemandedBits.h"
#include "llvm/IR/DataLayout.h"
#include "llvm/IR/LegacyPassManager.h"
#include "llvm/IR/Verifier.h"
#include "llvm/Support/CommandLine.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Transforms/AggressiveInstCombine/AggressiveInstCombine.h"
#include "llvm/Transforms/IPO.h"
#include "llvm/Transforms/IPO/ForceFunctionAttrs.h"
#include "llvm/Transforms/IPO/FunctionAttrs.h"
#include "llvm/Transforms/IPO/InferFunctionAttrs.h"
#include "llvm/Transforms/IPO/PassManagerBuilder.h"
#include "llvm/Transforms/InstCombine/InstCombine.h"
#include "llvm/Transforms/Instrumentation.h"
#include "llvm/Transforms/Scalar.h"
#include "llvm/Transforms/Scalar/GVN.h"
#include "llvm/Transforms/Scalar/NewGVN.h"
#include "llvm/Transforms/Scalar/EarlyCSE.h"
#include "llvm/Transforms/Scalar/SCCP.h"
#include "llvm/Transforms/Utils/Mem2Reg.h"
#include "llvm/Transforms/Utils/LowerSwitch.h"
#include "llvm/Transforms/Utils/LoopSimplify.h"
#include "llvm/Transforms/Utils/LCSSA.h"
#include "llvm/Transforms/Utils/FixIrreducible.h"
#include "llvm/Transforms/Scalar/InstSimplifyPass.h"
#include "llvm/Transforms/Scalar/IndVarSimplify.h"
#include "llvm/Transforms/Scalar/SimpleLoopUnswitch.h"
#include "llvm/Transforms/Scalar/JumpThreading.h"
#include "llvm/Transforms/Scalar/CorrelatedValuePropagation.h"
#include "llvm/Transforms/Scalar/CallSiteSplitting.h"
#include "llvm/Transforms/Scalar/SpeculativeExecution.h"
#include "llvm/Transforms/Scalar/BDCE.h"
#include "llvm/Transforms/Scalar/ADCE.h"
#include "llvm/Transforms/Scalar/Reassociate.h"
#include "llvm/Transforms/Scalar/LoopSimplifyCFG.h"
#include "llvm/Transforms/Scalar/SimplifyCFG.h"
#include "llvm/Transforms/Scalar/LoopPassManager.h"
#include "llvm/Transforms/Scalar/LoopUnrollPass.h"
#include "llvm/Transforms/Scalar/SROA.h"
#include "llvm/Transforms/Utils.h"
#include "llvm/Transforms/Utils/Cloning.h"
#include "llvm/Transforms/Vectorize.h"
#include "llvm/Transforms/Scalar/DeadStoreElimination.h"
#include <llvm/InitializePasses.h>
#include "llvm/Transforms/IPO/AlwaysInliner.h"

#include "souper/Parser/Parser.h"
#include <souper/Extractor/Candidates.h>
#include <souper/Extractor/ExprBuilder.h>
#include <souper/Extractor/Solver.h>
#include <souper/Infer/Z3Driver.h>
#include <souper/Infer/Z3Expr.h>

#include "llvm/Passes/PassBuilder.h"
#include "Passes/ClassifyingAliasAnalysisPass.h"
#include "Passes/ConstantConcretizationPass.h"
#include "Passes/ControlledNodeSplittingPass.h"
#include "Passes/generator_jit_sl_function.h"
#include "Passes/generator_jit_ast_compute.h"
#include "Passes/ControlFlowStructuringPass.h"
#include "Passes/JumpTableAnalysisPass.h"

#include "Utilities/magic_enum.hpp"

#include <API/RegionAPI/RegionAPI.h>
#include "API/ExportDef.h"
#include <tuple>
#include <chrono>
using namespace llvm::sl;


namespace Dna::Pipeline
{
	int constCount = 0;

	typedef struct ProvedConstant {
		souper::Inst* I;
		uint64_t V;

		ProvedConstant(souper::Inst* I, uint64_t V) : I(I), V(V) {}
	} ProvedConstant;

	struct SouperContext 
	{
		souper::InstContext& InstCtx;
		souper::ReplacementContext& ReplacementCtx;
		souper::ExprBuilderContext& ExprBuilderCtx;
		souper::ExprBuilderOptions& ExprBuilderOptions;

		std::unique_ptr<souper::ExprBuilder> ExprBuilder;

		Dna::Passes::tTrySolveConstant TrySolveConstant;
	};

	struct VariableSlice
	{
		uint32_t BitWidth;

		// The jump table index variable being solved for. E.g. if you have jmp(table[%100]), then this is the %100 variable.
		llvm::Value* Value;

		souper::Inst* Ast;

		VariableSlice(uint32_t bitWidth, llvm::Value* value, souper::Inst* ast)
		{
			this->BitWidth = bitWidth;
			this->Value = value;
			this->Ast = ast;
		}
	};

	struct UnboundableVariableSlice : VariableSlice
	{
		souper::Inst* UnboundableDependency;

		UnboundableVariableSlice(uint32_t bitWidth, llvm::Value* value, souper::Inst* ast, souper::Inst* unboundableDependency) : VariableSlice(bitWidth, value, ast)
		{
			this->UnboundableDependency = unboundableDependency;
		}
	};

	struct BoundableVariableSlice : VariableSlice
	{
		BoundableVariableSlice(uint32_t bitWidth, llvm::Value* value, souper::Inst* ast) : VariableSlice(bitWidth, value, ast)
		{

		}
	};

	struct SolvableLoadOrVariable
	{
		// The jump table index variable being solved for. E.g. if you have jmp(table[%100]), then this is the %100 variable.
		llvm::Value* Ptr;

		// The optional load instruction that dereferences the pointer. This is optional because not all solved variables get dereferenced.
		llvm::Value* LoadInst;

		SolvableLoadOrVariable(llvm::Value* ptr, llvm::Value* loadInst)
		{
			this->Ptr = ptr;
			this->LoadInst = loadInst;
		}
	};

	int count = 0;

	souper::CandidateReplacement* GetCandidate(souper::FunctionCandidateSet& FCS, souper::ExprBuilderOptions& EBO)
	{
		// Identify the candidate
		souper::CandidateReplacement* CR = nullptr;
		int count = 0;
		for (auto& B : FCS.Blocks) {
			for (auto& R : B->Replacements) {
				if ((R.Origin != EBO.CandidateFilterInstruction) || (R.Mapping.LHS->Width != 64))
					continue;
				CR = &R;
				count++;
			}
		}

		if (CR == nullptr || count > 1)
		{
			llvm::report_fatal_error("identifyChildren (2): no available candidates!");
			throw new std::invalid_argument("No available candidates or too many candidates!");
		}

		return CR;
	}

	souper::Inst* Slice(std::string toName, llvm::Function* function, llvm::Value* value, souper::InstContext& IC, souper::ReplacementContext& RC, souper::ExprBuilderContext& EBC, souper::ExprBuilderOptions& EBO)
	{
		llvm::outs() << *value;
		llvm::outs() << "\n";
		auto fcs = souper::ExtractCandidates(function, IC, EBC, EBO);

		auto cand = GetCandidate(fcs, EBO);
		llvm::outs() << "cand: " << cand;
		llvm::outs() << "\n";
		llvm::outs() << "toName: " << toName;
		llvm::outs() << "\n";

		//souper::Inst* ConstantVar = IC.createVar(cand->Mapping.LHS->Width, toName);
		printf("created constant var!");
		//cand->printLHS(llvm::outs(), RC, true);
		auto mapping =  cand->Mapping.LHS;
		printf("got lhs");
		return mapping;
	}

	bool GetSolutions(std::vector<ProvedConstant>& RecoveredConstants, souper::Inst& inst, souper::Inst& pc, souper::InstContext& InstCtx, Dna::Passes::tTrySolveConstant TrySolveConstant)
	{
		if (constCount == 0)
		{
			constCount++;
			//return false;
		}


		souper::Inst* ConstantVar = InstCtx.createVar(inst.Width, "ConstantVar" + std::to_string(constCount));
		souper::InstMapping IM(&inst, ConstantVar);
		constCount++;
		//auto RecoveredConstants = solvedConstants;

		do
		{
			std::vector<souper::Inst*> ModelInstructions;
			// Populate the preconditions
			std::vector<souper::Inst*> Preconditions;
			//Preconditions.push_back(&pc);
			souper::Inst* Precondition = nullptr;
			// Build the exclusions
			for (const auto& RecoveredConstant : RecoveredConstants) {
				souper::Inst* NotConstant =
					InstCtx.getInst(souper::Inst::Ne, 64, { &inst, RecoveredConstant.I });
				Preconditions.push_back(NotConstant);
			}


			Preconditions.push_back(&pc);
			// Add the preconditions
			if (!Preconditions.empty())
				Precondition = InstCtx.getInst(souper::Inst::And, 1, Preconditions);
			// Build the query
			souper::BlockPCs BPCs;
			std::vector<souper::InstMapping> PCs;

			// TODO: verify why the BPCs and PCs may lead to no results sometimes
			// const auto &Query = souper::BuildQuery(IC, CR->BPCs, CR->PCs, IM, &ModelInstructions, Precondition, true);
			const auto& Query = souper::BuildQuery(InstCtx, BPCs, PCs, IM, &ModelInstructions, Precondition, true);

			llvm::outs() << "[+] SMT Query:\n";
			llvm::outs() << Query << "\n";
			printf("queried!");


			auto name = ConstantVar->Name;
			uint64_t outConst = 0;
			auto result = TrySolveConstant(_strdup(Query.c_str()), _strdup(name.c_str()), &outConst);
			printf("query executed!");

			std::cout << "is sat: " << result << std::endl;
			if (result)
			{
				RecoveredConstants.push_back(ProvedConstant(&inst, outConst));
			}
			std::cout << "out const: " << outConst << std::endl;
		}

		while (true);
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
		// Setup souper context
		souper::InstContext IC;
		souper::ReplacementContext RC;
		souper::ExprBuilderContext EBC;
		souper::ExprBuilderOptions EBO;
		souper::FunctionCandidateSet FCS;

		// Create a persistent klee expression builder. 
		// This is necessary because we're iteratively building queries with shared state.
		auto EBP = souper::createKLEEBuilder(IC);


		// Extract the path constraints necessary to reach remill_jump.
		// Note that due to souper limitations, the expectation here is that the value being used is
		// defined in the same block as the remill_jump intrinsic.
		// If it's not defined the expectation is that the caller of Solve()
		// will create an expression *right* before the remill_jump call that looks like:
		//	%pc_ptr = add i64 %pc_to_solve, i64 0
		EBO.CandidateFilterInstruction = value;
		FCS = souper::ExtractCandidates(function, IC, EBC, EBO);
		auto cand = GetCandidate(FCS, EBO);
		souper::Inst* pcInst = IC.createVar(cand->Mapping.LHS->Width, "PcInst");
		auto indirectJumpPathConstraint = EBP->getBlockPCs(pcInst);

		std::vector<ProvedConstant> newOutConstants;

		auto hasSolution = GetSolutions(newOutConstants, *cand->Mapping.LHS, *indirectJumpPathConstraint, IC, trySolveConstant);

		// Create the solving set. 
		auto unsolvedVariables = std::vector<UnboundableVariableSlice>();
		auto solvingQueue = std::vector<SolvableLoadOrVariable>();
		// Push back (value to solve for - which is a pointer in this case, and nullptr which represents the LoadInst operand of SolvableLoadOrVariable).
		solvingQueue.push_back(SolvableLoadOrVariable(value, nullptr));
		int ct = 0;
		while (true)
		{
			// Throw if we encounter too many variables that are unboundable.
			// TODO: Support *multiple* dependant memory loads, so long as they don't recurse into each other. 
			if (solvingQueue.size() > 4)
			{
				throw std::invalid_argument("Failed to solve jump table. Number of unboundable inputs exceeds the maximum of 4.");
			}

			// Fetch the latest variable we are solving for.
			auto toSolve = solvingQueue.back();

			// Compute the ptr bitwidth.
			auto bitWidth = toSolve.LoadInst != nullptr ? toSolve.LoadInst->getType()->getIntegerBitWidth() : 64;

			auto possiblyBoundedIndex = Slice("PcInst" + std::to_string(ct), function, toSolve.Ptr, IC, RC, EBC, EBO);
			printf("sliced.");
			ct++;

			// If we've found a boundable variable, then we need to backtrack and 
			// try solving all of the collected slices using the bounds we have now.
			// Note that this works for 99.9% of jump tables, but it will fail
			// if you have like (bounded_load[a]) + (bounded_load[b]) where
			// bounded load B is encountered first in the backwards slicing.
			// In reality we should try to bound back as far as possible(up to a maximum depth of ~5-6).
			llvm::outs() << "trying to solve: ";
			llvm::outs() << toSolve.Ptr;
			llvm::outs() << *toSolve.Ptr;
			llvm::outs() << "\n";
			printf("checking if bounded.");
			std::vector<ProvedConstant> outConstants;

			auto hasSolution = GetSolutions(outConstants, *possiblyBoundedIndex, *indirectJumpPathConstraint, IC, trySolveConstant);
			//auto hasSolution = false;
			if (hasSolution)
			{
				printf("has solution!");
				// Construct a boundable variable slice instance.
				auto boundableVariable = BoundableVariableSlice(bitWidth, toSolve.Ptr, possiblyBoundedIndex);

				// Sort the unsolved variables in the order that they must be solved.
				// I.e. if you have a nested jump table, we order this such that the final 'jmp' target is placed at the
				// end of the list. Also note that the current boundable variable is not included in the list.
				std::reverse(solvingQueue.begin(), solvingQueue.end());

				// TODO: Solve the set of outgoing addresses.
				throw std::invalid_argument("todo!");
			}

			else
			{
				printf("does not have a solution!. Collect the input variables.");
				// Get all input variables.
				std::vector<souper::Inst*> inputVariables;
				printf("getting input variables.");
				CollectInputVariables(*possiblyBoundedIndex, inputVariables);
				printf("got input variables");
				// Throw if there is more than one input variable.
				if (inputVariables.size() != 1)
				{
					auto msg = "Found potentially too many or 0 input variables to unbounded expression.";
					llvm::report_fatal_error(msg);
					throw new std::invalid_argument(msg);
				}

				// Throw if we can't pin the input variable to a single input LLVM IR variable. (This is typically a load).
				printf("getting src");
				auto src = inputVariables[0];
				printf("got src.");
				auto llvmSrc = src->Origins[0];
				if (src->Origins.size() != 1)
				{
					auto msg = "Found potentially too many or 0 origins to unbounded expression.";
					llvm::report_fatal_error(msg);
					throw new std::invalid_argument(msg);
				}

				llvm::Value* ptr = llvmSrc;
				if (llvm::LoadInst* load = dyn_cast<llvm::LoadInst>(llvmSrc))
				{
					if (llvm::GetElementPtrInst* gep = dyn_cast<llvm::GetElementPtrInst>(load->getOperand(0)))
					{
						ptr = gep->getOperand(1);
					}

					else
					{
						auto msg = "Load operand is not a gep. It's probably a function argument or something.";
						llvm::report_fatal_error(msg);
						throw new std::invalid_argument(msg);
					}
				}

				llvm::outs() << "ptr: ";
				llvm::outs() << *ptr;
				llvm::outs() << "\n";

				for (auto queued : solvingQueue)
				{
					if (queued.Ptr == ptr)
					{
						auto msg = "Encountered recursive solving dependency.";
						llvm::report_fatal_error(msg);
						throw new std::invalid_argument(msg);
					}
				}

				// Record the set of data necessary to revisit and solve this jump table.
				// If we can bound one of the predecessors, then we revisit and solve this variable later.
				auto unboundableVariable = UnboundableVariableSlice(bitWidth, toSolve.Ptr, possiblyBoundedIndex, src);
				unsolvedVariables.push_back(unboundableVariable);

				// Queue up the ptr for solving.
				auto maybeSolvable = SolvableLoadOrVariable(ptr, llvmSrc);
				solvingQueue.push_back(maybeSolvable);
			}
		}
	}

	DNA_EXPORT Dna::API::ImmutableManagedVector* TrySolveAllValues(souper::InstContext* ctx, Dna::API::ImmutableManagedVector* blocksPcs, Dna::API::ImmutableManagedVector* pcs,
		souper::InstMapping* mapping, souper::Inst* precondition, bool negate, bool dropUb,
		Dna::Passes::tTrySolveConstant trySolveConstant,
		int maxSolutions, bool* success
	)
	{
		std::cout << "TrySolveAllValues!" << std::endl;
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

		std::cout << "creating builder!" << std::endl;
		auto EB = souper::createKLEEBuilder(*ctx);

		//souper::Inst* Cand = EB->GetCandidateExprForReplacement(BPCs, PCs, *mapping, precondition, negate, dropUb);

		std::cout << "creating solver!" << std::endl;
		souper::Z3Driver solver(mapping->LHS, PCs, *ctx, BPCs, {}, 1000);

		std::vector<ProvedConstant> RecoveredConstants;
		auto lhs = mapping->LHS;
		/*
		std::vector<souper::Inst*> Preconditions;
		while (true)
		{
			for (const auto& RecoveredConstant : RecoveredConstants) {
				souper::Inst* NotConstant =
					ctx.getInst(souper::Inst::Ne, 64, { lhs, RecoveredConstant.I });
				precondition.push_back(NotConstant);
			}
		}*/

		souper::Inst* ConstantVar = ctx->createVar(lhs->Width, "ConstantVar");
		// Map the variable to the left-hand side
		souper::InstMapping IM(lhs, ConstantVar);

		//std::cout << "about to solve!" << std::endl;
		int count = 0;
		bool solvedAll = false;

		auto Solver = solver.Solver;

		//std::cout << "got solver" << std::endl;
		std::chrono::steady_clock::time_point begin = std::chrono::steady_clock::now();
		auto Cond = EB->GetCandidateExprForReplacement(BPCs, PCs, IM, precondition, negate, dropUb);
	//	std::cout << "got cond" << std::endl;
		auto one = solver.ctx.bv_val(0, 1);
	//	std::cout << "got one" << std::endl;
		solver.Translate(Cond);
		auto assert = solver.Get(Cond) == one;
	//	std::cout << "got asert" << std::endl;
		Solver.add(assert);
	//	std::cout << "added" << std::endl;
		do
		{
			if (count > maxSolutions)
			{
				solvedAll = false;
				break;
			}

			count++;

		//	std::cout << "checking: " << std::endl;
			auto check = Solver.check();
			if (check == z3::sat)
			{
				solver.Model = Solver.get_model();
				//std::cout << "sat!" << std::endl;
				auto maybeConstant = solver.getModelVal(ConstantVar);
				//std::cout << "got maybe constant!" << std::endl;
				auto constant = maybeConstant.value().getZExtValue();
			//	std::cout << "got constant!" << std::endl;
				RecoveredConstants.push_back(ProvedConstant(lhs, constant));
			//	std::cout << "pushed!" << std::endl;
				auto imLhs = solver.Get(IM.LHS);
				//std::cout << "got imLhs!" << std::endl;
				auto tempCond = imLhs != solver.ctx.bv_val(constant, IM.LHS->Width);
				//std::cout << "got tempCond!" << std::endl;
				Solver.add(tempCond);
				//std::cout << "added tempCond!" << std::endl;

				//std::cout << "out const: " << std::hex << "0x" << constant << " ";
				//std::cout << "Time difference = " << std::chrono::duration_cast<std::chrono::milliseconds>(end - begin).count() << "[ms]" << std::endl;
			}

			else if (check == z3::unsat)
			{
				printf("found all possible solutions!\n");
				solvedAll = true;
				break;
			}

			else
			{
				std::cout << "z3 timeout!" << std::endl;
			//	printf("timeout!");
				llvm::report_fatal_error("z3 timeout!");
				exit(1);
			}
		}

		while (true);

		*success = solvedAll;
		auto vec = new std::vector<uint64_t*>();
		for (auto& constInt : RecoveredConstants)
		{
			auto ptr = new uint64_t;
			*ptr = constInt.V;
			vec->push_back(ptr);
		}

		std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();
		std::cout << "Ms spent solving: = " << std::chrono::duration_cast<std::chrono::milliseconds>(end - begin).count() << "[ms]" << std::endl;
		return Dna::API::ImmutableManagedVector::NonCopyingFrom(vec);
	}

	DNA_EXPORT void SliceValue(llvm::Function* function, llvm::Value* value)
	{
		// Use Souper
		souper::InstContext IC;
		souper::ExprBuilderContext EBC;
		souper::ExprBuilderOptions EBO;
		souper::FunctionCandidateSet FCS;

		EBO.CandidateFilterInstruction = value;
		FCS = souper::ExtractCandidates(function, IC, EBC, EBO);

		// Identify the candidate
		souper::CandidateReplacement* CR = nullptr;
		int count = 0;
		for (auto& B : FCS.Blocks) {
			for (auto& R : B->Replacements) {
				if ((R.Origin != EBO.CandidateFilterInstruction) || (R.Mapping.LHS->Width != 64))
					continue;
				CR = &R;
				count++;
			}
		}

		if (!CR || count > 1)
			llvm::report_fatal_error("identifyChildren (2): no available candidates!");
		// Get the solutions
		souper::Inst* Solution = nullptr;
		// Create a new variable
		souper::Inst* ConstantVar = IC.createVar(CR->Mapping.LHS->Width, "ConstantVar");
		// Map the variable to the left-hand side
		souper::InstMapping IM(CR->Mapping.LHS, ConstantVar);
		// Search the valid solutions

		do {
			std::vector<souper::Inst*> ModelInstructions;
			std::vector<llvm::APInt> ModelValues;
			// Populate the preconditions
			std::vector<souper::Inst*> Preconditions;
			souper::Inst* Precondition = nullptr;
			// Build the exclusions
			/*
			for (const auto &RecoveredConstant : RecoveredConstants) {
			  souper::Inst *NotConstant =
				  IC.getInst(souper::Inst::Ne, 64, {CR->Mapping.LHS, RecoveredConstant.I});
			  Preconditions.push_back(NotConstant);
			}
			 */
			 // Add the preconditions
			if (!Preconditions.empty())
				Precondition = IC.getInst(souper::Inst::And, 1, Preconditions);
			// Build the query
			souper::BlockPCs BPCs;
			std::vector<souper::InstMapping> PCs;
			// TODO: verify why the BPCs and PCs may lead to no results sometimes
			// const auto &Query = souper::BuildQuery(IC, CR->BPCs, CR->PCs, IM, &ModelInstructions, Precondition, true);
			const auto& Query = souper::BuildQuery(IC, BPCs, PCs, IM, &ModelInstructions, Precondition, true);
			// Debug print the SMT query if requested
			if (true) {
				llvm::outs() << "[+] SMT Query:\n";
				llvm::outs() << Query << "\n";
			}


		}

		while (true);
	}


	void InitializePasses()
	{
		// Initialize passes.
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		llvm::initializeCore(Registry);
		llvm::initializeCodeGen(Registry);
		initializeLoopStrengthReducePass(Registry);
		initializeLowerIntrinsicsPass(Registry);
		initializeUnreachableBlockElimLegacyPassPass(Registry);
		initializeConstantHoistingLegacyPassPass(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeVectorization(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
		llvm::initializeGlobalsAAWrapperPassPass(Registry);
		llvm::initializeGVNLegacyPassPass(Registry);
		llvm::initializeDependenceAnalysisWrapperPassPass(Registry);

		llvm::initializeLoopInfoWrapperPassPass(Registry);

		// For codegen passes, only passes that do IR to IR transformation are
		// supported.
		initializeExpandLargeDivRemLegacyPassPass(Registry);
		initializeExpandLargeFpConvertLegacyPassPass(Registry);
		initializeScalarizeMaskedMemIntrinLegacyPassPass(Registry);
		initializeSelectOptimizePass(Registry);
		initializeCallBrPreparePass(Registry);
		initializeWinEHPreparePass(Registry);
		initializeDwarfEHPrepareLegacyPassPass(Registry);
		initializeSafeStackLegacyPassPass(Registry);
		initializeSjLjEHPreparePass(Registry);
		initializePreISelIntrinsicLoweringLegacyPassPass(Registry);
		initializeGlobalMergePass(Registry);
		initializeInterleavedLoadCombinePass(Registry);
		initializeInterleavedAccessPass(Registry);
		initializeUnreachableBlockElimLegacyPassPass(Registry);
		initializeExpandReductionsPass(Registry);
		initializeWasmEHPreparePass(Registry);
		initializeWriteBitcodePassPass(Registry);
		initializeReplaceWithVeclibLegacyPass(Registry);
		initializeJMCInstrumenterPass(Registry);


		initializeTarget(Registry);
	}

	void OptimizeModuleNew(llvm::Function* f)
	{
		InitializePasses();


		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		FPM.addPass(llvm::ReassociatePass());

		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::SpeculativeExecutionPass());
		FPM.addPass(llvm::JumpThreadingPass(99999));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());

		FPM.addPass(llvm::PromotePass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::SpeculativeExecutionPass());

		FPM.addPass(llvm::JumpThreadingPass(999999));
		FPM.addPass(llvm::JumpThreadingPass(-1));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		LPM.addPass(llvm::LoopSimplifyCFGPass());

		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());

		MPM.addPass(llvm::ModuleInlinerPass());

		// Add various optimization passes.
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::JumpThreadingPass());
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(llvm::createAggressiveInstCombinerPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::SROAPass({}));
		//FPM.addPass(llvm::createMergedLoadStoreMotionPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		// TODO: Insert custom passes for stack var elimination and load/store propagation
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		// TODO: Adhoc instruction combining

		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SCCPPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass());

		FPM.addPass(llvm::GVNHoistPass());

		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());

		FPM.addPass(llvm::SimplifyCFGPass());

		LPM.addPass(llvm::IndVarSimplifyPass());

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass());
		//FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());

		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

		FPM.run(*f, FAM);
	}

	void OptimizeModule(llvm::Module* module,
		llvm::Function* f,
		bool aggressiveUnroll,
		bool runClassifyingAliasAnalysis,
		Dna::Passes::tGetAliasResult getAliasResult,
		bool runConstantConcretization,
		Dna::Passes::tReadBinaryContents readBinaryContents,
		bool runStructuring,
		bool justGVN,
		Dna::Passes::tStructureFunction structureFunction)
	{
		// Run the "new" pipeline. The new pipeline is substantially more efficient, with a better order / selection of passes.
		// Though it is missing the passes intended for binary deobfuscation(namely binary load propagation, heuristic alias analysis, and enhanced store to load propagation).
		// Also note that the some of the passes are incorrectly named... (structure function is no longer a control flow structuring pass!, though it once was)
		OptimizeModuleNew(f);
		return;

		count++;
		/*
		const char* argv[7] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-dse-memoryssa-scanlimit=10000000",
		"-dse-memoryssa-partial-store-limit=10000000",
		"-memdep-block-scan-limit=500"
		};
		llvm::cl::ParseCommandLineOptions(7, argv);
		*/

		justGVN = false;
		if (justGVN)
		{
			const char* argv[13] = { "mesa", "-simplifycfg-sink-common=false",
					"-memdep-block-number-limit=10000000",
					"-dse-memoryssa-defs-per-block-limit=10000000",
					"-gvn-max-num-deps=100000000",
					"-dse-memoryssa-scanlimit=10000000",
					"-dse-memoryssa-partial-store-limit=10000000",
					"-gvn-max-block-speculations=10000000",
					"-memdep-block-scan-limit=28000",
					"-unroll-count=1500",
					"-unroll-threshold=100000000",
					"-enable-store-refinement=1"
			};
			llvm::cl::ParseCommandLineOptions(12, argv);
		}

		else

		{
			const char* argv[14] = { "mesa", "-simplifycfg-sink-common=false",
		"-memdep-block-number-limit=10000000",
		"-dse-memoryssa-defs-per-block-limit=10000000",
		"-gvn-max-num-deps=25000000",
		"-dse-memoryssa-scanlimit=900000000",
		"-dse-memoryssa-partial-store-limit=90000000",
		"-gvn-max-block-speculations=90000000",
		"-memdep-block-scan-limit=1000000000",
		"-unroll-count=1500",
		"-unroll-threshold=100000000",
		"-enable-store-refinement=0",
		"-memssa-check-limit=99999999",
		"-memssa-check-limit=99999999"
			};
			llvm::cl::ParseCommandLineOptions(14, argv);
		}


		// Initialize passes.
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		llvm::initializeCore(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
		llvm::initializeGlobalsAAWrapperPassPass(Registry);
		llvm::initializeGVNLegacyPassPass(Registry);
		llvm::initializeDependenceAnalysisWrapperPassPass(Registry);
		//llvm::initializeIPSCCPLegacyPassPass(Registry);
		initializeTarget(Registry);

		// Create pass managers.
		llvm::FunctionPassManager FPM;
		//llvm::PassManagerBuilder PMB;
		llvm::legacy::PassManager module_manager;
		FunctionAnalysisManager FAM;
		ModulePassManager MPM;
		ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		// Configure pipeline.
	//	PMB.OptLevel = 3;
	//	PMB.SizeLevel = 2;
	//	PMB.DisableUnrollLoops = true; //!Guide.RunLoopPasses;
	//	PMB.SLPVectorize = false;
	//	PMB.LoopVectorize = false;


		/*
		const char* args[2] = { "-dse-memoryssa-defs-per-block-limit=1000000", "-earlycse-mssa-optimization-cap=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args);

		const char* args4[2] = { "test4", "-dse-memoryssa-defs-per-block-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args4);

		const char* args5[2] = { "test5", "-dse-memoryssa-partial-store-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args5);

		const char* args6[2] = { "test6", "-dse-memoryssa-path-check-limit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args6);

		const char* args7[2] = { "test7", "-dse-memoryssa-scanlimit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args7);

		const char* args8[2] = { "test8", "-dse-memoryssa-walklimit=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args8);

		const char* args9[2] = { "test9", "-dse-memoryssa-otherbb-cost=2" };
		llvm::cl::ParseCommandLineOptions(2, args9);

		const char* args13[2] = { "test13", "-gvn-max-num-deps=100000000" };
		llvm::cl::ParseCommandLineOptions(2, args13);

		const char* args14[2] = { "test14", "-gvn-max-block-speculations=10000000" };
		llvm::cl::ParseCommandLineOptions(2, args14);

		const char* args15[2] = { "test15", "-gvn-max-num-visited-insts=1000000" };
		llvm::cl::ParseCommandLineOptions(2, args15);
		*/
		//const char* args10[2] = { "test10", "memdep-block-number-limit=10000" };
		//llvm::cl::ParseCommandLineOptions(2, args10);


		/*
		const char* args99999[11] = {
			"foo"
			"-memdep-block-number-limit=10000",
			"-dse-memoryssa-defs-per-block-limit=1000000",
			"-gvn-max-num-deps=100000000",
			"-dse-memoryssa-scanlimit=1000000",
			"-dse-memoryssa-otherbb-cost=2",
			"-dse-memoryssa-partial-store-limit=1000000",
			"-gvn-max-block-speculations=1000000",
			"-memdep-block-scan-limit=1000000",
			"-unroll-count=1500",
			"-unroll-threshold=100000000"
		};
		llvm::cl::ParseCommandLineOptions(11, args99999);
		*/

		
		const char* argv67[12] = { "mesa", "-simplifycfg-sink-common=false",
				"-memdep-block-number-limit=10000000",
				"-dse-memoryssa-defs-per-block-limit=10000000",
				"-gvn-max-num-deps=100000000",
				"-dse-memoryssa-scanlimit=10000000",
				"-dse-memoryssa-partial-store-limit=10000000",
				"-gvn-max-block-speculations=10000000",
				"-memdep-block-scan-limit=10000000",
				"-unroll-count=1500",
				"-unroll-threshold=100000000",
				"-enable-store-refinement=1"
		};

		llvm::cl::ParseCommandLineOptions(12, argv67);
			

		if (justGVN)
		{
			printf("just gvn. \n");
			//FPM.add(llvm::createCFLSteensAAWrapperPass());
			//FPM.addPass(llvm::createGlobalsAAWrapperPass());
			//FPM.addPass(llvm::SCEVAAWrapperPass());
			//FPM.add(llvm::createTypeBasedAAWrapperPass());
			//MPM.addPass(llvm::ScopedNoAliasAAWrapperPass());
			//FPM.addPass(llvm::BasicAAWrapperPass());
			//FPM.addPass(llvm::createLCSSAPass());

			// TODO: Properly pass the alias analysis func ptr.
			Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			for (int i = 0; i < 100; i++)
			{
				printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
			}
			//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());

			//MPM.addPass(new Dna::Passes::SegmentsExternalAAWrapperPass());

			FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));

			//FPM.addPass(llvm::)

			//PMB.populateFunctionPassManager(FPM);
			//PMB.populateModulePassManager(module_manager);

			//FPM.doInitialization();

			//FPM.run(*f, FAM);

			MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));
			MPM.run(*f->getParent(), MAM);

			return;
		}
		// Add the alias analysis passes.
		// Note: CSE i
		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::EarlyCSEPass(true));
		//FPM.addPass(llvm::createGlobalsAAWrapperPass());
		//FPM.addPass(llvm::SCEVAAWrapperPass());
		//FPM.addPass(llvm::createTypeBasedAAWrapperPass());
		//FPM.addPass(llvm::createScopedNoAliasAAWrapperPass());
		//FPM.addPass(llvm::BasicAAWrapperPass());
		if (runClassifyingAliasAnalysis)
		{
			for (int i = 0; i < 100; i++)
			{
				printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
			}
			// TODO: Properly pass the alias analysis func ptr.
			//Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;

			//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());

			//MPM.addPass(Dna::Passes::SegmentsExternalAAWrapperPass());
		}
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass(true));
		FPM.addPass(llvm::SpeculativeExecutionPass());
		FPM.addPass(llvm::JumpThreadingPass(99999));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());

	
		//FPM.addPass(llvm::CallSiteSplittingPass());

		//FPM.addPass(llvm::createIPSCCPPass());

		//FPM.addPass(llvm::createCalledValuePropagationPass());
		FPM.addPass(llvm::PromotePass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::SpeculativeExecutionPass());
		
		//FPM.addPass(llvm::LazyValueInfo());
		FPM.addPass(llvm::JumpThreadingPass(999999));
		FPM.addPass(llvm::JumpThreadingPass(-1));
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		LPM.addPass(llvm::LoopSimplifyCFGPass());


		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		

		// Add various optimization passes.
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::JumpThreadingPass());
		FPM.addPass(llvm::CorrelatedValuePropagationPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(llvm::createAggressiveInstCombinerPass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::ReassociatePass());
		FPM.addPass(llvm::SROAPass({}));
		//FPM.addPass(llvm::createMergedLoadStoreMotionPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::BDCEPass());
		//FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass());

		FPM.addPass(llvm::GVNHoistPass());
		FPM.addPass(llvm::NewGVNPass());
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		// Note: This legacy GVN pass is necessary for DSE to work properly.


		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		FPM.addPass(llvm::InstCombinePass());
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SimplifyCFGPass());    // added
		FPM.addPass(llvm::InstCombinePass()); // added
		FPM.addPass(llvm::SimplifyCFGPass());    // added
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		//if (runConstantConcretization)
		//	FPM.addPass(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));

		if (true)
		{
			/*
				const char* args2[2] = { "testtwo", "-unroll-count=1500" };
				llvm::cl::ParseCommandLineOptions(2, args2);
				const char* args3[2] = { "testhree", "-unroll-threshold=100000000" };
				llvm::cl::ParseCommandLineOptions(2, args3);
				const char* args4[2] = { "testfiyr", "-memdep-block-scan-limit=1000000" };
				llvm::cl::ParseCommandLineOptions(2, args4);
				FPM.addPass(llvm::createLoopUnrollPass(3, false, false, 9999999999, -1, 1));
				*/

			//LPM.addPass((llvm::LoopUnrollPass*)llvm::createLoopUnrollPass(3, false, false, 2000, -1, 1));
			FPM.addPass(llvm::LoopUnrollPass(llvm::LoopUnrollOptions(3, false, false)));
		}

		//const char* args17[2] = { "testfiyr", "-memdep-block-scan-limit=1000000" };
		//llvm::cl::ParseCommandLineOptions(2, args17);

		/*
		else
		{
			const char* args11[2] = { "testeleven", "-memdep-block-scan-limit=1000000" };
			bool succ = llvm::cl::ParseCommandLineOptions(2, args11);
			if (!succ)
			{
				printf("oh no.");
			}
		}
		*/
		LPM.addPass(llvm::IndVarSimplifyPass());
		//FPM.addPass(llvm::CreateConstraintEliminationPass());

		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::EarlyCSEPass());
		FPM.addPass(llvm::DSEPass()); // added
		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::ADCEPass());
		//FPM.addPass(llvm::createReassociatePass());

		// Note: We should avoid pointer PHIs here.
		if (false)
		{
			FPM.addPass(llvm::SimplifyCFGPass());
			//FPM.addPass(new llvm::sl::ControlledNodeSplittingPass());
			FPM.addPass(llvm::SimplifyCFGPass());
		//	FPM.addPass(llvm::sl::createUnswitchPass());
		}

		//FPM.addPass(llvm::createLoopRotatePass());


		FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
		//FPM.addPass(llvm::createFixIrreduciblePass());

		//FPM.addPass(llvm::createFixIrreduciblePass());
		//FPM.addPass(llvm::createGVNPass(false));
		//FPM.addPass(llvm::createNewGVNPass());
		//FPM.addPass(llvm::createStructurizeCFGPass());


		bool cns = false;
		if (cns)
		{
			FPM.addPass(llvm::SimplifyCFGPass());
			FPM.addPass(llvm::sl::ControlledNodeSplittingPass());
			FPM.addPass(llvm::sl::UnswitchPass());       // get rid of all switch instructions
			LPM.addPass(llvm::LoopSimplifyCFGPass());    // ensure all exit blocks are dominated by
			// the loop header
			//FPM.addPass(llvm::sl::LoopExitEnumerationPass());  // ensure all loops have <= 1 exits
			FPM.addPass(llvm::sl::UnswitchPass());       // get rid of all switch instructions
			// introduced by the loop exit enumeration

			FPM.addPass(llvm::sl::ControlledNodeSplittingPass());

		//	FPM.addPass(new Dna::Passes::ControlFlowStructuringPass());
		}

		printf("running.");
		//if (count == 13 || count == 14 || count == 15 || count == 16 || count == 17)
		if(structureFunction != nullptr)
		{
			printf("countcf.");
			//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
			FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		}

		//FPM.addPass(llvm::createFixIrreduciblePass());
		//FPM.addPass(llvm::createStructurizeCFGPass());

	//	PMB.populateFunctionPassManager(FPM);
	//	PMB.populateModulePassManager(module_manager);

		if (false)
		{
			//auto structuringPass = (llvm::sl::StructuredControlFlowPass*)llvm::sl::createASTComputePass();
			//MPM.addPass(llvm::sl::StructuredControlFlowPass());
		}



		try
		{
			MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));
			MPM.run(*f->getParent(), MAM);
			//f->dump();

			//FPM.run(*f, FAM);
		}

		catch (...)
		{
			printf("exception\n");
			//f->dump();
		}

		//module_manager.run(*f->getParent());
	}

	DNA_EXPORT void RunCfgCanonicalizationPipeline(llvm::Function* f)
	{
		// Run static pass initializers.
		InitializePasses();

		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		// Remove all switches. This simplifies analysis since we don't need to handle
		// cases where more than two case predecessors exist.
		FPM.addPass(llvm::LowerSwitchPass());
		// Remove irreducible control flow. Thus we only work with sane loops.
		// NOTE: It may be preferable to do controlled node splitting instead of FixIrreducible(which uses runtime indirection instead of node splitting).
		// The constraints would likely be easier to solve.
		FPM.addPass(llvm::FixIrreduciblePass());
		// Canonicalize the loop. Make sure all loops have dedicated exits(that is, no exit block for the loop has a predecessor
		// that is outside the loop. This implies that all exit blocks are dominated by the loop header.)
		FPM.addPass(llvm::LoopSimplifyPass());
		FPM.addPass(llvm::LCSSAPass());

		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM))); // Not needed anymore? 
		FPM.run(*f, FAM);
	}

	DNA_EXPORT void RunJumpTableSolvingPass(llvm::Function* f, Dna::Passes::tAnalyzeJumpTableBounds analyzeJumpTableBounds, Dna::Passes::tTrySolveConstant trySolveConstant)
	{
		// Run static pass initializers.
		InitializePasses();
		llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
		//Registry.registerPass(Dna::Passes::JumpTableAnalysisPass);


		// Create pass managers.
		llvm::FunctionPassManager FPM;
		llvm::legacy::PassManager module_manager;
		llvm::LoopAnalysisManager LAM;
		llvm::FunctionAnalysisManager FAM;
		llvm::CGSCCAnalysisManager CGAM;
		llvm::ModulePassManager MPM;
		llvm::ModuleAnalysisManager MAM;
		llvm::LoopPassManager LPM;
		llvm::PassBuilder PB;

		// Remove all switches. This simplifies analysis since we don't need to handle
		// cases where more than two case predecessors exist.
		FPM.addPass(Dna::Passes::JumpTableAnalysisPass(analyzeJumpTableBounds, trySolveConstant));

		//FAM.registerPass([analyzeJumpTableBounds, trySolveConstant] {return Dna::Passes::JumpTableAnalysisPass(analyzeJumpTableBounds, trySolveConstant); });
		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM))); // Not needed anymore? 
		FPM.run(*f, FAM);
	}
}

void OptimizeVmpModule(llvm::Module* module,
	llvm::Function* f,
	bool aggressiveUnroll,
	bool runClassifyingAliasAnalysis,
	Dna::Passes::tGetAliasResult getAliasResult,
	bool runConstantConcretization,
	Dna::Passes::tReadBinaryContents readBinaryContents,
	bool runStructuring,
	bool justGVN,
	Dna::Passes::tStructureFunction structureFunction,
	Dna::Passes::tEliminateStackVars eliminateStackVars,
	Dna::Passes::tEliminateStackVars adhocInstCombine,
	Dna::Passes::tEliminateStackVars multiUseCloning)
{
	/*
	const char* argv[7] = { "mesa", "-simplifycfg-sink-common=false",
	"-memdep-block-number-limit=10000000",
	"-dse-memoryssa-defs-per-block-limit=10000000",
	"-dse-memoryssa-scanlimit=10000000",
	"-dse-memoryssa-partial-store-limit=10000000",
	"-memdep-block-scan-limit=500"
	};
	llvm::cl::ParseCommandLineOptions(7, argv);
	*/

	justGVN = false;
	if (justGVN)
	{
		const char* argv[13] = { "mesa", "-simplifycfg-sink-common=false",
				"-memdep-block-number-limit=10000000",
				"-dse-memoryssa-defs-per-block-limit=10000000",
				"-gvn-max-num-deps=100000000",
				"-dse-memoryssa-scanlimit=10000000",
				"-dse-memoryssa-partial-store-limit=10000000",
				"-gvn-max-block-speculations=10000000",
				"-memdep-block-scan-limit=28000",
				"-unroll-count=1500",
				"-unroll-threshold=100000000",
				"-enable-store-refinement=1"
		};
		llvm::cl::ParseCommandLineOptions(12, argv);
	}

	else

	{
		const char* argv[14] = { "mesa", "-simplifycfg-sink-common=false",
	"-memdep-block-number-limit=10000000",
	"-dse-memoryssa-defs-per-block-limit=10000000",
	"-gvn-max-num-deps=25000000",
	"-dse-memoryssa-scanlimit=900000000",
	"-dse-memoryssa-partial-store-limit=90000000",
	"-gvn-max-block-speculations=90000000",
	"-memdep-block-scan-limit=1000000000",
	"-unroll-count=3",
	"-unroll-threshold=100000000",
	"-enable-store-refinement=0",
	"-memssa-check-limit=99999999",
	"-memssa-check-limit=99999999"
		};
		llvm::cl::ParseCommandLineOptions(14, argv);
	}


	// Initialize passes.
	llvm::PassRegistry& Registry = *llvm::PassRegistry::getPassRegistry();
	llvm::initializeCore(Registry);
	llvm::initializeScalarOpts(Registry);
	llvm::initializeIPO(Registry);
	llvm::initializeAnalysis(Registry);
	llvm::initializeTransformUtils(Registry);
	llvm::initializeInstCombine(Registry);
	llvm::initializeTargetLibraryInfoWrapperPassPass(Registry);
	llvm::initializeGlobalsAAWrapperPassPass(Registry);
	llvm::initializeGVNLegacyPassPass(Registry);
	llvm::initializeDependenceAnalysisWrapperPassPass(Registry);
	//llvm::initializeIPSCCPLegacyPassPass(Registry);
	initializeTarget(Registry);

	// Create pass managers.
	llvm::FunctionPassManager FPM;
	//llvm::PassManagerBuilder PMB;
	llvm::legacy::PassManager module_manager;
	llvm::LoopAnalysisManager LAM;
	llvm::FunctionAnalysisManager FAM;
	llvm::CGSCCAnalysisManager CGAM;
	llvm::ModulePassManager MPM;
	llvm::ModuleAnalysisManager MAM;
	llvm::LoopPassManager LPM;
	llvm::PassBuilder PB;
	// Configure pipeline.
//	PMB.OptLevel = 3;
//	PMB.SizeLevel = 2;
//	PMB.DisableUnrollLoops = true; //!Guide.RunLoopPasses;
//	PMB.SLPVectorize = false;
//	PMB.LoopVectorize = false;


	/*
	const char* args[2] = { "-dse-memoryssa-defs-per-block-limit=1000000", "-earlycse-mssa-optimization-cap=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args);

	const char* args4[2] = { "test4", "-dse-memoryssa-defs-per-block-limit=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args4);

	const char* args5[2] = { "test5", "-dse-memoryssa-partial-store-limit=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args5);

	const char* args6[2] = { "test6", "-dse-memoryssa-path-check-limit=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args6);

	const char* args7[2] = { "test7", "-dse-memoryssa-scanlimit=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args7);

	const char* args8[2] = { "test8", "-dse-memoryssa-walklimit=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args8);

	const char* args9[2] = { "test9", "-dse-memoryssa-otherbb-cost=2" };
	llvm::cl::ParseCommandLineOptions(2, args9);

	const char* args13[2] = { "test13", "-gvn-max-num-deps=100000000" };
	llvm::cl::ParseCommandLineOptions(2, args13);

	const char* args14[2] = { "test14", "-gvn-max-block-speculations=10000000" };
	llvm::cl::ParseCommandLineOptions(2, args14);

	const char* args15[2] = { "test15", "-gvn-max-num-visited-insts=1000000" };
	llvm::cl::ParseCommandLineOptions(2, args15);
	*/
	//const char* args10[2] = { "test10", "memdep-block-number-limit=10000" };
	//llvm::cl::ParseCommandLineOptions(2, args10);


	/*
	const char* args99999[11] = {
		"foo"
		"-memdep-block-number-limit=10000",
		"-dse-memoryssa-defs-per-block-limit=1000000",
		"-gvn-max-num-deps=100000000",
		"-dse-memoryssa-scanlimit=1000000",
		"-dse-memoryssa-otherbb-cost=2",
		"-dse-memoryssa-partial-store-limit=1000000",
		"-gvn-max-block-speculations=1000000",
		"-memdep-block-scan-limit=1000000",
		"-unroll-count=1500",
		"-unroll-threshold=100000000"
	};
	llvm::cl::ParseCommandLineOptions(11, args99999);
	*/


	const char* argv67[12] = { "mesa", "-simplifycfg-sink-common=false",
			"-memdep-block-number-limit=10000000",
			"-dse-memoryssa-defs-per-block-limit=10000000",
			"-gvn-max-num-deps=100000000",
			"-dse-memoryssa-scanlimit=10000000",
			"-dse-memoryssa-partial-store-limit=10000000",
			"-gvn-max-block-speculations=10000000",
			"-memdep-block-scan-limit=10000000",
			"-unroll-count=1500",
			"-unroll-threshold=100000000",
			"-enable-store-refinement=1"
	};

	llvm::cl::ParseCommandLineOptions(12, argv67);


	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::BDCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	//FPM.addPass(llvm::EarlyCSEPass(true));
	FPM.addPass(llvm::ReassociatePass());
	//FPM.addPass(llvm::EarlyCSEPass(true));

	if (structureFunction != nullptr)
	{
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (runClassifyingAliasAnalysis)
	{
		for (int i = 0; i < 10; i++)
		{
			printf("TODO: Upgrade SegmentsExternalAAWrapperPass to LLVM16.");
		}
		// TODO: Properly pass the alias analysis func ptr.
		//Dna::Passes::ClassifyingAAResult::gGetAliasResult = getAliasResult;
		//MPM.addPass(Dna::Passes::createSegmentsAAWrapperPass());
		//MPM.addPass(Dna::Passes::SegmentsExternalAAWrapperPass());
	}
	FPM.addPass(llvm::EarlyCSEPass(true));
	FPM.addPass(llvm::SpeculativeExecutionPass());
	FPM.addPass(llvm::JumpThreadingPass(99999));
	FPM.addPass(llvm::CorrelatedValuePropagationPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	FPM.addPass(llvm::ReassociatePass());

	FPM.addPass(llvm::LoopSimplifyPass());
	LPM.addPass(llvm::LoopSimplifyCFGPass());
	LPM.addPass(llvm::LICMPass(500, 500, true)); // TODO: LICM
	LPM.addPass(llvm::LoopRotatePass());

	FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::LCSSAPass());
	LPM.addPass(llvm::IndVarSimplifyPass());
	LPM.addPass(llvm::LoopDeletionPass());

	FPM.addPass(llvm::PromotePass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::SpeculativeExecutionPass());

	//FPM.addPass(llvm::LazyValueInfo());
	FPM.addPass(llvm::JumpThreadingPass(999999));
	FPM.addPass(llvm::JumpThreadingPass(-1));
	FPM.addPass(llvm::CorrelatedValuePropagationPass());

	if (multiUseCloning != nullptr)
	{
		FPM.addPass(Dna::Passes::MultiUseCloningPass(multiUseCloning));
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::SROAPass({}));
		FPM.addPass(llvm::BDCEPass());
	}

	//FPM.addPass(FunctionDumpPass("test.ll"));
	FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::BDCEPass());



	MPM.addPass(llvm::ModuleInlinerPass());


	// Add various optimization passes.
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::JumpThreadingPass());
	FPM.addPass(llvm::CorrelatedValuePropagationPass());
	FPM.addPass(llvm::SimplifyCFGPass());
	//FPM.addPass(llvm::createAggressiveInstCombinerPass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::ReassociatePass());
	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::NewGVNPass());
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());

	if (structureFunction != nullptr)
	{
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));
		FPM.addPass(llvm::ADCEPass());
	}

	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());
	if (eliminateStackVars != nullptr)
	{
		FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));
		FPM.addPass(llvm::ADCEPass());
	}

	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::BDCEPass());
	if (adhocInstCombine != nullptr)
	{
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
	}

	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SCCPPass());
	//FPM.addPass(llvm::InstCombinePass());
	FPM.addPass(llvm::DSEPass());

	FPM.addPass(llvm::GVNHoistPass());
	//FPM.addPass(llvm::NewGVNPass());
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	// Note: This legacy GVN pass is necessary for DSE to work properly.


	FPM.addPass(llvm::ADCEPass());
	FPM.addPass(llvm::SimplifyCFGPass());

	if (adhocInstCombine != nullptr)
	{
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
	}

	//FPM.addPass(llvm::DSEPass()); // added
	FPM.addPass(llvm::SimplifyCFGPass());    // added
	//FPM.addPass(llvm::InstCombinePass()); // added
	//FPM.addPass(llvm::SimplifyCFGPass());    // added
	//FPM.addPass(llvm::DSEPass()); // added
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	//if (runConstantConcretization)
	//	FPM.addPass(Dna::Passes::getConstantConcretizationPassPass(readBinaryContents)); // added
	//FPM.addPass(llvm::DSEPass()); // added
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));


	LPM.addPass(llvm::IndVarSimplifyPass());
	//FPM.addPass(llvm::CreateConstraintEliminationPass());

	FPM.addPass(llvm::SROAPass({}));
	FPM.addPass(llvm::EarlyCSEPass());
	//FPM.addPass(llvm::DSEPass()); // added
	FPM.addPass(llvm::SCCPPass());
	FPM.addPass(llvm::ADCEPass());
	//FPM.addPass(llvm::createReassociatePass());

	// Note: We should avoid pointer PHIs here.
	if (false)
	{
		FPM.addPass(llvm::SimplifyCFGPass());
		//FPM.addPass(new llvm::sl::ControlledNodeSplittingPass());
		FPM.addPass(llvm::SimplifyCFGPass());
		//	FPM.addPass(llvm::sl::createUnswitchPass());
	}

	//FPM.addPass(llvm::createLoopRotatePass());
	//FPM.addPass(llvm::GVNPass(llvm::GVNOptions()));
	//FPM.addPass(llvm::createFixIrreduciblePass());
	//FPM.addPass(llvm::createFixIrreduciblePass());
	//FPM.addPass(llvm::createGVNPass(false));
	//FPM.addPass(llvm::createNewGVNPass());
	//FPM.addPass(llvm::createStructurizeCFGPass());

	if (structureFunction != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::ControlFlowStructuringPass(structureFunction));

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (eliminateStackVars != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::OpaqueStackVarEliminationPass(eliminateStackVars));

		FPM.addPass(llvm::SCCPPass());
		FPM.addPass(llvm::ADCEPass());
		FPM.addPass(llvm::BDCEPass());
	}

	if (adhocInstCombine != nullptr)
	{
		//llvm::FunctionPass* fp = new Dna::Passes::ControlFlowStructuringPass(structureFunction);
		FPM.addPass(Dna::Passes::AdhocInstCombinePass(adhocInstCombine));
		FPM.addPass(llvm::ADCEPass());
	}


	try
	{
		FAM.registerPass([&] { return PB.buildDefaultAAPipeline(); });
		PB.registerModuleAnalyses(MAM);
		PB.registerCGSCCAnalyses(CGAM);
		PB.registerFunctionAnalyses(FAM);
		PB.registerLoopAnalyses(LAM);
		PB.crossRegisterProxies(LAM, FAM, CGAM, MAM);

		//MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

		FPM.addPass(createFunctionToLoopPassAdaptor<LoopPassManager>(
			std::move(LPM), /*UseMemorySSA=*/true,
			/*UseBlockFrequencyInfo=*/true));


		FPM.run(*f, FAM);
	}

	catch (...)
	{
		printf("Exception in pass pipeline!\n");

	}

	//module_manager.run(*f->getParent());
}

DNA_EXPORT void OptimizeModuleVmp(llvm::Module* module,
	llvm::Function* f,
	bool aggressiveUnroll,
	bool runClassifyingAliasAnalysis,
	Dna::Passes::tGetAliasResult getAliasResult,
	bool runConstantConcretization,
	Dna::Passes::tReadBinaryContents readBinaryContents,
	bool runStructuring,
	bool justGVN,
	Dna::Passes::tStructureFunction structureFunction,
	Dna::Passes::tEliminateStackVars eliminateStackVars,
	Dna::Passes::tEliminateStackVars adhocInstCombine,
	Dna::Passes::tEliminateStackVars multiUseCloning)
{
	OptimizeVmpModule(module, f, aggressiveUnroll, runClassifyingAliasAnalysis, getAliasResult, runConstantConcretization, readBinaryContents, runStructuring, justGVN, structureFunction, eliminateStackVars, adhocInstCombine, multiUseCloning);
}
