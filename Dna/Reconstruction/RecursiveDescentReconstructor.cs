using Dna.ControlFlow;
using Dna.Extensions;
using Iced.Intel;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Reconstruction
{
    public class RecursiveDescentReconstructor : ICfgReconstructor
    {
        private readonly IDna dna;

        private ControlFlowGraph<Instruction> graph;

        public RecursiveDescentReconstructor(IDna dna)
        {
            this.dna = dna;
        }

        public ControlFlowGraph<Instruction> ReconstructCfg(ulong address)
        {
            // Initialize a control flow graph with a single node,
            // starting at the provided address.
            graph = new ControlFlowGraph<Instruction>();
            Node node = new Node(address.ToString("X"));
            var basicBlock = DisassembleBlock(address);
            node.UserData.Add(address.ToString("X"), basicBlock);
            graph.Nodes.Add(node);

            // Recursively follow all new paths.
            var edges = GetBlockEdges(basicBlock);
            foreach (var edge in edges)
                RecursiveHandleBlock(edge, node);
            return graph;
        }

        private BasicBlock<Instruction> DisassembleBlock(ulong address)
        {
            BasicBlock<Instruction> block = new BasicBlock<Instruction>();
            block.Address = address;
            while(true)
            {
                // Store the current instruction.
                var currInsn = dna.BinaryDisassembler.GetInstructionAt(address);
                block.Instructions.Add(currInsn);

                // If the instruction is a branch or termination, then we have reached
                // the end of the block.
                if (currInsn.FlowControl.IsBranch() || currInsn.FlowControl.IsRet())
                    return block;

                // Since we have not reached the end of the block,
                // we set the next address to disassemble at.
                address = currInsn.NextIP;
            }
        }

        private IEnumerable<ulong> GetBlockEdges(BasicBlock<Instruction> block)
        {
            List<ulong> edges = new List<ulong>();
            var exitInstruction = block.ExitInstruction;
            if(exitInstruction.FlowControl.IsBranch())
            {
                // If the jump destination can be resolved, then add it as an edge.
                if (exitInstruction.Op0Kind.IsImmediate())
                    edges.Add(exitInstruction.NearBranchTarget);

                // If we encounter a conditional instruction, then we add the not taken 
                // destination as an edge too.
                if (exitInstruction.FlowControl.IsConditional())
                    edges.Add(exitInstruction.NextIP);
            }

            return edges;
        }

        private Node RecursiveHandleBlock(ulong addrInitialBlock, Node source)
        {
            // If we have already traversed this block, then we add it as an edge
            // and return.
            var initialBlockIdentifier = addrInitialBlock.ToString("X");
            var existingNodes = graph.Nodes.Where(x => x.Name == initialBlockIdentifier);
            if(existingNodes.Any())
            {
                var existingNode = existingNodes.Single();
                if (!source.OutgoingEdges.Any(x => x.Target.Name == initialBlockIdentifier))
                    source.OutgoingEdges.Add(new Edge(source, existingNode));

                return existingNode;
            }

            // Extract basic block information.
            var basicBlock = DisassembleBlock(addrInitialBlock);
            var edges = GetBlockEdges(basicBlock);

            // Create a node for the block.
            var targetNode = new Node(initialBlockIdentifier);
            targetNode.UserData.Add(initialBlockIdentifier, basicBlock);
            graph.Nodes.Add(targetNode);
            graph.Edges.Add(source, targetNode);

            foreach(var addrOfEdge in edges)
            {
                var strEdgeAddr = addrOfEdge.ToString("X");
                Node edgeNode = null;
                existingNodes = graph.Nodes.Where(x => x.Name == strEdgeAddr);
                if (existingNodes.Count() == 1)
                    edgeNode = existingNodes.First();
                else if (existingNodes.Count() == 0)
                    edgeNode = RecursiveHandleBlock(addrOfEdge, targetNode);
                else
                    throw new InvalidOperationException(String.Format("Too many instances of node with name: {0}", strEdgeAddr));

                if (!targetNode.OutgoingEdges.Any(x => x.Target.Name == strEdgeAddr))
                    graph.Edges.Add(new Edge(targetNode, edgeNode));
            }

            return targetNode;
        }
    }
}
