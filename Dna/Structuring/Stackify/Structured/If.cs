using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Structuring.Stackify.Structured
{
    public class If : WasmBlock
    {
        public IOperand Cond { get; set; }

        public List<WasmBlock> IfTrue { get; set; } = new();

        public List<WasmBlock> IfFalse { get; set; } = new();
    }
}
