using BAIL.IL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Operand
{
    public class IntegerOperand : ILOperand
    {
        public bool Bool => (bool)Value;

        public byte U8 => (byte)Value;

        public sbyte I8 => (sbyte)Value;

        public UInt16 U16 => (UInt16)Value;

        public Int16 I16 => (Int16)Value;

        public UInt32 U32 => (UInt32)Value;

        public Int32 I32 => (Int32)Value;

        public UInt64 U64 => (UInt64)Value;

        public Int64 I64 => (Int64)Value;

        public IntegerOperand(bool value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(byte value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(sbyte value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(UInt16 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(Int16 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(UInt32 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(Int32 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(UInt64 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }

        public IntegerOperand(Int64 value, int size) : base(value)
        {
            ILType = new IntegerType(size);
        }
    }
}
