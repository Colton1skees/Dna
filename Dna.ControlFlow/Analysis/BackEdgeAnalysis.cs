using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    public static class BackEdgeAnalysis
    {
        /// <summary>
        /// Gets all outgoing edges where the target dominates its source.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="dominatorTree"></param>
        /// <returns></returns>
        public static HashSet<Edge> GetBackEdges(Graph graph, IReadOnlyDictionary<Node, HashSet<Node>> dominatorTree)
        {
            // Collect all outgoing edges where node {S} is jumping to node {T} which dominates {S}.
            var backEdges = graph.Nodes
                .SelectMany(node => node.OutgoingEdges)
                .Where(outEdge => dominatorTree[outEdge.Source].Contains(outEdge.Target));

            // Return a set of all back edges.
            return new HashSet<Edge>(backEdges);
        }
    }
}
