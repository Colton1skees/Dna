using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprMem : Expr
    {
        public Expr Ptr { get; }

        public ExprMem(Expr ptr, uint size) : base(size)
        {
            Ptr = ptr;
        }
    }
}
