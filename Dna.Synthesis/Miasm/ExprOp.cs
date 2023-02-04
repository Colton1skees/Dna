using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprOp : Expr
    {
        public string Op { get; }

        public List<Expr> Operands { get; }

        public ExprOp(uint size, string op, params Expr[] operands) : this(size, op, operands.ToList())
        {

        }

        public ExprOp(uint size, string op, List<Expr> operands) : base(size)
        {
            Op = op;
            Operands = operands;
            if (operands.Any(x => x.Size != operands.FirstOrDefault()?.Size))
                throw new InvalidOperationException("Operation sizes must be equal.");
        }
    }
}
