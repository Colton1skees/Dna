using Dna.Synthesis.Miasm;
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

        private List<List<long>> inputs;

        private Dictionary<string, List<Expr>> oracleMap;

        private readonly Regex regex = new Regex("^p[0-9]*");

        public SimplificationOracle(int variableCount, int sampleCount, string libraryPath)
        {
            this.variableCount = variableCount;
            this.sampleCount = sampleCount;
            this.libraryPath = libraryPath;
            inputs = InputSampling.GenInputs(variableCount, sampleCount);
            oracleMap = GenOracleMap();
        }

        private Dictionary<string, List<Expr>> GenOracleMap()
        {
            var output = new Dictionary<string, List<Expr>>();
            return output;
        }

        private ulong EvaluateExpression(Expr expr, List<ulong> inputs)
        {
            var replacements = new Dictionary<ExprId, ExprInt>();
            foreach(var variable in ExprUtilities.GetUniqueVariables(expr))
            {
                if (!regex.IsMatch(variable.Name))
                    continue;

                var index = Convert.ToInt32(variable.Name.Replace("p", ""));
                replacements[variable] = new ExprInt(inputs[index], variable.Size);

            }
        }

        private List<long> GetOutputs(Expr expr)
        {
            // TODO: Generate different expression outputs.
            return null;
        }
    }
}
