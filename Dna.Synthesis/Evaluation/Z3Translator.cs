using Dna.Synthesis.Miasm;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Evaluation
{
    internal class Z3Translator
    {
        private readonly Context ctx;

        public Z3Translator(Context context)
        {
            this.ctx = context;
        }

        public ulong EvaluateExpression(MiasmExpr expr)
        {
            var result = (BitVecNum)GetZ3Ast(expr).Simplify();
            return result.UInt64;
        }

        public BitVecExpr GetZ3Ast(MiasmExpr expression)
        {
            return expression switch
            {
                ExprId expr => ctx.MkBVConst(expr.Name, expr.Size),
                ExprInt expr => ctx.MkBV(expr.Value, expr.Size),
                ExprSlice expr => ctx.MkExtract(expr.Stop, expr.Start, GetZ3Ast(expr.Src)),
                ExprOp expr when expr.Op == "-" => ctx.MkBVXOR(GetZ3Ast(expr.Operands.Single()), ctx.MkBV(-1, expr.Size)),
                ExprOp expr when expr.Op == "*" => ctx.MkBVMul(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                ExprOp expr when expr.Op == "+" => ctx.MkBVAdd(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                ExprOp expr when expr.Op == "&" => ctx.MkBVAND(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                ExprOp expr when expr.Op == "|" => ctx.MkBVOR(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                ExprOp expr when expr.Op == "^" => ctx.MkBVXOR(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                ExprOp expr when expr.Op == "<<" => ctx.MkBVSHL(GetZ3Ast(expr.Operands[0]), GetZ3Ast(expr.Operands[1])),
                _ => throw new InvalidOperationException($"Cannot convert {expression} to Z3 AST.")
            };
        }
    }
}
