using Dna.DataStructures;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    /// <summary>
    /// Class for identifying back edges in a control flow graph.
    /// </summary>
    public static class BackEdgeAnalysis
    {
        /// <inheritdoc cref="BackEdgeAnalysis.GetBackEdges(Graph, ImmutableDomTree)"/>
        public static HashSet<Edge> GetBackEdges<T>(ControlFlowGraph<T> cfg) => GetBackEdges(cfg, new ImmutableDomTree<T>(cfg));

        /// <summary>
        /// Gets all outgoing edges where the target dominates its source.
        /// </summary>
        /// <returns></returns>
        public static HashSet<Edge> GetBackEdges<T>(Graph cfg, ImmutableDomTree<T> domTree)
        {
            // Collect all outgoing edges where node {S} is jumping to node {T} which dominates {S}.
            var backEdges = cfg.Nodes
                .SelectMany(node => node.OutgoingEdges)
                .Where(outEdge => domTree.IsDominatedBy(outEdge.Source, outEdge.Target));

            // Return a set of all back edges.
            return new HashSet<Edge>(backEdges);
        }
    }
}
