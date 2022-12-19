using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.ControlFlow.DataStructures;
using Dna.DataStructures;
using Rivers;
using Rivers.Analysis;
using Rivers.Analysis.Traversal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization.Ssa
{
    public class SsaDiGraph
    {
        private readonly ControlFlowGraph<AbstractInst> cfg;

        private readonly Dictionary<IOperand, HashSet<Block>> defs = new();

        private readonly Dictionary<Block, Dictionary<IOperand, InstPhi>> phiNodes = new();

        private readonly DominanceAnalysis analysis;

        private readonly Dictionary<Node, OrderedSet<Node>> frontier;

        private Dictionary<IOperand, int> stackLhs = new();

        private Dictionary<IOperand, int> stackRhs = new();

        // Mapping between <ssaVariable, nonSsa>
        private readonly Dictionary<IOperand, IOperand> ssaMapping = new();

        private ControlFlowGraph<AbstractInst> ssaGraph;

        private Dictionary<IOperand, (Block block, AbstractInst inst)> ssaToLocation = new();

        public SsaDiGraph(ControlFlowGraph<AbstractInst> ircfg)
        {
            this.cfg = ircfg;
            ssaGraph = new ControlFlowGraph<AbstractInst>(ircfg.StartAddress);

            Dictionary<ulong, Block> addrMapping = new();
            foreach(var block in ircfg.GetBlocks())
            {
                var newBlock = ssaGraph.CreateBlock(block.Address);
                addrMapping.Add(block.Address, newBlock);
            }

            // Create an empty clone of the basic block.
            foreach(var block in ircfg.GetBlocks())
            {
                var ssaBlock = addrMapping[block.Address];
                foreach (var incomingEdge in block.GetIncomingEdges())
                {
                    var source = addrMapping[incomingEdge.SourceBlock.Address];
                    var target = addrMapping[incomingEdge.TargetBlock.Address];
                    ssaBlock.IncomingEdges.Add(new Edge(source, target));
                }

                foreach (var outgoingEdge in block.GetOutgoingEdges())
                {
                    var source = addrMapping[outgoingEdge.SourceBlock.Address];
                    var target = addrMapping[outgoingEdge.TargetBlock.Address];
                    ssaBlock.OutgoingEdges.Add(new Edge(source, target));
                }
            }

            analysis = new DominanceAnalysis(cfg);
            frontier = analysis.GetDominanceFrontier(cfg.Nodes.First());
        }

        public void Transform()
        {
            // For each block, collect a list of all variables that it defines.
            InitializeRoutineDefs();

            PlacePhis();

            Rename();

            Console.WriteLine("Done");
        }

        private void InitializeRoutineDefs()
        {
            // Order the basic blocks in a depth first manner.
            var traversal = new DepthFirstTraversal();
            var recorder = new TraversalOrderRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var nodes = recorder.TraversedNodes;

            foreach (var block in nodes.Cast<Block>())
            {
                Console.WriteLine("Handling block: {0}", block.Address.ToString("X"));
                //Console.WriteLine("Frontier count: {0}", frontier.Count);
                InitializeBlockDefs(block);
            }

        }

        private void InitializeBlockDefs(Block block)
        {
            foreach(var inst in block.Instructions)
            {
                if (!inst.HasDestination)
                    continue;

                if (inst.Dest is not RegisterOperand)
                    continue;

                defs.TryAdd(inst.Dest, new HashSet<Block>());
                defs[inst.Dest].Add(block);
            }
        }

        private void PlacePhis()
        {
            // Compute a dominance frontier.
            var head = cfg.Nodes.First();

            foreach (IOperand variable in defs.Keys)
            {
                var done = new OrderedSet<Node>();
                var todo = new OrderedSet<Node>();
                var intodo = new OrderedSet<Node>();

                foreach(var block in defs[variable])
                {
                    todo.Add(block);
                    intodo.Add(block);
                }

                while(todo.Any())
                {
                    // Pop the latest todo item.
                    var block = todo.Last() as Block;
                    todo.Remove(block);

                    var blockInfo = frontier.ContainsKey(block) ? frontier[block] : new ControlFlow.DataStructures.OrderedSet<Node>();
                    foreach (var node in blockInfo)
                    {
                        // Skip if we've already processed this node.
                        if (done.Contains(node))
                            continue;

                        // Create an empty phi for the variable.
                        var irBlock = (Block)node;
                        phiNodes.TryAdd(irBlock, new Dictionary<IOperand, InstPhi>());
                        var phi = new InstPhi(variable, irBlock);
                        phiNodes[irBlock].Add(variable, phi);

                        done.Add(node);
                        if(!intodo.Contains(node))
                        {
                            intodo.Add(node);
                            todo.Add(node);
                        }
                    }
                }
            }
        }

        private void Rename()
        {
            var head = cfg.Nodes.First();

            var idoms = analysis.GetImmediateDominators(head);


            var dominatorTree = DepthFirstSearch(head, idoms);

            var domTree = new ControlFlowGraph<AbstractInst>(cfg.StartAddress);

            Dictionary<ulong, Block> blockMapping = new Dictionary<ulong, Block>();
            foreach(var idom in idoms)
            {
                var domBlock = domTree.CreateBlock((idom.Key as Block).Address);

            }

            foreach(var idom in idoms)
            {
                var srcBlock = idom.Key as Block;
                var dst = idom.Value as Block;
            }


            var stack = new Stack<Dictionary<IOperand, int>>();
            stack.Push(stackRhs);

            foreach(var block in dominatorTree.Cast<Block>())
            {
                // Pop and create a copy of the current ssa variable stack.
                var popped = stack.Pop();
                stackRhs = new Dictionary<IOperand, int>(popped);

                // Transform variables of phi functions on LHS into ssa.
                RenamePhiLhs(block);

                // Transform all non-phi expressions into SSA.
                RenameExpressions(block);

                foreach(var outgoingEdge in block.GetOutgoingEdges())
                {
                    var successor = outgoingEdge.TargetBlock;
                    // TODO: Rename phi RHS.
                    RenamePhiRhs(successor);
                }

                // Save the current SSA variable stack for the successors in the dominator tree.
                var node = dominatorTree.Find(block);
                if (node.Next == null)
                    continue;

                stack.Append(stackRhs);
            }

        }

        private void RenamePhiLhs(Block block)
        {
            if (!phiNodes.ContainsKey(block))
                return;

            var phis = phiNodes[block];
            foreach(var dst in phis.ToDictionary(x => x.Key, x => x.Value))
            {
                // Transform variables on LHS inplace.
                var transformed = TransformExpressionLhs(dst.Key);
                phiNodes[block].Remove(dst.Key);
                phiNodes[block][transformed] = dst.Value;
            }
        }

        private SsaOperand TransformExpressionLhs(IOperand operand)
        {
            // Transform lhs
            var ssaVar = TransformVarLhs(operand);

            // Increment the ssa variable counter.
            stackLhs[operand] += 1;

            // Return the expression.
            return ssaVar;
        }

        private SsaOperand TransformVarLhs(IOperand operand)
        {
            if (!stackLhs.ContainsKey(operand))
                stackLhs[operand] = 0;

            stackRhs[operand] = stackLhs[operand];

            var stack = stackLhs;
            var ssaVar = GenVarExpr(operand, stack);
            return ssaVar;
        }

        /// <summary>
        /// Transforms variables and expressions
        /// of an IRBlock into SSA form.
        /// </summary>
        /// <param name="block"></param>
        private Dictionary<AbstractInst, List<IOperand>> RenameExpressions(Block block)
        {
            var newOperands = new Dictionary<AbstractInst, List<IOperand>>();

            foreach(var inst in block.Instructions)
            {
                newOperands.TryAdd(inst, new List<IOperand>());
                var opList = newOperands[inst];

                IOperand dstSsa = inst.HasDestination ? TransformExpressionLhs(inst.Dest) : null;
                if (inst.HasDestination)
                {
                    if (dstSsa == null)
                        throw new InvalidOperationException("");

                    opList.Add(dstSsa);

                    ssaToLocation[dstSsa] = (block, inst);
                }

                var rhs = new List<IOperand>();

                foreach(var operand in inst.Operands)
                {
                    if (operand is ImmediateOperand)
                        opList.Append(operand);
                    else
                        opList.Append(TransformExpressionRhs(operand));
                }
            }

            foreach(var pair in newOperands)
            {
                var inst = pair.Key;
                var newOps = pair.Value;
                if(inst.HasDestination)
                {
                    inst.Dest = newOps.First();
                    for(int i = 1; i < newOps.Count; i++)
                    {
                        inst.Operands[i] = newOps[i];
                    }
                }

                else
                {
                    for(int i = 0; i < newOps.Count; i++)
                    {
                        inst.Operands[i] = newOps[i];
                    }
                }
            }

            return newOperands;
        }

        private IOperand TransformExpressionRhs(IOperand src)
        {
            return TransformVarRhs(src);
        }

        private IOperand TransformVarRhs(IOperand src)
        {
            // The variable has never been assigned to, therefore an SSA var is not needed.
            if (!stackRhs.ContainsKey(src))
                return src;

            // Variable has been on the RHS.
            var stack = stackRhs;
            return GenVarExpr(src, stack);
        }

        private void RenamePhiRhs(Block successor)
        {
            if (!phiNodes.ContainsKey(successor))
                return;

            foreach(var phiNode in phiNodes[successor])
            {
                var nonSsa = ssaMapping.ContainsKey(phiNode.Key) ? ssaMapping[phiNode.Key] : phiNode.Key;

                var srcSsa = TransformExpressionRhs(nonSsa);

                phiNode.Value.Operands.Add(srcSsa);
            }
        }

        private SsaOperand GenVarExpr(IOperand input, Dictionary<IOperand, int> stack)
        {
            stack.TryAdd(input, 0);
            var index = stack[input];

            // Note: Commented this out for quick prototyping.
            //var ssaVar = new SsaOperand(input, index);
            //ssaMapping[ssaVar] = input;

            // return ssaVar;
            return null;
        }

        private LinkedList<Node> DepthFirstSearch(Node head, Dictionary<Node, Node> dominatorTree)
        {
            LinkedList<Node> nodes = new LinkedList<Node>();
            nodes.Append(head);
            while(true)
            {
                if (!dominatorTree.ContainsKey(head))
                    break;

                head = dominatorTree[head];
                nodes.Append(head);
            }

            return nodes;
        }



        private InstPhi GetEmptyPhi(IOperand variable)
        {
            return new InstPhi(variable, null);
        }
    }
}
