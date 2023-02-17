using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Emulation
{
    public interface ICpuEmulator
    {
        /// <summary>
        /// Gets the register value.
        /// </summary>
        /// <returns></returns>
        public ulong GetRegister(register_e regId);

        /// <summary>
        /// Sets the register value.
        /// </summary>
        public void SetRegister(register_e regId, ulong value);

        /// <summary>
        /// Maps a memory page at the provided address.
        /// </summary>
        public void MapMemory(ulong addr, int size);

        /// <summary>
        /// Reads memory contents at the provided address.
        /// </summary>
        public T ReadMemory<T>(ulong addr)
        {
            var size = MarshalType<T>.Size;
            var buffer = ReadMemory(addr, size);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }

        /// <summary>
        /// Reads memory contents at the provided address.
        /// </summary>
        public byte[] ReadMemory(ulong addr, int size);

        /// <summary>
        /// Writes memory contents at the provided address.
        /// </summary>
        public void WriteMemory<T>(ulong addr, T value)
        {
            var buffer = MarshalType<T>.ObjectToByteArray(value);
            WriteMemory(addr, buffer);
        }

        /// <summary>
        /// Writes memory contents at the provided address.
        /// </summary>
        public void WriteMemory(ulong addr, byte[] value);
    }
}
