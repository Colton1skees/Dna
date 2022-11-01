using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extraction
{
    /// <summary>
    /// Represents an extracted virtualized function from a compiled binary.
    /// </summary>
    public interface IExtractedFunction
    {
        /// <summary>
        /// Gets the control flow graph of the extracted function.
        /// </summary>
        public ControlFlowGraph<Instruction> Graph { get; }

        /// <summary>
        /// Gets the graphs of all functions which are called by the extracted function.
        /// Note: The callee list is comprehensive, as a virtualized function will never emit any form of indirect branching.
        /// </summary>
        public IEnumerable<IExtractedFunction> Callees { get; }
    }
}
