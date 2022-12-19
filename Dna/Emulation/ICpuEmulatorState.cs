using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Emulation
{
    public interface ICpuEmulatorState
    {
        public ulong GetRegister(register_e regId);

        public void SetRegister(register_e regId, ulong value);

        public T ReadMemory<T>(ulong addr);

        public void WriteMemory<T>(ulong addr, T value);

        public void WriteMemory(ulong addr, byte[] value);
    }
}
