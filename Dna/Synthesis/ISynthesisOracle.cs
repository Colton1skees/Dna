using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Synthesis
{
    /// <summary>
    /// Class for computing synthesis oracles.
    /// </summary>
    public interface ISynthesisOracle
    {
        public IEnumerable<OracleExpression> GetOracleExpressions();
    }
}
