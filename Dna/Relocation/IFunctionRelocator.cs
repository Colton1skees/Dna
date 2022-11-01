using Dna.ControlFlow;
using Dna.Extraction;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Reinsertion
{
    /// <summary>
    /// Class for relocating control flow graphs.
    /// </summary>
    public interface IFunctionRelocator
    {
        /// <summary>
        /// Attempts to relocate a control flow graph to an arbitrary address.
        /// </summary>
        /// <param name="function">The provided graph.</param>
        /// <param name="outputAddress">The address to relocate to.</param>
        /// <returns>A collection of the compiled graph bytes. </returns>
        public byte[] RelocateFunction(IExtractedFunction function, ulong relocRip, out ulong endRip);
    }
}
