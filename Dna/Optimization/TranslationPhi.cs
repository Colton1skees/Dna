using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Optimization
{
    public class TranslationPhi
    {
        public BasicBlock<AbstractInst> Block { get; }

        public bool IsUndef { get; }

        public HashSet<TranslationPhi> Sources { get; } = new();

        public HashSet<TranslationPhi> Users { get; } = new();

        public TranslationPhi(BasicBlock<AbstractInst> block, bool isUndef = false)
        {
            Block = block;
            IsUndef = isUndef;
        }
    }
}
