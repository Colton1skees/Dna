using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Symbolic
{
    public class Z3AstBuilder
    {
        public Context Ctx { get; }

        public Solver Solver { get; }

        public Z3AstBuilder(Context context)
        {
            Ctx = context;
            Solver = Ctx.MkSolver("QF_BV");
            Solver.Check();
        }

        /// <summary>
        /// Gets a Z3 AST for the provided expression.
        /// Note: If a variable is not defined, then it is symbolized.
        /// </summary>
        /// <param name="expr"></param>
        public Expr GetZ3Ast(AbstractNode expr)
        {
            return GetZ3Ast(expr, (AbstractNode node) => false, (AbstractNode node) => new IntegerNode(0xC8, node.BitSize));
        }

        /// <summary>
        /// Gets a Z3 ast for the provided expression.
        /// </summary>
        /// <param name="expression">The expression to be converted.</param>
        /// <param name="isDefined">The callback used to determine whether an undefined variable(e.g. RAX) should be substituted.</param>
        /// <param name="getVar">If <paramref name="isDefined"/> returns true, this callback is used to substitute the variable with another AST.</param>
        public Expr GetZ3Ast(AbstractNode expression, Func<AbstractNode, bool> isDefined, Func<AbstractNode, AbstractNode> getVar)
        {
            // Concise operand AST getter methods.
            var op1 = () => GetZ3Ast(expression.Children[0], isDefined, getVar);
            var op2 = () => GetZ3Ast(expression.Children[1], isDefined, getVar);
            var op3 = () => GetZ3Ast(expression.Children[2], isDefined, getVar);

            // Concise bit vector AST getter methods.
            var bv1 = () => (BitVecExpr)GetZ3Ast(expression.Children[0], isDefined, getVar);
            var bv2 = () => (BitVecExpr)GetZ3Ast(expression.Children[1], isDefined, getVar);
            var bv3 = () => (BitVecExpr)GetZ3Ast(expression.Children[2], isDefined, getVar);

            // Wrapper for evaluating symbolic variables(e.g. registers, ssa variables, etc.)
            var evaluateVariable = (AbstractNode node) => isDefined(node) ? 
                GetZ3Ast(getVar(node), isDefined, getVar) : Ctx.MkBVConst(GetVariableNode(node), node.BitSize);

            Expr? z3Ast = expression switch
            {
                // Evaluate expressions.
                BvaddNode => Ctx.MkBVAdd(bv1(), bv2()),
                BvandNode => Ctx.MkBVAND(bv1(), bv2()),
                BvashrNode => Ctx.MkBVASHR(bv1(), bv2()),
                BvlshrNode => Ctx.MkBVLSHR(bv1(), bv2()),
                BvmulNode => Ctx.MkBVMul(bv1(), bv2()),
                BvnegNode => Ctx.MkBVNeg(bv1()),
                BvnotNode => Ctx.MkBVNot(bv1()),
                BvorNode => Ctx.MkBVOR(bv1(), bv2()),
                BvrolNode => Ctx.MkBVRotateLeft(bv1(), bv2()),
                BvrorNode => Ctx.MkBVRotateRight(bv1(), bv2()),
                BvsdivNode => Ctx.MkBVSDiv(bv1(), bv2()),
                EqualNode => Ctx.MkITE(Ctx.MkEq(op1(), op2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvsgeNode => Ctx.MkITE(Ctx.MkBVSGE(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvsgtNode => Ctx.MkITE(Ctx.MkBVSGT(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvsleNode => Ctx.MkITE(Ctx.MkBVSLE(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvsltNode => Ctx.MkITE(Ctx.MkBVSLT(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvugeNode => Ctx.MkITE(Ctx.MkBVUGE(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvugtNode => Ctx.MkITE(Ctx.MkBVUGT(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvuleNode => Ctx.MkITE(Ctx.MkBVULE(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvultNode => Ctx.MkITE(Ctx.MkBVULT(bv1(), bv2()), Ctx.MkBV(1, 1), Ctx.MkBV(0, 1)),
                BvsmodNode => Ctx.MkBVSMod(bv1(), bv2()),
                BvsremNode => Ctx.MkBVSRem(bv1(), bv2()),
                BvsubNode => Ctx.MkBVSub(bv1(), bv2()),
                BvudivNode => Ctx.MkBVUDiv(bv1(), bv2()),
                BvuremNode => Ctx.MkBVURem(bv1(), bv2()),
                BvxorNode => Ctx.MkBVXOR(bv1(), bv2()),
                ConcatNode node => FromConcat(node, evaluateVariable),
                ExtractNode node => Ctx.MkExtract((uint)node.High.Value, (uint)node.Low.Value, bv3()),
                IteNode node => Ctx.MkITE(Ctx.MkEq(op1(), Ctx.MkBV(1, 1)), op2(), op3()),
                SxNode node => Ctx.MkSignExt((uint)node.SizeExt.Value, bv2()),
                ZxNode node => Ctx.MkZeroExt((uint)node.SizeExt.Value, bv2()),

                // Evaluate variables(e.g. registers, ssa variables) using the provided callback.
                // The callback allows the user to substitute symbolic or concrete values without modifying the AST.
                UndefNode node => Ctx.MkBVConst("undef" + node.BitSize, node.BitSize),
                RegisterNode node => evaluateVariable(node),
                SsaVariableNode node => evaluateVariable(node),
                TemporaryNode node => evaluateVariable(node),
                IntegerNode node => Ctx.MkBV(node.Value, node.BitSize),

                _ => throw new InvalidOperationException(String.Format("Cannot get AST for type: {0}", expression.GetType().Name))
            };

            return z3Ast;
        }

        private string GetVariableNode(AbstractNode node)
        {
            var text = node.ToString().Replace("(", "").Replace(")", "");
            Console.WriteLine(text);
            return text;
        }

        private Expr FromConcat(ConcatNode concat, Func<AbstractNode, Expr> evaluate)
        {
            var currentValue = evaluate(concat.Children[0]);

            foreach(var child in concat.Children.Skip(1))
            {
                currentValue = Ctx.MkConcat((BitVecExpr)currentValue, (BitVecExpr)evaluate(child));
            }

            return currentValue;
        }
    }
}
