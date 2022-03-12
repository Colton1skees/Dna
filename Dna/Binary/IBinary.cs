using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary
{
    public interface IBinary
    {
        /// <summary>
        /// Gets the bitness of the binary(i.e 32 or 64).
        /// </summary>
        public int Bitness { get; }

        /// <summary>
        /// Gets or sets the bytes used internally when interfacing with the binary.
        /// </summary>
        public byte[] Bytes { get; set; }
        
        /// <summary>
        /// Gets the base address of the binary.
        /// </summary>
        public ulong BaseAddress { get; }

        /// <summary>
        /// Reads the specified number of bytes at the provided address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        byte[] ReadBytes(ulong address, int count = 15);


        /// <summary>
        /// Writes the provided bytes at the input address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        void WriteBytes(ulong address, byte[] bytes);
    }
}
