using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Structuring
{
    /// <summary>
    /// The following algorithm is a reimplementation from the paper:
    /// - "A Comb for Decompiled C Code (2020)"
    /// - https://rev.ng/downloads/asiaccs-2020-paper.pdf
    /// </summary>
    public class ControlFlowGraphRestructurer<T>
    {
        private readonly ControlFlowGraph<T> graph;

        public ControlFlowGraphRestructurer(ControlFlowGraph<T> graph)
        {
            this.graph = graph;
        }
    }
}
