using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extraction
{
    /// <summary>
    /// Provides functionality for extracting a compiled virtualized method from a given binary.
    /// </summary>
    public interface IFunctionExtractor
    {
        /// <summary>
        /// Attempts to extract a function at the provided RVA.
        /// </summary>
        /// <param name="start">The start RVA of the function.</param>
        /// <param name="end">The optional end RVA of the function.</param>
        public IExtractedFunction ExtractFunction(ulong start, ulong? end = null);
    }
}
