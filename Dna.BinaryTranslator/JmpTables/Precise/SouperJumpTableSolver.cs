using Dna.Binary;
using Dna.ControlFlow.Extensions;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.Souper.Candidate;
using Dna.LLVMInterop.Souper.ExprBuilder;
using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    /// <summary>
    /// This class implements a recursive algorithm for solving the bounds of jump table index variables(although it can be used to bound any variable in theory).
    /// 
    /// The first step is to collect the set of path constraints necessary to reach the basic block containing the indirect jump.
    /// Then we statically backwards slice the ast of the jump address variable, and feed both the ast and path constraints to z3.
    /// Z3 is then able to tell us if the variable is boundable at all.
    /// 
    /// If the variable is not boundable, then we check if the variable AST contains only a single input variable node. 
    /// If there is only one input variable, then we push the current slice onto a queue(to be revisited later)
    /// and recurse into solving the input variable.
    /// 
    /// The algorithm will recurse a maximum of three times. If we encounter more than 4 unboundable variables in the jump table address chain,
    /// or if any of the unboundable children are dependent on more than one input variable, an exception is thrown.
    /// 
    /// If a boundable variable is found within the recursion limit, then we use z3 to solve for the set of possible values.
    /// Finally we iterate through the collected ASTs in reverse order, collecting a set of concrete evaluations using all possible values.
    /// At the end of this algorithm we are left with a precise set of values for the jump table target.
    /// </summary>
    public class SouperJumpTableSolver
    {
        private readonly IBinary binary;

        private readonly ulong jmpFromAddress;

        private readonly LLVMValueRef function;

        private readonly LLVMValueRef indJmp;

        private LLVMValueRef jmpPtr;

        private readonly LoopInfo loopInfo;

        private readonly SouperInstContext instCtx = new();

        private readonly SouperExprBuilderContext builderCtx = new();

        private readonly LLVMBuilderRef builder;

        private SouperInst indJmpConstraint = null;

        public SouperJumpTableSolver(IBinary binary, ulong jmpFromAddress, LLVMValueRef indJmp, LLVMValueRef jmpPtr, LoopInfo loopInfo)
        {
            this.binary = binary;
            this.jmpFromAddress = jmpFromAddress;
            this.indJmp = indJmp;
            this.function = indJmp.InstructionParent.Parent;
            this.jmpPtr = indJmp.GetOperand(1);
            //this.jmpPtr = jmpPtr;
            this.loopInfo = loopInfo;
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
        }

        public JmpTable IterativelySolve()
        {
            // This is a hack to move the definition of the indirect jump pointer into the 
            // same basic block as the call to remill_jump. This allows souper to slice the set of 
            // path conditions necessary to reach the remill_jump - which it previously cannot do
            // since remill_jump returns a ptr(which is ignored during slicing).
            // Context: There is no guarantee that the indirect jump pointer and the call to remill_jump
            // are in the same basic block, and the set of path constraints necessary to reach from the definition
            // to the jump may further refine down the set of solutions.
            builder.PositionBefore(indJmp);
            var intraBlockPtr = builder.BuildAdd(jmpPtr, LLVMValueRef.CreateConstInt(jmpPtr.TypeOf, 0));
            indJmp.SetOperand(1, intraBlockPtr);

            //jmpPtr = intraBlockPtr;
            function.GlobalParent.PrintToFile("translatedFunction.ll");

            // Build the set of path constraints necessary to reach the remill_jump.
            var exprBuilder = new SouperExprBuilder(instCtx);
            var cand = ExtractCandidate(intraBlockPtr);
            //indJmpConstraint = cand.PathConditions.Single().Lhs;
            //indJmpConstraint = instCtx.GetConst(1, 1);

            var solutions = new Dictionary<LLVMValueRef, HashSet<ulong>?>();
            RecursivelySolve(jmpPtr, 0, solutions);

            var newSolutions = solutions[jmpPtr];
            if (newSolutions?.Count == 0)
                throw new InvalidOperationException($"Failed to solve jump table!");


            return new JmpTable(jmpFromAddress, newSolutions.ToList(), Enumerable.Empty<ulong>().ToList(), isComplete: false);
        }

        public void RecursivelySolve(LLVMValueRef toSolve, int currDepth, Dictionary<LLVMValueRef, HashSet<ulong>> solved)
        {
            // If the LLVM value is a constant then just add the constant to the solution list.
            if(toSolve.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                solved[toSolve] = new HashSet<ulong>() { toSolve.ConstIntZExt };
                return;
            }

            // If we've already solved this variable, or if it's currently being processed(think: a recursive dependency),
            // then return the current solution.
            if (solved.ContainsKey(toSolve))
                return;

            // If the recursive solving depth becomes greater than or equal to 5 for any subtree, stop and consider the variable unbounded.
            // Note this may cause unwanted behavior in the event you have two diverging paths:
            //  a -> b -> c -> d -> e -> f
            //  a -> f
            // , because `f` may be marked as null due to the recursion limit while it is actually reachable before the recursion limit 
            // when taking path a -> f. In the future we should consider some type of dependency graph analysis to select the correct
            // order to visit and solve nodes.
            if (currDepth >= 5)
            {
                Console.WriteLine($"Hit recursion depth for: {toSolve}");
                solved.TryAdd(toSolve, null);
                return;
            }

            // To avoid infinite recursion, temporarily add a null solution placeholder.
            solved.TryAdd(toSolve, null);

            // If this is an argument, skip it because it's impossible to bound.
            if (toSolve.Kind == LLVMValueKind.LLVMArgumentValueKind)
                return;

            // If a load is slice, we need to special case it.
            if(toSolve.Kind == LLVMValueKind.LLVMInstructionValueKind && toSolve.InstructionOpcode == LLVMOpcode.LLVMLoad)
            {
                RecursivelySolveLoad(toSolve, currDepth, solved);
                return;
            }

            // Use souper to slice an AST for the value we are solving.
            var cand = ExtractCandidate(toSolve);
            if(cand == null)
            {
                Console.WriteLine($"Failed to slice {toSolve}. Souper could not identify any candidates.");
                return;
            }
            var ast = cand.InstMapping.Lhs;

            // Collect the input variables. These can be function arguments, memory loads,
            // or PHI nodes. Note that PHI nodes will only be considered an input variable if they are defined outside of the current loop,
            // or if they are defined inside of the loop header. 
            var inputVars = InputVariableCollector.Collect(ast);

            // Recursively solve all input variables.
            foreach(var inputVar in inputVars)
            {
                // Otherwise recursively solve the input variable to this expression.
                var inputOrigin = inputVar.Origins.Single();
                RecursivelySolve(inputOrigin, currDepth + 1, solved);
            }

            // See the comments on 'TrySolvingPhiBounds' to understand why this is necessary.
            // Here we have a special case that tries to apply bounds to phi nodes when souper's slicing
            // overapproximates.
            var origin = ast.Origins.SingleOrDefault();
            if(origin != null && origin.Kind == LLVMValueKind.LLVMInstructionValueKind && origin.InstructionOpcode == LLVMOpcode.LLVMPHI)
            {
                var phiSolutions = new HashSet<LLVMValueRef>();
                bool solvable = TryGetPhiSolutions(origin, new HashSet<LLVMValueRef>(), phiSolutions);
                if(solvable)
                {
                    // Get a clone of the current set of solutions for the this phi node.
                    var currentSolutions = solved.GetValueOrDefault(origin, () => new())?.ToHashSet();
                    if (currentSolutions == null)
                        currentSolutions = new();

                    // Mutate the current set of solutions to also contain the set of solutions returned by `TryGetPhiSolutions`.
                    currentSolutions.UnionWith(phiSolutions.Select(x => x.ConstIntZExt));

                    // Update the solution mapping to contain the refined solution list.
                    solved[origin] = currentSolutions;
                }
            }

            // For each input variable with at least one known solution, build a 'SouperInst'
            // representing a constraint that would tell z3 which values this variable is allowed to hold.
            var inputBoundingConstraints = inputVars
                .Where(x => solved.GetValueOrDefault(x.Origins.Single(), () => null)?.Count() > 0)
                .Select(x => BuildConstraintToBoundVariable(instCtx, x, solved[x.Origins.Single()]))
                .ToList();

            // Create a list of all constraints.
            var allConstraints = new List<SouperInst>();
            // Add all input bounding constraints.
            allConstraints.AddRange(inputBoundingConstraints);
            // Add the optional path constraint necessary to reach to place where the current variable is defined.
            // Note that there should usually only be 0 or 1 I think, because souper ANDs all path constraints together.
            // I could be wrong though.
            var pathConstraint = cand.PathConditions.Select(x => x.Lhs).FirstOrDefault();
            foreach(var pathCond in cand.PathConditions.Skip(1))
            {
               // pathConstraint = instCtx.GetInst(SouperInstKind.Or, 1, pathConstraint, pathCond.Lhs);
            }

            //if (pathConstraint != null)
             //   allConstraints.Add(pathConstraint);
            //if (cand.PathConditions.Any())
               // allConstraints.AddRange(cand.PathConditions.Select(x => x.Lhs));

            // Create a final, single constraint that will be given to z3 when we try to solve the set of possible values.
            // Initially we set it to true, and then AND on all additional constraints.
            var finalConstraint = instCtx.GetConst(1, 1);
            foreach(var constraint in allConstraints)
                finalConstraint = instCtx.GetInst(SouperInstKind.And, 1, finalConstraint, constraint);

            // Try to solve all possible values.
            Console.WriteLine($"Solving all values for: {toSolve}");

            var sw = Stopwatch.StartNew();
            var maybeSolutions = SlicingApi.TrySolveAllValues(instCtx, new List<SouperBlockPCMapping>().AsReadOnly(), cand.PathConditions, cand.InstMapping, finalConstraint, true, false, 0, 256);
            sw.Stop();
            //var maybeSolutionssdsd = TrySolveAllValues(ast, cand.PathConditions.ToList(), finalConstraint);

            // If we successfully identified all possible values, update the solution list accordingly.
            if (maybeSolutions.success)
                solved[toSolve] = maybeSolutions.results.ToHashSet();

            Console.WriteLine("Solved.");
        }

        private void RecursivelySolveLoad(LLVMValueRef loadInst, int currDepth, Dictionary<LLVMValueRef, HashSet<ulong>> solved)
        {
            // Get the ptr.
            var ptr = loadInst.GetOperand(0);

            // If it's a constant int then we just invoke the recursive solver out of laziness.
            if (ptr.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                RecursivelySolve(ptr, currDepth, solved);
            // If it's a GEP then we try to recursively solve all indices.
            else if (ptr.Kind == LLVMValueKind.LLVMInstructionValueKind && ptr.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr)
            {
                ptr = ptr.GetOperand(1);
                RecursivelySolve(ptr, currDepth, solved);
            }
            else
                RecursivelySolve(ptr, currDepth, solved);

            // If we don't know the set of possible addresses then do nothing. The solutions for the load inst has already been set to null
            // inside of `RecursivelySolve`.
            var addresses = solved.GetValueOrDefault(ptr, () => new());
            if (addresses == null || addresses.Count == 0)
                return;

            // Otherwise we know all addresses, we just need to dereference them.
            var solutions = DereferenceAddresses(addresses.ToList(), loadInst.TypeOf.IntWidth);
            solved[loadInst] = solutions.ToHashSet();
            Console.WriteLine("");
        }

        /// <summary>
        /// Given a set of jump table solutions(aka &jumpTable[index]), return the set of dereferenced values.
        /// </summary>
        private IReadOnlyList<ulong> DereferenceAddresses(IReadOnlyList<ulong> addresses, uint bitWidth)
            => addresses.Select(x => BinaryContentsReader.Dereference(binary, x, bitWidth)).ToList().AsReadOnly();

        // This is intended to address the recursive constant phi scenario.
        // In the wild I've seen jump tables inside of loops that use recursive phis like this:
        //     - %0 = phi i64 [ 1, %bb_1400986DA.i ], [ 5, %bb_1400986EF.i ], [ %1, %bb_140098833.i ], [ %1, %bb_14009882A.i ]
        //     - %1 = phi i64 [ 0, %bb_140098694.i ], [ %0, %bb_140098839.i ]
        // , where the phi is obviously reducible to a set of constants(0, 1, 5), but souper symbolizes it(i.e. replaces it with an unbounded variable that could be anything),
        // causing the the number of possible solutions to be nearly infinite. 
        // To address this, we implement a recursive algorithm to harvest the bounds of a phi if and only if it is a phi with all constant operands.
        // Optionally the phi is allowed to recursively depend on itself too.
        // TODO: Extend the algorithm to handle more convoluted cases, e.g. phis with a `select` of constant operands.
        private static bool TryGetPhiSolutions(LLVMValueRef parentPhi, HashSet<LLVMValueRef> seenPhis, HashSet<LLVMValueRef> seenConstantOperands)
        {
            // If we've already seen this phi then skip it.
            if (seenPhis.Contains(parentPhi))
                return true;

            // Mark the phi node as seen.
            seenPhis.Add(parentPhi);

            // Get the operands. If we have a select cond, x, y, remove the condition
            // from the list.
            HashSet<LLVMValueRef> phiOperands = new();
            var operands = parentPhi.GetOperands();
            if (parentPhi.InstructionOpcode == LLVMOpcode.LLVMSelect)
                operands = operands.Skip(1);

            // Process all operands.
            foreach (var operand in operands)
            {
                // If the phi contains a constant operand then add it to the set and skip to the next item.
                if (operand.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                {
                    seenConstantOperands.Add(operand);
                    continue;
                }

                // For now we only support phis with constant operands or phis with
                // (optionally recursive) phis that contain only constant operands.
                // So if the LLVM value is not an instruction, then we return false to indicate
                // that we cannot bound this PHI.
                if (operand.Kind != LLVMValueKind.LLVMInstructionValueKind)
                    return false;

                // Return false if the instruction is not a PHI. We can't bound it.
                if (operand.InstructionOpcode == LLVMOpcode.LLVMPHI)
                {
                    // Add the set of phi operands to the list.
                    phiOperands.Add(operand);
                    continue;
                }

                // Optionally we also allow `select` statements that recursively use PHI nodes.
                if(operand.InstructionOpcode == LLVMOpcode.LLVMSelect)
                {
                    phiOperands.Add(operand);
                    continue;
                }

                return false;
            }

            // Now at this point we may only have constant operands(which are already handled) and phi operands.
            // Process each phi.
            foreach (var phi in phiOperands)
            {
                // If the child phi contains any phi operands that are not itself or a constant, return false.
                bool notSolvable = !TryGetPhiSolutions(phi, seenPhis, seenConstantOperands);
                if (notSolvable)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Given a souper instruction(which can be a variable or phi node for example, and a set of all possible values that the variable may hold,
        /// return a single instruction which represents a constraint on the range of values.
        /// For example if you have solutions [0..4] for variable x, this would return a souper instruction modeling the constraint "x >= 0 && x <=4".
        /// </summary>
        /// <param name="toBound">The instruction being bounded.</param>
        /// <param name="knownSolutions">The list of all known concrete solutions.</param>
        /// <returns></returns>
        private static SouperInst BuildConstraintToBoundVariable(SouperInstContext instCtx, SouperInst toBound, HashSet<ulong> knownSolutions)
        {
            if (!knownSolutions.Any())
                throw new InvalidOperationException($"The instruction {toBound} should not be bounded! The solution list is empty.");

            // A (> min && < max) constraint is presumably easier to solve than a
            // set of nested ORs representing all possible solutions. But we can only
            // do this if the value range is contiguous. TODO: Group non contiguous ranges
            // into multiple (> min && <max) constraints if it makes sense.
            if (IsContiguous(knownSolutions))
            {
                // Create const vars to represent the min / max.
                var umin = instCtx.GetConst(toBound.Width, knownSolutions.Min());
                var umax = instCtx.GetConst(toBound.Width, knownSolutions.Max());

                // Create an instruction to assert that the input variable cannot be less than the minimum value.
                var inverseBoundMin = instCtx.GetInst(SouperInstKind.Ult, 1, toBound, umin);
                var boundMin = instCtx.GetInst(SouperInstKind.Ne, 1, inverseBoundMin, instCtx.GetConst(1, 1));

                // Create an instruction to assert that the input variable must be less than or equal to the maximum.
                var boundMax = instCtx.GetInst(SouperInstKind.Ule, 1, toBound, umax);

                // And the two constraints together
                var boundedConstraint = instCtx.GetInst(SouperInstKind.And, 1, boundMin, boundMax);
                return boundedConstraint;
            }

            // Otherwise we use a set of nested OR statements to assert that the value being bounded must be one of the known solutions.
            else
            {
                // Initially set the first constraint to inputVar == solution_1.
                var first = instCtx.GetConst(toBound.Width, knownSolutions.First());
                var constraint = instCtx.GetInst(SouperInstKind.Eq, toBound.Width, toBound, first);

                // Then for all the other known solutions, assert that the input var may also equal the other solution.
                foreach (var solution in knownSolutions.Skip(1))
                {
                    var otherConstraint = instCtx.GetInst(SouperInstKind.Eq, toBound.Width, toBound, instCtx.GetConst(toBound.Width, solution));
                    constraint = instCtx.GetInst(SouperInstKind.Or, 1, constraint, otherConstraint);
                }

                return constraint;
            }
        }

        // Computes whether the value range is contiguous.
        // E.g. if the value range is [0..30] then it's contigous,
        // but [0..10]..[20..30] is not. 
        private static bool IsContiguous(HashSet<ulong> inputSolutions)
        {
            // Compute whether the range is contigous. 
            // E.g. if the value range is [0..30] then it's contigous,
            // but [0..10]..[20..30] is not. 
            var min = inputSolutions.Min();
            var max = inputSolutions.Max();
            for (ulong i = min; i < max + 1; i++)
            {
                if (inputSolutions.Contains(min + i))
                    continue;
                return false;
            }

            return true;
        }

        private HashSet<ulong>? TrySolveAllValues(SouperInst toSolve, List<SouperInstMapping> pcs, SouperInst constraint)
        {
            var constVar = instCtx.CreateVar(toSolve.Width, "ConstantVar");
            var instMapping = new SouperInstMapping(toSolve, constVar);
            var builder = new SouperExprBuilder(instCtx);

            HashSet<ulong> knownSolutions = new();
            while(true)
            {
                // Return null if there are too many potential solutions.
                if (knownSolutions.Count > 128)
                    return null;

                // Build the query.
                var modelInstructions = new List<SouperInst>();
                SouperInst precondition = constraint;
                var query = builder.BuildQuery(instCtx, new List<SouperBlockPCMapping>().AsReadOnly(), pcs, instMapping, modelInstructions, precondition, true, true);

                Console.WriteLine(query);
                //return null;
                Console.WriteLine("");
                Console.WriteLine($"Query size: {query.Length}");
                Console.WriteLine($"Ast size: {toSolve.ToString().Length}");
                Console.WriteLine($"Pcs size: {pcs.Sum(x => x.Lhs.ToString().Length)}");
                Console.WriteLine($"Precondition size: {precondition.ToString().Length}");
                // Solve the query.
                var hasSolution = ConstantSolverWithCache.TrySolveConstant(query, "ConstantVar", out ulong constant);
                if (hasSolution)
                {
                    // Append the solution to solution list.
                    knownSolutions.Add(constant);
                    Console.WriteLine($"Found solution: 0x{constant.ToString("X")}");

                    // Update the constraint to exclude the solution we just found.
                    var ne = instCtx.GetInst(SouperInstKind.Ne, toSolve.Width, constVar, instCtx.GetConst(toSolve.Width, constant));
                    constraint = instCtx.GetInst(SouperInstKind.And, 1, constraint, ne);
                }

                else
                {
                    // Break and return since we've solved all possible values.
                    Console.WriteLine("Found all possible solutions.");
                    break;
                }
            }

            return knownSolutions;
        }

        private SouperCandidateReplacement ExtractCandidate(LLVMValueRef value)
        {
            var options = new SouperExprBuilderOptions(false, value);
            var fcs = SouperCandidateExtractor.ExtractCandidates(function, instCtx, builderCtx, options);
            var replacements = fcs.Blocks.SelectMany(x => x.Replacements).Where(x => x.Origin == value).ToList();
            return replacements.SingleOrDefault();
        }
    }
}
