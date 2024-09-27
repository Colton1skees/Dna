using Dna.ControlFlow.Extensions;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.Symbolic;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;

namespace Dna.BinaryTranslator.JmpTables.Slicing
{
    public class SymbolicExpressionSlicer
    {
        private readonly LLVMValueRef function;

        private readonly LLVMValueRef jmpCall;

        private readonly LLVMValueRef srcValue;

        private readonly LoopInfo loopInfo;

        private readonly MemorySSA mssa;

        private Dictionary<LLVMBasicBlockRef, HashSet<AbstractNode>> collectedPathConstraints = new();

        private HashSet<LLVMBasicBlockRef> visitedBlocks = new();

        private HashSet<LLVMBasicBlockRef> visitedOutgoing = new();

        private Dictionary<LLVMValueRef, AbstractNode> definitionMapping = new();


        private HashSet<LLVMValueRef> currentPhis = new();

        private uint tempCount;

        public IReadOnlyDictionary<LLVMValueRef, AbstractNode> DefinitionMapping => definitionMapping.AsReadOnly();

        public IReadOnlyDictionary<AbstractNode, LLVMValueRef> AstToLlvmMapping => GetNodeToLlvmMapping();

        public SymbolicExpressionSlicer(LLVMValueRef function, LLVMValueRef value, LoopInfo loopInfo, MemorySSA mssa)
        {
            this.function = function;
            srcValue = value;
            this.loopInfo = loopInfo;
            this.mssa = mssa;
        }

        private IReadOnlyDictionary<AbstractNode, LLVMValueRef> GetNodeToLlvmMapping()
        {
            var output = new Dictionary<AbstractNode, LLVMValueRef>();
            foreach(var pair in DefinitionMapping)
            {
                output.TryAdd(pair.Value, pair.Key);
            }

            return output.AsReadOnly();
        }

        public HashSet<AbstractNode> ComputePathConstraints(LLVMBasicBlockRef block)
        {
            // Skip if we've already processed this block.
            if (visitedBlocks.Contains(block))
                return collectedPathConstraints[block];

            // Set the default value to an empty hashset.
            collectedPathConstraints.TryAdd(block, new HashSet<AbstractNode>());

            // Mark our block as visited.
            visitedBlocks.Add(block);

            // If this is the entry basic block then set the initial constraint to true.
            if (block == function.EntryBasicBlock)
                collectedPathConstraints[block].Add(new IntegerNode(1, 1));

            // Collect path constraints for each visitable predecessor.
            foreach (var predecessor in block.GetPredecessors())
            {
                // If we've already processed this predecessor, then do nothing.
                if (visitedBlocks.Contains(predecessor))
                    continue;

                // For now we stop at any loop header. TODO: Handle loops.
                if (loopInfo.IsLoopHeader(predecessor))
                {
                    // If this is a loop header, we still want to collect the set of outgoing constraints..
                    // but we don't want to process items beyond the header.
                    CollectOutgoingConstraints(predecessor);
                    continue;
                }

                // If the predecessor is not a loop header, and it hasn't been visited yet,
                // then we need to collect it's path constraints before we process the current block.
                ComputePathConstraints(predecessor);
            }

            CollectOutgoingConstraints(block);
            return collectedPathConstraints[block];
        }

        private void CollectOutgoingConstraints(LLVMBasicBlockRef block)
        {
            if (visitedOutgoing.Contains(block))
                return;

            visitedOutgoing.Add(block);

            // At this point we have all path constraints of all predecessors(ignoring loops).
            // Now we compute the path constraints needed to reach the successors of this block,
            // and then store the results to the path constraints of the destination.
            var terminatorInst = block.LastInstruction;
            collectedPathConstraints.TryAdd(block, new());
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
                    AbstractNode falseCond = new BvnotNode(cond);
                    var trueBlock = terminatorInst.GetOperand(2).AsBasicBlock();
                    var falseBlock = terminatorInst.GetOperand(1).AsBasicBlock();

                    collectedPathConstraints.TryAdd(trueBlock, new HashSet<AbstractNode>());
                    collectedPathConstraints.TryAdd(falseBlock, new HashSet<AbstractNode>());
                    var trueConstraintMapping = collectedPathConstraints[trueBlock];
                    var falseConstraintMapping = collectedPathConstraints[falseBlock];

                    if (constraintsMapping.Any())
                    {
                        cond = new BvandNode(cond, constraintsMapping.Single());
                        falseCond = new BvandNode(falseCond, constraintsMapping.Single());
                    }

                    AddConstraint(trueConstraintMapping, cond);
                    AddConstraint(falseConstraintMapping, falseCond);
                }

                else if (terminatorInst.OperandCount == 1)
                {
                    var trueBlock = terminatorInst.GetOperand(0).AsBasicBlock();
                    collectedPathConstraints.TryAdd(trueBlock, new HashSet<AbstractNode>());
                    var trueConstraintMapping = collectedPathConstraints[trueBlock];
                    if (constraintsMapping.Any())
                    {
                        AddConstraint(trueConstraintMapping, constraintsMapping.Single());
                    }
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

        private void AddConstraint(HashSet<AbstractNode> mapping, AbstractNode constraint)
        {
            if (mapping.Count > 1)
                throw new InvalidOperationException("Block cannot have more than one constraint mapping.");

            if (mapping.Count == 0)
            {
                mapping.Add(constraint);
                return;
            }

            var single = mapping.Single();
            var newConstraint = new BvorNode(single, constraint);
            mapping.Remove(single);
            mapping.Add(newConstraint);
        }

        public HashSet<AbstractNode> GetConstraints(LLVMBasicBlockRef block)
        {
            return collectedPathConstraints[block];
        }

        public AbstractNode GetDefinition(LLVMValueRef value)
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
                case LLVMOpcode.LLVMFreeze:
                    if (inst.OperandCount > 1)
                        throw new InvalidOperationException($"Freeze inst {inst} has more than one operand.");
                    emit(op1());
                    break;
                case LLVMOpcode.LLVMPHI:
                    // Build a symbolic ITE node to represent the PHI.
                    emit(BuildIteForPhi(inst));
                    break;
                case LLVMOpcode.LLVMCall:
                    var callee = inst.GetOperands().Last();
                    if (callee.Name == "llvm.fshl.i32")
                    {
                        /*
                        var w = inst.GetOperand(0).TypeOf.IntWidth;
                        var arg0 = op1();
                        var arg1 = op2();
                        var arg2 = op3();

                        // Compute the input.
                        var zext = new ZxNode((w * 2) - w, arg0);
                        var shl = new BvshlNode(zext, new IntegerNode(w, zext.BitSize));
                        var arg1Zext = new ZxNode((w * 2) - w, arg1);
                        var input = new BvorNode(shl, arg1Zext);

                        // Compute the output.
                        AbstractNode urem = new BvuremNode(arg2, new IntegerNode(w, arg2.BitSize));
                        urem = new ZxNode((w * 2) - w, urem);
                        var output = new BvshlNode(input, urem);
                        w -= 1; 
                        var extracted = new ExtractNode(w + w, w, output);
                        emit(extracted);
                        */
                        if (inst.GetOperand(0).ToString() != inst.GetOperand(1).ToString())
                            throw new InvalidOperationException();

                        var fshl = new BvrolNode(op1(), op3());
                        emit(fshl);
                        break;
                    }

                    else if (callee.Name.StartsWith("llvm.umax.i"))
                    {
                        var arg0 = op1();
                        var arg1 = op2();
                        var select = new IteNode(new BvugeNode(arg0, arg1), arg0, arg1);
                        emit(select);
                        break;
                    }

                    goto default;
                default:
                    throw new InvalidOperationException($"Failed to translate LLVM inst {inst} to AST. The OpCode {(inst.Kind == LLVMValueKind.LLVMInstructionValueKind ? inst.InstructionOpcode : inst.Kind)} is not supported.");
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

        /*
        private List<(LLVMBasicBlockRef block, LLVMValueRef unrolledPhi)> UnrollPhiOperands(LLVMValueRef phi)
        {
            var unrolled = new HashSet<(LLVMBasicBlockRef block, LLVMValueRef unrolledPhi)>();
            var toUnroll = new HashSet<(LLVMBasicBlockRef block, LLVMValueRef unrolledPhi)>();
            var variables = new HashSet<LLVMValueRef>();
            while (toUnroll.Any())
            {
                var popped = unrolled.First();
                toUnroll.Remove(popped);
                unrolled.Add(popped.blo)

                foreach(var operand in popped)
            }
            
            

            return null;
        }
        

        private List<(AbstractNode value, AbstractNode pathConstraint)> UnrollPhis(HashSet<LLVMValueRef> )
        {

        }
        */
        private AbstractNode BuildIteForPhi(LLVMValueRef phiInst)
        {
            var phiValues = new List<(AbstractNode value, AbstractNode pathConstraint)>();

            for (uint i = 0; i < phiInst.OperandCount; i++)
            {
                // Collect the path constraints of the basic block that the phi value comes from.
                var incomingBlock = phiInst.GetIncomingBlock(i);
                Console.WriteLine(collectedPathConstraints.ContainsKey(incomingBlock));
                Console.WriteLine($"Maybe computing path constraint for phi operand {phiInst.GetOperand(i)} with block {incomingBlock}");
                var constraint = ComputePathConstraints(incomingBlock).SingleOrDefault();

                // Grab the constraints. If there is no constraint(e.g. this is an entry block),
                // then we set the constraint to true. Then later on we apply a sorting
                // to ensure that this 'true' constraint is placed at the end of the nested ite - as a default case.
                if (constraint == null)
                    constraint = new IntegerNode(1, 1);

                // Add the symbolic ast and the constraint.
                Console.WriteLine($"Get definition for phi operand: {phiInst.GetOperand(i)}");
                phiValues.Add((GetDefinition(phiInst.GetOperand(i)), constraint));
            }

            // Apply an ordering such that the longer path constraints get checked first. 
            phiValues = phiValues.OrderByDescending(x => x.pathConstraint.ToString().Length).ToList();

            // Iterate through the phis in reverse.
            AbstractNode last = null;
            for(int i = phiValues.Count - 1; i >= 0; i++)
            {
                var phi = phiValues[i];

                // We represent PHI nodes as sets of nested ITEs.
                // Given phi [0, bb0], [1, bb1], [2, bb2], it is transformed into:
                //      ite(bb0_constraint, 0, ite(bb1constraint, 1, 2)))
                //
                // Thus we grab the last element in the phi value list and make it the default case.
                // If the constraints to reach bb0 and bb1 aren't true then the value must belong to phi2.
                if (i == phiValues.Count - 1)
                {
                    last = phi.value;
                    continue;
                }

                // Build the ite node. Since we're iterating backwards and have already set the default case(bb2 from my example)
                // to 'last`, there is always a value to use in the else node.
                last = new IteNode(phi.pathConstraint, phi.value, last);
            }

            return last;
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
            if (loopInfo.GetLoopDepth(value.InstructionParent) != loopInfo.GetLoopDepth(srcValue.InstructionParent))
                return false;
            // If the function has no loops, then *nothing* can be defined in a loop.
            if (loopInfo.LoopsInPreorder.Count == 0)
                return false;

            // Otherwise return true if the loops are the samed.
            return loopInfo.GetLoopFor(value.InstructionParent)?.Name != loopInfo.GetLoopFor(srcValue.InstructionParent)?.Name;
        }

        private TemporaryNode CreateTemp(uint bitWidth, string name)
        {
            var node = new TemporaryNode(tempCount, bitWidth, name);
            tempCount++;
            return node;
        }
    }
}
