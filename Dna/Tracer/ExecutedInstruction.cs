using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Tracer
{
    /// <summary>
    /// Class for modeling the inputs and outputs of an emulated instruction.
    /// </summary>
    public class ExecutedInstruction
    {
        /// <summary>
        /// The disassembled instruction.
        /// </summary>
        public Iced.Intel.Instruction Instruction { get; set; }

        /// <summary>
        /// The runtime values of each input register for an instruction.
        /// </summary>
        public Dictionary<Register, ulong> InputRegisters { get; set; } = new();

        // The runtime values of each input memory address for an instruction.
        public Dictionary<ulong, ulong> InputMemoryValues { get; set; } = new();

        /// <summary>
        /// The runtimes value of each modified register after an instruction execution.
        /// </summary>
        public Dictionary<Register, ulong> OutputRegisters { get; set; } = new();

        /// <summary>
        /// The runtime values of each modified memory address after an instruction execution.
        /// </summary>
        public Dictionary<ulong, ulong> OutputMemory { get; set; } = new();
    }
}
