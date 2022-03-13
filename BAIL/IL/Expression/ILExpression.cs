using BAIL.IL.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Expression
{
    public abstract class ILExpression
    {
        public List<ILOperand> Operands { get; set; } = new List<ILOperand>();

        public ILOperand Op0
        {
            get => Operands[0];
            set => Operands[0] = value;
        }

        public ILOperand Op1
        {
            get => Operands[1];
            set => Operands[1] = value;
        }

        public ILOperand Op2
        {
            get => Operands[2];
            set => Operands[2] = value;
        }

        public ILExpression()
        {

        }

        public ILExpression(ILOperand op0, ILOperand op1, ILOperand op2)
        {
            Op0 = op0;
            Op1 = op1;
            Op2 = op2;
        }
    }
}
