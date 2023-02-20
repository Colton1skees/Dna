using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.Structured
{
    public class Leaf : WasmBlock
    {
        public BasicBlock<AbstractInst> Block { get; set; }
    }
}
