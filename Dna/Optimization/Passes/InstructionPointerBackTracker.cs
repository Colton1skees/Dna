using Dna.ControlFlow;
using Dna.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch.X86;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization.Passes
{
    public class InstructionPointerBackTracker
    {
        public Tuple<IReadOnlyList<ulong>, IOperand?> BacktrackInstructionPointer(Block block)
        {
            // Construct ssa form for the basic block.
            var versionMapping = BlockSsaConstructor.ConstructSsa(block);

            // Backtrace the possible RIP values;
            var slice = Slice(block, versionMapping);

            // Execute out of ssa transformation.
            BlockSsaConstructor.DestructSsa(block);

            return slice;
        }

        private Tuple<IReadOnlyList<ulong>, IOperand?> Slice(Block block, Dictionary<IOperand, OrderedSet<SsaOperand>> versionMapping)
        {
            var output = new List<ulong>();

            // Get the last assignment to RIP.
            var ripDefinition = versionMapping[new RegisterOperand(X86Registers.Rip)].Last();

            // Iteratively backward track the RIP definition until one or two destinations are found.
            Queue<SsaOperand> worklist = new Queue<SsaOperand>();
            worklist.Enqueue(ripDefinition);

            // Helper function for following through chains of operands
            // & resolving them.
            var track = (IOperand operand) =>
            {
                if (operand is SsaOperand ssaOp)
                {
                    // If the operand is not an immediate, then we need to continue backwards
                    // tracking through definitions.
                    // Note: Immediate nodes are never placed inside of SsaOperands.
                    worklist.Enqueue(versionMapping[ssaOp.BaseOperand].Last());
                }

                else if (operand is ImmediateOperand immOp)
                {
                    if (output.Count <= 1)
                        output.Add(immOp.Value);
                    else
                        throw new InvalidOperationException("Found more than two possible outgoing destinations.");
                }

                else
                {
                    throw new InvalidOperationException($"Cannot process node type: {operand.GetType().Name}");
                }
            };

            IOperand? jccCond = null;
            while (worklist.Any())
            {
                // Pop an SSA definition from the worklist, then consider the current definition
                // as 'destroyed'.
                var definition = worklist.Dequeue();
                versionMapping[definition.BaseOperand].Remove(definition);

                // Attempt to track the value through a copy node.
                if (definition.Definition is InstCopy copyInst)
                {
                    track(copyInst.Op1);
                }

                // Attempt to track the value through a select node
                else if (definition.Definition is InstSelect selectInst)
                {
                    if (jccCond == null)
                        jccCond = selectInst.Op1;
                    else
                        throw new InvalidOperationException("Found multiple selecting contributing to the final RIP.");
                    track(selectInst.Op2);
                    track(selectInst.Op3);
                }

                // Otherwise, a more advanced analysis than basic copy propagation is needed to determine the RIP.
                else
                {
                    throw new InvalidOperationException($"Cannot slice RIP through instruction: {definition.Definition}");
                }
            }

            return new Tuple<IReadOnlyList<ulong>, IOperand?>(output, jccCond);
        }
    }
}
