using Dna.Synthesis.Miasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Utilities
{
    public static class UnificationUtility
    {
        public static Dictionary<MiasmExpr, MiasmExpr> GetUnificationDict(MiasmExpr expr)
        {
            var candidates = GetUnificationCandidates(expr);
         
            var unification = new Dictionary<MiasmExpr, MiasmExpr>();
            int i = 0;
            foreach(var candidate in candidates)
            {
                unification.Add(candidate, new ExprId($"p{i}", candidate.Size));
                i++;
            }

            return unification;
        }

        public static List<MiasmExpr> GetUnificationCandidates(MiasmExpr expr)
        {
            var results = new HashSet<MiasmExpr>();

            var addToSet = (MiasmExpr input) =>
            {
                if(input is ExprMem || input is ExprId)
                    results.Add(input);
            };

            ExprVisitor.DfsVisit(expr, addToSet);


            // To provide deterministic behavior, the list is sorted.
            return results
                .OrderBy(x => ExpressionFormatter.FormatExpression(x))
                .ToList();
        }

        public static MiasmExpr ReverseUnification(MiasmExpr expr, Dictionary<MiasmExpr, MiasmExpr> unification)
        {
            var inverted = InvertDictionary(unification);

            return ExprVisitor.DfsReplace(expr, (MiasmExpr dfsExpr) =>
            {
                return inverted.ContainsKey(dfsExpr) ? inverted[dfsExpr] : null;
            });
        }

        public static Dictionary<T, T> InvertDictionary<T>(Dictionary<T, T> dict)
        {
            var output = new Dictionary<T, T>();
            foreach(var pair in dict)
            {
                output[pair.Value] = pair.Key;
            }

            return output;
        }
    }
}
