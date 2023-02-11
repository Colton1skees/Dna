using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    public interface IMetaRegion<T>
    {
        public int Index { get; }

        public bool IsSCS { get; }

        public HashSet<BasicBlock<T>> Nodes { get; }

        public IMetaRegion<T> ParentRegion { get; set; }

        void ReplaceNodes(HashSet<BasicBlock<T>> nodes);

        void UpdateNodes(HashSet<BasicBlock<T>> removal,
            BasicBlock<T> collapsed,
            IEnumerable<BasicBlock<T>> dispatcher,
            IEnumerable<BasicBlock<T>> defaultEntrySet,
            IEnumerable<BasicBlock<T>> outlinedNodes);

        HashSet<BasicBlock<T>> GetSuccessors();

        HashSet<BlockEdge<T>> GetOutgoingEdges();

        HashSet<BlockEdge<T>> GetIncomingEdges();

        bool IntersectsWith(IMetaRegion<T> region);

        bool IsSubSet(IMetaRegion<T> region);

        bool IsSuperSet(IMetaRegion<T> region);

        bool NodesEquality(IMetaRegion<T> region);

        void MergeWith(IMetaRegion<T> region);

        bool ContainsNode(BasicBlock<T> node);

        void InsertNode(BasicBlock<T> node);

        void RemoveNode(BasicBlock<T> node);

        BasicBlock<T> GetProbableEntry(IEnumerable<BasicBlock<T>> reversePostOrderTraveral);
    }
}
