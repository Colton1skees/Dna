using Dna.Binary;
using Dna.Reconstruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna
{
    public class Dna : IDna
    {
        /// <summary>
        /// <see cref="IDna.Binary"/>
        /// </summary>
        public IBinary Binary { get; }

        /// <summary>
        /// <see cref="IDna.BinaryDisassembler"/>
        /// </summary>
        public BinaryDisassembler BinaryDisassembler { get; }

        /// <summary>
        /// <see cref="IDna.LinearSweep"/>
        /// </summary>
        public ICfgReconstructor LinearSweep { get; }

        /// <summary>
        /// <see cref="IDna.RecursiveDescent"/>
        /// </summary>
        public ICfgReconstructor RecursiveDescent { get; }

        public Dna(IBinary binary)
        {
            Binary = binary;
            BinaryDisassembler = new BinaryDisassembler(binary);
            LinearSweep = new LinearSweepReconstructor(this);
            RecursiveDescent = new RecursiveDescentReconstructor(this);
        }
    }
}
