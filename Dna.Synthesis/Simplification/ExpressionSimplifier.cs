using Dna.Synthesis.Miasm;
using Dna.Synthesis.Utilities;
using Dna.Synthesis.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dna.Synthesis.Simplification
{
    public class ExpressionSimplifier
    {
        private readonly SimplificationOracle oracle;

        private readonly bool enforceEquivalence;

        private readonly int solverTimeout;

        private readonly Regex regex = new Regex("^p[0-9]*");

        private const string globalVariablePrefix = "global_reg";

        public ExpressionSimplifier(SimplificationOracle oracle, bool enforceEquivalence, int solverTimeout)
        {
            this.oracle = oracle;
            this.enforceEquivalence = enforceEquivalence;
            this.solverTimeout = solverTimeout;
        }

        public MiasmExpr Simplify(MiasmExpr expr)
        {
            // Traditionally, miasm uses some sort of transformation to
            // increase the expression depth(i.e. unroll a + b + c + d)
            // into (((a + b) + c)) + d.
            // However, we omit this step for now.
            var globalUnificationDict = new Dictionary<MiasmExpr, MiasmExpr>();

            var globalCtr = 0;

            while(true)
            {
                // Use the 'replace' utility as a clone function, via replacing zero nodes.
                var before = ExprVisitor.DfsReplace(expr, (MiasmExpr input) => null);

                foreach(var subtree in ExprUtilities.GetSubExpressions(expr))
                {
                    // Skip subtree if possible)
                    if (ShouldSkipSubtree(subtree))
                        continue;

                    // Build a unification dict.
                    var unificationDict = UnificationUtility.GetUnificationDict(subtree);

                    // Determine the subtree's equivalence class.
                    var equivClass = GetEquivalenceClass(ExprVisitor.DfsReplace(subtree, (MiasmExpr dfsExpr) =>
                    {
                        return unificationDict.ContainsKey(dfsExpr) ? unificationDict[dfsExpr] : null;
                    }));

                    // If the equivalence class is contained within the pre-computed oracle:
                    if(oracle.OracleMap.ContainsKey(equivClass))
                    {
                        // Check if there is a simpler subtree in the equivalence class.
                        var simplificationResult = FindSuitableSimplificationCandidate(equivClass, subtree, unificationDict);

                        if (!simplificationResult.Item1)
                            continue;

                        var globalVariable = GetGlobalVariableReplacement(globalCtr, subtree.Size);
                        globalCtr += 1;

                        globalUnificationDict[globalVariable] = simplificationResult.Item2;

                        // Replace the original subtree with global placeholder variable.
                        expr = ExprVisitor.DfsReplace(expr, (MiasmExpr dfsVisited) =>
                        {
                            return dfsVisited == subtree ? globalVariable : null;
                        });

                        break;
                    }
                }

                // Check if fixpoint is reached.
                if (before.Equals(expr))
                    break;
            }

            // Replace global placeholder variables with simplified subtrees in ast.
            expr = ReverseGlobalUnification(expr, globalUnificationDict);

            return expr;
        }

        private string GetEquivalenceClass(MiasmExpr expr)
        {
            // Get a list of expression outputs, using a list of inputs from the oracle.
            var outputs = oracle.GetOutputs(expr);

            // Get the expression equivalence class.
            var equivClass = oracle.GetExpressionEquivalenceClass(expr, outputs);

            // If all outputs evaluate to the same constant, add / replace
            // the equivalence class with a constant.
            if(new HashSet<ulong>(outputs).Count == 1)
            {
                oracle.OracleMap[equivClass] = new List<MiasmExpr>()
                {
                    new ExprInt(outputs[0], expr.Size)
                };
            }

            return equivClass;
        }

        private bool ShouldSkipSubtree(MiasmExpr expr)
        {
            return expr is ExprId || expr is ExprInt;
        }

        private Tuple<bool, MiasmExpr> FindSuitableSimplificationCandidate(string equivClass, MiasmExpr expr, Dictionary<MiasmExpr, MiasmExpr> unificationDict)
        {
            foreach(var candidate in oracle.OracleMap[equivClass])
            {
                // Reverse unification of simplification candidate.
                var simplified = UnificationUtility.ReverseUnification(candidate, unificationDict);

                // Skip simplification if necessary.
                if (!GetIsSuitableSimplificationCandidate(expr, simplified))
                    continue;

                return new Tuple<bool, MiasmExpr>(true, simplified);
            }

            return new Tuple<bool, MiasmExpr>(false, expr);
        }

        private bool GetIsSuitableSimplificationCandidate(MiasmExpr expr, MiasmExpr simplified)
        {
            // Get all unique variables of the simplified expression.
            var uniqueVariables = ExprUtilities.GetUniqueVariables(simplified);
            if(uniqueVariables.Any(x => regex.IsMatch(x.Name)))
            {
                throw new Exception("Incorrect variable replacement.");
            }

            // Skip if the original is smaller than the simplified expression.
            if (ExprUtilities.GetExpressionLength(expr) <= ExprUtilities.GetExpressionLength(simplified))
                return false;

            // Skip if they are the same normalized expression.
            // Note: Msynth applies some simplification steps here that we are missing.
            // This is a potential source of unwanted behavior.
            if (simplified is not ExprInt && expr == simplified)
                return false;

            // TODO: Check SMT solver equivalence.
            return true;
        }

        private MiasmExpr GetGlobalVariableReplacement(int index, uint size)
        {
            return new ExprId($"{globalVariablePrefix}{index}", size);
        }

        private MiasmExpr ReverseGlobalUnification(MiasmExpr expr, Dictionary<MiasmExpr, MiasmExpr> unificationDict)
        {
            var uniqueVariables = ExprUtilities.GetUniqueVariables(expr);

            while(ExprUtilities.GetUniqueVariables(expr).Any(x => x.Name.StartsWith(globalVariablePrefix)))
            {
                expr = ExprVisitor.DfsReplace(expr, (MiasmExpr x) => 
                {
                    return unificationDict.ContainsKey(x) ? unificationDict[x] : null;
                });
            }

            return expr;
        }
    }
}
