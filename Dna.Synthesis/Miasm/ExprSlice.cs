using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprSlice : MiasmExpr
    {
        public MiasmExpr Src { get; }

        public uint Start { get; }

        public uint Stop { get; }

        // TODO: Handle endianness.
        public ExprSlice(MiasmExpr src, uint start, uint stop) : base(stop - start)
        {
            Src = src;
            Start = start;
            Stop = stop;
        }
    }
}
