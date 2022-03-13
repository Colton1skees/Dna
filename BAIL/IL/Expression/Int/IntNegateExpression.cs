using BAIL.IL.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Expression.Int
{
    internal class IntNegateExpression : AssignmentExpression
    {
        public IntNegateExpression(ILOperand dest, ILOperand op0, ILOperand op1 = null, ILOperand op2 = null) : base(dest, op0, op1, op2)
        {
        }
    }
}
