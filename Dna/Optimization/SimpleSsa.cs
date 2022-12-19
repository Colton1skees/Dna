using Dna.ControlFlow;
using Rivers.Analysis.Traversal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;
namespace Dna.Optimization
{
    public class SimpleSsa
    {
        private readonly ControlFlowGraph<AbstractInst> cfg;

        private readonly Dictionary<IOperand, HashSet<Block>> defs = new();

        private readonly HashSet<Block> sealedBlocks = new();

        public SimpleSsa(ControlFlowGraph<AbstractInst> cfg)
        {
            this.cfg = cfg;
        }

        public void Compute()
        {
            // Collect all variable definitions for each block.
            InitializeRoutineDefs();

            var traversal = new DepthFirstTraversal();
            var recorder = new TraversalOrderRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var nodes = recorder.TraversedNodes;

            foreach(var node in nodes)
            {
                var block = (Block)node;
            }
        }

        private void InitializeRoutineDefs()
        {
            // Order the basic blocks in a depth first manner.
            var traversal = new DepthFirstTraversal();
            var recorder = new TraversalOrderRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var nodes = recorder.TraversedNodes;

            foreach (var block in nodes.Cast<Block>())
            {
                InitializeBlockDefs(block);
            }

        }

        private void InitializeBlockDefs(Block block)
        {
            foreach (var inst in block.Instructions)
            {
                if (!inst.HasDestination)
                    continue;

                defs.TryAdd(inst.Dest, new HashSet<Block>());
                defs[inst.Dest].Add(block);
            }
        }

        private void InsertPhis()
        {

        }
    }
}
