using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms
{
    public static unsafe class ScalarPasses
    {
        //===----------------------------------------------------------------------===//
        //
        // AlignmentFromAssumptions - Use assume intrinsics to set load/store
        // alignments.
        //
        public unsafe static FunctionPass CreateAlignmentFromAssumptionsPass()
        {
            return NativeScalarApi.CreateAlignmentFromAssumptionsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // AnnotationRemarks - Emit remarks for !annotation metadata.
        //
        public unsafe static FunctionPass CreateAnnotationRemarksLegacyPass()
        {
            return NativeScalarApi.CreateAnnotationRemarksLegacyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // SCCP - Sparse conditional constant propagation.
        //
        public unsafe static FunctionPass CreateSCCPPass()
        {
            return NativeScalarApi.CreateSCCPPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // RedundantDbgInstElimination - This pass removes redundant dbg intrinsics
        // without modifying the CFG of the function.  It is a FunctionPass.
        //
        public unsafe static Pass CreateRedundantDbgInstEliminationPass()
        {
            return NativeScalarApi.CreateRedundantDbgInstEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // DeadCodeElimination - This pass is more powerful than DeadInstElimination,// because it is worklist driven that can potentially revisit instructions when
        // their other instructions become dead, to eliminate chains of dead
        // computations.
        //
        public unsafe static FunctionPass CreateDeadCodeEliminationPass()
        {
            return NativeScalarApi.CreateDeadCodeEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // DeadStoreElimination - This pass deletes stores that are post-dominated by
        // must-aliased stores and are not loaded used between the stores.
        //
        public unsafe static FunctionPass CreateDeadStoreEliminationPass()
        {
            return NativeScalarApi.CreateDeadStoreEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // CallSiteSplitting - This pass split call-site based on its known argument
        // values.
        public unsafe static FunctionPass CreateCallSiteSplittingPass()
        {
            return NativeScalarApi.CreateCallSiteSplittingPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // AggressiveDCE - This pass uses the SSA based Aggressive DCE algorithm.  This
        // algorithm assumes instructions are dead until proven otherwise, which makes
        // it more successful are removing non-obviously dead instructions.
        //
        public unsafe static FunctionPass CreateAggressiveDCEPass()
        {
            return NativeScalarApi.CreateAggressiveDCEPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // GuardWidening - An optimization over the @llvm.experimental.guard intrinsic
        // that (optimistically) combines multiple guards into one to have fewer checks
        // at runtime.
        //
        public unsafe static FunctionPass CreateGuardWideningPass()
        {
            return NativeScalarApi.CreateGuardWideningPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopGuardWidening - Analogous to the GuardWidening pass, but restricted to a
        // single loop at a time for use within a LoopPassManager.  Desired effect is
        // to widen guards into preheader or a single guard within loop if that's not
        // possible.
        //
        public unsafe static Pass CreateLoopGuardWideningPass()
        {
            return NativeScalarApi.CreateLoopGuardWideningPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // BitTrackingDCE - This pass uses a bit-tracking DCE algorithm in order to
        // remove computations of dead bits.
        //
        public unsafe static FunctionPass CreateBitTrackingDCEPass()
        {
            return NativeScalarApi.CreateBitTrackingDCEPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // SROA - Replace aggregates or pieces of aggregates with scalar SSA values.
        //
        public unsafe static FunctionPass CreateSROAPass()
        {
            return NativeScalarApi.CreateSROAPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // InductiveRangeCheckElimination - Transform loops to elide range checks on
        // linear functions of the induction variable.
        //
        public unsafe static Pass CreateInductiveRangeCheckEliminationPass()
        {
            return NativeScalarApi.CreateInductiveRangeCheckEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // InductionVariableSimplify - Transform induction variables in a program to all
        // use a single canonical induction variable per loop.
        //
        public unsafe static Pass CreateIndVarSimplifyPass()
        {
            return NativeScalarApi.CreateIndVarSimplifyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LICM - This pass is a loop invariant code motion and memory promotion pass.
        //
        public unsafe static Pass CreateLICMPass()
        {
            return NativeScalarApi.CreateLICMPass();
        }

        public unsafe static Pass CreateLICMPass(uint LicmMssaOptCap, uint LicmMssaNoAccForPromotionCap, bool AllowSpeculation)
        {
            return NativeScalarApi.CreateLICMPass(LicmMssaOptCap, LicmMssaNoAccForPromotionCap, AllowSpeculation);
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopSink - This pass sinks invariants from preheader to loop body where
        // frequency is lower than loop preheader.
        //
        public unsafe static Pass CreateLoopSinkPass()
        {
            return NativeScalarApi.CreateLoopSinkPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopPredication - This pass does loop predication on guards.
        //
        public unsafe static Pass CreateLoopPredicationPass()
        {
            return NativeScalarApi.CreateLoopPredicationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopInterchange - This pass interchanges loops to provide a more
        // cache-friendly memory access patterns.
        //
        public unsafe static Pass CreateLoopInterchangePass()
        {
            return NativeScalarApi.CreateLoopInterchangePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopFlatten - This pass flattens nested loops into a single loop.
        //
        public unsafe static FunctionPass CreateLoopFlattenPass()
        {
            return NativeScalarApi.CreateLoopFlattenPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopStrengthReduce - This pass is strength reduces GEP instructions that use
        // a loop's canonical induction variable as one of their indices.
        //
        public unsafe static Pass CreateLoopStrengthReducePass()
        {
            return NativeScalarApi.CreateLoopStrengthReducePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopInstSimplify - This pass simplifies instructions in a loop's body.
        //
        public unsafe static Pass CreateLoopInstSimplifyPass()
        {
            return NativeScalarApi.CreateLoopInstSimplifyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopUnroll - This pass is a simple loop unrolling pass.
        //
        public unsafe static Pass CreateLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV, int Threshold, int Count, int AllowPartial, int Runtime, int UpperBound, int AllowPeeling)
        {
            return NativeScalarApi.CreateLoopUnrollPass(OptLevel, OnlyWhenForced, ForgetAllSCEV, Threshold, Count, AllowPartial, Runtime, UpperBound, AllowPeeling);
        }

        // Create an unrolling pass for full unrolling that uses exact trip count only
        // and also does peeling.
        public unsafe static Pass CreateSimpleLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV)
        {
            return NativeScalarApi.CreateSimpleLoopUnrollPass(OptLevel, OnlyWhenForced, ForgetAllSCEV);
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopUnrollAndJam - This pass is a simple loop unroll and jam pass.
        //
        public unsafe static Pass CreateLoopUnrollAndJamPass(int OptLevel)
        {
            return NativeScalarApi.CreateLoopUnrollAndJamPass(OptLevel);
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopReroll - This pass is a simple loop rerolling pass.
        //
        public unsafe static Pass CreateLoopRerollPass()
        {
            return NativeScalarApi.CreateLoopRerollPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopRotate - This pass is a simple loop rotating pass.
        //
        public unsafe static Pass CreateLoopRotatePass(int MaxHeaderSize, bool PrepareForLTO)
        {
            return NativeScalarApi.CreateLoopRotatePass(MaxHeaderSize, PrepareForLTO);
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopIdiom - This pass recognizes and replaces idioms in loops.
        //
        public unsafe static Pass CreateLoopIdiomPass()
        {
            return NativeScalarApi.CreateLoopIdiomPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopVersioningLICM - This pass is a loop versioning pass for LICM.
        //
        public unsafe static Pass CreateLoopVersioningLICMPass()
        {
            return NativeScalarApi.CreateLoopVersioningLICMPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // DemoteRegisterToMemoryPass - This pass is used to demote registers to memory
        // references. In basically undoes the PromoteMemoryToRegister pass to make cfg
        // hacking easier.
        //
        public unsafe static FunctionPass CreateDemoteRegisterToMemoryPass()
        {
            return NativeScalarApi.CreateDemoteRegisterToMemoryPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // Reassociate - This pass reassociates commutative expressions in an order that
        // is designed to promote better constant propagation, GCSE, LICM, PRE...
        //
        // For example:  4 + (x + 5)  ->  x + (4 + 5)
        //
        public unsafe static FunctionPass CreateReassociatePass()
        {
            return NativeScalarApi.CreateReassociatePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // JumpThreading - Thread control through mult-pred/multi-succ blocks where some
        // preds always go to some succ. Thresholds other than minus one
        // override the internal BB duplication default threshold.
        //
        public unsafe static FunctionPass CreateJumpThreadingPass(int Threshold)
        {
            return NativeScalarApi.CreateJumpThreadingPass(Threshold);
        }

        //===----------------------------------------------------------------------===//
        //
        // DFAJumpThreading - When a switch statement inside a loop is used to
        // implement a deterministic finite automata we can jump thread the switch
        // statement reducing number of conditional jumps.
        //
        public unsafe static FunctionPass CreateDFAJumpThreadingPass()
        {
            return NativeScalarApi.CreateDFAJumpThreadingPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // CFGSimplification - Merge basic blocks, eliminate unreachable blocks,// simplify terminator instructions, convert switches to lookup tables, etc.
        //
        public unsafe static FunctionPass CreateCFGSimplificationPass()
        {
            return NativeScalarApi.CreateCFGSimplificationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // FlattenCFG - flatten CFG, reduce number of conditional branches by using
        // parallel-and and parallel-or mode, etc...
        //
        public unsafe static FunctionPass CreateFlattenCFGPass()
        {
            return NativeScalarApi.CreateFlattenCFGPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // CFG Structurization - Remove irreducible control flow
        //
        ///
        /// When \p SkipUniformRegions is true the structizer will not structurize
        /// regions that only contain uniform branches.
        public unsafe static Pass CreateStructurizeCFGPass(bool SkipUniformRegions)
        {
            return NativeScalarApi.CreateStructurizeCFGPass(SkipUniformRegions);
        }

        //===----------------------------------------------------------------------===//
        //
        // TailCallElimination - This pass eliminates call instructions to the current
        // function which occur immediately before return instructions.
        //
        public unsafe static FunctionPass CreateTailCallEliminationPass()
        {
            return NativeScalarApi.CreateTailCallEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // EarlyCSE - This pass performs a simple and fast CSE pass over the dominator
        // tree.
        //
        public unsafe static FunctionPass CreateEarlyCSEPass(bool UseMemorySSA)
        {
            return NativeScalarApi.CreateEarlyCSEPass(UseMemorySSA);
        }

        //===----------------------------------------------------------------------===//
        //
        // GVNHoist - This pass performs a simple and fast GVN pass over the dominator
        // tree to hoist common expressions from sibling branches.
        //
        public unsafe static FunctionPass CreateGVNHoistPass()
        {
            return NativeScalarApi.CreateGVNHoistPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // GVNSink - This pass uses an "inverted" value numbering to decide the
        // similarity of expressions and sinks similar expressions into successors.
        //
        public unsafe static FunctionPass CreateGVNSinkPass()
        {
            return NativeScalarApi.CreateGVNSinkPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // MergedLoadStoreMotion - This pass merges loads and stores in diamonds. Loads
        // are hoisted into the header, while stores sink into the footer.
        //
        public unsafe static FunctionPass CreateMergedLoadStoreMotionPass(bool SplitFooterBB)
        {
            return NativeScalarApi.CreateMergedLoadStoreMotionPass(SplitFooterBB);
        }

        //===----------------------------------------------------------------------===//
        //
        // GVN - This pass performs global value numbering and redundant load
        // elimination cotemporaneously.
        //
        public unsafe static FunctionPass CreateNewGVNPass()
        {
            return NativeScalarApi.CreateNewGVNPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // DivRemPairs - Hoist/decompose integer division and remainder instructions.
        //
        public unsafe static FunctionPass CreateDivRemPairsPass()
        {
            return NativeScalarApi.CreateDivRemPairsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // MemCpyOpt - This pass performs optimizations related to eliminating memcpy
        // calls and/or combining multiple stores into memset's.
        //
        public unsafe static FunctionPass CreateMemCpyOptPass()
        {
            return NativeScalarApi.CreateMemCpyOptPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopDeletion - This pass performs DCE of non-infinite loops that it
        // can prove are dead.
        //
        public unsafe static Pass CreateLoopDeletionPass()
        {
            return NativeScalarApi.CreateLoopDeletionPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // ConstantHoisting - This pass prepares a function for expensive constants.
        //
        public unsafe static FunctionPass CreateConstantHoistingPass()
        {
            return NativeScalarApi.CreateConstantHoistingPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // ConstraintElimination - This pass eliminates conditions based on found
        //                         constraints.
        //
        public unsafe static FunctionPass CreateConstraintEliminationPass()
        {
            return NativeScalarApi.CreateConstraintEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // Sink - Code Sinking
        //
        public unsafe static FunctionPass CreateSinkingPass()
        {
            return NativeScalarApi.CreateSinkingPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerAtomic - Lower atomic intrinsics to non-atomic form
        //
        public unsafe static Pass CreateLowerAtomicPass()
        {
            return NativeScalarApi.CreateLowerAtomicPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerGuardIntrinsic - Lower guard intrinsics to normal control flow.
        //
        public unsafe static Pass CreateLowerGuardIntrinsicPass()
        {
            return NativeScalarApi.CreateLowerGuardIntrinsicPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerMatrixIntrinsics - Lower matrix intrinsics to vector operations.
        //
        public unsafe static Pass CreateLowerMatrixIntrinsicsPass()
        {
            return NativeScalarApi.CreateLowerMatrixIntrinsicsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerMatrixIntrinsicsMinimal - Lower matrix intrinsics to vector operations
        //                               (lightweight, does not require extra analysis)
        //
        public unsafe static Pass CreateLowerMatrixIntrinsicsMinimalPass()
        {
            return NativeScalarApi.CreateLowerMatrixIntrinsicsMinimalPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerWidenableCondition - Lower widenable condition to i1 true.
        //
        public unsafe static Pass CreateLowerWidenableConditionPass()
        {
            return NativeScalarApi.CreateLowerWidenableConditionPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // MergeICmps - Merge integer comparison chains into a memcmp
        //
        public unsafe static Pass CreateMergeICmpsLegacyPass()
        {
            return NativeScalarApi.CreateMergeICmpsLegacyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // ValuePropagation - Propagate CFG-derived value information
        //
        public unsafe static Pass CreateCorrelatedValuePropagationPass()
        {
            return NativeScalarApi.CreateCorrelatedValuePropagationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // InferAddressSpaces - Modify users of addrspacecast instructions with values
        // in the source address space if using the destination address space is slower
        // on the target. If AddressSpace is left to its default value, it will be
        // obtained from the TargetTransformInfo.
        //
        public unsafe static FunctionPass CreateInferAddressSpacesPass(uint AddressSpace)
        {
            return NativeScalarApi.CreateInferAddressSpacesPass(AddressSpace);
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerExpectIntrinsics - Removes llvm.expect intrinsics and creates
        // "block_weights" metadata.
        public unsafe static FunctionPass CreateLowerExpectIntrinsicPass()
        {
            return NativeScalarApi.CreateLowerExpectIntrinsicPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // TLSVariableHoist - This pass reduce duplicated TLS address call.
        //
        public unsafe static FunctionPass CreateTLSVariableHoistPass()
        {
            return NativeScalarApi.CreateTLSVariableHoistPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LowerConstantIntrinsicss - Expand any remaining llvm.objectsize and
        // llvm.is.constant intrinsic calls, even for the unknown cases.
        //
        public unsafe static FunctionPass CreateLowerConstantIntrinsicsPass()
        {
            return NativeScalarApi.CreateLowerConstantIntrinsicsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // PartiallyInlineLibCalls - Tries to inline the fast path of library
        // calls such as sqrt.
        //
        public unsafe static FunctionPass CreatePartiallyInlineLibCallsPass()
        {
            return NativeScalarApi.CreatePartiallyInlineLibCallsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // SeparateConstOffsetFromGEP - Split GEPs for better CSE
        //
        public unsafe static FunctionPass CreateSeparateConstOffsetFromGEPPass(bool LowerGEP)
        {
            return NativeScalarApi.CreateSeparateConstOffsetFromGEPPass(LowerGEP);
        }

        //===----------------------------------------------------------------------===//
        //
        // SpeculativeExecution - Aggressively hoist instructions to enable
        // speculative execution on targets where branches are expensive.
        //
        public unsafe static FunctionPass CreateSpeculativeExecutionPass()
        {
            return NativeScalarApi.CreateSpeculativeExecutionPass();
        }

        // Same as createSpeculativeExecutionPass, but does nothing unless
        // TargetTransformInfo::hasBranchDivergence() is true.
        public unsafe static FunctionPass CreateSpeculativeExecutionIfHasBranchDivergencePass()
        {
            return NativeScalarApi.CreateSpeculativeExecutionIfHasBranchDivergencePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // StraightLineStrengthReduce - This pass strength-reduces some certain
        // instruction patterns in straight-line code.
        //
        public unsafe static FunctionPass CreateStraightLineStrengthReducePass()
        {
            return NativeScalarApi.CreateStraightLineStrengthReducePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // PlaceSafepoints - Rewrite any IR calls to gc.statepoints and insert any
        // safepoint polls (method entry, backedge) that might be required.  This pass
        // does not generate explicit relocation sequences - that's handled by
        // RewriteStatepointsForGC which can be run at an arbitrary point in the pass
        // order following this pass.
        //
        public unsafe static FunctionPass CreatePlaceSafepointsPass()
        {
            return NativeScalarApi.CreatePlaceSafepointsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // RewriteStatepointsForGC - Rewrite any gc.statepoints which do not yet have
        // explicit relocations to include explicit relocations.
        //
        public unsafe static ModulePass CreateRewriteStatepointsForGCLegacyPass()
        {
            return NativeScalarApi.CreateRewriteStatepointsForGCLegacyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // Float2Int - Demote floats to ints where possible.
        //
        public unsafe static FunctionPass CreateFloat2IntPass()
        {
            return NativeScalarApi.CreateFloat2IntPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // NaryReassociate - Simplify n-ary operations by reassociation.
        //
        public unsafe static FunctionPass CreateNaryReassociatePass()
        {
            return NativeScalarApi.CreateNaryReassociatePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopDistribute - Distribute loops.
        //
        public unsafe static FunctionPass CreateLoopDistributePass()
        {
            return NativeScalarApi.CreateLoopDistributePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopFuse - Fuse loops.
        //
        public unsafe static FunctionPass CreateLoopFusePass()
        {
            return NativeScalarApi.CreateLoopFusePass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopLoadElimination - Perform loop-aware load elimination.
        //
        public unsafe static FunctionPass CreateLoopLoadEliminationPass()
        {
            return NativeScalarApi.CreateLoopLoadEliminationPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopVersioning - Perform loop multi-versioning.
        //
        public unsafe static FunctionPass CreateLoopVersioningPass()
        {
            return NativeScalarApi.CreateLoopVersioningPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopDataPrefetch - Perform data prefetching in loops.
        //
        public unsafe static FunctionPass CreateLoopDataPrefetchPass()
        {
            return NativeScalarApi.CreateLoopDataPrefetchPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LibCallsShrinkWrap - Shrink-wraps a call to function if the result is not
        // used.
        //
        public unsafe static FunctionPass CreateLibCallsShrinkWrapPass()
        {
            return NativeScalarApi.CreateLibCallsShrinkWrapPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // LoopSimplifyCFG - This pass performs basic CFG simplification on loops,// primarily to help other loop passes.
        //
        public unsafe static Pass CreateLoopSimplifyCFGPass()
        {
            return NativeScalarApi.CreateLoopSimplifyCFGPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // WarnMissedTransformations - This pass emits warnings for leftover forced
        // transformations.
        //
        public unsafe static Pass CreateWarnMissedTransformationsPass()
        {
            return NativeScalarApi.CreateWarnMissedTransformationsPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // This pass does instruction simplification on each
        // instruction in a function.
        //
        public unsafe static FunctionPass CreateInstSimplifyLegacyPass()
        {
            return NativeScalarApi.CreateInstSimplifyLegacyPass();
        }

        //===----------------------------------------------------------------------===//
        //
        // createScalarizeMaskedMemIntrinPass - Replace masked load, store, gather
        // and scatter intrinsics with scalar code when target doesn't support them.
        //
        public unsafe static FunctionPass CreateScalarizeMaskedMemIntrinLegacyPass()
        {
            return NativeScalarApi.CreateScalarizeMaskedMemIntrinLegacyPass();
        }
    }
}
