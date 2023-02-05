using Dna.Synthesis.Miasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Evaluation
{
    public static class ExpressionEvaluator
    {
        public static ulong EvaluateExpression(Expr expr)
        {
            switch(expr)
            {
                case ExprInt exprInt:
                    return FromExprInt(exprInt);
                case ExprOp exprOp:
                    return FromExprOp(exprOp);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static ulong FromExprInt(ExprInt expr)
        {
            return expr.Value;
        }

        private static ulong FromExprSlice(ExprSlice expr)
        {
            return 0;
            /*
            var src = EvaluateExpression(expr.Src);
            if(expr.Start != 0)
            {
                (long)src >>> expr.Start;
            }
            */
        }

        private static ulong FromExprOp(ExprOp expr)
        {
            return 0;
        }
    }
}
