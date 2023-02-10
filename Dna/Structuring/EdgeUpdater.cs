using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    public static class EdgeUpdater
    {
        public static BlockEdge<T> ReplaceTarget<T>(BlockEdge<T> edge, BasicBlock<T> newTarget)
        {
            // Get the source block.
            var srcBlock = edge.SourceBlock;

            // Remove the edge from {source} to {target}.
            srcBlock.OutgoingEdges.Remove(edge);

            // Add an edge {source} to {newTarget}.
            var newEdge = new BlockEdge<T>(srcBlock, newTarget);
            srcBlock.OutgoingEdges.Add(newEdge);
            return newEdge;
        }

        public static BlockEdge<T> ReplaceSource<T>(BlockEdge<T> edge, BasicBlock<T> newSource)
        {
            // Get the target block.
            var targetBlock = edge.TargetBlock;

            // Remove the edge from {target} to {source}.
            targetBlock.IncomingEdges.Remove(edge);

            // Add an edge from {newSource} to {target}.
            var newEdge = new BlockEdge<T>(newSource, targetBlock);
            targetBlock.IncomingEdges.Add(newEdge);
            return newEdge;
        }

        public static BlockEdge<T> AddEdge<T>(BasicBlock<T> src, BasicBlock<T> dst)
        {
            var edge = new BlockEdge<T>(src, dst);
            src.OutgoingEdges.Add(edge);
            return edge;
        }
    }
}
