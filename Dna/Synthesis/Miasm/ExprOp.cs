using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprOp : Expr
    {
        public List<ExprOp> Operands { get; }

        public ExprOp(uint size, string op, params ExprOp[] operands) : this(size, op, operands.ToList())
        {

        }

        public ExprOp(uint size, string op, List<ExprOp> operands) : base(size)
        {
            Operands = operands;
            if (operands.Any(x => x.Size != operands.FirstOrDefault()?.Size))
                throw new InvalidOperationException("Operation sizes must be equal.");
        }
    }
}
