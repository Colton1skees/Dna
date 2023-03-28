using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using LLVMSharp.Interop;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring.Stacker
{
    public class StackingStructurer
    {
        private readonly ControlFlowGraph<LLVMValueRef> cfg;
        IReadOnlyDictionary<Rivers.Node, OrderedSet<Rivers.Node>> dominatorTree;

        HashSet<Rivers.Edge> retreatingEdges;

        HashSet<BasicBlock<LLVMValueRef>> loopHeaders;

        HashSet<BasicBlock<LLVMValueRef>> visited = new();

        private readonly LoopAnalysis<LLVMValueRef> loopAnalysis;

        private StringBuilder sb = new();

        private int indent = 0;

        public StackingStructurer(ControlFlowGraph<LLVMValueRef> cfg, LoopAnalysis<LLVMValueRef> loopAnalysis)
        {
            this.cfg = cfg;
            this.loopAnalysis = loopAnalysis;
        }

        public void Structure()
        {
            // Get the dominator tree.
            dominatorTree = DominatorAnalysis.GetDominatorTree(cfg);

            // Get all retreating edges.
            retreatingEdges = BackEdgeAnalysis.GetBackEdges(cfg);

            // Get all loop headers.
            loopHeaders = retreatingEdges.Select(x => (BasicBlock<LLVMValueRef>)x.Target).ToHashSet();
            Console.WriteLine("");

            ProcessNode(cfg.GetBlocks().First());
        }

        public void ProcessNode(BasicBlock<LLVMValueRef> block)
        {
       
        }

        private bool IsLoopHeader(BasicBlock<LLVMValueRef> block)
        {
            return loopHeaders.Contains(block);
        }

        private bool IsLoopExit(BasicBlock<LLVMValueRef> block)
        {
            return false;
        }

        private void AppendLine(string s)
        {
            var text = s.SkipWhile(x => x == ' ');
            s = new string(text.ToArray());

            sb.AppendLine(GetIndent() + s);
        }

        private string GetIndent()
        {
            return new string(' ', 4 * indent);
        }
    }
}
