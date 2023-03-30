using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms
{
    public static unsafe class NativeScalarApi
    {
        //===----------------------------------------------------------------------===//
        //
        // AlignmentFromAssumptions - Use assume intrinsics to set load/store
        // alignments.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateAlignmentFromAssumptionsPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateAlignmentFromAssumptionsPass();

        //===----------------------------------------------------------------------===//
        //
        // AnnotationRemarks - Emit remarks for !annotation metadata.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateAnnotationRemarksLegacyPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateAnnotationRemarksLegacyPass();

        //===----------------------------------------------------------------------===//
        //
        // SCCP - Sparse conditional constant propagation.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSCCPPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSCCPPass();

        //===----------------------------------------------------------------------===//
        //
        // RedundantDbgInstElimination - This pass removes redundant dbg intrinsics
        // without modifying the CFG of the function.  It is a FunctionPass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateRedundantDbgInstEliminationPass")]
        public unsafe static extern LLVMOpaquePass* CreateRedundantDbgInstEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // DeadCodeElimination - This pass is more powerful than DeadInstElimination,// because it is worklist driven that can potentially revisit instructions when
        // their other instructions become dead, to eliminate chains of dead
        // computations.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateDeadCodeEliminationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateDeadCodeEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // DeadStoreElimination - This pass deletes stores that are post-dominated by
        // must-aliased stores and are not loaded used between the stores.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateDeadStoreEliminationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateDeadStoreEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // CallSiteSplitting - This pass split call-site based on its known argument
        // values.
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateCallSiteSplittingPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateCallSiteSplittingPass();

        //===----------------------------------------------------------------------===//
        //
        // AggressiveDCE - This pass uses the SSA based Aggressive DCE algorithm.  This
        // algorithm assumes instructions are dead until proven otherwise, which makes
        // it more successful are removing non-obviously dead instructions.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateAggressiveDCEPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateAggressiveDCEPass();

        //===----------------------------------------------------------------------===//
        //
        // GuardWidening - An optimization over the @llvm.experimental.guard intrinsic
        // that (optimistically) combines multiple guards into one to have fewer checks
        // at runtime.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateGuardWideningPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateGuardWideningPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopGuardWidening - Analogous to the GuardWidening pass, but restricted to a
        // single loop at a time for use within a LoopPassManager.  Desired effect is
        // to widen guards into preheader or a single guard within loop if that's not
        // possible.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopGuardWideningPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopGuardWideningPass();

        //===----------------------------------------------------------------------===//
        //
        // BitTrackingDCE - This pass uses a bit-tracking DCE algorithm in order to
        // remove computations of dead bits.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateBitTrackingDCEPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateBitTrackingDCEPass();

        //===----------------------------------------------------------------------===//
        //
        // SROA - Replace aggregates or pieces of aggregates with scalar SSA values.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSROAPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSROAPass();

        //===----------------------------------------------------------------------===//
        //
        // InductiveRangeCheckElimination - Transform loops to elide range checks on
        // linear functions of the induction variable.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateInductiveRangeCheckEliminationPass")]
        public unsafe static extern LLVMOpaquePass* CreateInductiveRangeCheckEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // InductionVariableSimplify - Transform induction variables in a program to all
        // use a single canonical induction variable per loop.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateIndVarSimplifyPass")]
        public unsafe static extern LLVMOpaquePass* CreateIndVarSimplifyPass();

        //===----------------------------------------------------------------------===//
        //
        // LICM - This pass is a loop invariant code motion and memory promotion pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLICMPass")]
        public unsafe static extern LLVMOpaquePass* CreateLICMPass();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLICMPass")]
        public unsafe static extern LLVMOpaquePass* CreateLICMPass(uint LicmMssaOptCap, uint LicmMssaNoAccForPromotionCap, bool AllowSpeculation);

        //===----------------------------------------------------------------------===//
        //
        // LoopSink - This pass sinks invariants from preheader to loop body where
        // frequency is lower than loop preheader.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopSinkPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopSinkPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopPredication - This pass does loop predication on guards.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopPredicationPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopPredicationPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopInterchange - This pass interchanges loops to provide a more
        // cache-friendly memory access patterns.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopInterchangePass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopInterchangePass();

        //===----------------------------------------------------------------------===//
        //
        // LoopFlatten - This pass flattens nested loops into a single loop.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopFlattenPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopFlattenPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopStrengthReduce - This pass is strength reduces GEP instructions that use
        // a loop's canonical induction variable as one of their indices.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopStrengthReducePass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopStrengthReducePass();

        //===----------------------------------------------------------------------===//
        //
        // LoopInstSimplify - This pass simplifies instructions in a loop's body.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopInstSimplifyPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopInstSimplifyPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopUnroll - This pass is a simple loop unrolling pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopUnrollPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV, int Threshold, int Count, int AllowPartial, int Runtime, int UpperBound, int AllowPeeling);

        // Create an unrolling pass for full unrolling that uses exact trip count only
        // and also does peeling.
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSimpleLoopUnrollPass")]
        public unsafe static extern LLVMOpaquePass* CreateSimpleLoopUnrollPass(int OptLevel, bool OnlyWhenForced, bool ForgetAllSCEV);

        //===----------------------------------------------------------------------===//
        //
        // LoopUnrollAndJam - This pass is a simple loop unroll and jam pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopUnrollAndJamPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopUnrollAndJamPass(int OptLevel);

        //===----------------------------------------------------------------------===//
        //
        // LoopReroll - This pass is a simple loop rerolling pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopRerollPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopRerollPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopRotate - This pass is a simple loop rotating pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopRotatePass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopRotatePass(int MaxHeaderSize, bool PrepareForLTO);

        //===----------------------------------------------------------------------===//
        //
        // LoopIdiom - This pass recognizes and replaces idioms in loops.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopIdiomPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopIdiomPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopVersioningLICM - This pass is a loop versioning pass for LICM.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopVersioningLICMPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopVersioningLICMPass();

        //===----------------------------------------------------------------------===//
        //
        // DemoteRegisterToMemoryPass - This pass is used to demote registers to memory
        // references. In basically undoes the PromoteMemoryToRegister pass to make cfg
        // hacking easier.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateDemoteRegisterToMemoryPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateDemoteRegisterToMemoryPass();

	//===----------------------------------------------------------------------===//
	//
	// Reassociate - This pass reassociates commutative expressions in an order that
	// is designed to promote better constant propagation, GCSE, LICM, PRE...
	//
	// For example:  4 + (x + 5)  ->  x + (4 + 5)
	//
	[DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateReassociatePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateReassociatePass();

        //===----------------------------------------------------------------------===//
        //
        // JumpThreading - Thread control through mult-pred/multi-succ blocks where some
        // preds always go to some succ. Thresholds other than minus one
        // override the internal BB duplication default threshold.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateJumpThreadingPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateJumpThreadingPass(int Threshold);

        //===----------------------------------------------------------------------===//
        //
        // DFAJumpThreading - When a switch statement inside a loop is used to
        // implement a deterministic finite automata we can jump thread the switch
        // statement reducing number of conditional jumps.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateDFAJumpThreadingPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateDFAJumpThreadingPass();

        //===----------------------------------------------------------------------===//
        //
        // CFGSimplification - Merge basic blocks, eliminate unreachable blocks,// simplify terminator instructions, convert switches to lookup tables, etc.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateCFGSimplificationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateCFGSimplificationPass();

        //===----------------------------------------------------------------------===//
        //
        // FlattenCFG - flatten CFG, reduce number of conditional branches by using
        // parallel-and and parallel-or mode, etc...
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateFlattenCFGPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateFlattenCFGPass();

        //===----------------------------------------------------------------------===//
        //
        // CFG Structurization - Remove irreducible control flow
        //
        ///
        /// When \p SkipUniformRegions is true the structizer will not structurize
        /// regions that only contain uniform branches.
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateStructurizeCFGPass")]
        public unsafe static extern LLVMOpaquePass* CreateStructurizeCFGPass(bool SkipUniformRegions);

        //===----------------------------------------------------------------------===//
        //
        // TailCallElimination - This pass eliminates call instructions to the current
        // function which occur immediately before return instructions.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateTailCallEliminationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateTailCallEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // EarlyCSE - This pass performs a simple and fast CSE pass over the dominator
        // tree.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateEarlyCSEPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateEarlyCSEPass(bool UseMemorySSA);

        //===----------------------------------------------------------------------===//
        //
        // GVNHoist - This pass performs a simple and fast GVN pass over the dominator
        // tree to hoist common expressions from sibling branches.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateGVNHoistPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateGVNHoistPass();

        //===----------------------------------------------------------------------===//
        //
        // GVNSink - This pass uses an "inverted" value numbering to decide the
        // similarity of expressions and sinks similar expressions into successors.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateGVNSinkPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateGVNSinkPass();

        //===----------------------------------------------------------------------===//
        //
        // MergedLoadStoreMotion - This pass merges loads and stores in diamonds. Loads
        // are hoisted into the header, while stores sink into the footer.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateMergedLoadStoreMotionPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateMergedLoadStoreMotionPass(bool SplitFooterBB);

        //===----------------------------------------------------------------------===//
        //
        // GVN - This pass performs global value numbering and redundant load
        // elimination cotemporaneously.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateNewGVNPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateNewGVNPass();

        //===----------------------------------------------------------------------===//
        //
        // DivRemPairs - Hoist/decompose integer division and remainder instructions.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateDivRemPairsPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateDivRemPairsPass();

        //===----------------------------------------------------------------------===//
        //
        // MemCpyOpt - This pass performs optimizations related to eliminating memcpy
        // calls and/or combining multiple stores into memset's.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateMemCpyOptPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateMemCpyOptPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopDeletion - This pass performs DCE of non-infinite loops that it
        // can prove are dead.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopDeletionPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopDeletionPass();

        //===----------------------------------------------------------------------===//
        //
        // ConstantHoisting - This pass prepares a function for expensive constants.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateConstantHoistingPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateConstantHoistingPass();

        //===----------------------------------------------------------------------===//
        //
        // ConstraintElimination - This pass eliminates conditions based on found
        //                         constraints.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateConstraintEliminationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateConstraintEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // Sink - Code Sinking
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSinkingPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSinkingPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerAtomic - Lower atomic intrinsics to non-atomic form
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerAtomicPass")]
        public unsafe static extern LLVMOpaquePass* CreateLowerAtomicPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerGuardIntrinsic - Lower guard intrinsics to normal control flow.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerGuardIntrinsicPass")]
        public unsafe static extern LLVMOpaquePass* CreateLowerGuardIntrinsicPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerMatrixIntrinsics - Lower matrix intrinsics to vector operations.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerMatrixIntrinsicsPass")]
        public unsafe static extern LLVMOpaquePass* CreateLowerMatrixIntrinsicsPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerMatrixIntrinsicsMinimal - Lower matrix intrinsics to vector operations
        //                               (lightweight, does not require extra analysis)
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerMatrixIntrinsicsMinimalPass")]
        public unsafe static extern LLVMOpaquePass* CreateLowerMatrixIntrinsicsMinimalPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerWidenableCondition - Lower widenable condition to i1 true.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerWidenableConditionPass")]
        public unsafe static extern LLVMOpaquePass* CreateLowerWidenableConditionPass();

        //===----------------------------------------------------------------------===//
        //
        // MergeICmps - Merge integer comparison chains into a memcmp
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateMergeICmpsLegacyPass")]
        public unsafe static extern LLVMOpaquePass* CreateMergeICmpsLegacyPass();

        //===----------------------------------------------------------------------===//
        //
        // ValuePropagation - Propagate CFG-derived value information
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateCorrelatedValuePropagationPass")]
        public unsafe static extern LLVMOpaquePass* CreateCorrelatedValuePropagationPass();

        //===----------------------------------------------------------------------===//
        //
        // InferAddressSpaces - Modify users of addrspacecast instructions with values
        // in the source address space if using the destination address space is slower
        // on the target. If AddressSpace is left to its default value, it will be
        // obtained from the TargetTransformInfo.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateInferAddressSpacesPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateInferAddressSpacesPass(uint AddressSpace);

	//===----------------------------------------------------------------------===//
	//
	// LowerExpectIntrinsics - Removes llvm.expect intrinsics and creates
	// "block_weights" metadata.
	[DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerExpectIntrinsicPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLowerExpectIntrinsicPass();

        //===----------------------------------------------------------------------===//
        //
        // TLSVariableHoist - This pass reduce duplicated TLS address call.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateTLSVariableHoistPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateTLSVariableHoistPass();

        //===----------------------------------------------------------------------===//
        //
        // LowerConstantIntrinsicss - Expand any remaining llvm.objectsize and
        // llvm.is.constant intrinsic calls, even for the unknown cases.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLowerConstantIntrinsicsPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLowerConstantIntrinsicsPass();

        //===----------------------------------------------------------------------===//
        //
        // PartiallyInlineLibCalls - Tries to inline the fast path of library
        // calls such as sqrt.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreatePartiallyInlineLibCallsPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreatePartiallyInlineLibCallsPass();

        //===----------------------------------------------------------------------===//
        //
        // SeparateConstOffsetFromGEP - Split GEPs for better CSE
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSeparateConstOffsetFromGEPPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSeparateConstOffsetFromGEPPass(bool LowerGEP);

        //===----------------------------------------------------------------------===//
        //
        // SpeculativeExecution - Aggressively hoist instructions to enable
        // speculative execution on targets where branches are expensive.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSpeculativeExecutionPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSpeculativeExecutionPass();

        // Same as createSpeculativeExecutionPass, but does nothing unless
        // TargetTransformInfo::hasBranchDivergence() is true.
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateSpeculativeExecutionIfHasBranchDivergencePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateSpeculativeExecutionIfHasBranchDivergencePass();

        //===----------------------------------------------------------------------===//
        //
        // StraightLineStrengthReduce - This pass strength-reduces some certain
        // instruction patterns in straight-line code.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateStraightLineStrengthReducePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateStraightLineStrengthReducePass();

        //===----------------------------------------------------------------------===//
        //
        // PlaceSafepoints - Rewrite any IR calls to gc.statepoints and insert any
        // safepoint polls (method entry, backedge) that might be required.  This pass
        // does not generate explicit relocation sequences - that's handled by
        // RewriteStatepointsForGC which can be run at an arbitrary point in the pass
        // order following this pass.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreatePlaceSafepointsPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreatePlaceSafepointsPass();

        //===----------------------------------------------------------------------===//
        //
        // RewriteStatepointsForGC - Rewrite any gc.statepoints which do not yet have
        // explicit relocations to include explicit relocations.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateRewriteStatepointsForGCLegacyPass")]
        public unsafe static extern LLVMOpaqueModulePass* CreateRewriteStatepointsForGCLegacyPass();

        //===----------------------------------------------------------------------===//
        //
        // Float2Int - Demote floats to ints where possible.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateFloat2IntPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateFloat2IntPass();

        //===----------------------------------------------------------------------===//
        //
        // NaryReassociate - Simplify n-ary operations by reassociation.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateNaryReassociatePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateNaryReassociatePass();

        //===----------------------------------------------------------------------===//
        //
        // LoopDistribute - Distribute loops.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopDistributePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopDistributePass();

        //===----------------------------------------------------------------------===//
        //
        // LoopFuse - Fuse loops.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopFusePass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopFusePass();

        //===----------------------------------------------------------------------===//
        //
        // LoopLoadElimination - Perform loop-aware load elimination.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopLoadEliminationPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopLoadEliminationPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopVersioning - Perform loop multi-versioning.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopVersioningPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopVersioningPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopDataPrefetch - Perform data prefetching in loops.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopDataPrefetchPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLoopDataPrefetchPass();

        //===----------------------------------------------------------------------===//
        //
        // LibCallsShrinkWrap - Shrink-wraps a call to function if the result is not
        // used.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLibCallsShrinkWrapPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateLibCallsShrinkWrapPass();

        //===----------------------------------------------------------------------===//
        //
        // LoopSimplifyCFG - This pass performs basic CFG simplification on loops,// primarily to help other loop passes.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateLoopSimplifyCFGPass")]
        public unsafe static extern LLVMOpaquePass* CreateLoopSimplifyCFGPass();

        //===----------------------------------------------------------------------===//
        //
        // WarnMissedTransformations - This pass emits warnings for leftover forced
        // transformations.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateWarnMissedTransformationsPass")]
        public unsafe static extern LLVMOpaquePass* CreateWarnMissedTransformationsPass();

        //===----------------------------------------------------------------------===//
        //
        // This pass does instruction simplification on each
        // instruction in a function.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateInstSimplifyLegacyPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateInstSimplifyLegacyPass();

        //===----------------------------------------------------------------------===//
        //
        // createScalarizeMaskedMemIntrinPass - Replace masked load, store, gather
        // and scatter intrinsics with scalar code when target doesn't support them.
        //
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateScalarizeMaskedMemIntrinLegacyPass")]
        public unsafe static extern LLVMOpaqueFunctionPass* CreateScalarizeMaskedMemIntrinLegacyPass();
    }
}
