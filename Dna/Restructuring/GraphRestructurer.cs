using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Restructuring
{
    public class GraphRestructurer<T>
    {
        private readonly ControlFlowGraph<T> cfg;

        public GraphRestructurer(ControlFlowGraph<T> cfg)
        {
            this.cfg = cfg;
        }

        public void Restructure()
        {

        }
    }
}
