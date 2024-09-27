using Dna.ControlFlow;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Color = Microsoft.Msagl.Drawing.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Node = Microsoft.Msagl.Drawing.Node;
using Microsoft.Msagl.GraphViewerGdi;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;

namespace Dna.Example
{
    public class ToGraph<T>
    {
        private readonly ControlFlowGraph<T> cfg;

        private readonly Graph graph;

        private readonly HashSet<BasicBlock<T>> visited = new();

        public ToGraph(ControlFlowGraph<T> cfg)
        {
            this.cfg = cfg;
            graph = new Graph();
            Traverse(cfg.GetBlocks().First().GetOutgoingEdges().First().TargetBlock);
            graph.Attr.LayerDirection = LayerDirection.TB;

        }

        public void Traverse(BasicBlock<T> block)
        {
            var q = new Queue<BasicBlock<T>>();
            q.Enqueue(block);
            while(q.Count > 0)
            {
                var b = q.Dequeue();
                if (visited.Contains(b))
                    continue;
                visited.Add(b);
                visited.Add(b);
            }
        }

        private Node CreateGraphNode(BasicBlock<T> block)
        {
            var node = graph.AddNode(block.Name);
            node.Attr.LabelMargin = 5;
            node.UserData = block;
            node.Label.FontName = "Lucida Console";
            node.Label.FontSize = 10f;
            // TODO: Use actual name.
            node.LabelText = block.Name;

            return node;
        }

        public  void GetGraph<T>(ControlFlowGraph<T> cfg)
        {
            var newGraph = new Graph
            {
                LayoutAlgorithmSettings =
                {
                    PackingMethod = PackingMethod.Columns,
                    EdgeRoutingSettings = new EdgeRoutingSettings
                    {
                        EdgeRoutingMode = EdgeRoutingMode.RectilinearToCenter,
                        CornerRadius = 5.0,
                    },
                    
                },
                Attr =
                {
                    BackgroundColor = Color.Transparent,
                    LayerDirection = LayerDirection.TB, // prefer top-to-bottom layout
                },
            };


            var nodeMapping = new Dictionary<BasicBlock<T>, Node>();
            foreach(var block in cfg.GetBlocks())
            {
                var newNode = new Node(block.Name)
                {
                    LabelText = GraphFormatter.FormatBlock(block),
                    UserData = block.Name
                };

                nodeMapping.Add(block, newNode);
                newGraph.AddNode(newNode);
            }

            foreach(var edge in cfg.Edges.Reverse())
            {
                var newEdge = newGraph.AddEdge(edge.Source.Name, edge.Target.Name);
            }

            GraphRenderer gr = new GraphRenderer(newGraph);
            gr.CalculateLayout();
            var bitmap = new Bitmap((int)newGraph.Width, (int)newGraph.Height);
            gr.Render(bitmap); 
            bitmap.Save(Path.Combine(Directory.GetCurrentDirectory(), @"example.png"), ImageFormat.Png);
            Debugger.Break();

        }
    }
}
