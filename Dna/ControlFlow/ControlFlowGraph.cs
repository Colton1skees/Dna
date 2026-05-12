using Dna.Extensions;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class ControlFlowGraph<T> : Graph
    {
        public ulong StartAddress { get; }

        public ControlFlowGraph(ulong startAddress)
        {
            Name = startAddress.ToString("X");
            StartAddress = startAddress;
        }

        /// <summary>
        /// Creates a new basic block and adds it to the control flow graph.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public BasicBlock<T> CreateBlock(ulong address)
        {
            var block = new BasicBlock<T>(address);
            block.Address = address;
            block.UserData.Add(block.Address.ToString("X"), block);
            Nodes.Add(block);
            return block;
        }

        /// <summary>
        /// Creates a new basic block and adds it to the control flow graph.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public BasicBlock<T> TryCreateBlock(ulong address)
        {
            var name = address.ToString("X");
            if (Nodes.Contains(name))
                return (BasicBlock<T>)Nodes[name];

            var block = new BasicBlock<T>(address);
            block.Address = address;
            block.UserData.Add(name, block);
            Nodes.Add(block);
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

        public bool WhileEachBlockInReversePostOrder(BasicBlock<T> block, Func<BasicBlock<T>, bool> func)
        {
            return false;
        }

        public override string ToString()
        {
            return GraphFormatter.FormatGraph(this);
        }

        public static ControlFlowGraph<T> Clone(ControlFlowGraph<T> src)
        {
            // Create a new destination control flow graph.
            var dst = new ControlFlowGraph<T>(src.StartAddress);

            // Create a mapping of <src block, dst block>.
            var blockMapping = src.GetBlocks().ToDictionary(x => x, x => dst.CreateBlock(x.Address));

            foreach((BasicBlock<T> srcBlock, BasicBlock<T> dstBlock) in blockMapping)
            {
                // For each source outgoing edge, create a new outgoing edge which utilizes the corresponding basic blocks in the dst control flow graph.
                var clonedEdges = srcBlock.GetOutgoingEdges().Select(x => new BlockEdge<T>(blockMapping[x.SourceBlock], blockMapping[x.TargetBlock]));

                // Add all of the newly created outgoing edges.
                dstBlock.AddOutgoingEdges(clonedEdges);

                // Copy over the instructions.
                dstBlock.Instructions.AddRange(srcBlock.Instructions);
            }

            return dst;
        }
    }
}
