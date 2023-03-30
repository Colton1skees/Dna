using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using LLVMSharp.Interop;
using Rivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring.Stacker
{
    public class StackingStructurer
    {
        private readonly ControlFlowGraph<LLVMValueRef> cfg;

        private readonly ImmutableDomTree<LLVMValueRef> domTree;
        HashSet<Rivers.Edge> retreatingEdges;

        HashSet<BasicBlock<LLVMValueRef>> loopHeaders;

        HashSet<BasicBlock<LLVMValueRef>> visited = new();


        private BlockEdge<LLVMValueRef> latestEdge = null;

        private readonly LoopAnalysis<LLVMValueRef> loopAnalysis;

        private StringBuilder sb = new();

        private int indent = 0;

        private Dictionary<Node, OrderedSet<Node>> seenIncomingDEstinations = new Dictionary<Node, OrderedSet<Node>>();

        private OrderedSet<BasicBlock<LLVMValueRef>> blockQueue = new();

        public StackingStructurer(ControlFlowGraph<LLVMValueRef> cfg, ImmutableDomTree<LLVMValueRef> domTree)
        {
            this.cfg = cfg;
            this.domTree = domTree;
            this.loopAnalysis = new LoopAnalysis<LLVMValueRef>(cfg, domTree);

            foreach(var node in cfg.Nodes)
            {
                seenIncomingDEstinations.Add(node, new OrderedSet<Node>());
            }
        }

        public void Structure()
        {
            // Get all retreating edges.
            retreatingEdges = BackEdgeAnalysis.GetBackEdges(cfg);

            // Get all loop headers.
            loopHeaders = retreatingEdges.Select(x => (BasicBlock<LLVMValueRef>)x.Target).ToHashSet();
            Console.WriteLine("");

            blockQueue.Add(cfg.GetBlocks().First());
            while(blockQueue.Any())
            {
                latestEdge = null;
                var popped = blockQueue.Last();
                blockQueue.Remove(popped);
                ProcessNode(popped);
            }
         

            Console.WriteLine(sb.ToString());
            Console.WriteLine("");
        }

        public void ProcessNode(BasicBlock<LLVMValueRef> block)
        {
            if (block.Instructions.First().ToString().Contains("%6 = getelementptr inbounds i8, ptr %0,"))
            {
              //  Console.WriteLine("foo");
               // Console.WriteLine(" ");
              //  Console.WriteLine(sb.ToString());
              //Debugger.Break();
            }
            // If we encounter a merge node and haven't processed all of the dominators, then do nothing.
            visited.Add(block);
            foreach(var dominator in domTree.GetNodeDominators(block))
            {
                if (!visited.Contains(dominator))
                    return;
            }

            if(latestEdge != null && !domTree.IsDominatedBy(block, latestEdge.SourceBlock))
            {
                blockQueue.Add(block);
                return;
            }

            if (latestEdge != null)
            {
                seenIncomingDEstinations[block].Add(latestEdge.Source);

                var allIncomingNodes = block.IncomingEdges.Select(x => x.Source);
                foreach (var incomingNode in allIncomingNodes)
                {
                    if(incomingNode == block)
                    {
                        continue;
                    }

                    if(domTree.IsDominatedBy(incomingNode, block))
                    {
                        continue;
                    }

                    if (!seenIncomingDEstinations[block].Contains(incomingNode))
                    {
                        blockQueue.Add(block);
                        return;
                    }
                }
            }


            // Print all instructions, except for the exit instruction.
            foreach (var instruction in block.Instructions)
            {
                if (instruction == block.ExitInstruction)
                    continue;

                AppendLine(instruction.ToString());
            }

            if(ProcessIfExit(block))
            {

            }

            else if(ProcessSingleExit(block))
            {

            }

            else if(ProcessRet(block))
            {

            }

            else
            {
                throw new InvalidOperationException();
            }
        }

        private bool ProcessIfExit(BasicBlock<LLVMValueRef> block)
        {
            if (block.OutgoingEdges.Count != 2)
                return false;


            // Process the first edge.
            var edge1 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(2).Handle.ToString("X"));
            AppendLine($"if({block.ExitInstruction.GetOperand(0).Name})");
            AppendLine("{");
            indent += 1;
            ProcessEdge(edge1);
            indent -= 1;
            AppendLine("}");

            // Process the second edge.
            var edge2 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(1).Handle.ToString("X"));
            AppendLine("else ");
            AppendLine("{");
            indent += 1;
            ProcessEdge(edge2);
            indent -= 1;
            AppendLine("}");

            return true;
        }

        private bool ProcessSingleExit(BasicBlock<LLVMValueRef> block)
        {
            if (block.OutgoingEdges.Count != 1)
                return false;

            var edge = block.GetOutgoingEdges().Single();
            ProcessEdge(edge);
           // ProcessNode(edge.TargetBlock);

            return true;
        }

        private bool ProcessRet(BasicBlock<LLVMValueRef> block)
        {
            if (block.OutgoingEdges.Count != 0)
                return false;

            AppendLine("return;");
            return true;
        }

        private void ProcessEdge(BlockEdge<LLVMValueRef> edge1)
        {
            latestEdge = edge1;
            var block = edge1.SourceBlock;
            if (IsLoopHeader(edge1.TargetBlock))
            {
                // If the current block is jumping to a loop header, and that loop dominates the current block,
                // then this must be a continue.
                if (domTree.IsDominatedBy(block, edge1.TargetBlock))
                {
                    AppendLine("continue;");
                    return;
                }

                // Otherwise we're jumping INTO a loop, so we want to process it.
                AppendLine("while(true)");
                AppendLine("{");
                indent += 1;
                ProcessNode(edge1.TargetBlock);
                indent -= 1;
                AppendLine("}");
                return;
            }
            
            // If we're jumping to a loop exit, then we need to break.
            if(IsLoopExit(edge1.TargetBlock))
            {
                AppendLine("break;");
                //ProcessNode(edge1.TargetBlock);
                return;
            }

            // Otherwise, this is just a normal branch, and we need to handle it traditionally.
            ProcessNode(edge1.TargetBlock);
        }

        private bool ProcessLoopExit(BasicBlock<LLVMValueRef> block)
        {
            return false;
        }

        private bool IsLoopHeader(BasicBlock<LLVMValueRef> block)
        {
            return loopAnalysis.loopHeaders.Contains(block);
        }

        private bool IsLoopExit(BasicBlock<LLVMValueRef> block)
        {
            return loopAnalysis.loopExitToHeaderMapping.ContainsKey(block);
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
