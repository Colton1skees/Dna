using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;

namespace Dna.Extensions
{
    public static class TritonExtensions
    {
        public static int GetFlagBitIndex(this register_e regId)
        {
            switch (regId)
            {
                case register_e.ID_REG_X86_CF:
                    return 0;
                case register_e.ID_REG_X86_PF:
                    return 2;
                case register_e.ID_REG_X86_AF:
                    return 3;
                case register_e.ID_REG_X86_ZF:
                    return 6;
                case register_e.ID_REG_X86_SF:
                    return 7;
                case register_e.ID_REG_X86_TF:
                    return 8;
                case register_e.ID_REG_X86_IF:
                    return 9;
                case register_e.ID_REG_X86_DF:
                    return 10;
                case register_e.ID_REG_X86_OF:
                    return 11;
                case register_e.ID_REG_X86_NT:
                    return 14;
                case register_e.ID_REG_X86_RF:
                    return 16;
                case register_e.ID_REG_X86_VM:
                    return 17;
                case register_e.ID_REG_X86_AC:
                    return 18;
                case register_e.ID_REG_X86_VIF:
                    return 19;
                case register_e.ID_REG_X86_VIP:
                    return 20;
                case register_e.ID_REG_X86_ID:
                    return 21;
                default:
                    throw new InvalidOperationException(string.Format("{0} does not belong to rflags.", regId));
            }
        }
    }
}
