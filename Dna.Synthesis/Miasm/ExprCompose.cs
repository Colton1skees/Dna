using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprCompose : MiasmExpr
    {
        public List<MiasmExpr> Operands { get; }

        public ExprCompose(params MiasmExpr[] expressions) : this(expressions.ToList())
        {
 
        }

        public ExprCompose(List<MiasmExpr> expressions) : base((uint)expressions.Select(x => (int)x.Size).Sum())
        {
            Operands = expressions;
        }
    }
}
