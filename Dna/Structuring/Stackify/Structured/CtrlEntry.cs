using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.Structured
{
    public enum CtrlEntryType
    {
        Block,
        Loop,
        IfThenElse,
    }

    public abstract record CtrlEntry
    {
        public record CtrlEntryBlock(BasicBlock<AbstractInst> outBlock) : CtrlEntry();
        public record CtrlEntryLoop(BasicBlock<AbstractInst> header) : CtrlEntry();
        public record CtrlEntryIfThenElse() : CtrlEntry();

        public BasicBlock<AbstractInst> Label()
        {
            return this switch
            {
                CtrlEntryBlock ctrl => ctrl.outBlock,
                CtrlEntryLoop ctrl => ctrl.header,
                CtrlEntryIfThenElse ctrl => null,
                _ => throw new InvalidOperationException($"Cannot get label for {this}"),
            };
        }
    }
}
