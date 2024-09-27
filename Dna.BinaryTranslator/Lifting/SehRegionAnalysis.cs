using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Lifting
{
    /// <summary>
    /// Class for querying information about SEH regions within a control flow graph.
    /// </summary>
    public static class SehRegionAnalysis
    {
        /// <summary>
        /// Get all edges which branch from inside a TRY statement to a point outside of the TRY statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startBlock">The first(entry) basic block of the TRY statement.</param>
        /// <param name="regionBeginAddr">The begin address of the SEH TRY guarded region.</param>
        /// <param name="regionEndAddr">The end address of the SEH try guarded region.</param>
        /// <returns></returns>
        public static HashSet<BlockEdge<T>> GetExitingEdgesFromRegion<T>(BasicBlock<T> startBlock, ulong regionBeginAddr, ulong regionEndAddr)
        {
            // SEH regions can be viewed as SEME(single entry multi exit regions). Here we get all basic guarded basic blocks
            // which branch to an unguarded block.
            var worklist = new Queue<BasicBlock<T>>();
            var seen = new HashSet<BasicBlock<T>>();

            worklist.Enqueue(startBlock);
            var exitEdges = new HashSet<BlockEdge<T>>();
            while (worklist.Any())
            {
                var popped = worklist.Dequeue();
                seen.Add(popped);

                foreach (var outgoingEdge in popped.GetOutgoingEdges())
                {
                    // Get the target block.
                    var targetBlock = outgoingEdge.TargetBlock;

                    // Check if the target block is within the guarded range.
                    bool isInsideRange = targetBlock.Address >= regionBeginAddr && targetBlock.Address < regionEndAddr;

                    // If it's outside the range then it's considered an exit.
                    if (!isInsideRange)
                    {
                        exitEdges.Add(outgoingEdge);
                        continue;
                    }

                    // Otherwise it's a branch to some other area of the guarded region. 
                    // Now we need to add it to the visiting set.
                    if (!seen.Contains(targetBlock))
                        worklist.Enqueue(targetBlock);
                }
            }

            return exitEdges;
        }
    }
}
