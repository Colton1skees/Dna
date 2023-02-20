using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.Structured
{
    public class Loop : WasmBlock
    {
        public List<WasmBlock> Body { get; set; } = new();

        public BasicBlock<AbstractInst> Header { get; set; }
    }
}
