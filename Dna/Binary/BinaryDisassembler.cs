using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary
{
    public class BinaryDisassembler
    {
        private readonly IBinary binary;

        public BinaryDisassembler(IBinary binary)
        {
            this.binary = binary;
        }

        /// <summary>
        /// Reads assembly bytes at the provided address,
        /// and disassembles them into a single iced instruction.
        /// </summary>
        /// <param name="address">The address to start reading from.</param>
        /// <returns></returns>
        public Instruction GetInstructionAt(ulong address)
        {
            var bytes = binary.ReadBytes(address);
            return GetInstructionFromBytes(address, bytes);
        }

        /// <summary>
        /// Disassembles a collection of bytes into a single iced instruction.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public Instruction GetInstructionFromBytes(ulong address, byte[] bytes)
        {
            var codeReader = new ByteArrayCodeReader(bytes);
            var decoder = Iced.Intel.Decoder.Create(binary.Bitness, codeReader);
            decoder.IP = address;
            return decoder.Decode();
        }
    }
}
