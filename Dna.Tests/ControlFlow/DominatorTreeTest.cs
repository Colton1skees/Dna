using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.ControlFlow.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Tests.ControlFlow
{
    [TestClass]
    public class DominatorTreeTest
    {
        [TestMethod]
        public void If()
        {
            var graph = new Graph();
            var n0 = new Node("0");
            var n1 = new Node("1");
            var n2 = new Node("2");
            var n3 = new Node("3");
            var n4 = new Node("4");

            graph.Nodes.Add(n0);
            graph.Nodes.Add(n1);
            graph.Nodes.Add(n2);
            graph.Nodes.Add(n3);
            graph.Nodes.Add(n4);

            n0.OutgoingEdges.Add(n1);
            n1.OutgoingEdges.Add(n2);
            n1.OutgoingEdges.Add(n3);
            n2.OutgoingEdges.Add(n4);
            n3.OutgoingEdges.Add(n4);

            var analysis = new DominatorAnalysis(graph);
            var tree = analysis.GetDominatorTree();

            var dictionary = graph.Nodes.Select(x => new KeyValuePair<Node, HashSet<Node>>(x, new HashSet<Node>()));
            var expectedOutput = new Dictionary<Node, HashSet<Node>>(dictionary);
            expectedOutput[n0].AddRange(n0);
            expectedOutput[n1].AddRange(n0, n1);
            expectedOutput[n2].AddRange(n0, n1, n2);
            expectedOutput[n3].AddRange(n0, n1, n2);
        }
    }
}
