using DotNetGraph;
using DotNetGraph.Edge;
using DotNetGraph.Node;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public static class GraphVisualizer
    {
        /// <summary>
        /// Gets a graphviz representation of the control flow graph.
        /// </summary>
        public static DotGraph GetDotGraph<T>(ControlFlowGraph<T> graph)
        {
            // Create a dotviz block for each basic block.
            var dotGraph = new DotGraph(graph.Name, true);
            var blockMapping = new Dictionary<BasicBlock<T>, DotNode>();
            foreach(var block in graph.GetBlocks())
            {
                var node = GetDotNode(block);
                blockMapping.Add(block, node);
                dotGraph.Elements.Add(node);
            }

            // Create dotviz edges between basic blocks.
            foreach(var block in graph.GetBlocks())
            {
                var outgoingEdges = block.GetOutgoingEdges().ToList();
                for(int i = 0; i < outgoingEdges.Count; i++)
                {
                    var blockEdge = outgoingEdges[i];
                    var lineColor = GetEdgeColor(i, outgoingEdges.Count);
                    var dotEdge = GetDotEdge(blockMapping[blockEdge.TargetBlock], blockMapping[blockEdge.SourceBlock], lineColor);
                    dotGraph.Elements.Add(dotEdge);
                }
            }

            return dotGraph;
        }

        private static DotNode GetDotNode<T>(BasicBlock<T> block)
        {
            return new DotNode(block.Name)
            {
                Shape = DotNodeShape.Box,
                Label = GraphFormatter.FormatBlock(block),
                Color = Color.FromArgb(255, 66, 66, 66),
                FontColor = Color.Black,
            };
        }

        /// <summary>
        /// Gets a line color for the provided edge.
        /// </summary>
        private static Color GetEdgeColor(int index, int count)
        {
            var thenColor = Color.Green;
            var elseColor = Color.Red;
            var switchColor = Color.LightBlue;

            if (count > 2)
                return switchColor;
            if (index == 0)
                return thenColor;
            else
                return elseColor;
        }

        private static DotEdge GetDotEdge(DotNode from, DotNode to, Color lineColor)
        {
            return new DotEdge(to, from)
            {
                ArrowHead = DotEdgeArrowType.Normal,
                ArrowTail = DotEdgeArrowType.None,
                Color = lineColor,
                Label = String.Empty,
                Style = DotEdgeStyle.Solid,
            };
        }
    }
}
