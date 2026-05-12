using Dna.ControlFlow;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extensions
{
    public static class ControlFlowExtensions
    {
        public static BasicBlock<T> GetBlock<T>(this Node node)
        {
            return (BasicBlock<T>)node.UserData.Values.First();
        }

        public static void SetBlock<T>(this Node node, BasicBlock<T> block)
        {
            node.UserData.Clear();
            node.UserData.Add(block.Address.ToString("X"), block);
        }

        public static BlockEdge<T> ToBlockEdge<T>(this Edge edge)
        {
            var source = (BasicBlock<T>)edge.Source;
            var target = (BasicBlock<T>)edge.Target;
            return new BlockEdge<T>(source, target);
        }
    }
}
