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
        public IBinary Binary { get; }

        public BinaryDisassembler BinaryDisassembler { get; }

        public ICfgReconstructor LinearSweep { get; }

        public ICfgReconstructor RecursiveDescent { get; }
    }
}
