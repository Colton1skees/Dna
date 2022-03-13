using BAIL.IL.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Expression.Misc
{
    public class WriteMemExpression : ILExpression
    {
        public WriteMemExpression()
        {

        }

        public WriteMemExpression(ILOperand op0, ILOperand op1, ILOperand op2) : base(op0, op1, op2)
        {

        }
    }
}
