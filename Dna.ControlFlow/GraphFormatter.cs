using Dna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public static class GraphFormatter
    {
        /// <summary>
        /// Gets a string representation of a control flow graph.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static string FormatGraph<T>(ControlFlowGraph<T> graph)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(var node in graph.Nodes)
            {
                // Compute string representations of the outgoing edge addresses.
                var strEdges = "";
                foreach (var outgoingEdge in node.OutgoingEdges)
                    strEdges += outgoingEdge.Target.UserData.Keys.First() + ", ";

                // Format the output.
                stringBuilder.AppendLine(String.Format("Basic block {0} has the following outgoing edges: {1}", node.UserData.Keys.First(), strEdges));
                stringBuilder.AppendLine(FormatBlock(node.GetBlock<T>()));
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("");
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets a string representation of a basic block.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="block"></param>
        /// <returns></returns>
        public static string FormatBlock<T>(BasicBlock<T> block)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(String.Format("BasicBlock (0x{0}):", block.Address.ToString("X")));
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine();
                Console.WriteLine(instruction);
                stringBuilder.AppendLine(instruction?.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}
