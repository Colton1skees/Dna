using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Structuring.Stackify.Structured
{
    public class Select : WasmBlock
    {
        public IOperand Selector { get; set; }

        public List<WasmLabel> Targets { get; set; } = new();

        public WasmLabel Default { get; set; }
    }
}
