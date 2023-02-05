using Dna.Synthesis.Miasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Utilities
{
    public static class ExprVisitor
    {
        public static void DfsVisit(MiasmExpr expr, Action<MiasmExpr> onVisited)
        {
            onVisited(expr);
            if (expr is ExprId exprId)
            {
                return;
            }

            else if (expr is ExprCond exprCond)
            {
                DfsVisit(exprCond.Cond, onVisited);
                DfsVisit(exprCond.Src1, onVisited);
                DfsVisit(exprCond.Src2, onVisited);
            }

            else if(expr is ExprMem exprMem)
            {
                DfsVisit(exprMem.Ptr, onVisited);
            }

            else if(expr is ExprOp exprOp)
            {
                foreach (var operand in exprOp.Operands)
                    DfsVisit(operand, onVisited);
            }

            else if(expr is ExprSlice exprSlice)
            {
                DfsVisit(exprSlice.Src, onVisited);
            }

            else if(expr is ExprCompose exprCompose)
            {
                foreach(var operand in exprCompose.Operands)
                    DfsVisit(operand, onVisited);
            }

            else if(expr is ExprInt exprInt)
            {
                return;
            }
        }

        public static MiasmExpr DfsReplace(MiasmExpr expr, Func<MiasmExpr, MiasmExpr?> getReplacement)
        {
            var replacement = getReplacement(expr);
            if (replacement != null)
                return replacement;

            if (expr is ExprId exprId)
            {
                return new ExprId(exprId.Name, exprId.Size);
            }

            else if (expr is ExprCond exprCond)
            {
                var cond = DfsReplace(exprCond.Cond, getReplacement);
                var src1 = DfsReplace(exprCond.Src1, getReplacement);
                var src2 = DfsReplace(exprCond.Src2, getReplacement);
                return new ExprCond(cond, src1, src2, expr.Size);
            }

            else if (expr is ExprMem exprMem)
            {
                var ptr = DfsReplace(exprMem.Ptr, getReplacement);
                return new ExprMem(ptr, exprMem.Size);
            }

            else if (expr is ExprOp exprOp)
            {
                var operands = new List<MiasmExpr>();
                foreach (var operand in exprOp.Operands)
                    operands.Add(DfsReplace(operand, getReplacement));

                return new ExprOp(exprOp.Size, exprOp.Op, operands);
            }

            else if (expr is ExprSlice exprSlice)
            {
                var src = DfsReplace(exprSlice.Src, getReplacement);
                return new ExprSlice(src, exprSlice.Start, exprSlice.Start);
            }

            else if (expr is ExprCompose exprCompose)
            {
                var operands = new List<MiasmExpr>();
                foreach (var operand in exprCompose.Operands)
                    operands.Add(DfsReplace(operand, getReplacement));

                return new ExprCompose(operands);
            }

            else if (expr is ExprInt exprInt)
            {
                return new ExprInt(exprInt.Value, exprInt.Size);
            }

            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
