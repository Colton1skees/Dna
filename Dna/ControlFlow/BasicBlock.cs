using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class BasicBlock<T>
    {
        private List<T> instructions = new List<T>();

        /// <summary>
        /// Gets or sets the address of the basic block.
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// Gets the list of basic block instructions.
        /// </summary>
        public List<T> Instructions => instructions;

        /// <summary>
        /// Gets or sets the first basic block instruction.
        /// </summary>
        public T EntryInstruction
        {
            get => instructions.First();
            set
            {
                instructions.RemoveAt(0);
                instructions.Insert(0, value);
            }
        }

        /// <summary>
        /// Gets or sets the last basic block instruction.
        /// </summary>
        public T ExitInstruction
        {
            get => instructions.Last();
            set
            {
                instructions.RemoveAt(instructions.Count - 1);
                instructions.Add(value);
            }
        }

        public override string ToString()
        {
            return GraphFormatter.FormatBlock(this);
        }
    }
}
