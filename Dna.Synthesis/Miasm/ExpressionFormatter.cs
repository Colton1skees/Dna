using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public static class ExpressionFormatter
    {
        public static string FormatExpression(MiasmExpr expr)
        {
            StringBuilder sb = new StringBuilder();
            FormatExpressionInternal(expr, ref sb);
            return sb.ToString();
        }

        private static void FormatExpressionInternal(MiasmExpr expr, ref StringBuilder sb)
        {
            // If we are at the root of the tree, create a new expression builder.
            sb ??= new StringBuilder();

            switch(expr)
            {
                case ExprId exprId:
                    sb.AppendFormat(@"ExprId(""{0}"", {1})", exprId.Name, exprId.Size);
                    break;
                case ExprInt exprInt:
                    sb.AppendFormat("ExprInt({0}, {1})", exprInt.Value, exprInt.Size);
                    break;
                case ExprOp exprOp:
                    if(exprOp.Op == "^" && exprOp.Operands[1] is ExprInt xorInt && xorInt.Value == ulong.MaxValue)
                    {
                        sb.Append("~");
                        FormatExpressionInternal(exprOp.Operands[0], ref sb);
                        return;
                    }

                    sb.AppendFormat(@"ExprOp(""{0}"", ", exprOp.Op);
                    FormatExpressionInternal(exprOp.Operands[0], ref sb);
                    if (exprOp.Operands.Count == 2)
                    {
                        sb.Append(", ");
                        FormatExpressionInternal(exprOp.Operands[1], ref sb);
                    }

                    else if (exprOp.Operands.Count > 2)
                    {
                        throw new InvalidOperationException();
                    }

                    sb.AppendFormat(")");
                    break;
                case ExprSlice exprSlice:
                    sb.Append("ExprSlice(");
                    FormatExpressionInternal(exprSlice.Src, ref sb);
                    sb.AppendFormat(", {0}, {1})", exprSlice.Start, exprSlice.Stop);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
