using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.Structured
{
    public class Block : WasmBlock
    {
        public List<WasmBlock> Body { get; set; } = new();

        public BasicBlock<AbstractInst> Out { get; set; }
    }
}
