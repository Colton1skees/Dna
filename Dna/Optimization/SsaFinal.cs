using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization
{
    public class SsaFinal
    {
        private readonly ControlFlowGraph<AbstractInst> cfg;

        private HashSet<Block> sealedBlocks = new();

        public SsaFinal(ControlFlowGraph<AbstractInst> cfg)
        {
            this.cfg = cfg;
        }

        private void Compute()
        {

        }

        private void SealBlock(Block block)
        {

        }
    }
}
