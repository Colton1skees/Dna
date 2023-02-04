using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprCompose : Expr
    {
        public List<Expr> Operands { get; }

        public ExprCompose(params Expr[] expressions) : this(expressions.ToList())
        {
 
        }

        public ExprCompose(List<Expr> expressions) : base((uint)expressions.Select(x => (int)x.Size).Sum())
        {
            Operands = expressions;
        }
    }
}
