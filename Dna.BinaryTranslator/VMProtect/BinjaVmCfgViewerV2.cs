using Dna.ControlFlow;
using Dna.BinaryTranslator.VMProtect.Rewrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VmCfg = Dna.ControlFlow.InstGraph<Dna.BinaryTranslator.VMProtect.VmHandler, Dna.BinaryTranslator.VMProtect.Rewrite.HandlerMetadata>;

namespace Dna.BinaryTranslator.VMProtect
{
    /// <summary>
    /// Produces a Binary Ninja Python script that displays a VM instruction graph.
    /// Each <see cref="VmHandler"/> in the graph is represented by its own flow graph node.
    /// </summary>
    public class BinjaVmCfgViewerV2
    {
        private readonly VmCfg cfg;

        public BinjaVmCfgViewerV2(VmCfg cfg)
        {
            this.cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        public string Run(Dictionary<ulong, HandlerData> handlerRipToRegisters, Dictionary<VmHandler, ulong> handlerToVkey)
        {
            var sb = new StringBuilder();
            var handlerToPythonVar = new Dictionary<VmHandler, string>();
            var handlers = cfg.Instructions.Keys.OrderBy(handler => handler).ToList();

            sb.AppendLine("graph = FlowGraph()");

            // InstGraph nodes are individual VM instructions, so create exactly one
            // Binary Ninja flow graph node for every handler in the graph.
            for (var i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                var metadata = cfg.Instructions[handler].Metadata;
                var nodeName = $"node_{i}";
                handlerToPythonVar.Add(handler, nodeName);

                var regs = handlerRipToRegisters[handler.NativeRip];
                sb.AppendLine($"{nodeName} = FlowGraphNode(graph)");
                AppendLine(sb, nodeName, $"Handler: {handler}");
                AppendLine(sb, nodeName, $"VIP Reg: {regs.Vip.Name.ToString()}");
                AppendLine(sb, nodeName, $"Native RIP: 0x{handler.NativeRip:X}");
                if (regs.Vkey != null)
                    AppendLine(sb, nodeName, $"VKEY Reg: {regs.Vkey.Name.ToString()}");
                if (handlerToVkey.ContainsKey(handler))
                    AppendLine(sb, nodeName, $"VKEY: 0x{handlerToVkey[handler]:X}");
                AppendLine(sb, nodeName, $"Complete: {metadata.IsComplete}");
                sb.AppendLine($"graph.append({nodeName})");
            }

            foreach (var handler in handlers)
            {
                var sourceNode = handlerToPythonVar[handler];
                var successors = cfg.Instructions[handler].Successors
                    .OrderBy(successor => successor)
                    .ToList();

                if (successors.Count == 1)
                {
                    var targetNode = handlerToPythonVar[successors[0]];
                    sb.AppendLine($"{sourceNode}.add_outgoing_edge(BranchType.UnconditionalBranch, {targetNode})");
                    continue;
                }

                // InstGraph stores successors in a HashSet and does not retain branch
                // polarity. Use UserDefinedBranch for every multi-way edge rather than
                // incorrectly labeling arbitrary successors as true and false branches.
                foreach (var successor in successors)
                {
                    var targetNode = handlerToPythonVar[successor];
                    sb.AppendLine($"{sourceNode}.add_outgoing_edge(BranchType.UserDefinedBranch, {targetNode})");
                }
            }

            sb.AppendLine("show_graph_report(\"VM Instruction CFG\", graph)");
            return sb.ToString();
        }

        private static void AppendLine(StringBuilder sb, string nodeName, string text)
        {
            sb.AppendLine($"{nodeName}.lines = {nodeName}.lines + ['{EscapePythonString(text)}']");
        }

        private static string EscapePythonString(string text)
        {
            return text
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
        }
    }
}
