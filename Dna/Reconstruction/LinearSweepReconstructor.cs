using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Reconstruction
{
    public class LinearSweepReconstructor : ICfgReconstructor
    {
        private readonly IDna dna;

        public LinearSweepReconstructor(IDna dna)
        {
            this.dna = dna;
        }

        public ControlFlowGraph<Instruction> ReconstructCfg(ulong address)
        {
            throw new NotImplementedException();
        }
    }
}
