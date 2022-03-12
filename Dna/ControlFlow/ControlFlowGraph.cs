using Dna.Extensions;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public class ControlFlowGraph<T> : Graph
    {
        public IEnumerable<T> GetInstructions()
        {
            return Nodes.SelectMany(x => x.GetBlock<T>().Instructions);
        }

        public override string ToString()
        {
            return GraphFormatter.FormatGraph(this);
        }
    }
}
