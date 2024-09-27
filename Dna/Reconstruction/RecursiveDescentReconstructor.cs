using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Relocation;
using Iced.Intel;
using Rivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

        public ControlFlowGraph<Instruction> ReconstructCfg(ulong address, Func<BasicBlock<Instruction>, IEnumerable<ulong>> pGetOutgoingEdges = null, IEnumerable<ulong> sehExceptBlockAddresses = null)
        {
            // Initialize a control flow graph with a single node,
            // starting at the provided address.
            graph = new ControlFlowGraph<Instruction>(address);

            // Apply recursive descent at the block start.
            RecursivelyDescend(graph, address, pGetOutgoingEdges);

            // For each SEH '__except' block, add it to the control flow graph
            // with no incoming edges. This is necessary since `__except` are valid parts of a control flow graph,
            // but they are often not reachable through recursive descent.
            foreach(var addr in sehExceptBlockAddresses ?? Enumerable.Empty<ulong>())
                RecursivelyDescend(graph, addr, pGetOutgoingEdges);

            return graph;
        }

        // Apply recursive descent starting at the provided address.
        private void RecursivelyDescend(ControlFlowGraph<Instruction> graph, ulong addr, Func<BasicBlock<Instruction>, IEnumerable<ulong>> pGetOutgoingEdges)
        {
            // Skip if the __except block was already added to the cfg.
            if (graph.Nodes.Contains(addr.ToString("X")))
                return;

            // Disassemble the block.
            var basicBlock = DisassembleBlock(graph, addr);

            // Recursively follow all new paths.
            var edges = GetBlockEdges(basicBlock, pGetOutgoingEdges);
            foreach (var edge in edges)
                RecursiveHandleBlock(graph, edge, basicBlock, pGetOutgoingEdges);
        }

        private BasicBlock<Instruction> DisassembleBlock(ControlFlowGraph<Instruction> graph, ulong address)
        {
            BasicBlock<Instruction> block = graph.CreateBlock(address);
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

        private IEnumerable<ulong> GetBlockEdges(BasicBlock<Instruction> block, Func<BasicBlock<Instruction>, IEnumerable<ulong>> pGetOutgoingEdges = null)
        {
            List<ulong> edges = new List<ulong>();
            var exitInstruction = block.ExitInstruction;
            if(exitInstruction.FlowControl.IsBranch())
            {
                // If the jump destination can be resolved, then add it as an edge.
                if (exitInstruction.Op0Kind.IsImmediate())
                    edges.Add(exitInstruction.GetImmediate(0));
                else if (exitInstruction.Op0Kind.IsBranchOpKind())
                    edges.Add(exitInstruction.GetBranchTarget(exitInstruction.Op0Kind));

                // If we encounter a conditional instruction, then we add the not taken 
                // destination as an edge too.
                if (exitInstruction.FlowControl.IsConditional())
                    edges.Add(exitInstruction.NextIP);
            }

            if(edges.Count == 0 && pGetOutgoingEdges != null)
            {
                var learnedEdges = pGetOutgoingEdges(block);
                return learnedEdges == null ? new List<ulong>() : learnedEdges;
            }

            return edges;
        }

        private Node RecursiveHandleBlock(ControlFlowGraph<Instruction> graph, ulong addrInitialBlock, Node source, Func<BasicBlock<Instruction>, IEnumerable<ulong>> pGetOutgoingEdges = null)
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
            var basicBlock = DisassembleBlock(graph, addrInitialBlock);
            var edges = GetBlockEdges(basicBlock, pGetOutgoingEdges);

            // Create a node for the block.
            var targetNode = basicBlock;
            graph.Edges.Add(source, basicBlock);

            foreach(var addrOfEdge in edges)
            {
                var strEdgeAddr = addrOfEdge.ToString("X");
                Node edgeNode = null;
                existingNodes = graph.Nodes.Where(x => x.Name == strEdgeAddr);
                if (existingNodes.Count() == 1)
                    edgeNode = existingNodes.First();
                else if (existingNodes.Count() == 0)
                    edgeNode = RecursiveHandleBlock(graph,addrOfEdge, targetNode, pGetOutgoingEdges);
                else
                    throw new InvalidOperationException(String.Format("Too many instances of node with name: {0}", strEdgeAddr));

                if (!targetNode.OutgoingEdges.Any(x => x.Target.Name == strEdgeAddr))
                    graph.Edges.Add(new Edge(targetNode, edgeNode));
            }

            return targetNode;
        }
    }
}
