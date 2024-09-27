using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X86Block = Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>;

namespace Dna.BinaryTranslator.Safe
{
    public delegate bool dgShouldNotSplit(X86Block block, int instIndex);

    public static class X86CfgSplitter
    {
        public static (IReadOnlySet<X86Block> splitTargets, HashSet<ulong> fallthroughFromIps) SplitBlocksAtSeh(ControlFlowGraph<Instruction> cfg, IReadOnlySet<ulong> sehPoints)
        {
            dgShouldNotSplit shouldSplit = (X86Block block, int instIndex) =>
            {
                var inst = block.Instructions[instIndex];
                var hasNext = instIndex < block.Instructions.Count - 1;
                if (!hasNext)
                    return false;

                var next = block.Instructions[instIndex + 1];
                if (sehPoints.Contains(next.IP))
                    return true;
                return false;
            };

            return SplitBlocks(cfg, shouldSplit);
        }

        public static (IReadOnlySet<X86Block> splitTargets, HashSet<ulong> fallthroughFromIps) SplitBlocksAtCalls(ControlFlowGraph<Instruction> cfg)
        {
            dgShouldNotSplit shouldSplit = (X86Block block, int instIndex) =>
            {
                var inst = block.Instructions[instIndex];
                var hasNext = instIndex < block.Instructions.Count - 1;

                if (inst.Mnemonic == Mnemonic.Call && hasNext)
                    return true;

                return false;
            };

            return SplitBlocks(cfg, shouldSplit);
        }


        /// <summary>
        /// At each `CALL` instruction within a basic block, move all instructions following the 'CALL' into it's own basic block.
        /// When a block is split, it's outgoing edges are forwarded to the split block.
        /// What this effectively does is introduce a separate basic block for any sequence of instructions starting with a 'CALL', allowing it to be
        /// directly jumped to later on.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns>A set of newly introduced "split" blocks. (i.e. the set of all newly introduced basic blocks which start with a 'CALL')</returns>
        public static (IReadOnlySet<X86Block> splitTargets, HashSet<ulong> fallthroughFromIps) SplitBlocks(ControlFlowGraph<Instruction> cfg, dgShouldNotSplit shouldSplit)
        {
            // Create a list of current basic blocks. Note that we use clone to create a copy of the list.
            var blocks = cfg.GetBlocks().ToList();

            var worklist = new Queue<X86Block>(blocks);
            Dictionary<ulong, X86Block> addressMapping = new(blocks.ToDictionary(x => x.Address, x => x));
            HashSet<X86Block> splitTargets = new();
            var fallthroughFromIps = new HashSet<ulong>();

            while (worklist.Any())
            {
                // Pop any basic block.
                var block = worklist.Dequeue();

                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    // Get the current instruction.
                    var instruction = block.Instructions.ElementAt(i);

                    if (!shouldSplit(block, i))
                        continue;

                    fallthroughFromIps.Add(instruction.IP);
                    
                    var splitResult = SplitAt(cfg, block, i, addressMapping.AsReadOnly());
                    splitTargets.Add(splitResult.targetBlock);
                    if (splitResult.isNew)
                    {
                        // If this is a newly created split block, then we need to queue it up for processing.
                        // This is because the remaining instructions after a `call` are copied into this block.
                        worklist.Enqueue(splitResult.targetBlock);

                        // Now we also need to keep track of all newly inserted split blocks, 
                        // since the vm entry point now needs to a virtual dispatcher to select
                        addressMapping.Add(splitResult.targetBlock.Address, splitResult.targetBlock);
                    }

                    break;
                }
            }

            return (splitTargets, fallthroughFromIps);
        }

        private static (bool isNew, X86Block targetBlock) SplitAt(ControlFlowGraph<Instruction> cfg, X86Block srcBlock, int index, IReadOnlyDictionary<ulong, X86Block> addressMapping)
        {
            // Compute the address of the instruction following the `call`. 
            var newAddress = srcBlock.Instructions.ElementAt(index + 1).IP;

            // If we haven't already created a split block at this new address point, we need to do so.
            bool isNew = !addressMapping.TryGetValue(newAddress, out X86Block splitBlock);
            if (isNew)
            {
                // Compute the set of instructions which are removed out of source block and emplaced into the new split block target.
                var removedInstructions = srcBlock.Instructions.Skip(index + 1);

                // Copy the instructions into the new block.
                splitBlock = cfg.CreateBlock(newAddress);
                splitBlock.Instructions.AddRange(removedInstructions);
            }

            // Now, we have two instruction streams: The split block(which is a subset of the srcBlock, starting from the call we were provided), and the source block.
            // We need to remove all splitblock instructions from the source block.
            srcBlock.Instructions = srcBlock.Instructions.Take(index + 1).ToList();

            // Now since the exit block has changed, we need to update the exit edges of the source block to point only
            // to the new split block.
            var oldEdges = srcBlock.GetOutgoingEdges().ToList();
            srcBlock.OutgoingEdges.Clear();
            srcBlock.OutgoingEdges.Add(new BlockEdge<Instruction>(srcBlock, splitBlock));

            // Lastly we need to copy the original outgoing edges to the new src block.
            splitBlock.OutgoingEdges.AddRange(oldEdges.Select(x => new BlockEdge<Instruction>(splitBlock, x.TargetBlock)));

            // Finally we can return knowing that this basic block has been split successfully.
            return (isNew, splitBlock);
        }
    }
}
