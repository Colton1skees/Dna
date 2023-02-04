using Dna.Synthesis.Miasm;
using Dna.Synthesis.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Utils
{
    public static class ExprUtilities
    {
        /// <summary>
        /// Get all unique variables(ExprIds) within an expression.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static HashSet<Expr> GetUniqueVariables(Expr expr)
        {
            // TODO: Do not visit memory ptr expressions.
            var uniqueVariables = new HashSet<Expr>();
            ExprVisitor.DfsVisit(expr, (Expr visitedExpr) =>
            {
                if (visitedExpr is not ExprId)
                    return;

                uniqueVariables.Add(visitedExpr);
            });

            return uniqueVariables;
        }
    }
}
