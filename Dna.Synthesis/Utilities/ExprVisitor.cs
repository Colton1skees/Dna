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
        public static void DfsVisit(Expr expr, Action<Expr> onVisited)
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
    }
}
