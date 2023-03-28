using Dna.DataStructures;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    public class ImmutableDomTree<T>
    {
        // The source control flow graph.
        private readonly ControlFlowGraph<T> cfg;

        // Given a node {N}, this contains a set of all nodes which dominate {N}.
        private readonly IReadOnlyDictionary<Node, OrderedSet<Node>> dominatorTree;

        // Given a node {N}, this contains a set of all nodes which are dominated by {N}.
        private readonly IReadOnlyDictionary<Node, OrderedSet<Node>> domineeTree;

        public ImmutableDomTree(ControlFlowGraph<T> cfg)
        {
            this.cfg = cfg;

            // Compute the dominator tree.
            dominatorTree = DominatorAnalysis.GetDominatorTree(cfg);

            // Compute the dominee tree.
            domineeTree = GetDomineeTree();
        }   

        /// <summary>
        /// Gets whether node {A} is dominated by node {B}.
        /// </summary>
        public bool IsDominatedBy(Node a, Node b) => dominatorTree[a].Contains(b);

        /// <summary>
        /// Gets the immediate dominator of node {N}.
        /// </summary>
        public Node GetImmediateDominator(Node node) => dominatorTree[node].Last();

        /// <summary>
        /// Gets all nodes which dominate node {N}.
        /// </summary>
        public OrderedSet<Node> GetNodeDominators(Node node) => dominatorTree[node];

        /// <summary>
        /// Gets all nodes which are dominated by node {N}.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public OrderedSet<Node> GetNodeDominees(Node node) => domineeTree[node];

        private IReadOnlyDictionary<Node, OrderedSet<Node>> GetDomineeTree()
        {
            // Create a dictionary entry for each node.
            var dominees = new Dictionary<Node, OrderedSet<Node>>();
            foreach (var node in cfg.Nodes)
                dominees.Add(node, new OrderedSet<Node>());

            // For each node {N}, build a new set containing all nodes which are dominated by {N}.
            foreach(var dominatorMapping in dominatorTree)
            {
                var dominator = dominatorMapping.Key;
                foreach (var dominee in dominatorMapping.Value)
                    dominees[dominee].Add(dominatorMapping.Key);
            }

            return dominees;
        }
    }
}
