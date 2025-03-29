using Dna.Binary;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.Symbolic;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.Passes
{
    public class JumpTableAnalysisPassV1
    {
        private LoopInfo loopInfo;

        private Dictionary<LLVMBasicBlockRef, HashSet<AbstractNode>> collectedPathConstraints = new();

        private HashSet<LLVMBasicBlockRef> visitedBlocks = new();

        private Dictionary<LLVMValueRef, AbstractNode> definitionMapping = new();

        private uint tempCount;

        private LLVMValueRef boundInstruction;

        public dgSolveJumpTableBounds PtrAnalyzeBounds { get; }

        public unsafe JumpTableAnalysisPassV1()
        {
            PtrAnalyzeBounds = new dgSolveJumpTableBounds(Analyze);
        }

        public unsafe void Analyze(LLVMOpaqueValue* function, nint pLoopInfo, nint pMssa, nint lazyValueInfo, nint trySolveConstant) => AnalyzeJumpTableBounds(function, new LoopInfo(pLoopInfo), new MemorySSA(pMssa), lazyValueInfo, trySolveConstant);

        private void AnalyzeJumpTableBounds(LLVMValueRef function, LoopInfo loopInfo, MemorySSA mssa, nint lazyValueInfo, nint trySolveConstant)
        {
            this.loopInfo = loopInfo;

            // Dump the module to file just for debugging.
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Get the instruction which contains the symbolic index for the jump table. 
            var boundInstruction = function.GetInstructions().Single(x => x.ToString().Contains("%conv.i22.i145.i = zext i32 %add.i.i144.i to i64"));

            var destBlock = function.GetInstructions().Single(x => x.ToString().Contains("dna_jump")).InstructionParent;
            GetPathConstraints(destBlock);

            var constraints = collectedPathConstraints[destBlock];
            for (int i = 0; i < 10; i++)
                Console.WriteLine("");

            //var z3Ast = definitionMapping[boundInstruction];
            var boundDef = GetDefinition(boundInstruction);
            Console.WriteLine($"Index ast: {boundDef}");

            Console.WriteLine("Path constraints needed to reach indirect jump:");
            foreach (var constraint in constraints)
            {
                Console.WriteLine("    " + constraint);
            }

            var z3Translator = new Z3AstBuilder(new Context());
            var z3IndexAst = z3Translator.GetZ3Ast(boundDef);
            var solver = z3Translator.Ctx.MkSolver("QF_BV");
            foreach (var constraint in constraints)
            {
                var z3Constraint = z3Translator.GetZ3Ast(constraint);

                var bvTrue = z3Translator.Ctx.MkBV(1, 1);

                var bvConstraint = z3Translator.Ctx.MkEq(z3Constraint, bvTrue);


                solver.Add(bvConstraint);
            }

            List<ulong> solutions = new();
            while (true)
            {
                var check = solver.Check();
                if (check == Status.UNSATISFIABLE)
                {
                    break;
                }

                var model = solver.Model;
                var evaluation = model.Eval(z3IndexAst);
                if (evaluation is not BitVecNum num)
                {
                    break;
                }

                solutions.Add(num.UInt64);
                solver.Add(z3Translator.Ctx.MkNot(z3Translator.Ctx.MkEq(z3IndexAst, z3Translator.Ctx.MkBV(num.UInt64, boundDef.BitSize))));
            }

            for (int i = 0; i < 5; i++)
                Console.WriteLine("");

            solutions = solutions.OrderByDescending(x => x).Reverse().ToList();
            Console.WriteLine($"Jump table bounds can be summarized as [{solutions.First()}...{solutions.Last()}]");
            Console.WriteLine($"Successfully identified: {solutions.Count} valid jump table indices:");
            foreach (var solution in solutions)
            {
                Console.WriteLine($"    {solution}");
            }
            Debugger.Break();
        }

        public void GetPathConstraints(LLVMBasicBlockRef block)
        {
            // Set the default value to an empty hashset.
            collectedPathConstraints.TryAdd(block, new HashSet<AbstractNode>());

            // Mark our block as visited.
            visitedBlocks.Add(block);

            // Collect path constraints for each visitable predecessor.
            foreach (var predecessor in block.GetPredecessors())
            {
                // For now we stop at any loop header. TODO: Handle loops.
                if (loopInfo.IsLoopHeader(predecessor))
                    continue;
                // If we've already processed this predecessor, then do nothing.
                if (visitedBlocks.Contains(predecessor))
                    continue;

                // If the predecessor is not a loop header, and it hasn't been visited yet,
                // then we need to collect it's path constraints before we process the current block.
                GetPathConstraints(predecessor);
            }

            // Ok at this point we have all path constraints of all predecessors(ignoring loops).
            // Now we compute the path constraints needed to reach the successors of this block,
            // and then store the results to the path constraints of the destination.
            var terminatorInst = block.LastInstruction;
            var constraintsMapping = collectedPathConstraints[block];

            // Here we have either a jcc or a direct branch. If we have a JCC then we compute a symbolic AST
            // for the jump condition, and then add it as a constraint to the outgoing blocks(with the condition ast being negated for the false branch).
            // Then, for both JCCs and direct branches, we add the path constraints of the current block to it's successor(s).
            if (terminatorInst.InstructionOpcode == LLVMOpcode.LLVMBr)
            {
                if (terminatorInst.OperandCount == 3)
                {
                    // https://github.com/numba/llvmlite/issues/741
                    // Branches are strange.  The operands are ordered:
                    // [Cond, FalseDest,] TrueDest.
                    var cond = GetDefinition(terminatorInst.GetOperand(0));
                    var trueBlock = terminatorInst.GetOperand(2).AsBasicBlock();
                    var falseBlock = terminatorInst.GetOperand(1).AsBasicBlock();

                    collectedPathConstraints.TryAdd(trueBlock, new HashSet<AbstractNode>());
                    collectedPathConstraints.TryAdd(falseBlock, new HashSet<AbstractNode>());
                    var trueConstraintMapping = collectedPathConstraints[trueBlock];
                    var falseConstraintMapping = collectedPathConstraints[falseBlock];

                    // Update the constraints depending on the condition.
                    trueConstraintMapping.Add(cond);
                    falseConstraintMapping.Add(new BvnotNode(cond));

                    // Take the constraints of the current block
                    // and add them to the set of possible constraints for the
                    // successor blocks.
                    trueConstraintMapping.AddRange(constraintsMapping);
                    falseConstraintMapping.AddRange(constraintsMapping);
                }

                else if (terminatorInst.OperandCount == 1)
                {
                    // Take the constraints of the current block
                    // and add them to the set of possible constraints for the
                    // single successor block.
                    var trueBlock = terminatorInst.GetOperand(0).AsBasicBlock();
                    collectedPathConstraints.TryAdd(trueBlock, new HashSet<AbstractNode>());
                    var trueConstraintMapping = collectedPathConstraints[trueBlock];
                    trueConstraintMapping.AddRange(constraintsMapping);
                }
            }

            // Do nothing on RETs.
            else if (terminatorInst.InstructionOpcode == LLVMOpcode.LLVMRet)
            {

            }

            // Throw on switches.
            else if (terminatorInst.InstructionOpcode == LLVMOpcode.LLVMSwitch)
            {
                throw new InvalidOperationException();
            }
        }

        private AbstractNode GetDefinition(LLVMValueRef value)
        {
            // If we already have a definition for the value, return it.
            if (definitionMapping.ContainsKey(value))
                return definitionMapping[value];

            // If it's an argument, then create a new symbolic definition mapping.
            if (value.Kind == LLVMValueKind.LLVMArgumentValueKind)
            {
                var intWidth = value.TypeOf.IntWidth;
                var temp = CreateTemp(intWidth, value.Name);
                definitionMapping.Add(value, temp);
                return temp;
            }

            // If the value is a constant, store the definition as a constant.
            if (value.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                var constInt = new IntegerNode(value.ConstIntZExt, value.TypeOf.IntWidth);
                definitionMapping.Add(value, constInt);
                return constInt;
            }

            // Throw if it's not an instruction. We shouldn't be attempting to compute definitions for anything else.
            if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
                throw new InvalidOperationException($"Cannot compute definition for {value}. The kind {value.Kind} is not supported.");

            // Create a symbolic definition for the variable if it's defined inside of a different loop.
            // This basically asserts that values defined inside of loops can be anything.
            if (IsDefinedInDifferentLoop(value))
            {
                var unreachableDefinition = CreateTemp(value.TypeOf.IntWidth, value.Name);
                definitionMapping.Add(value, unreachableDefinition);
                return unreachableDefinition;
            }

            var emit = (AbstractNode node) =>
            {
                definitionMapping.TryAdd(value, node);
            };

            var op1 = () =>
            {
                var val = GetDefinition(value.GetOperand(0));
                return val;
            };

            var op2 = () =>
            {
                var val = GetDefinition(value.GetOperand(1));
                return val;
            };

            var op3 = () =>
            {
                var val = GetDefinition(value.GetOperand(2));
                return val;
            };

            var inst = value;
            switch (inst.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                    emit(new BvaddNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAnd:
                    emit(new BvandNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAShr:
                    emit(new BvashrNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMLShr:
                    emit(new BvlshrNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMMul:
                    emit(new BvmulNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMOr:
                    emit(new BvorNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSDiv:
                    emit(new BvsdivNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMShl:
                    emit(new BvshlNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMICmp:
                    // Note: Since SMTLIB2 does not have an `NEQ` type node,
                    // we need to utilize an EQ node & swap the order of the ite input nodes.
                    if (inst.ICmpPredicate == LLVMIntPredicate.LLVMIntNE)
                    {
                        emit(new EqualNode(op2(), op1()));
                        break;
                    }

                    var predicate = GetCondType(inst.ICmpPredicate);
                    AbstractNode cmp = predicate switch
                    {
                        CondType.Eq => new EqualNode(op1(), op2()),
                        CondType.Sge => new BvsgeNode(op1(), op2()),
                        CondType.Sgt => new BvsgtNode(op1(), op2()),
                        CondType.Sle => new BvsleNode(op1(), op2()),
                        CondType.Slt => new BvsltNode(op1(), op2()),
                        CondType.Uge => new BvugeNode(op1(), op2()),
                        CondType.Ugt => new BvugtNode(op1(), op2()),
                        CondType.Ule => new BvuleNode(op1(), op2()),
                        CondType.Ult => new BvultNode(op1(), op2()),
                        _ => throw new InvalidOperationException(string.Format("CondType {0} is not valid.", predicate))
                    };

                    emit(cmp);
                    break;
                case LLVMOpcode.LLVMSRem:
                    emit(new BvsremNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSub:
                    emit(new BvsubNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMUDiv:
                    emit(new BvudivNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMURem:
                    emit(new BvuremNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMXor:
                    emit(new BvxorNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMTrunc:
                    var destTy = inst.TypeOf;
                    emit(new ExtractNode(destTy.IntWidth - 1u, 0, op1()));
                    break;
                case LLVMOpcode.LLVMSelect:
                    emit(new IteNode(op1(), op2(), op3()));
                    break;
                case LLVMOpcode.LLVMSExt:
                    var sxTy = inst.TypeOf;
                    var srcTy = inst.GetOperand(0).TypeOf;
                    emit(new SxNode(new IntegerNode(sxTy.IntWidth - srcTy.IntWidth, sxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMZExt:
                    var zxTy = inst.TypeOf;
                    var srcZxTy = inst.GetOperand(0).TypeOf;
                    emit(new ZxNode(new IntegerNode(zxTy.IntWidth - srcZxTy.IntWidth, zxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMLoad:
                    var intWidth = value.TypeOf.IntWidth;
                    var temp = CreateTemp(intWidth, value.ToString().Split(" = ")[0].Replace(" ", ""));
                    emit(temp);
                    break;
                case LLVMOpcode.LLVMPHI:
                    // Here is where things get REALLY complicated. When we encounter a phi node, 
                    // we must represent the value as set of nested ITEs, which select the value
                    // depending on the incoming branch.
                    // To determine which branch we came from, we use the set of path constraints collected
                    // from the predecessor block.
                    break;
                default:
                    throw new InvalidOperationException($"Failed to translate LLVM inst {inst} to AST. The OPCode is not supported.");
            }

            return definitionMapping[value];
        }

        private CondType GetCondType(LLVMIntPredicate type)
        {
            return type switch
            {
                LLVMIntPredicate.LLVMIntEQ => CondType.Eq,
                LLVMIntPredicate.LLVMIntSGE => CondType.Sge,
                LLVMIntPredicate.LLVMIntSGT => CondType.Sgt,
                LLVMIntPredicate.LLVMIntSLE => CondType.Sle,
                LLVMIntPredicate.LLVMIntSLT => CondType.Slt,
                LLVMIntPredicate.LLVMIntUGE => CondType.Uge,
                LLVMIntPredicate.LLVMIntUGT => CondType.Ugt,
                LLVMIntPredicate.LLVMIntULE => CondType.Ule,
                LLVMIntPredicate.LLVMIntULT => CondType.Ult,
                _ => throw new InvalidOperationException($"Cond type {type} cannot be converted to smtlib cond.")
            };
        }

        /// <summary>
        /// Returns true if the value is not defined in the same loop as the sliced jump table.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsDefinedInDifferentLoop(LLVMValueRef value)
        {
            // If it's not an instruction, then it's a parameter or something which cannot be defined in a loop.
            if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            // If the loop depths don't match, then they're in different loops.
            if (loopInfo.GetLoopDepth(value.InstructionParent) != loopInfo.GetLoopDepth(boundInstruction.InstructionParent))
                return false;
            // If the function has no loops, then *nothing* can be defined in a loop.
            if (loopInfo.LoopsInPreorder.Count == 0)
                return false;

            // Otherwise return true if the loops are the samed.
            return loopInfo.GetLoopFor(value.InstructionParent)?.Name != loopInfo.GetLoopFor(boundInstruction.InstructionParent)?.Name;
        }

        private TemporaryNode CreateTemp(uint bitWidth, string name)
        {
            var node = new TemporaryNode(tempCount, bitWidth, name);
            tempCount++;
            return node;
        }
    }
}
