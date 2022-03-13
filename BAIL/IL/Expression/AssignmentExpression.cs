using BAIL.IL.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Expression
{
    public abstract class AssignmentExpression : ILExpression
    {
        public virtual ILOperand Dest { get; set; }

        public AssignmentExpression(ILOperand dest, ILOperand op0, ILOperand op1 = null, ILOperand op2 = null) : base(op0, op1, op2)
        {
            Dest = dest;
        }
    }
}
