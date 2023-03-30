using Dna.ControlFlow.Extensions;
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
    /// Class for querying general information about loops within a control flow graph.
    /// Note: Only reducible flow graphs are supported.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LoopAnalysis<T>
    {
        // The source control flow graph.
        private readonly ControlFlowGraph<T> cfg;

        // The immutable dominator tree.
        private readonly ImmutableDomTree<T> domTree;

        // A set of all retreating edges within the control flow graph.
        private readonly HashSet<Edge> retreatingEdges;

        // A set of all loop headers within the control flow graph.
        public readonly HashSet<Node> loopHeaders;

        // A mapping of <loop header, loop exit>.
        // Note: This assumes only SESE regions exit.
        public readonly Dictionary<Node, Node> loopExitToHeaderMapping = new();

        public readonly Dictionary<Node, Node> loopHeaderToExitMapping = new();

        public readonly HashSet<Node> loopExits = new();

        public LoopAnalysis(ControlFlowGraph<T> cfg) : this(cfg, new ImmutableDomTree<T>(cfg))
        {
        }

        public LoopAnalysis(ControlFlowGraph<T> cfg, ImmutableDomTree<T> domTree)
        {
            this.cfg = cfg;
            this.domTree = domTree;

            // Get a list of all back edges.
            retreatingEdges = BackEdgeAnalysis.GetBackEdges(cfg);

            // Use the back edges to identify all loop headers.
            loopHeaders = retreatingEdges.Select(x => x.Target).ToHashSet();

            foreach(var header in loopHeaders)
            {
                var exit = GetLoopExitNode(header);
                loopHeaderToExitMapping.Add(header, exit);
                loopExitToHeaderMapping.TryAdd(exit, header);
            }
        }

        private Node GetLoopExitNode(Node loopHeader)
        {
            // Get all nodes which are dominated by the loop header.
            var dominees = domTree.GetNodeDominees(loopHeader);

            // Get all unique outgoing destinations.
            var outgoingDestinations = new OrderedSet<Node>();
            foreach(var dominee in dominees)
            {
                outgoingDestinations.AddRange(dominee.OutgoingEdges.Select(x => x.Target));
            }

            var loopExit = outgoingDestinations.Where(x => !domTree.IsDominatedBy(x, loopHeader)).Single();

            var exit = loopExit.IncomingEdges.Select(x => x.Source).Single(x => domTree.IsDominatedBy(x, loopHeader));
            return exit;
        }
    }
}
