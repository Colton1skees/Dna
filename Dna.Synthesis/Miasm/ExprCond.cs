using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprCond : MiasmExpr
    {
        public MiasmExpr Cond { get; }

        public MiasmExpr Src1 { get; }

        public MiasmExpr Src2 { get; }

        public ExprCond(MiasmExpr cond, MiasmExpr src1, MiasmExpr src2, uint size) : base(size)
        {
            if (src1.Size != src2.Size)
                throw new InvalidOperationException("Conditional sources must be of equal size.");
            Cond = cond;
            Src1 = src1;
            Src2 = src2;
        }
    }
}
