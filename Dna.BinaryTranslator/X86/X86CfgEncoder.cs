using Dna.Binary;
using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.X86
{
    public static class X86CfgEncoder
    {
        public static EncodedCfg<Instruction> EncodeCfg(IBinary binary, ControlFlowGraph<Instruction> cfg)
        {
            // Build a mapping between <rip, instBytes>.
            var mapping = new Dictionary<ulong, byte[]>();
            foreach (var inst in cfg.GetInstructions())
            {
                var bytes = binary.ReadBytes(inst.IP, inst.Length);
                mapping.TryAdd(inst.IP, bytes);
            }

            return new EncodedCfg<Instruction>(cfg, mapping.AsReadOnly());
        }
    }
}
