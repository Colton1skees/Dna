using BAIL.IL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Operand
{
    public abstract class ILOperand
    {
        /// <summary>
        /// Gets or sets the internal value of the operand.
        /// </summary>
        public object Value { get; protected set; }

        /// <summary>
        /// Gets or sets the type of the operand.
        /// </summary>
        public ILType ILType { get; protected set; }

        public ILOperand(object value)
        {
            Value = value;
        }
    }
}
