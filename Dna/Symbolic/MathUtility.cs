using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Symbolic
{
    public static class MathUtility
    {
        public static ulong GetMaxValue(uint bitSize)
        {
            switch (bitSize)
            {
                case 8:
                    return (ulong)sbyte.MaxValue;
                case 16:
                    return (ulong)short.MaxValue;
                case 32:
                    return (ulong)int.MaxValue;
                case 64:
                    return (ulong)long.MaxValue;
                default:
                    throw new InvalidOperationException();
            }
        }

        public static ulong GetMinValue(uint bitSize)
        {
            unchecked
            {
                switch (bitSize)
                {
                    case 8:
                        return (ulong)sbyte.MinValue;
                    case 16:
                        return (ulong)short.MinValue;
                    case 32:
                        return (ulong)int.MinValue;
                    case 64:
                        return (ulong)long.MinValue;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
    }
}
