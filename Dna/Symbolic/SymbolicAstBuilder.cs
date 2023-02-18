using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Expression;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Symbolic
{
    /// <inheritdoc/>
    public class SymbolicAstBuilder : ISymbolicAstBuilder
    {
        /// <summary>
        /// The function pointer used to query the current symbolic ast for an operand.
        /// </summary>
        private readonly Func<IOperand, AbstractNode> evaluateSymbolicAst;

        public SymbolicAstBuilder(Func<IOperand, AbstractNode> evaluateSymbolicAst)
        {
            this.evaluateSymbolicAst = evaluateSymbolicAst;
        }

        /// <inheritdoc/>
        public (IOperand destination, AbstractNode source) GetAst(AbstractInst instruction)
        {
            // Concise operand AST getter methods.
            var op1 = () =>  evaluateSymbolicAst(instruction.Operands[0]);
            var op2 = () => evaluateSymbolicAst(instruction.Operands[1]);
            var op3 = () => evaluateSymbolicAst(instruction.Operands[2]);

            // Construct an AST for the value of each operation.
            AbstractNode? valueAst = instruction switch
            {
                InstAdd => new BvaddNode(op1(), op2()),
                InstAnd => new BvandNode(op1(), op2()),
                InstAshr => new BvashrNode(op1(), op2()),
                InstLshr => new BvlshrNode(op1(), op2()),
                InstMul => new BvmulNode(op1(), op2()),
                InstNeg => new BvnegNode(op1()),
                InstNot => new BvnotNode(op1()),
                InstOr => new BvorNode(op1(), op2()),
                InstRol => new BvrolNode(op1(), op2()),
                InstRor => new BvrorNode(op1(), op2()),
                InstSdiv => new BvsdivNode(op1(), op2()),
                InstCond inst => FromCond(inst),
                InstSmod => new BvsmodNode(op1(), op2()),
                InstSrem => new BvsremNode(op1(), op2()),
                InstSub => new BvsubNode(op1(), op2()),
                InstUdiv => new BvudivNode(op1(), op2()),
                InstUrem => new BvuremNode(op1(), op2()),
                InstXor => new BvxorNode(op1(), op2()),
                InstConcat => new ConcatNode(instruction.Operands.Select(x => evaluateSymbolicAst(x))),
                InstExtract inst => new ExtractNode((IntegerNode)op1(), (IntegerNode)op2(), op3()),
                InstSelect inst => new IteNode(op1(), op2(), op3()),
                InstSx inst => new SxNode(op1(), op2()),
                InstZx inst => new ZxNode(op1(), op2()),
                InstCopy => op1(),
                InstLoad => new MemoryNode(op1(), instruction.Dest.Bitsize),
                _ => throw new InvalidOperationException(String.Format("Cannot get AST for type: {0}", instruction.GetType().FullName))
            };

            return (instruction.Dest, valueAst);
        }

        private AbstractNode FromCond(InstCond cond)
        {
            // Concise operand AST getter methods.
            var op1 = () => evaluateSymbolicAst(cond.Operands[0]);
            var op2 = () => evaluateSymbolicAst(cond.Operands[1]);
            var op3 = () => evaluateSymbolicAst(cond.Operands[2]);

            return cond.CondType switch
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
                _ => throw new InvalidOperationException(String.Format("CondType {0} is not valid.", cond.CondType))
            };
        }

        public (MemoryNode destination, AbstractNode source) GetStoreAst(InstStore inst)
        {
            // Get nodes for the source and destination.
            var dst = new MemoryNode(evaluateSymbolicAst(inst.Dest), inst.Bitsize);
            var src = evaluateSymbolicAst(inst.Op1);

            return (dst, src);
        }
    }
}
