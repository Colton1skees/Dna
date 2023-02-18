using Dna.Symbolic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Emulation.Symbolic
{
    public class SymbolicEmulator : ICpuEmulator
    {
        private readonly SymbolicExecutionEngine engine;

        public SymbolicEmulator()
        {
            engine = new SymbolicExecutionEngine();
        }

        public ulong GetRegister(register_e regId)
        {
            throw new NotImplementedException();
        }

        public void MapMemory(ulong addr, int size)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadMemory(ulong addr, int size)
        {
            throw new NotImplementedException();
        }

        public void SetMemoryReadCallback(dgOnMemoryRead callback)
        {
            throw new NotImplementedException();
        }

        public void SetMemoryWriteCallback(dgOnMemoryWrite callback)
        {
            throw new NotImplementedException();
        }

        public void SetRegister(register_e regId, ulong value)
        {
            throw new NotImplementedException();
        }

        public void WriteMemory(ulong addr, byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
