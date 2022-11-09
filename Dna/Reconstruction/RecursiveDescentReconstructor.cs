using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Relocation;
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

        private readonly Assembler assembler;

        private ControlFlowGraph<Instruction> graph;

        public RecursiveDescentReconstructor(IDna dna)
        {
            assembler = new Assembler(dna.Binary.Bitness);
            this.dna = dna;
        }

        public ControlFlowGraph<Instruction> ReconstructCfg(ulong address)
        {
            // Initialize a control flow graph with a single node,
            // starting at the provided address.
            graph = new ControlFlowGraph<Instruction>(address);
            var basicBlock = DisassembleBlock(graph, address);

            // Recursively follow all new paths.
            var edges = GetBlockEdges(basicBlock);
            foreach (var edge in edges)
                RecursiveHandleBlock(graph, edge, basicBlock);

            // Collapse pairs of duplicated blocks into a single block.
            RemoveDuplicatedBlocks();
            return graph;
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

        private IEnumerable<ulong> GetBlockEdges(BasicBlock<Instruction> block)
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

            return edges;
        }

        private Node RecursiveHandleBlock(ControlFlowGraph<Instruction> graph, ulong addrInitialBlock, Node source)
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
            var edges = GetBlockEdges(basicBlock);

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
                    edgeNode = RecursiveHandleBlock(graph,addrOfEdge, targetNode);
                else
                    throw new InvalidOperationException(String.Format("Too many instances of node with name: {0}", strEdgeAddr));

                if (!targetNode.OutgoingEdges.Any(x => x.Target.Name == strEdgeAddr))
                    graph.Edges.Add(new Edge(targetNode, edgeNode));
            }

            return targetNode;
        }

        private void RemoveDuplicatedBlocks()
        {
            // Get a list of all basic blocks.
            var blocks = graph.GetBlocks();

            // Group the basic blocks via their exit instruction's address.
            var blockGrouping = blocks
                .GroupBy(x => x.ExitInstruction.IP)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach(var group in blockGrouping.Where(x => x.Value.Count > 1))
            {
                // Throw if the block was duplicated more than once.
                // TODO: Refactor this to handle an infinite amount of block duplications.
                if (group.Value.Count > 2)
                    throw new InvalidOperationException("Basic blocks with more than a single duplication cannot be collapsed.");

                // Identify the source block which was copied from, along with the block
                // which contains the copy.
                var blockWithCopy = group.Value.MaxBy(x => x.Instructions.Count);
                var originalBlock = group.Value.Single(x => x != blockWithCopy);

                // Remove all duplicated instructions from the block.
                var startIndex = blockWithCopy.Instructions.IndexOf(originalBlock.EntryInstruction);
                blockWithCopy.Instructions.RemoveRange(startIndex, blockWithCopy.Instructions.Count - startIndex);

                // Update the block exit instruction.
                var jmpInstIP = blockWithCopy.ExitInstruction.NextIP;
                assembler.jmp(jmpInstIP);
                var jmpInst = InstructionRelocator.RelocateInstructions(new List<Instruction>() { assembler.Instructions.Last() }, jmpInstIP).Single();

                // Insert a jump to the copied from block, then update the edges.
                blockWithCopy.Instructions.Add(jmpInst);
                blockWithCopy.OutgoingEdges.Clear();
                blockWithCopy.OutgoingEdges.Add(new Edge(blockWithCopy, originalBlock));

                // Collect any previously existing circular references.
                var edgesToDelete = blockWithCopy.IncomingEdges
                    .Where(x => x.Source == originalBlock && x.Target == originalBlock)
                    .ToList();

                // Delete all circular references.
                foreach(var edge in edgesToDelete)
                    blockWithCopy.IncomingEdges.Remove(edge);
            }
        }

        
    }
}
