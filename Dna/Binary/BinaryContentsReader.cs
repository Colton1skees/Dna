using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary
{
    public static class BinaryContentsReader
    {
        public static ulong Dereference(IBinary binary, ulong address, uint bitSize)
        {
            var bytes = binary.ReadBytes(address, (int)bitSize / 8);
            var value = bitSize switch
            {
                8 => bytes[0],
                16 => BitConverter.ToUInt16(bytes),
                32 => BitConverter.ToUInt32(bytes),
                64 => BitConverter.ToUInt64(bytes),
                _ => throw new InvalidOperationException()
            };

            return value;
        }
    }
}
