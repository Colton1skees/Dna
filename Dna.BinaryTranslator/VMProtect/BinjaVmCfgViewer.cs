using Dna.ControlFlow;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public class BinjaVmCfgViewer
    {
        private readonly StringBuilder sb = new();

        private readonly ControlFlowGraph<VmHandler> cfg;

        private Dictionary<BasicBlock<VmHandler>, string> blockToPythonVar = new();

        public BinjaVmCfgViewer(ControlFlowGraph<VmHandler> cfg)
        {
            this.cfg = cfg;
        }

        public string Run()
        {
            // Convert the blocks to strings.
            sb.AppendLine("graph = FlowGraph()");
            int i = 0;
            foreach (var block in cfg.GetBlocks())
            {
                var blockName = $"node_{i}";
                blockToPythonVar.Add(block, blockName);

                sb.AppendLine($"{blockName} = FlowGraphNode(graph)");

                sb.AppendLine($"{blockName}.lines = {blockName}.lines + ['Block: {block.Name}']");
                foreach(var inst in block.Instructions)
                    sb.AppendLine($"{blockName}.lines = {blockName}.lines + ['    {inst}']");

                sb.AppendLine($"graph.append({blockName})");
                i++;
            }

            // Add the edges.
            foreach (var block in cfg.GetBlocks())
            {
                var blkName = blockToPythonVar[block];
                var outgoingEdges = block.GetOutgoingEdges().ToList();

                if (outgoingEdges.Count == 0)
                {
                    // do nothing
                }

                else if (outgoingEdges.Count == 1)
                {
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.UnconditionalBranch, {blockToPythonVar[outgoingEdges.Single().TargetBlock]})");
                }

                else if (outgoingEdges.Count == 2)
                {
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.TrueBranch, {blockToPythonVar[outgoingEdges[0].TargetBlock]})");
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.FalseBranch, {blockToPythonVar[outgoingEdges[1].TargetBlock]})");
                }

                else
                {
                    throw new InvalidOperationException($"TODO: Switch!");
                }
            }

            sb.AppendLine($"show_graph_report(\"Custom Graph\", graph)");
            return sb.ToString();
        }
    }
}