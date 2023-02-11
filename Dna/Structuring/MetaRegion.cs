using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    public class MetaRegion<T> : IMetaRegion<T>
    {
        public int Index { get; }

        public bool IsSCS { get; }

        public HashSet<BasicBlock<T>> Nodes { get; private set; }

        public IMetaRegion<T> ParentRegion { get; set; }

        public MetaRegion(int index, HashSet<BasicBlock<T>> nodes, bool isSCS)
        {
            Index = index;
            Nodes = nodes;
            IsSCS = isSCS;
        }

        public void ReplaceNodes(HashSet<BasicBlock<T>> nodes)
        {
            Nodes = new HashSet<BasicBlock<T>>(nodes);
        }

        public void UpdateNodes(HashSet<BasicBlock<T>> removal,
           BasicBlock<T> collapsed,
           IEnumerable<BasicBlock<T>> dispatcher,
           IEnumerable<BasicBlock<T>> defaultEntrySet,
           IEnumerable<BasicBlock<T>> outlinedNodes)
        {
            // Remove old SCS nodes.
            bool needSubstitution = false;
            foreach(var node in removal)
            {
                // Skip if the node does not need to be removed.
                if (!Nodes.Contains(node))
                    continue;

                // Erase the node otherwise.
                Nodes.Remove(node);
                needSubstitution = true;
            }

            if (!needSubstitution)
                return;

            Nodes.Add(collapsed);
            Nodes.AddRange(dispatcher);
            Nodes.AddRange(defaultEntrySet);
            Nodes.AddRange(outlinedNodes);
        }

        public HashSet<BasicBlock<T>> GetSuccessors()
        {
            var successors = Nodes
                .SelectMany(x => x.GetOutgoingEdges())
                .Select(x => x.TargetBlock);

            return new HashSet<BasicBlock<T>>(successors);
        }

        public HashSet<BlockEdge<T>> GetOutgoingEdges()
        {
            var outgoingEdges = Nodes.SelectMany(x => x.GetOutgoingEdges());

            return new HashSet<BlockEdge<T>>(outgoingEdges);
        }

        public HashSet<BlockEdge<T>> GetIncomingEdges()
        {
            var outgoingEdges = Nodes.SelectMany(x => x.GetIncomingEdges());

            return new HashSet<BlockEdge<T>>(outgoingEdges);
        }

        public bool IntersectsWith(IMetaRegion<T> region)
        {
            // If the region contains any node from our node list, there is an intersection.
            foreach(var node in Nodes)
            {
                if (region.Nodes.Contains(node))
                    return true;
            }

            // Otherwise, there is no intersection.
            return false;
        }

        // TODO: Validate.
        public bool IsSubSet(IMetaRegion<T> region)
        {
            foreach(var node in region.Nodes)
            {
                if (!Nodes.Contains(node))
                    return false;
            }

            return true;
        }

        // TODO: Validate.
        public bool IsSuperSet(IMetaRegion<T> region)
        {
            foreach(var node in Nodes)
            {
                if (!region.Nodes.Contains(node))
                    return false;
            }

            return true;
        }

        // TODO: Validate.
        public bool NodesEquality(IMetaRegion<T> region)
        {
            return region.Nodes.SequenceEqual(Nodes);
        }

        public void MergeWith(IMetaRegion<T> region) => Nodes.AddRange(region.Nodes);

        public bool ContainsNode(BasicBlock<T> node) => Nodes.Contains(node);

        public void InsertNode(BasicBlock<T> node) => Nodes.Add(node);

        public void RemoveNode(BasicBlock<T> node) => Nodes.Remove(node);

        public BasicBlock<T> GetProbableEntry(IEnumerable<BasicBlock<T>> reversePostOrderTraveral)
        {
            foreach(var node in reversePostOrderTraveral)
            {
                if(ContainsNode(node)) 
                    return node;
            }

            throw new InvalidOperationException("Failed to find probable entry.");
        }
    }
}
