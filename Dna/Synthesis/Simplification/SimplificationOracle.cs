using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Simplification
{
    public class SimplificationOracle
    {
        private readonly int numVariables;

        private readonly int numSamples;

        private readonly string libraryPath;

        public SimplificationOracle(int numVariables, int numSamples, string libraryPath)
        {
            this.numVariables = numVariables;
            this.numSamples = numSamples;
            this.libraryPath = libraryPath;
        }


    }
}
