using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator
{
    public interface ICallingConvention
    {
        /// <summary>
        /// Gets the set of registers preserved by the calling convention.
        /// </summary>
        /// <returns></returns>
        public IReadOnlySet<string> SavedRegisters { get; }

        /// <summary>
        /// Gets the set of registers clobbered by the calling convention.
        /// </summary>
        /// <returns></returns>
        public IReadOnlySet<string> ClobberedRegisters { get; }
    }
}
