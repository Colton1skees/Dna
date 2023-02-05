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
                case ExprSlice exprSlice:
                    return FromExprSlice(exprSlice);
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
            // Remove trailing bits.
            var src = EvaluateExpression(expr.Src);
            if(expr.Start != 0)
            {
                src = src >>> (int)expr.Start;
            }

            // Remove leading bits.
            var shiftSize = (1u << (int)(expr.Stop - expr.Start)) - 1;
            var anded = src & shiftSize;
            return Trunc(anded, expr.Size);
        }

        private static ulong FromExprOp(ExprOp expr)
        {
            var getOp1 = () => { return EvaluateExpression(expr.Operands[0]); };
            var getOp2 = () => { return EvaluateExpression(expr.Operands[1]); };
            switch (expr.Op)
            {
                case "-":
                    return Trunc(~getOp1(), expr.Size);
                case "*":
                    return Trunc(Multiply(getOp1(), getOp2(), expr.Size), expr.Size);
                case "+":
                    return Trunc(Add(getOp1(), getOp2(), expr.Size), expr.Size);
                case "&":
                    return Trunc(getOp1() & getOp2(), expr.Size);
                case "|":
                    return Trunc(getOp1() | getOp2(), expr.Size);
                case "^":
                    return Trunc(getOp1() ^ getOp2(), expr.Size);
                case "<<":
                    return Trunc(LeftShift(getOp1(), getOp2(), expr.Size), expr.Size);
                default:
                    throw new NotImplementedException();
            }
        }

        private static ulong Trunc(ulong value, uint size)
        {
            if (size == 8)
                return (byte)value;
            else if (size == 16)
                return (ushort)value;
            else if (size == 32)
                return (uint)value;
            else if (size == 64)
                return value;
            else
                throw new NotImplementedException();
        }

        private static ulong Multiply(ulong a, ulong b, uint size)
        {
            if (size == 8)
            {
                var result = (byte)a * (byte)b;
                return (byte)result;
            }

            else if (size == 16)
            {
                var result = (ushort)a * (ushort)b;
                return (ushort)result;
            }

            else if (size == 32)
            {
                var result = (uint)a * (uint)b;
                return result;
            }

            else if (size == 64)
            {
                return a * b;
            }

            else
            {
                throw new NotImplementedException();
            }
        }

        private static ulong Add(ulong a, ulong b, uint size)
        {
            if (size == 8)
            {
                var result = (byte)a + (byte)b;
                return (byte)result;
            }

            else if (size == 16)
            {
                var result = (ushort)a + (ushort)b;
                return (ushort)result;
            }

            else if (size == 32)
            {
                var result = (uint)a + (uint)b;
                return result;
            }

            else if (size == 64)
            {
                return a + b;
            }

            else
            {
                throw new NotImplementedException();
            }
        }

        private static ulong LeftShift(ulong a, ulong b, uint size)
        {
            if (size == 8)
            {
                var result = (byte)a << (byte)b;
                return (byte)result;
            }

            else if (size == 16)
            {
                var result = (ushort)a << (ushort)b;
                return (ushort)result;
            }

            else if (size == 32)
            {
                var result = (uint)a << (ushort)b;
                return result;
            }

            else if (size == 64)
            {
                return a << (ushort)b;
            }

            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
