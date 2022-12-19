using Dna.ControlFlow.DataStructures;
using Rivers;
using Rivers.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    public class DominanceAnalysis
    {
        private readonly Graph graph;

        public DominanceAnalysis(Graph graph)
        {
            this.graph = graph;
        }

        public Dictionary<Node, OrderedSet<Node>> GetDominanceFrontier(Node head)
        {
            var idoms = GetImmediateDominators(head);
            var frontier = new Dictionary<Node, OrderedSet<Node>>();

            foreach(var item in idoms)
            {
                var node = item.Key;
                if(node.IncomingEdges.Count >= 2)
                {
                    foreach(var incomingEdge in node.IncomingEdges)
                    {
                        var predecessor = incomingEdge.Source;
                        var runner = predecessor;
                        if (!idoms.ContainsKey(runner))
                            continue;
                        while(runner != idoms[node])
                        {
                            if(!frontier.ContainsKey(runner))
                            {
                                frontier[runner] = new OrderedSet<Node>();
                            }

                            frontier[runner].Add(node);
                            runner = idoms[runner];
                        }
                    }
                }
            }

            return frontier;
        }

        public Dictionary<Node, Node> GetImmediateDominators(Node head)
        {
            var reachable = GetReachableNodes(head).ToList();
            var dominators = GetGenericDominators(head, reachable);
            var idoms = new Dictionary<Node, Node>();

            foreach(var node in dominators)
            {
                foreach(var predecessor in WalkGenericDominator(node.Key, dominators))
                {
                    if(dominators[node.Key].Contains(predecessor) && node.Key != predecessor)
                    {
                        idoms[node.Key] = predecessor;
                        break;
                    }
                }
            }

            return idoms;
        }

        private Dictionary<Node, OrderedSet<Node>> GetGenericDominators(Node head, IEnumerable<Node> reachable)
        {
            var nodes = new OrderedSet<Node>(reachable);
            var dominators = new Dictionary<Node, OrderedSet<Node>>();
            foreach(var node in nodes)
            {
                dominators[node] = new OrderedSet<Node>(nodes);
            }

            dominators[head] = new OrderedSet<Node>() 
            {
                head
            };
            var todo = new OrderedSet<Node>(nodes);

            while(todo.Any())
            {
                // Pop the last item.
                var node = todo.Last();
                todo.Remove(node);

                // Heads state must not be changed.
                if (node == head)
                    continue;

                // Compute intersection of all predecessors' dominators.
                OrderedSet<Node> newDom = null;
                foreach(var incomingEdge in node.IncomingEdges)
                {
                    var pred = incomingEdge.Source;
                    if (!nodes.Contains(pred))
                        continue;

                    if (newDom == null)
                        newDom = new OrderedSet<Node>(dominators[pred]);

                    var predDom = dominators[pred];
                    foreach(var item in newDom.ToList())
                    {
                        if(!predDom.Contains(item))
                        {
                            newDom.Remove(item);
                        }
                    }
                }

                if (newDom == null)
                    throw new InvalidOperationException("New dominator set cannot be null.");

                // This might be wrong?
                newDom.Add(node);

                if (AreSetsEqual(newDom, dominators[node]))
                    continue;

                dominators[node] = newDom;
                foreach(var outgoingEdge in node.OutgoingEdges)
                {
                    var succ = outgoingEdge.Target;
                    todo.Add(succ);
                }
            }

            return dominators;
        }

        private IEnumerable<Node> GetReachableNodes(Node head)
        {
            var todo = new OrderedSet<Node>();
            todo.Add(head);
            var reachable = new OrderedSet<Node>();
            while(todo.Any())
            {
                // Pop an item from the todo list.
                var node = todo.Last();
                todo.Remove(node);
                
                if (reachable.Contains(node))
                    continue;

                reachable.Add(node);
                yield return node;

                foreach(var next in node.OutgoingEdges)
                {
                    todo.Add(next.Target);
                }
            }
        }

        private IEnumerable<Node> WalkGenericDominator(Node node, Dictionary<Node, OrderedSet<Node>> dominators)
        {
            var done = new HashSet<Node>();
            if (!dominators.ContainsKey(node))
                yield break;

            var nodeGenDominators = new OrderedSet<Node>(dominators[node]);
            var todo = new OrderedSet<Node>();
            todo.Add(node);

            // Avoid working on itself
            nodeGenDominators.Remove(node);

            while(nodeGenDominators.Any())
            {
                Node newNode = null;

                while(todo.Any())
                {
                    node = todo.Last();
                    todo.Remove(node);

                    if (done.Contains(node))
                        continue;

                    if (nodeGenDominators.Contains(node))
                    {
                        newNode = node;
                        break;
                    }

                    // Avoid loops.
                    done.Add(node);
                    
                    // TODO: Validate that I'm not supposed to be iterating over outgoing edges in other places.
                    foreach(var incomingEdge in node.IncomingEdges)
                    {
                        todo.Add(incomingEdge.Source);
                    }
                }

                if (newNode == null)
                    throw new InvalidOperationException("New node cannot be null.");

                yield return newNode;
                nodeGenDominators.Remove(newNode);
                todo = new OrderedSet<Node>();
                todo.Add(newNode);
            }
        }

        private bool AreSetsEqual<T>(OrderedSet<T> setOne, OrderedSet<T> setTwo)
        {
            // If the sets have a different size, then they cannot be equal.
            if (setOne.Count != setTwo.Count)
                return false;

            // If set two is missing any items from set one, then they cannot be equal.
            foreach(var item in setOne)
            {
                if (!setTwo.Contains(item))
                    return false;
            }

            // Otherwise they are equal.
            return true;
        }
    }
}
