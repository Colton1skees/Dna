using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Reconstruction
{
    public interface ICfgReconstructor
    {
        /// <summary>
        /// Attempts to reconstruct a control flow graph at the provided address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ControlFlowGraph<Instruction> ReconstructCfg(ulong address);
    }
}
