using Dna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.X86
{
    public class FastcallConvention : ICallingConvention
    {
        public IReadOnlySet<string> SavedRegisters { get; }

        public IReadOnlySet<string> ClobberedRegisters { get; }

        public FastcallConvention()
        {
            SavedRegisters = new HashSet<string>() { "RBX", "RBP", "RDI", "RSI", "RSP", "R12", "R13", "R14", "R15" }.AsReadOnly();
            ClobberedRegisters = new HashSet<string>() { "RAX", "RCX", "RDX", "R8", "R9", "R10", "R11" }.AsReadOnly();
        }
    }
}
