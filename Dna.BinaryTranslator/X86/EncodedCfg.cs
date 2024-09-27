using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.X86
{
    public class EncodedCfg<T>
    {
        /// <summary>
        /// The source control flow graph.
        /// </summary>
        public ControlFlowGraph<T> Cfg { get; }

        /// <summary>
        /// A mapping between (inst address, inst byte encoding).
        /// </summary>
        public IReadOnlyDictionary<ulong, byte[]> EncodingAtMapping { get; }

        /// <summary>
        /// </summary>
        /// <param name="cfg">The source control flow graph.</param>
        /// <param name="encodingAtMapping">A mapping between (inst address, inst byte encoding).</param>
        public EncodedCfg(ControlFlowGraph<T> cfg, IReadOnlyDictionary<ulong, byte[]> encodingAtMapping)
        {
            Cfg = cfg;
            EncodingAtMapping = encodingAtMapping;
        }

        public byte[] GetInstructionEncodingAt(ulong address)
        {
            return EncodingAtMapping[address];
        }
    }
}
