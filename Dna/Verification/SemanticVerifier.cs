using Dna.Emulation.Unicorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Verification
{
    /// <summary>
    /// Class for verifying that the semantics of our intermediate language translator
    /// are correct(or atleast at parity with unicorn engine).
    /// </summary>
    public class SemanticVerifier
    {
        public SemanticVerifier(ICpuArchitecture architecture, UnicornEmulator emulator)
        {

        }
    }
}
