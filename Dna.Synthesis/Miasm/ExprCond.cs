using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprCond : Expr
    {
        public Expr Cond { get; }

        public Expr Src1 { get; }

        public Expr Src2 { get; }

        public ExprCond(Expr cond, Expr src1, Expr src2, uint size) : base(size)
        {
            if (src1.Size != src2.Size)
                throw new InvalidOperationException("Conditional sources must be of equal size.");
            Cond = cond;
            Src1 = src1;
            Src2 = src2;
        }
    }
}
