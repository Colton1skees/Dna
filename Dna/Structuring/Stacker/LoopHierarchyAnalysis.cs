using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using LLVMSharp.Interop;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring.Stacker
{
    public class LoopHierarchyAnalysis<T>
    {
        private readonly ControlFlowGraph<T> cfg;

        private IReadOnlyDictionary<Node, OrderedSet<Node>> dominatorTree;

        HashSet<Edge> retreatingEdges;

        HashSet<BasicBlock<T>> loopHeaders;

        Dictionary<BasicBlock<T>, BasicBlock<T>> loopExits = new();

        public LoopHierarchyAnalysis(ControlFlowGraph<T> cfg)
        {
            this.cfg = cfg;

            // Build the dominator tree.
            dominatorTree = DominatorAnalysis.GetDominatorTree(cfg);

            // Get all retreating edges.
            retreatingEdges = BackEdgeAnalysis.GetBackEdges(cfg);

            // Get all loop headers.
            loopHeaders = retreatingEdges.Select(x => (BasicBlock<T>)x.Target).ToHashSet();
        }

        public void BuildLoopHierarchy()
        {

        }

        private bool IsLoopHeader(BasicBlock<T> block) => loopHeaders.Contains(block);
    }
}
