using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Structuring.Stackify.Structured;
using Iced.Intel;
using Rivers;
using Rivers.Analysis.Traversal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using static Dna.Structuring.Stackify.Structured.CtrlEntry;

namespace Dna.Structuring.Stackify
{
    /// <summary>
    /// Class for structuring arbitrary control flow graphs using the stackify algorithm. References:
    /// - https://github.com/cfallin/waffle/blob/main/src/backend/stackify.rs
    /// </summary>
    public class CfgStackifier
    {
        private ControlFlowGraph<AbstractInst> cfg;

        private Stack<CtrlEntry> ctrlStack = new Stack<CtrlEntry>();

        private IReadOnlyDictionary<Node, HashSet<Node>> dominatorTree;

        private HashSet<BasicBlock<AbstractInst>> loopHeaders;

        private HashSet<BasicBlock<AbstractInst>> thismergeNodes;

        Dictionary<Node, int> rpoPositions;

        public List<WasmBlock> Stackify(ControlFlowGraph<AbstractInst> cfg)
        {
            // Compute merge nodes and loop headers.
            this.cfg = cfg;
            dominatorTree = DominatorAnalysis.GetDominatorTree(cfg);
            var info = ComputeMergeNodesAndLoopHeaders();

            var body = new List<WasmBlock>();
            HandleDomSubtree(cfg.GetBlocks().First(), ref body);

            Console.WriteLine("Done");
            return body;

            //Console.WriteLine(info);
        }

        private (HashSet<BasicBlock<AbstractInst>> mergeNodes, HashSet<BasicBlock<AbstractInst>> loopHeaders) ComputeMergeNodesAndLoopHeaders()
        {
            loopHeaders = new HashSet<BasicBlock<AbstractInst>>();
            thismergeNodes = new HashSet<BasicBlock<AbstractInst>>();
            var branchedOnce = new HashSet<BasicBlock<AbstractInst>>();

            // Record a reverse postorder traversal of the control flow.
            var traversal = new DepthFirstTraversal();
            var recorder = new PostOrderRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var rpo = recorder.GetOrder().Reverse().ToList();

            // Store a position of each block in the RPO, if reachable.
            rpoPositions = new();
            for (int i = 0; i < rpo.Count; i++)
                rpoPositions.Add(rpo[i], i);

            // Collect a set of merge nodes and loop headers.
            foreach(var entry in rpoPositions)
            {
                var block = entry.Key;
                var blockRpo = entry.Value;
                foreach(var successor in block.OutgoingEdges.Select(x => (BasicBlock<AbstractInst>)x.Target))
                {
                    var succRpo = rpoPositions[successor];
                    if(succRpo <= blockRpo)
                    {
                        // If the successor node does not dominate the current block.
                        if (!dominatorTree[block].Contains(successor))
                        {
                            throw new InvalidOperationException("Encountered irreducible control flow.");
                        }

                        loopHeaders.Add(successor);
                    }

                    else
                    {
                        if(!branchedOnce.Add(successor))
                            thismergeNodes.Add(successor);
                    }
                }
            }

            // I *think* the algorithm does some special case here for switch cases.
            // TODO: Handle switch statements.
            if (cfg.Nodes.Any(x => x.OutgoingEdges.Count > 2))
                throw new InvalidOperationException("TODO: Handle switch cases");
            return (thismergeNodes, loopHeaders);
        }

        private void Compute()
        {

        }

        private void HandleDomSubtree(BasicBlock<AbstractInst> block, ref List<WasmBlock> into)
        {

            /*
            var mergeNodeChildren = dominatorTree[block]
                .Where(x => mergeNodes.Contains(x))
                .Cast<BasicBlock<AbstractInst>>()
                .ToList();
            

            
            
            var mergeNodeChildren = dominatorTree.Keys
                .Where(x => dominatorTree[x].Contains(block) && x != block)
                .Cast<BasicBlock<AbstractInst>>()
                .ToList();
            */

            var dominatorChildren = dominatorTree.Keys.Where(x => dominatorTree[x].Contains(block))
                .Cast<BasicBlock<AbstractInst>>()
                .ToHashSet();

            dominatorChildren.Remove(block);
            var mergeNodeChildren = dominatorChildren.Where(x => thismergeNodes.Contains(x));

            // Sort merge nodes so the highest RPO number comes first.
            mergeNodeChildren.OrderByDescending(x => rpoPositions[x]);

            var isLoopHeader = loopHeaders.Contains(block);

            if(isLoopHeader)
            {
                ctrlStack.Push(new CtrlEntryLoop(block));
                var body = new List<WasmBlock>();
                NodeWithin(block, mergeNodeChildren, ref body);
                ctrlStack.Pop();
                into.Add(new Loop()
                {
                    Body = body,
                    Header = block,
                });
            }

            else
            {
                NodeWithin(block, mergeNodeChildren, ref into);
            }
        }


        void NodeWithin(BasicBlock<AbstractInst> block, IEnumerable<BasicBlock<AbstractInst>> mergeNodes, ref List<WasmBlock> into)
        {
            if (mergeNodes.Any())
            {
                var first = mergeNodes.First();
                ctrlStack.Push(new CtrlEntryBlock(first));
                var body = new List<WasmBlock>();
                NodeWithin(block, mergeNodes.Skip(1), ref body);
                into.Add(new Block()
                {
                    Body = body,
                    Out = first
                });

                ctrlStack.Pop();
                HandleDomSubtree(first, ref into);
            }

            else
            {
                into.Add(new Leaf()
                {
                    Block = block,
                });

                switch(block.ExitInstruction) 
                {
                    case InstJmp inst:
                        var target = cfg.GetBlocks().Single(x => x.Address == inst.JumpDestination.Value);
                        DoBranch(block, target, ref into);
                        break;
                    case InstJcc inst:
                        // Compute the blocks which are taken depending on the JCC.
                        var thenBlock = cfg.GetBlocks().Single(x => x.Address == inst.ThenOp.Value);
                        var elseBlock = cfg.GetBlocks().Single(x => x.Address == inst.ElseOp.Value);

                        // Push an ITE node.
                        ctrlStack.Push(new CtrlEntryIfThenElse());

                        // Handle the true block.
                        var ifTrueBody = new List<WasmBlock>();
                        DoBranch(block, thenBlock, ref ifTrueBody);

                        // Handle the false block.
                        var ifFalseBody = new List<WasmBlock>();
                        DoBranch(block, elseBlock, ref ifFalseBody);

                        // Push the if statement.
                        ctrlStack.Pop();
                        into.Add(new If()
                        {
                            Cond = inst.Op1,
                            IfTrue = ifTrueBody,
                            IfFalse = ifFalseBody,
                        });
                        break;
                    case InstRet:
                        into.Add(new Return());
                        break;
                    default:
                        throw new Exception($"Handle handle terminator {block.ExitInstruction}");
                }
            }
        }

        private void DoBranch(BasicBlock<AbstractInst> source, BasicBlock<AbstractInst> target, ref List<WasmBlock> into)
        {
            // This will be a branch to some entry in the control stack if
            // the target is either a merge block, or is a backward branch
            // (by RPO number).
            if(thismergeNodes.Contains(target) || rpoPositions[target] <= rpoPositions[source])
            {
                var index = ResolveTarget(target);
                DoBlockParamTransfer(ref into);
                into.Add(new Br()
                {
                    Target = index,
                });
            }

            else
            {
                // Otherwise, we must dominate the block, so just emit it inline.
                if (!dominatorTree[target].Contains(source))
                    throw new InvalidOperationException($"Expected target node {target} to be dominated by source {source}.");

                DoBlockParamTransfer(ref into);
                HandleDomSubtree(target, ref into);
            }
        }

        private WasmLabel ResolveTarget(BasicBlock<AbstractInst> target)
        {
            // This is horribly inefficient. TODO: Refactor.
            var index = ctrlStack
                .Reverse()
                .ToList()
                .FirstOrDefault(x => x.Label() == target);

            {
                if (index == null)
                {
                    Console.WriteLine("Failed to find target for {0}", target.Address);
                }
            }

            return new WasmLabel(index?.Label());
        }

        private void DoBlockParamTransfer(ref List<WasmBlock> into)
        {
            // Our IR does not have block params, so we do nothing.
            // Still, we have to push this empty entry since the rest
            // of the algorithm expects this to be on the stack.
            into.Add(new BlockParams());
        }
    }
}
