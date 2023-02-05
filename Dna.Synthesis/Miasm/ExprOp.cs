using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprOp : MiasmExpr
    {
        public string Op { get; }

        public List<MiasmExpr> Operands { get; }

        public ExprOp(uint size, string op, params MiasmExpr[] operands) : this(size, op, operands.ToList())
        {

        }

        public ExprOp(uint size, string op, List<MiasmExpr> operands) : base(size)
        {
            Op = op;
            Operands = operands;
            if (operands.Any(x => x.Size != operands.FirstOrDefault()?.Size))
                throw new InvalidOperationException("Operation sizes must be equal.");
        }
    }
}
