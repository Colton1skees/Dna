using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    /// <summary>
    /// The following algorithm is a reimplementation from the paper:
    /// - "A Comb for Decompiled C Code (2020)"
    /// - https://rev.ng/downloads/asiaccs-2020-paper.pdf
    /// </summary>
    public class ControlFlowGraphRestructurer<T>
    {
        private readonly ControlFlowGraph<T> graph;

        public ControlFlowGraphRestructurer(ControlFlowGraph<T> graph)
        {
            this.graph = graph;
        }

        public void Restructure()
        {
            // Create the initial set of SCS regions via identifying all back edges.
            var backEdges = GetScsRegions();

            // Refine the SCS regions.
            RefineScsRegions(backEdges);
        }


        private HashSet<BlockEdge<T>> GetScsRegions()
        {
            // Gets all outgoing edges where the target dominates its source.
            var scsRegions = BackEdgeAnalysis.GetBackEdges(graph).Cast<BlockEdge<T>>();

            // Cast the set of 
            return new HashSet<BlockEdge<T>>(scsRegions);
        }

        private void RefineScsRegions(HashSet<BlockEdge<T>> backEdges)
        {
            // Insert a dummy node for each retreating edge.
            // TODO: Add code to JMP to the correct destination.
            foreach(var backEdge in backEdges)
            {
                var target = backEdge.TargetBlock;
                var dummy = NodeInserter.InsertArtificalNode(graph);
                EdgeUpdater.ReplaceTarget(backEdge, dummy);
                EdgeUpdater.AddEdge(dummy, target);
            }

            // Recompute back edges.
            backEdges = GetScsRegions();

            // Assert that the source of each retreating edge is a dummy node.
            if (backEdges.Any(x => x.SourceBlock.Instructions.Any()))
                throw new InvalidOperationException("The source node of a retreating edge must now always be a dummy node.");

        }

        private void CreateMetaRegions(HashSet<BlockEdge<T>> backEdges)
        {

        }
    }
}
