#pragma once

#include <llvm/Transforms/Scalar.h>
#include <API/ExportDef.h>

using namespace llvm;

//===----------------------------------------------------------------------===//
//
// AlignmentFromAssumptions - Use assume intrinsics to set load/store
// alignments.
//
DNA_EXPORT FunctionPass* CreateAlignmentFromAssumptionsPass()
{
    return createAlignmentFromAssumptionsPass();
}

//===----------------------------------------------------------------------===//
//
// AnnotationRemarks - Emit remarks for !annotation metadata.
//
DNA_EXPORT FunctionPass* CreateAnnotationRemarksLegacyPass()
{
    return createAnnotationRemarksLegacyPass();
}

//===----------------------------------------------------------------------===//
//
// SCCP - Sparse conditional constant propagation.
//
DNA_EXPORT FunctionPass* CreateSCCPPass()
{
    return createSCCPPass();
}

//===----------------------------------------------------------------------===//
//
// RedundantDbgInstElimination - This pass removes redundant dbg intrinsics
// without modifying the CFG of the function.  It is a FunctionPass.
//
DNA_EXPORT Pass* CreateRedundantDbgInstEliminationPass()
{
    return createRedundantDbgInstEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// DeadCodeElimination - This pass is more powerful than DeadInstElimination,// because it is worklist driven that can potentially revisit instructions when
// their other instructions become dead, to eliminate chains of dead
// computations.
//
DNA_EXPORT FunctionPass* CreateDeadCodeEliminationPass()
{
    return createDeadCodeEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// DeadStoreElimination - This pass deletes stores that are post-dominated by
// must-aliased stores and are not loaded used between the stores.
//
DNA_EXPORT FunctionPass* CreateDeadStoreEliminationPass()
{
    return createDeadStoreEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// CallSiteSplitting - This pass split call-site based on its known argument
// values.
DNA_EXPORT FunctionPass* CreateCallSiteSplittingPass()
{
    return createCallSiteSplittingPass();
}

//===----------------------------------------------------------------------===//
//
// AggressiveDCE - This pass uses the SSA based Aggressive DCE algorithm.  This
// algorithm assumes instructions are dead until proven otherwise, which makes
// it more successful are removing non-obviously dead instructions.
//
DNA_EXPORT FunctionPass* CreateAggressiveDCEPass()
{
    return createAggressiveDCEPass();
}

//===----------------------------------------------------------------------===//
//
// GuardWidening - An optimization over the @llvm.experimental.guard intrinsic
// that (optimistically) combines multiple guards into one to have fewer checks
// at runtime.
//
DNA_EXPORT FunctionPass* CreateGuardWideningPass()
{
    return createGuardWideningPass();
}

//===----------------------------------------------------------------------===//
//
// LoopGuardWidening - Analogous to the GuardWidening pass, but restricted to a
// single loop at a time for use within a LoopPassManager.  Desired effect is
// to widen guards into preheader or a single guard within loop if that's not
// possible.
//
DNA_EXPORT Pass* CreateLoopGuardWideningPass()
{
    return createLoopGuardWideningPass();
}

//===----------------------------------------------------------------------===//
//
// BitTrackingDCE - This pass uses a bit-tracking DCE algorithm in order to
// remove computations of dead bits.
//
DNA_EXPORT FunctionPass* CreateBitTrackingDCEPass()
{
    return createBitTrackingDCEPass();
}

//===----------------------------------------------------------------------===//
//
// SROA - Replace aggregates or pieces of aggregates with scalar SSA values.
//
DNA_EXPORT FunctionPass* CreateSROAPass()
{
    return createSROAPass();
}

//===----------------------------------------------------------------------===//
//
// InductiveRangeCheckElimination - Transform loops to elide range checks on
// linear functions of the induction variable.
//
DNA_EXPORT Pass* CreateInductiveRangeCheckEliminationPass()
{
    return createInductiveRangeCheckEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// InductionVariableSimplify - Transform induction variables in a program to all
// use a single canonical induction variable per loop.
//
DNA_EXPORT Pass* CreateIndVarSimplifyPass()
{
    return createIndVarSimplifyPass();
}

//===----------------------------------------------------------------------===//
//
// LICM - This pass is a loop invariant code motion and memory promotion pass.
//
DNA_EXPORT Pass* CreateLICMPass()
{
    return createLICMPass();
}

DNA_EXPORT Pass* CreateLICMPass2(unsigned LicmMssaOptCap, unsigned LicmMssaNoAccForPromotionCap, bool AllowSpeculation)
{
    return createLICMPass(LicmMssaOptCap, LicmMssaNoAccForPromotionCap, AllowSpeculation);
}

//===----------------------------------------------------------------------===//
//
// LoopSink - This pass sinks invariants from preheader to loop body where
// frequency is lower than loop preheader.
//
DNA_EXPORT Pass* CreateLoopSinkPass()
{
    return createLoopSinkPass();
}

//===----------------------------------------------------------------------===//
//
// LoopPredication - This pass does loop predication on guards.
//
DNA_EXPORT Pass* CreateLoopPredicationPass()
{
    return createLoopPredicationPass();
}

//===----------------------------------------------------------------------===//
//
// LoopInterchange - This pass interchanges loops to provide a more
// cache-friendly memory access patterns.
//
DNA_EXPORT Pass* CreateLoopInterchangePass()
{
    return createLoopInterchangePass();
}

//===----------------------------------------------------------------------===//
//
// LoopFlatten - This pass flattens nested loops into a single loop.
//
DNA_EXPORT FunctionPass* CreateLoopFlattenPass()
{
    return createLoopFlattenPass();
}

//===----------------------------------------------------------------------===//
//
// LoopStrengthReduce - This pass is strength reduces GEP instructions that use
// a loop's canonical induction variable as one of their indices.
//
DNA_EXPORT Pass* CreateLoopStrengthReducePass()
{
    return createLoopStrengthReducePass();
}

//===----------------------------------------------------------------------===//
//
// LoopInstSimplify - This pass simplifies instructions in a loop's body.
//
DNA_EXPORT Pass* CreateLoopInstSimplifyPass()
{
    return createLoopInstSimplifyPass();
}

//===----------------------------------------------------------------------===//
//
// LoopUnroll - This pass is a simple loop unrolling pass.
//
DNA_EXPORT Pass* CreateLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV, int Threshold, int Count, int AllowPartial, int Runtime, int UpperBound, int AllowPeeling)
{
    return createLoopUnrollPass(OptLevel, OnlyWhenForced, ForgetAllSCEV, Threshold, Count, AllowPartial, Runtime, UpperBound, AllowPeeling);
}

// Create an unrolling pass for full unrolling that uses exact trip count only
// and also does peeling.
DNA_EXPORT Pass* CreateSimpleLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV)
{
    return createSimpleLoopUnrollPass(OptLevel, OnlyWhenForced, ForgetAllSCEV);
}

//===----------------------------------------------------------------------===//
//
// LoopUnrollAndJam - This pass is a simple loop unroll and jam pass.
//
DNA_EXPORT Pass* CreateLoopUnrollAndJamPass(int OptLevel)
{
    return createLoopUnrollAndJamPass(OptLevel);
}

//===----------------------------------------------------------------------===//
//
// LoopReroll - This pass is a simple loop rerolling pass.
//
DNA_EXPORT Pass* CreateLoopRerollPass()
{
    return createLoopRerollPass();
}

//===----------------------------------------------------------------------===//
//
// LoopRotate - This pass is a simple loop rotating pass.
//
DNA_EXPORT Pass* CreateLoopRotatePass(int MaxHeaderSize, bool PrepareForLTO)
{
    return createLoopRotatePass(MaxHeaderSize, PrepareForLTO);
}

//===----------------------------------------------------------------------===//
//
// LoopIdiom - This pass recognizes and replaces idioms in loops.
//
DNA_EXPORT Pass* CreateLoopIdiomPass()
{
    return createLoopIdiomPass();
}

//===----------------------------------------------------------------------===//
//
// DemoteRegisterToMemoryPass - This pass is used to demote registers to memory
// references. In basically undoes the PromoteMemoryToRegister pass to make cfg
// hacking easier.
//
DNA_EXPORT FunctionPass* CreateDemoteRegisterToMemoryPass()
{
    return createDemoteRegisterToMemoryPass();
}

extern char& DemoteRegisterToMemoryID;
//===----------------------------------------------------------------------===//
//
// Reassociate - This pass reassociates commutative expressions in an order that
// is designed to promote better constant propagation, GCSE, LICM, PRE...
//
// For example:  4 + (x + 5)  ->  x + (4 + 5)
//
DNA_EXPORT FunctionPass* CreateReassociatePass()
{
    return createReassociatePass();
}

//===----------------------------------------------------------------------===//
//
// JumpThreading - Thread control through mult-pred/multi-succ blocks where some
// preds always go to some succ. Thresholds other than minus one
// override the internal BB duplication default threshold.
//
DNA_EXPORT FunctionPass* CreateJumpThreadingPass(int Threshold)
{
    return createJumpThreadingPass(Threshold);
}

//===----------------------------------------------------------------------===//
//
// DFAJumpThreading - When a switch statement inside a loop is used to
// implement a deterministic finite automata we can jump thread the switch
// statement reducing number of conditional jumps.
//
DNA_EXPORT FunctionPass* CreateDFAJumpThreadingPass()
{
    return createDFAJumpThreadingPass();
}

//===----------------------------------------------------------------------===//
//
// CFGSimplification - Merge basic blocks, eliminate unreachable blocks,// simplify terminator instructions, convert switches to lookup tables, etc.
//
DNA_EXPORT FunctionPass* CreateCFGSimplificationPass()
{
    return createCFGSimplificationPass();
}

//===----------------------------------------------------------------------===//
//
// FlattenCFG - flatten CFG, reduce number of conditional branches by using
// parallel-and and parallel-or mode, etc...
//
DNA_EXPORT FunctionPass* CreateFlattenCFGPass()
{
    return createFlattenCFGPass();
}

//===----------------------------------------------------------------------===//
//
// CFG Structurization - Remove irreducible control flow
//
///
/// When \p SkipUniformRegions is true the structizer will not structurize
/// regions that only contain uniform branches.
DNA_EXPORT Pass* CreateStructurizeCFGPass(bool SkipUniformRegions)
{
    return createStructurizeCFGPass(SkipUniformRegions);
}

//===----------------------------------------------------------------------===//
//
// TailCallElimination - This pass eliminates call instructions to the current
// function which occur immediately before return instructions.
//
DNA_EXPORT FunctionPass* CreateTailCallEliminationPass()
{
    return createTailCallEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// EarlyCSE - This pass performs a simple and fast CSE pass over the dominator
// tree.
//
DNA_EXPORT FunctionPass* CreateEarlyCSEPass(bool UseMemorySSA)
{
    return createEarlyCSEPass(UseMemorySSA);
}

//===----------------------------------------------------------------------===//
//
// GVNHoist - This pass performs a simple and fast GVN pass over the dominator
// tree to hoist common expressions from sibling branches.
//
DNA_EXPORT FunctionPass* CreateGVNHoistPass()
{
    return createGVNHoistPass();
}

//===----------------------------------------------------------------------===//
//
// GVNSink - This pass uses an "inverted" value numbering to decide the
// similarity of expressions and sinks similar expressions into successors.
//
DNA_EXPORT FunctionPass* CreateGVNSinkPass()
{
    return createGVNSinkPass();
}

//===----------------------------------------------------------------------===//
//
// MergedLoadStoreMotion - This pass merges loads and stores in diamonds. Loads
// are hoisted into the header, while stores sink into the footer.
//
DNA_EXPORT FunctionPass* CreateMergedLoadStoreMotionPass(bool SplitFooterBB)
{
    return createMergedLoadStoreMotionPass(SplitFooterBB);
}

//===----------------------------------------------------------------------===//
//
// GVN - This pass performs global value numbering and redundant load
// elimination cotemporaneously.
//
DNA_EXPORT FunctionPass* CreateNewGVNPass()
{
    return createNewGVNPass();
}

//===----------------------------------------------------------------------===//
//
// DivRemPairs - Hoist/decompose integer division and remainder instructions.
//
DNA_EXPORT FunctionPass* CreateDivRemPairsPass()
{
    return createDivRemPairsPass();
}

//===----------------------------------------------------------------------===//
//
// MemCpyOpt - This pass performs optimizations related to eliminating memcpy
// calls and/or combining multiple stores into memset's.
//
DNA_EXPORT FunctionPass* CreateMemCpyOptPass()
{
    return createMemCpyOptPass();
}

//===----------------------------------------------------------------------===//
//
// LoopDeletion - This pass performs DCE of non-infinite loops that it
// can prove are dead.
//
DNA_EXPORT Pass* CreateLoopDeletionPass()
{
    return createLoopDeletionPass();
}

//===----------------------------------------------------------------------===//
//
// ConstantHoisting - This pass prepares a function for expensive constants.
//
DNA_EXPORT FunctionPass* CreateConstantHoistingPass()
{
    return createConstantHoistingPass();
}

//===----------------------------------------------------------------------===//
//
// ConstraintElimination - This pass eliminates conditions based on found
//                         constraints.
//
DNA_EXPORT FunctionPass* CreateConstraintEliminationPass()
{
    return createConstraintEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// Sink - Code Sinking
//
DNA_EXPORT FunctionPass* CreateSinkingPass()
{
    return createSinkingPass();
}

//===----------------------------------------------------------------------===//
//
// LowerAtomic - Lower atomic intrinsics to non-atomic form
//
DNA_EXPORT Pass* CreateLowerAtomicPass()
{
    return createLowerAtomicPass();
}

//===----------------------------------------------------------------------===//
//
// LowerGuardIntrinsic - Lower guard intrinsics to normal control flow.
//
DNA_EXPORT Pass* CreateLowerGuardIntrinsicPass()
{
    return createLowerGuardIntrinsicPass();
}

//===----------------------------------------------------------------------===//
//
// LowerMatrixIntrinsics - Lower matrix intrinsics to vector operations.
//
DNA_EXPORT Pass* CreateLowerMatrixIntrinsicsPass()
{
    return createLowerMatrixIntrinsicsPass();
}

//===----------------------------------------------------------------------===//
//
// LowerMatrixIntrinsicsMinimal - Lower matrix intrinsics to vector operations
//                               (lightweight, does not require extra analysis)
//
DNA_EXPORT Pass* CreateLowerMatrixIntrinsicsMinimalPass()
{
    return createLowerMatrixIntrinsicsMinimalPass();
}

//===----------------------------------------------------------------------===//
//
// LowerWidenableCondition - Lower widenable condition to i1 true.
//
DNA_EXPORT Pass* CreateLowerWidenableConditionPass()
{
    return createLowerWidenableConditionPass();
}

//===----------------------------------------------------------------------===//
//
// MergeICmps - Merge integer comparison chains into a memcmp
//
DNA_EXPORT Pass* CreateMergeICmpsLegacyPass()
{
    return createMergeICmpsLegacyPass();
}

//===----------------------------------------------------------------------===//
//
// ValuePropagation - Propagate CFG-derived value information
//
DNA_EXPORT Pass* CreateCorrelatedValuePropagationPass()
{
    return createCorrelatedValuePropagationPass();
}

//===----------------------------------------------------------------------===//
//
// InferAddressSpaces - Modify users of addrspacecast instructions with values
// in the source address space if using the destination address space is slower
// on the target. If AddressSpace is left to its default value, it will be
// obtained from the TargetTransformInfo.
//
DNA_EXPORT FunctionPass* CreateInferAddressSpacesPass(unsigned AddressSpace)
{
    return createInferAddressSpacesPass(AddressSpace);
}

extern char& InferAddressSpacesID;
//===----------------------------------------------------------------------===//
//
// LowerExpectIntrinsics - Removes llvm.expect intrinsics and creates
// "block_weights" metadata.
DNA_EXPORT FunctionPass* CreateLowerExpectIntrinsicPass()
{
    return createLowerExpectIntrinsicPass();
}

//===----------------------------------------------------------------------===//
//
// TLSVariableHoist - This pass reduce duplicated TLS address call.
//
DNA_EXPORT FunctionPass* CreateTLSVariableHoistPass()
{
    return createTLSVariableHoistPass();
}

//===----------------------------------------------------------------------===//
//
// LowerConstantIntrinsicss - Expand any remaining llvm.objectsize and
// llvm.is.constant intrinsic calls, even for the unknown cases.
//
DNA_EXPORT FunctionPass* CreateLowerConstantIntrinsicsPass()
{
    return createLowerConstantIntrinsicsPass();
}

//===----------------------------------------------------------------------===//
//
// PartiallyInlineLibCalls - Tries to inline the fast path of library
// calls such as sqrt.
//
DNA_EXPORT FunctionPass* CreatePartiallyInlineLibCallsPass()
{
    return createPartiallyInlineLibCallsPass();
}

//===----------------------------------------------------------------------===//
//
// SeparateConstOffsetFromGEP - Split GEPs for better CSE
//
DNA_EXPORT FunctionPass* CreateSeparateConstOffsetFromGEPPass(bool LowerGEP)
{
    return createSeparateConstOffsetFromGEPPass(LowerGEP);
}

//===----------------------------------------------------------------------===//
//
// SpeculativeExecution - Aggressively hoist instructions to enable
// speculative execution on targets where branches are expensive.
//
DNA_EXPORT FunctionPass* CreateSpeculativeExecutionPass()
{
    return createSpeculativeExecutionPass();
}

// Same as createSpeculativeExecutionPass, but does nothing unless
// TargetTransformInfo::hasBranchDivergence() is true.
DNA_EXPORT FunctionPass* CreateSpeculativeExecutionIfHasBranchDivergencePass()
{
    return createSpeculativeExecutionIfHasBranchDivergencePass();
}

//===----------------------------------------------------------------------===//
//
// StraightLineStrengthReduce - This pass strength-reduces some certain
// instruction patterns in straight-line code.
//
DNA_EXPORT FunctionPass* CreateStraightLineStrengthReducePass()
{
    return createStraightLineStrengthReducePass();
}

//===----------------------------------------------------------------------===//
//
// PlaceSafepoints - Rewrite any IR calls to gc.statepoints and insert any
// safepoint polls (method entry, backedge) that might be required.  This pass
// does not generate explicit relocation sequences - that's handled by
// RewriteStatepointsForGC which can be run at an arbitrary point in the pass
// order following this pass.
//
DNA_EXPORT FunctionPass* CreatePlaceSafepointsPass()
{
    return createPlaceSafepointsPass();
}

//===----------------------------------------------------------------------===//
//
// RewriteStatepointsForGC - Rewrite any gc.statepoints which do not yet have
// explicit relocations to include explicit relocations.
//
DNA_EXPORT ModulePass* CreateRewriteStatepointsForGCLegacyPass()
{
    return createRewriteStatepointsForGCLegacyPass();
}

//===----------------------------------------------------------------------===//
//
// Float2Int - Demote floats to ints where possible.
//
DNA_EXPORT FunctionPass* CreateFloat2IntPass()
{
    return createFloat2IntPass();
}

//===----------------------------------------------------------------------===//
//
// NaryReassociate - Simplify n-ary operations by reassociation.
//
DNA_EXPORT FunctionPass* CreateNaryReassociatePass()
{
    return createNaryReassociatePass();
}

//===----------------------------------------------------------------------===//
//
// LoopDistribute - Distribute loops.
//
DNA_EXPORT FunctionPass* CreateLoopDistributePass()
{
    return createLoopDistributePass();
}

//===----------------------------------------------------------------------===//
//
// LoopFuse - Fuse loops.
//
DNA_EXPORT FunctionPass* CreateLoopFusePass()
{
    return createLoopFusePass();
}

//===----------------------------------------------------------------------===//
//
// LoopLoadElimination - Perform loop-aware load elimination.
//
DNA_EXPORT FunctionPass* CreateLoopLoadEliminationPass()
{
    return createLoopLoadEliminationPass();
}

//===----------------------------------------------------------------------===//
//
// LoopDataPrefetch - Perform data prefetching in loops.
//
DNA_EXPORT FunctionPass* CreateLoopDataPrefetchPass()
{
    return createLoopDataPrefetchPass();
}

//===----------------------------------------------------------------------===//
//
// LibCallsShrinkWrap - Shrink-wraps a call to function if the result is not
// used.
//
DNA_EXPORT FunctionPass* CreateLibCallsShrinkWrapPass()
{
    return createLibCallsShrinkWrapPass();
}

//===----------------------------------------------------------------------===//
//
// LoopSimplifyCFG - This pass performs basic CFG simplification on loops,// primarily to help other loop passes.
//
DNA_EXPORT Pass* CreateLoopSimplifyCFGPass()
{
    return createLoopSimplifyCFGPass();
}

//===----------------------------------------------------------------------===//
//
// WarnMissedTransformations - This pass emits warnings for leftover forced
// transformations.
//
DNA_EXPORT Pass* CreateWarnMissedTransformationsPass()
{
    return createWarnMissedTransformationsPass();
}

//===----------------------------------------------------------------------===//
//
// This pass does instruction simplification on each
// instruction in a function.
//
DNA_EXPORT FunctionPass* CreateInstSimplifyLegacyPass()
{
    return createInstSimplifyLegacyPass();
}

//===----------------------------------------------------------------------===//
//
// createScalarizeMaskedMemIntrinPass - Replace masked load, store, gather
// and scatter intrinsics with scalar code when target doesn't support them.
//
DNA_EXPORT FunctionPass* CreateScalarizeMaskedMemIntrinLegacyPass()
{
    return createScalarizeMaskedMemIntrinLegacyPass();
}