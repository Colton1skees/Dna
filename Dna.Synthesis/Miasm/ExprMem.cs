using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprMem : MiasmExpr
    {
        public MiasmExpr Ptr { get; }

        public ExprMem(MiasmExpr ptr, uint size) : base(size)
        {
            Ptr = ptr;
        }
    }
}
