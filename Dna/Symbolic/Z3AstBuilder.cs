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
        private Context ctx;

        public Z3AstBuilder(Context context)
        {
            this.ctx = context;
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
                GetZ3Ast(getVar(node), isDefined, getVar) : ctx.MkBVConst(GetVariableNode(node), node.BitSize);

            Expr? z3Ast = expression switch
            {
                // Evaluate expressions.
                BvaddNode => ctx.MkBVAdd(bv1(), bv2()),
                BvandNode => ctx.MkBVAND(bv1(), bv2()),
                BvashrNode => ctx.MkBVASHR(bv1(), bv2()),
                BvlshrNode => ctx.MkBVLSHR(bv1(), bv2()),
                BvmulNode => ctx.MkBVMul(bv1(), bv2()),
                BvnegNode => ctx.MkBVNeg(bv1()),
                BvnotNode => ctx.MkBVNot(bv1()),
                BvorNode => ctx.MkBVOR(bv1(), bv2()),
                BvrolNode => ctx.MkBVRotateLeft(bv1(), bv2()),
                BvrorNode => ctx.MkBVRotateRight(bv1(), bv2()),
                BvsdivNode => ctx.MkBVSDiv(bv1(), bv2()),
                EqualNode => ctx.MkITE(ctx.MkEq(op1(), op2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvsgeNode => ctx.MkITE(ctx.MkBVSGE(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvsgtNode => ctx.MkITE(ctx.MkBVSGT(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvsleNode => ctx.MkITE(ctx.MkBVSLE(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvsltNode => ctx.MkITE(ctx.MkBVSLT(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvugeNode => ctx.MkITE(ctx.MkBVUGE(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvugtNode => ctx.MkITE(ctx.MkBVUGT(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvuleNode => ctx.MkITE(ctx.MkBVULE(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvultNode => ctx.MkITE(ctx.MkBVULT(bv1(), bv2()), ctx.MkBV(1, 1), ctx.MkBV(0, 1)),
                BvsmodNode => ctx.MkBVSMod(bv1(), bv2()),
                BvsremNode => ctx.MkBVSRem(bv1(), bv2()),
                BvsubNode => ctx.MkBVSub(bv1(), bv2()),
                BvudivNode => ctx.MkBVUDiv(bv1(), bv2()),
                BvuremNode => ctx.MkBVURem(bv1(), bv2()),
                BvxorNode => ctx.MkBVXOR(bv1(), bv2()),
                ConcatNode node => FromConcat(node, (AbstractNode expr) => GetZ3Ast(expression, isDefined, getVar)),
                ExtractNode node => ctx.MkExtract((uint)node.High.Value, (uint)node.Low.Value, bv3()),
                IteNode node => ctx.MkITE(ctx.MkEq(op1(), ctx.MkBV(1, 1)), op2(), op3()),
                SxNode node => ctx.MkSignExt((uint)node.SizeExt.Value, bv2()),
                ZxNode node => ctx.MkZeroExt((uint)node.SizeExt.Value, bv2()),

                // Evaluate variables(e.g. registers, ssa variables) using the provided callback.
                // The callback allows the user to substitute symbolic or concrete values without modifying the AST.
                UndefNode node => ctx.MkBVConst("undef" + node.BitSize, node.BitSize),
                RegisterNode node => evaluateVariable(node),
                SsaVariableNode node => evaluateVariable(node),
                TemporaryNode node => evaluateVariable(node),
                IntegerNode node => ctx.MkBV(node.Value, node.BitSize),

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
                currentValue = ctx.MkConcat((BitVecExpr)currentValue, (BitVecExpr)evaluate(child));
            }

            return currentValue;
        }
    }
}
