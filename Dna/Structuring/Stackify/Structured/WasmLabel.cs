using Dna.ControlFlow;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify.Structured
{
    public class WasmLabel
    {
        public BasicBlock<AbstractInst> Index { get; set; }

        public WasmLabel(BasicBlock<AbstractInst> index)
        {
            Index = index;  
        }
    }
}
