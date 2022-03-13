using BAIL.IL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Operand
{
    public class FloatOperand : ILOperand
    {
        public double F32 => (float)Value;

        public double F64 => (double)Value;

        public FloatOperand(double value) : base(value)
        {
            ILType = new FloatType();
        }

        public FloatOperand(float value) : base(value)
        {
            ILType = new FloatType();
        }
    }
}
