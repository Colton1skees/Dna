using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Extensions;
using Dna.Structuring.Stackify.StackEntries;
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
using TritonTranslator.Intermediate.Operands;
using static Dna.Structuring.Stackify.StackEntries.StackEntry;
using static Dna.Structuring.Stackify.Structured.CtrlEntry;

namespace Dna.Structuring.Stackify
{
    /// <summary>
    /// Stackify implementation to produce structured control flow from an arbitrary CFG.
    /// 
    /// See the paper:
    ///     - Norman Ramsey. Beyond Relooper: recursive translation of
    ///     unstructured control flow to structured control flow. In ICFP
    ///     2022 (Functional Pearl). https://dl.acm.org/doi/10.1145/3547621
    ///     
    /// This implementation is heavily inspired by cfallin's implementation in
    /// waffle(https://github.com/cfallin/waffle/blob/main/src/backend/stackify.rs).
    /// </summary>
    public class StackifierTwo
    {
        private ControlFlowGraph<AbstractInst> cfg;

        private IReadOnlyDictionary<Node, HashSet<Node>> dominatorTree;

        private HashSet<BasicBlock<AbstractInst>> loopHeaders = new();

        private HashSet<BasicBlock<AbstractInst>> thismergeNodes = new();

        private List<CtrlEntry> ctrlStack = new();

        private List<StackEntry> processStack = new();

        private List<List<WasmBlock>> result = new();

        private List<List<BasicBlock<AbstractInst>>> mergeNodeChildren = new();

        Dictionary<Node, int> rpoPositions;

        public List<WasmBlock> Stackify(ControlFlowGraph<AbstractInst> cfg)
        {
            // Compute merge nodes and loop headers.
            this.cfg = cfg;
            dominatorTree = DominatorAnalysis.GetDominatorTree(cfg);
            var info = ComputeMergeNodesAndLoopHeaders();

            Console.WriteLine("Done");
            return Compute();

            //Console.WriteLine(info);
        }

        private (HashSet<BasicBlock<AbstractInst>> mergeNodes, HashSet<BasicBlock<AbstractInst>> loopHeaders) ComputeMergeNodesAndLoopHeaders()
        {
            // Record a reverse post order traversal of nodes.
            var branchedOnce = new HashSet<BasicBlock<AbstractInst>>();
            var traversal = new DepthFirstTraversal();
            var recorder = new PostOrderRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var rpo = recorder.GetOrder().Reverse().ToList();

            // Store a position of each block in the RPO, if reachable.
            rpoPositions = new();
            for (int i = 0; i < rpo.Count; i++)
                rpoPositions.Add(rpo[i], i);

            foreach(var entry in rpoPositions)
            {
                var block = entry.Key;
                var blockRpo = entry.Value;
                foreach (var successor in block.OutgoingEdges.Select(x => (BasicBlock<AbstractInst>)x.Target))
                {
                    Console.WriteLine($"Block {block.Name} has succ {successor.Name}");

                    var succRpo = rpoPositions[successor];
                    Console.WriteLine($"Succ rpo {succRpo}");
                    if(succRpo <= blockRpo)
                    {
                        if (!dominatorTree[block].Contains(successor))
                        {
                            throw new InvalidOperationException("Irreducible control flow.");
                        }

                        loopHeaders.Add(successor);
                    }

                    else
                    {
                        if(!branchedOnce.Add(successor))
                        {
                            thismergeNodes.Add(successor);
                        }
                    }
                }
            }

            // I *think* the algorithm does some special case here for switch cases.
            // TODO: Handle switch statements.
            if (cfg.Nodes.Any(x => x.OutgoingEdges.Count > 2))
                throw new InvalidOperationException("TODO: Handle switch cases");
            return (thismergeNodes, loopHeaders);
        }

        private List<WasmBlock> Compute()
        {
            result.Push(new List<WasmBlock>());
            processStack.Push(new DomSubtree(cfg.GetBlocks().First()));
            while(processStack.Any())
            {
                Process(processStack.Pop());
            }

            return result.Pop();
        }

        private void Process(StackEntry stackEntry)
        {
            switch(stackEntry)
            {
                case DomSubtree entry:
                    HandleDomSubtree(entry.block);
                    break;
                case EndDomSubtree entry:
                    EndDomSubtree();
                    break;
                case NodeWithin entry:
                    NodeWithin(entry.block, entry.usize);
                    break;
                case FinishLoop entry:
                    FinishLoop(entry.block);
                    break;
                case FinishBlock entry:
                    FinishBlock(entry.block);
                    break;
                case Else entry:
                    Else();
                    break;
                case FinishIf entry:
                    FinishIf(entry.cond);
                    break;
                case DoBranch entry:
                    DoBranch(entry.block, entry.target);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void HandleDomSubtree(BasicBlock<AbstractInst> block)
        {
            // Get all nodes which are dominated by block.
            var domChildren = dominatorTree
                .Where(x => x.Value.Contains(block))
                .Select(x => x.Key)
                .Where(x => thismergeNodes.Contains(x))
                .Cast<BasicBlock<AbstractInst>>()
                .ToHashSet();

            // Remove block from the list of dominator children,
            // since we don't want to recurse infinitely.
            domChildren.Remove(block);

            domChildren.OrderByDescending(x => rpoPositions[x]);

            var isLoopHeader = loopHeaders.Contains(block);

            var names = String.Join(",", domChildren.Select(x => x.Address.ToString("X")));
            Console.WriteLine($"Handle dom subtree: block {block.Name} has merge children: {names}\n (isLoopHeader:{isLoopHeader})");

            // `merge_node_children` stack entry is popped by `EndDomSubtree`.
            mergeNodeChildren.Push(domChildren.ToList());
            processStack.Push(new EndDomSubtree());

            if(isLoopHeader)
            {
                // Control stack and block-list-result-stack entries are
                // popped by `FinishLoop`.
                ctrlStack.Push(new CtrlEntryLoop(block));
                result.Push(new List<WasmBlock>());
                processStack.Push(new FinishLoop(block));
                processStack.Push(new NodeWithin(block, 0));
            }

            else
            {
                // "tail-call" to `NodeWithin` step, but use existing
                // result-stack entry.
                processStack.Push(new NodeWithin(block, 0));
            }
        }

        private void EndDomSubtree()
        {
            mergeNodeChildren.Pop();
        }

        private void NodeWithin(BasicBlock<AbstractInst> block, uint mergeNodeStart)
        {
            // The translation of slicing here is probably incorrect.
            var mergeNodes = mergeNodeChildren.Last();
            mergeNodes = mergeNodes.Skip((int)mergeNodeStart).ToList();

            var into = result.Last();

            if(mergeNodes.Any())
            {
                var first = mergeNodes.First();
                processStack.Push(new DomSubtree(first));
                ctrlStack.Push(new CtrlEntryBlock(first));
                result.Push(new List<WasmBlock>());
                processStack.Push(new FinishBlock(first));
                processStack.Push(new NodeWithin(block, mergeNodeStart + 1));
            }

            else
            {
                // Leaf node: emit contents!
                into.Push(new Leaf()
                {
                    Block = block,
                });

                switch(block.ExitInstruction)
                {
                    case InstJmp inst:
                        var target = cfg.GetBlocks().Single(x => x.Address == inst.JumpDestination.Value);
                        processStack.Push(new DoBranch(block, target));
                        break;
                    case InstJcc inst:
                        // Compute the blocks which are taken depending on the JCC.
                        var thenBlock = cfg.GetBlocks().Single(x => x.Address == inst.ThenOp.Value);
                        var elseBlock = cfg.GetBlocks().Single(x => x.Address == inst.ElseOp.Value);

                        // Something is off here - in the stackify source,
                        // they push the else block after finish if?
                        // Maybe the order needs to be inverted..
                        // NOTE: We just manually inverted this.
                        ctrlStack.Push(new CtrlEntryIfThenElse());
                        processStack.Push(new FinishIf(inst.Op1));
                        //processStack.Push(new DoBranch(block, elseBlock));
                        processStack.Push(new DoBranch(block, elseBlock));
                        processStack.Push(new Else());
                        //processStack.Push(new DoBranch(block, thenBlock));
                        processStack.Push(new DoBranch(block, thenBlock));
                        result.Push(new List<WasmBlock>()); // if-body
                        break;
                    case InstRet:
                        into.Push(new Return());
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private void FinishLoop(BasicBlock<AbstractInst> block)
        {
            ctrlStack.Pop();
            var body = result.Pop();
            result.Last().Push(new Loop()
            {
                Body = body,
                Header = block
            });
        }

        private WasmLabel ResolveTarget(List<CtrlEntry> ctrl_stack, BasicBlock<AbstractInst> target)
        {
            // TODO: Return the index of the control entry to target?
            var stack = ctrl_stack.ToList();
            stack.Reverse();
            var labelDest = stack.First(x => x.Label() == target);
            return new WasmLabel(target);
        }


        private void FinishBlock(BasicBlock<AbstractInst> block)
        {
            ctrlStack.Pop();
            var body = result.Pop();
            result
                .Last()
                .Push(new Block()
                {
                    Body = body,
                    Out = block
                });
        }

        private void Else()
        {
            result.Push(new List<WasmBlock>());
        }

        private void FinishIf(object cond)
        {
            var elseBody = result.Pop();
            var ifBody = result.Pop();
            ctrlStack.Pop();
            result.Last().Push(new If()
            {
                Cond = (IOperand)cond,
                IfTrue = ifBody,
                IfFalse = elseBody,
            });
        }

        private void DoBranch(BasicBlock<AbstractInst> source, BasicBlock<AbstractInst> target)
        {
            var into = result.Last();

            // This will be a branch to some entry in the control stack if
            // the target is either a merge block, or is a backward branch
            // (by RPO number).
            if(thismergeNodes.Contains(target) || rpoPositions[target] <= rpoPositions[source])
            {
                var index = ResolveTarget(ctrlStack, target);
                DoBlockParamTransfer(ref into);
                into.Push(new Br()
                {
                    Target = index,
                });
            }

            else
            {
                if (!dominatorTree[target].Contains(source))
                {
                    throw new InvalidOperationException();
                }

                DoBlockParamTransfer(ref into);
                processStack.Push(new DomSubtree(target));
            }
        }

        private void DoBlockParamTransfer(ref List<WasmBlock> into)
        {
            into.Push(new BlockParams());
        }
    }
}
