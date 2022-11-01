using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class BasicBlock<T>
    {
        /// <summary>
        /// Gets the parent graph containing the node.
        /// </summary>
        public ControlFlowGraph<T> ParentGraph => (ControlFlowGraph<T>)Node.ParentGraph;

        /// <summary>
        /// Gets or sets the node containing the basic block.
        /// </summary>
        public Node Node { get; internal set; }

        /// <summary>
        /// Gets or sets the address of the basic block.
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// Gets a collection of basic block instructions.
        /// </summary>
        public List<T> Instructions { get; } = new List<T>();

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

        public BasicBlock()
        {

        }

        public override string ToString()
        {
            return GraphFormatter.FormatBlock(this);
        }
    }
}
