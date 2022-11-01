using Dna.Extensions;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class ControlFlowGraph<T> : Graph
    {
        public ulong StartAddress { get; }

        public ControlFlowGraph(ulong startAddress)
        {
            StartAddress = startAddress;
        }

        /// <summary>
        /// Creates a new basic block and adds it to the control flow graph.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public BasicBlock<T> CreateBlock(ulong address)
        {
            var node = new Node(address.ToString("X"));
            var block = new BasicBlock<T>();
            block.Node = node;
            return block;
        }

        public IEnumerable<BasicBlock<T>> GetBlocks()
        {
            return Nodes.Select(x => x.GetBlock<T>());
        }

        public IEnumerable<T> GetInstructions()
        {
            return Nodes.SelectMany(x => x.GetBlock<T>().Instructions);
        }

        public override string ToString()
        {
            return GraphFormatter.FormatGraph(this);
        }
    }
}
