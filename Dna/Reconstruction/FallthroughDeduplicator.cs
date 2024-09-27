using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X86Block = Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>;

namespace Dna.Reconstruction
{
    public static class FallthroughDeduplicator
    {
        /// <summary>
        /// The recursive descent algorithm inside of Dna has no concept of fallthrough edges.
        /// This can resolve in large amounts of code duplication, as fallthrough edges are always duplicated.
        /// To correct this, we provide this class which can undo all duplication via inserting fallthrough edges.
        /// </summary>
        /// <param name="cfg">The source control flow graph.</param>
        /// <param name="additionalFallthroughTargets">An optional set of block start addresses that will be turned into fallthrough targets if they were duplicated.</param>
        /// <returns></returns>
        public static (ControlFlowGraph<Instruction> newCfg, HashSet<ulong> fallthroughFromIps) DeduplicateFallthroughEdges(ControlFlowGraph<Instruction> cfg, IEnumerable<ulong> additionalFallthroughTargets)
        {
            HashSet<ulong> fallthroughFromIps = new();

            // Create a list of current basic blocks. Note that we use clone to create a copy of the list.
            var blocks = cfg.GetBlocks().ToList();
            Dictionary<ulong, X86Block> addressMapping = new(blocks.ToDictionary(x => x.Address, x => x));
            Dictionary<ulong, X86Block> newAddressMapping = new();

            // Create a new cfg and create empty blocks at each known block start address.
            var newCfg = new ControlFlowGraph<Instruction>(cfg.StartAddress);
            foreach (var block in blocks)
                newAddressMapping.Add(block.Address, newCfg.CreateBlock(block.Address));
            // Append optionally added fallthrough targets into the block set.
            foreach (var blockAddr in additionalFallthroughTargets)
                newAddressMapping[blockAddr] = newCfg.TryCreateBlock(blockAddr);
            
            // Clone each basic block while deduplicating fallthrough edges.
            foreach(var block in blocks)
            {
                // Get the new block and append the first instruction.
                var newBlock = newAddressMapping[block.Address];
                newBlock.Instructions.Add(block.Instructions.First());

                // Copy over all other instructions.
                for(int i = 1; i <  block.Instructions.Count; i++)
                {
                    var inst = block.Instructions[i];
                    // If there is a basic block that starts with this instruction,
                    // then we stop cloning instructions. This creates what is called a fallthrough edge.
                    if (newAddressMapping.ContainsKey(inst.IP))
                    {
                        // Insert a fallthrough edge from the current basic block to the fallthrough destination.
                        newBlock.AddOutgoingEdge(new BlockEdge<Instruction>(newBlock, newAddressMapping[inst.IP]));
                        // Store the fact that the instruction preceding this instruction is guaranteed to fall through to a basic block,
                        // without using a terminator instruction.
                        fallthroughFromIps.Add(block.Instructions[i - 1].IP);
                        break;
                    }

                    // Otherwise we copy the instruction into the new basic block.
                    newBlock.Instructions.Add(inst);
                }
            }

            // Clone all previously existing basic block edges.
            foreach(var (address, block) in newAddressMapping)
            {
                // Skip newly created fallthrough edges, because the old cfg edge set is not accurate.
                var exitInst = block.ExitInstruction;
                if (fallthroughFromIps.Contains(exitInst.IP))
                    continue;

                var srcBlock = addressMapping[address];
                var oldEdges = srcBlock.GetOutgoingEdges().ToList();
                block.AddOutgoingEdges(oldEdges.Select(x => new BlockEdge<Instruction>(newAddressMapping[x.SourceBlock.Address], newAddressMapping[x.TargetBlock.Address])));
            }

            return (newCfg, fallthroughFromIps);
        }
    }
}
