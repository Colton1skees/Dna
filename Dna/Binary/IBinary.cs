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
        /// Returns whether the data at the specified address is constant.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        bool IsConstantData(ulong address);

        /// <summary>
        /// Gets a byte at the specified address.
        /// </summary>
        byte ReadByte(ulong address) => ReadBytes(address, 1)[0];

        ushort ReadUint16(ulong address) => BitConverter.ToUInt16(new byte[] { ReadByte(address), ReadByte(address + 1) });

        uint ReadUint32(ulong address) => BitConverter.ToUInt32(ReadBytes(address, 4));

        /// <summary>
        /// Reads the specified number of bytes at the provided address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        byte[] ReadBytes(ulong address, int count = 15);

        /// <summary>
        /// Gets a ushort at the specified address.
        /// </summary>
        ushort ReadUShort(ulong address) => BitConverter.ToUInt16(ReadBytes(address, 2));

        /// <summary>
        /// Writes the provided bytes at the input address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        void WriteBytes(ulong address, byte[] bytes);
    }
}
