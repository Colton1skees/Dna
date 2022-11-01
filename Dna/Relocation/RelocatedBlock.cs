using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Relocation
{
    /// <summary>
    /// Represents a basic block which has been allocated at an arbitrary address.
    /// </summary>
    public class RelocatedBlock
    {
        public BasicBlock<Instruction> InputBlock { get; }

        public ulong OriginalRip { get; }

        public ulong RelocatedRip { get; set; }

        public int RelocatedSize { get; }

        public List<Instruction> RelocatedInstructions { get; } = new List<Instruction>();

        public bool HasBeenRewritten { get; set; }

        public RelocatedBlock(BasicBlock<Instruction> inputBlock, ulong originalRip, ulong relocatedRip, int relocatedSize)
        {
            InputBlock = inputBlock;
            OriginalRip = originalRip;
            RelocatedRip = relocatedRip;
            RelocatedSize = relocatedSize;
        }
    }
}
