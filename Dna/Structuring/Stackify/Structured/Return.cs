using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Structuring.Stackify.Structured
{
    public class Return : WasmBlock
    {
        public List<IOperand> Values { get; set; } = new();
    }
}
