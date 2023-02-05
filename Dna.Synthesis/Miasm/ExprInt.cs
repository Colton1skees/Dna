using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprInt : MiasmExpr
    {
        public ulong Value { get; }

        public ExprInt(ulong value, uint size) : base(size)
        {
            Value = value;
        }
    }
}
