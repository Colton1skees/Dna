using Dna.Extensions;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class BasicBlock<T> : Node
    {
        /// <summary>
        /// Gets or sets the address of the basic block.
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// Gets a collection of basic block instructions.
        /// </summary>
        public List<T> Instructions { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the first basic block instruction.
        /// </summary>
        public T EntryInstruction
        {
            get => Instructions.First();
            set
            {
                Instructions.RemoveAt(0);
                Instructions.Insert(0, value);
            }
        }

        /// <summary>
        /// Gets or sets the last basic block instruction.
        /// </summary>
        public T ExitInstruction
        {
            get => Instructions.Last();
            set
            {
                Instructions.RemoveAt(Instructions.Count - 1);
                Instructions.Add(value);
            }
        }

        public BasicBlock(ulong address) : base(address.ToString("X"))
        {

        }

        public IEnumerable<BlockEdge<T>> GetIncomingEdges() => 
            IncomingEdges.Select(x => x.ToBlockEdge<T>());

        public IEnumerable<BlockEdge<T>> GetOutgoingEdges() => 
            OutgoingEdges.Select(x => x.ToBlockEdge<T>());

        public void AddIncomingEdge(BlockEdge<T> edge) => IncomingEdges.Add(edge);

        public void AddOutgoingEdge(BlockEdge<T> edge) => OutgoingEdges.Add(edge);

        public void AddIncomingEdges(IEnumerable<BlockEdge<T>> edges)
        {
            foreach (var edge in edges)
                IncomingEdges.Add(edge);
        }

        public void AddOutgoingEdges(IEnumerable<BlockEdge<T>> edges)
        {
            foreach (var edge in edges)
                OutgoingEdges.Add(edge);
        }

        public override string ToString() => GraphFormatter.FormatBlock(this);
    }
}
