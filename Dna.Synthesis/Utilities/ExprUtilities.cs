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
        public static HashSet<ExprId> GetUniqueVariables(Expr expr)
        {
            // TODO: Do not visit memory ptr expressions.
            var uniqueVariables = new HashSet<ExprId>();
            ExprVisitor.DfsVisit(expr, (Expr visitedExpr) =>
            {
                if (visitedExpr is not ExprId exprId)
                    return;

                uniqueVariables.Add(exprId);
            });

            return uniqueVariables;
        }

        /// <summary>
        /// Rebuild an expression via visiting sub-expressions, while replacing known expressions.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="replacements"></param>
        /// <returns></returns>
        public static Expr ExprReplace(Expr expr, Dictionary<Expr, Expr> replacements)
        {

        }
    }
}
