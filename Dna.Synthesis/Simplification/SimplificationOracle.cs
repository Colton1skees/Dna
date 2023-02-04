using Dna.Synthesis.Miasm;
using Dna.Synthesis.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // TODO: Implement oracle map generation.
            throw new NotImplementedException();
        }

        private List<long> GetOutputs(Expr expr)
        {
            // TODO: Generate different expression outputs.
            return null;
        }
    }
}
