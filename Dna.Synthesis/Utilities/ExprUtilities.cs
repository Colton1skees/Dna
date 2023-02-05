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
        public static HashSet<ExprId> GetUniqueVariables(MiasmExpr expr)
        {
            // TODO: Do not visit memory ptr expressions.
            var uniqueVariables = new HashSet<ExprId>();
            ExprVisitor.DfsVisit(expr, (MiasmExpr visitedExpr) =>
            {
                if (visitedExpr is not ExprId exprId)
                    return;

                uniqueVariables.Add(exprId);
            });

            return uniqueVariables;
        }

        public static int GetExpressionLength(MiasmExpr expr)
        {
            // Visit all nodes while incrementing a count per node.
            int count = 0;
            ExprVisitor.DfsVisit(expr, (MiasmExpr visited) =>
            {
                count++;
            });

            return count;
        }

        public static List<MiasmExpr> GetSubExpressions(MiasmExpr expr)
        {
            List<MiasmExpr> output = new List<MiasmExpr>();
            ExprVisitor.DfsVisit(expr, (MiasmExpr inputExpr) =>
            {
                output.Add(inputExpr);
            });

            output.Reverse();
            return output;
        }
    }
}
