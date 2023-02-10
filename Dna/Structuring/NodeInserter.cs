using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    public class NodeInserter
    {
        public static BasicBlock<T> InsertArtificalNode<T>(ControlFlowGraph<T> graph)
        {
            // Allocate the block at an arbitrary address. TODO: Refactor.
            var artificialAddress = graph.GetBlocks().MaxBy(x => x.Address).Address + 0x1000;

            var block = graph.CreateBlock(artificialAddress);
            return block;
        }
    }
}
