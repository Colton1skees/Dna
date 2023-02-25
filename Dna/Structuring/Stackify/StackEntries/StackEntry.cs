using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.StackEntries
{
    public abstract record StackEntry
    {
        public record DomSubtree(BasicBlock<AbstractInst> block) : StackEntry();
        public record EndDomSubtree() : StackEntry();
        public record NodeWithin(BasicBlock<AbstractInst> block, uint usize) : StackEntry();
        public record FinishLoop(BasicBlock<AbstractInst> block) : StackEntry();
        public record FinishBlock(BasicBlock<AbstractInst> block) : StackEntry();
        public record Else() : StackEntry();
        public record FinishIf(object cond) : StackEntry();
        public record DoBranch(BasicBlock<AbstractInst> block, BasicBlock<AbstractInst> target) : StackEntry();
    }
}
