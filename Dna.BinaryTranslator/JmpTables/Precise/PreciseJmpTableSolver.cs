using Dna.Binary;
using Dna.BinaryTranslator.JmpTables.Slicing;
using Dna.DataStructures;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    /// <summary>
    /// Represents a LLVM IR variable where the set of values is being solved for.
    /// </summary>
    /// <param name="Ptr">The jump table index variable being solved for. E.g. if you have jmp(table[%100]), then this is the %100 variable.</param>
    /// <param name="LoadInst">The optional load instruction that dereferences the pointer. This is optional because not all solved variables get dereferenced.
    /// Specifically the i64 address that's passed to remill_jump does not get referenced - and that is what is typically initially passed to the precise jmp table solver.
    /// </param>
    public record struct SolvableLoadOrVariable(LLVMValueRef Ptr, LLVMValueRef? LoadInst);

    public abstract record VariableSlice(uint BitWidth, LLVMValueRef Value, AbstractNode Ast, HashSet<AbstractNode> ConstraintsToReach);
    public record UnboundableVariableSlice(uint BitWidth, LLVMValueRef Value, AbstractNode Ast, HashSet<AbstractNode> ConstraintsToReach, TemporaryNode UnboundableDependency) : VariableSlice(BitWidth, Value, Ast, ConstraintsToReach);
    public record BoundableVariableSlice(uint BitWidth, LLVMValueRef Value, AbstractNode Ast, HashSet<AbstractNode> ConstraintsToReach) : VariableSlice(BitWidth, Value, Ast, ConstraintsToReach);

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
    public class PreciseJmpTableSolver
    {
        private readonly IBinary binary;

        private readonly ulong jmpFromAddress;

        private readonly LLVMBasicBlockRef jmpTableFromBlock;

        private readonly LLVMValueRef jmpPtr;

        private readonly LoopInfo loopInfo;

        public PreciseJmpTableSolver(IBinary binary, ulong jmpFromAddress, LLVMBasicBlockRef jmpTableFromBlock, LLVMValueRef jmpPtr, LoopInfo loopInfo)
        {
            this.binary = binary;
            this.jmpFromAddress = jmpFromAddress;
            this.jmpTableFromBlock = jmpTableFromBlock;
            this.jmpPtr = jmpPtr;
            this.loopInfo = loopInfo;
        }

        public JmpTable IterativelySolve()
        {
            OrderedSet<SolvableLoadOrVariable> solvingQueue = new();
            OrderedSet<UnboundableVariableSlice> unsolvedVariables = new();
            solvingQueue.Add(new SolvableLoadOrVariable(jmpPtr, (LLVMValueRef?)null));

            // Identify the first bounded value.
            while(true)
            {
                if (solvingQueue.Count > 4)
                    throw new InvalidOperationException($"Failed to solve jump table. Number of unboundable inputs exceeds the maximum of 4.");

                // Fetch the latest variable we are solving for.
                var toSolve = solvingQueue.Last();

                // Compute the ptr bitwidth.
                var bitWidth = toSolve.LoadInst != null ? toSolve.LoadInst.Value.TypeOf.IntWidth : 64;

                // Backwards slice the AST for the index ptr while collecting the set of constraints
                // necessary to reach the jump table basic block.
                var slicer = new SymbolicExpressionSlicer(jmpTableFromBlock.Parent, toSolve.Ptr, loopInfo, null);
                var possiblyBoundedIndex = slicer.GetDefinition(toSolve.Ptr);
                var constraints = slicer.ComputePathConstraints(jmpTableFromBlock);
                if (constraints.Count == 0)
                    throw new InvalidOperationException($"TODO: Cannot jump tables with no constraints.");

                // If we've found a boundable variable, then we need to backtrack and 
                // try solving all of the collected slices using the bounds we have now.
                // TODO: The "HasAnySolution" implementation is a trick that seems to work so far,
                // but I'm not sure how well it works in the presence of data bounding(as opposed to control flow bounding).
                // This most likely needs to be refactored into a binary search for the min/max value using iterative z3 queries.
                var hasSolution = Z3BoundSolver.HasAnySolution(possiblyBoundedIndex, constraints);
                if (hasSolution)
                {
                    // Construct a boundable variable slice instance.
                    var boundableVariable = new BoundableVariableSlice(bitWidth, toSolve.Ptr, possiblyBoundedIndex, constraints);

                    // Sort the unsolved variables in the order that they must be solved.
                    // I.e. if you have a nested jump table, we order this such that the final 'jmp' target is placed at the
                    // end of the list. Also note that the current boundable variable is not included in the list.
                    var fifoToSolve = unsolvedVariables.Reverse().ToList().AsReadOnly();

                    // Solve the set of outgoing jump table addresses.
                    var outgoingAddresses = Solve(boundableVariable, fifoToSolve);
                    return new JmpTable(jmpFromAddress, outgoingAddresses, Enumerable.Empty<ulong>().ToList(), isComplete: false);
                }

                // Otherwise the variable is not boundable. Try to identify and recursively
                // bound a single input variable.
                else
                {
                    // Get all input variables.
                    var inputVariables = AstFlattener.GetInputVariables(possiblyBoundedIndex);

                    // Throw if there is more than one input variable.
                    if (inputVariables.Count != 1)
                        throw new InvalidOperationException();
                    // Undefined input variables must a temporary node.
                    var src = (TemporaryNode)inputVariables.Single();

                    // Throw if the input variable is not the result of a 'load'.
                    // I can't think of any case where this would happen.
                    var astToLlvmMapping = slicer.AstToLlvmMapping;
                    var srcInst = astToLlvmMapping[src];

                    LLVMValueRef ptr = null;
                    if (srcInst.InstructionOpcode == LLVMOpcode.LLVMLoad)
                    {
                        // If the load operand is not a 'GetElementPtr' inst then it's probably a function argument or something - which is not supported and shouldn't 
                        // really happen inside in-the-wild binaries.
                        var gep = srcInst.GetOperand(0);
                        if (gep.Kind != LLVMValueKind.LLVMInstructionValueKind || gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                            throw new InvalidOperationException($"Jumping to arbitrary unbounded values is not allowed.");

                        // If the solving queue already contains the ptr we just identified, it means there is some type of recursive memory dependency.
                        // This shouldn't happen.
                        ptr = gep.GetOperand(1);
                    }

                    else
                    {
                        // Usually PHIs.
                        ptr = srcInst;
                    }

                    if (solvingQueue.Any(x => x.Ptr == ptr))
                        throw new InvalidOperationException($"Encountered recursive memory dependency on jump table operand.");

                    // Record the set of data necessary to revisit and solve this jump table.
                    // If we can bound one of the predecessors, then we revisit and solve this variable later.
                    unsolvedVariables.Add(new UnboundableVariableSlice(bitWidth, toSolve.Ptr, possiblyBoundedIndex, constraints, src));

                    // Queue up the ptr for solving.
                    solvingQueue.Add(new SolvableLoadOrVariable(ptr, srcInst));
                }
            }
        }

        private IReadOnlyList<ulong> Solve(BoundableVariableSlice boundableVariable, IReadOnlyList<UnboundableVariableSlice> fifoToSolve)
        {
            // Solve the set of possible boundable variable values, then dereference them.
            // TODO: If a constant value(e.g. 0x1400A100) was directly fed to remill_jump, this would return gibberish. We should only dereference
            // if there are multiple values within the queue.
            var solutions = DereferenceAddresses(Z3BoundSolver.GetSolutions(boundableVariable.Ast, boundableVariable.ConstraintsToReach), boundableVariable.BitWidth);

            var evaluator = new AstEvaluator();
            var queue = new Queue<UnboundableVariableSlice>(fifoToSolve);
            while(queue.Any())
            {
                // Pop the latest jump table variable to solve.
                var toSolve = queue.Dequeue();

                // Plug each solution from the last dependency into the current variable ast.
                var newSolutions = new List<ulong>();
                foreach (var solution in solutions )
                {
                    var solutionAst = new IntegerNode(solution, toSolve.UnboundableDependency.BitSize);

                    var eval = evaluator.Evaluate(toSolve.Ast, new AstSubstitution(toSolve.UnboundableDependency, solutionAst));
                    if (eval == null)
                        throw new InvalidOperationException($"Failed to evaluate ast: {toSolve.Ast}");
                    newSolutions.Add(eval.Value);
                }

                // Update the solutions list to contain the set of solutions for the variable we just solved.
                if (queue.Any())
                    solutions = DereferenceAddresses(newSolutions, toSolve.BitWidth);
                else
                    solutions = newSolutions;
            }

            return solutions;
            
        }

        /// <summary>
        /// Given a set of jump table solutions(aka &jumpTable[index]), return the set of dereferenced values.
        /// </summary>
        private IReadOnlyList<ulong> DereferenceAddresses(IReadOnlyList<ulong> addresses, uint bitWidth)
            => addresses.Select(x => BinaryContentsReader.Dereference(binary, x, bitWidth)).ToList().AsReadOnly();
    }
}
