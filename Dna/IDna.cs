using Dna.Binary;
using Dna.Reconstruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna
{
    public interface IDna
    {
        /// <summary>
        /// Class for parsing and manipulating binaries.
        /// </summary>
        public IBinary Binary { get; }

        /// <summary>
        /// Class for disassembling instructions.
        /// </summary>
        public BinaryDisassembler BinaryDisassembler { get; }

        /// <summary>
        /// Class for performing linear sweep disassembly.
        /// </summary>
        public ICfgReconstructor LinearSweep { get; }

        /// <summary>
        /// Class for performing recursive descent disassembly.
        /// </summary>
        public ICfgReconstructor RecursiveDescent { get; }
    }
}
