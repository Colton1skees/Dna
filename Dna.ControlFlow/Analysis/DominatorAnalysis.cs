using Dna.DataStructures;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    public static class DominatorAnalysis
    {
        /// <summary>
        /// Constructs a dominator tree for the control flow graph.
        /// For each node {N}, yield a set of nodes all nodes which dominate {N}.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IReadOnlyDictionary<Node, OrderedSet<Node>> GetDominatorTree(Graph graph)
        {
            if (graph.Nodes.First().IncomingEdges.Count > 0)
                throw new InvalidOperationException("Root nodes cannot have incoming edges.");

            // Create a dominator tree structure and setup the first node.
            var dominatorTree = new Dictionary<Node, HashSet<Node>>();
            var nodeSet = new HashSet<Node>(graph.Nodes);
            var rootNode = graph.Nodes.First();
            dominatorTree[rootNode] = new HashSet<Node> { rootNode };

            // For each node, make the dominator list contain *all* cfg nodes.
            foreach(var node in graph.Nodes.Skip(1))
            {
                dominatorTree[node] = new HashSet<Node>(graph.Nodes);
            }

            // Naively iterate until a fixed point has been reached.
            bool isChanged = true;
            while(isChanged)
            {
                isChanged = false;
                foreach (var node in graph.Nodes.Skip(1))
                {
                    // Intersect the edge dominators, so that
                    // only shared dominators between all predecessors are preserved.
                    var temp = new HashSet<Node>(nodeSet);
                    foreach (var predEdge in node.IncomingEdges)
                        temp.IntersectWith(dominatorTree[predEdge.Source]);

                    // Since any Node {n} is dominated by itself,
                    // add Node {n} to the dominator list.
                    temp.Add(node);
                    
                    // Skip if no change has occurred.
                    var dominators = dominatorTree[node];
                    if (dominators.SetEquals(temp))
                        continue;

                    // Otherwise, update the dominators & signal a change.
                    isChanged = true;
                    dominatorTree[node] = temp;
                }
            }


            var output = new Dictionary<Node, OrderedSet<Node>>();
            foreach(var pair in dominatorTree)
            {
                output.Add(pair.Key, new OrderedSet<Node>(pair.Value));
            }

            return output;
        }
    }
}
