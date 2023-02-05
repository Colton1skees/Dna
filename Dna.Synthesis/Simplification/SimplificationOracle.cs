using Dna.Synthesis.Evaluation;
using Dna.Synthesis.Miasm;
using Dna.Synthesis.Parsing;
using Dna.Synthesis.Utilities;
using Dna.Synthesis.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dna.Synthesis.Simplification
{
    public class SimplificationOracle
    {
        private readonly int variableCount;

        private readonly int sampleCount;

        private readonly string libraryPath;

        private List<List<ulong>> inputs;

        private readonly Regex regex = new Regex("^p[0-9]*");

        internal readonly Z3Translator translator = new Z3Translator(new Microsoft.Z3.Context());

        public Dictionary<string, List<MiasmExpr>> OracleMap { get; }

        public SimplificationOracle(int variableCount, int sampleCount, string libraryPath)
        {
            this.variableCount = variableCount;
            this.sampleCount = sampleCount;
            this.libraryPath = libraryPath;
            inputs = InputSampling.GenInputs(variableCount, sampleCount);
            OracleMap = GenOracleMap(libraryPath);
        }

        private Dictionary<string, List<MiasmExpr>> GenOracleMap(string libraryPath)
        {
            var oracleMapTmp = new Dictionary<string, HashSet<MiasmExpr>>();
            var oracleMap = new Dictionary<string, List<MiasmExpr>>();

            var exprStrs = File.ReadAllLines(libraryPath);
            foreach(var str in exprStrs)
            {
                // Parse the string into a miasm expression.
                var expr = ExpressionDatabaseParser.ParseExpression(str);

                // Do not add integers to the oracle.
                if (expr is ExprInt)
                    continue;
                
                // Get the expression equivalence class.
                var equivClass = GetExpressionEquivalenceClass(expr);

                oracleMapTmp.TryAdd(equivClass, new HashSet<MiasmExpr>());
                oracleMapTmp[equivClass].Add(expr);
            }

            foreach(var equivClass in oracleMapTmp.Keys)
            {
                var exprList = oracleMapTmp[equivClass].OrderBy(x => ExprUtilities.GetExpressionLength(x));
                oracleMap[equivClass] = exprList.ToList();
            }

            return oracleMap;
        }

        public string GetExpressionEquivalenceClass(MiasmExpr expr)
        {
            // Calculate output behavior.
            var outputs = GetOutputs(expr);

            return GetExpressionEquivalenceClass(expr, outputs);
        }

        public string GetExpressionEquivalenceClass(MiasmExpr expr, List<ulong> outputs)
        {
            // Use the size of the expression as an identifier.
            var identifier = expr.Size.ToString();

            // Compute a SHA1 of the expression, represented as a string.
            return StringHasher.Hash(identifier + String.Join(", ", outputs));
        }

        public List<ulong> GetOutputs(MiasmExpr expr)
        {
            var outputs = inputs.Select(x => EvaluateExpression(expr, x));
            return outputs.ToList();
        }

        /// <summary>
        /// Evaluates an expression with a set of concrete inputs.
        /// </summary>
        private ulong EvaluateExpression(MiasmExpr expr, List<ulong> inputs)
        {
            // Create a mapping of <variable, constant> to evaluate the AST with.
            var replacements = new Dictionary<MiasmExpr, ExprInt>();
            foreach(var variable in ExprUtilities.GetUniqueVariables(expr))
            {
                if (!regex.IsMatch(variable.Name))
                    continue;

                var index = Convert.ToInt32(variable.Name.Replace("p", ""));
                replacements[variable] = new ExprInt(inputs[index], variable.Size);
            }

            // Clone the AST while replacing variable nodes with constants.
            var constantAst = ExprVisitor.DfsReplace(expr, (MiasmExpr input) =>
            {
                return replacements.ContainsKey(input) ? replacements[input] : null;
            });

            // Evaluate the expression to a single immediate value.
            // Note: If the expression does not evaluate to a constant, then
            // an exception is thrown.
            var result = ExpressionEvaluator.EvaluateExpression(constantAst);

            var result4 = translator.EvaluateExpression(constantAst);

            return result;
        }
    }
}
