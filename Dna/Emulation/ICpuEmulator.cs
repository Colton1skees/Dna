using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Emulation
{
    public delegate void dgOnMemoryRead(ulong address, int size, ulong value);

    public delegate void dgOnMemoryWrite(ulong address, int size, ulong value);

    public delegate void dgOnInstExecuted(ulong address, int size);

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
        public T ReadMemory<T>(ulong addr);

        /// <summary>
        /// Reads memory contents at the provided address.
        /// </summary>
        public byte[] ReadMemory(ulong addr, int size);

        /// <summary>
        /// Writes memory contents at the provided address.
        /// </summary>
        public void WriteMemory<T>(ulong addr, T value);

        /// <summary>
        /// Writes memory contents at the provided address.
        /// </summary>
        public void WriteMemory(ulong addr, byte[] value);

        /// <summary>
        /// Sets a callback which will be invoked on each memory read.
        /// </summary>
        /// <param name="callback"></param>
        public void SetMemoryReadCallback(dgOnMemoryRead callback);

        /// <summary>
        /// Sets a callback which will be invoked on each memory write.
        /// </summary>
        /// <param name="callback"></param>
        public void SetMemoryWriteCallback(dgOnMemoryWrite callback);

        /// <summary>
        /// Sets a callback which will be invoked on each instruction execution.
        /// </summary>
        /// <param name="callback"></param>
        public void SetInstExecutedCallback(dgOnInstExecuted callback);
    }
}
