using Dna.Extensions;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class BlockEdge<T> : Edge
    {
        public BasicBlock<T> SourceBlock => Source.GetBlock<T>();

        public BasicBlock<T> TargetBlock => Target.GetBlock<T>();

        public BlockEdge(BasicBlock<T> source, BasicBlock<T> target) : base(source, target)
        {

        }
    }
}
