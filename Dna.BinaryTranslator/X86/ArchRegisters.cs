using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.X86
{
    public static class ArchRegisters
    {
        public static IReadOnlySet<RemillRegister> GetRootGprs(RemillArch arch)
        {
            var registers = GetRootRegisters(arch);
            return registers
                .Where(x => IsGpr(x))
                .ToHashSet()
                .AsReadOnly();
        }

        public static IReadOnlySet<RemillRegister> GetRootRegisters(RemillArch arch)
        {
            return BuildRootParentMapping(arch)
                .Select(x => x.Value)
                .ToHashSet()
                .AsReadOnly();
        }

        public static bool IsGpr(RemillRegister register)
        {
            switch (register.Name.ToUpper())
            {
                case "RAX":
                case "RBX":
                case "RCX":
                case "RDX":
                case "RSI":
                case "RDI":
                case "RSP":
                case "RBP":
                case "RIP":
                case "R8":
                case "R9":
                case "R10":
                case "R11":
                case "R12":
                case "R13":
                case "R14":
                case "R15":
                    return true;
                default:
                    return false;
            }
        }

        public static Dictionary<RemillRegister, RemillRegister> BuildRootParentMapping(RemillArch arch)
        {
            var rootParentMapping = new Dictionary<RemillRegister, RemillRegister>();
            foreach (var reg in arch.Registers)
            {
                // Find the root register(i.e. for the registers al,ax,eax,rax, we want to return rax in all cases).
                var root = reg;
                while (root.Parent != null)
                    root = root.Parent;

                rootParentMapping.Add(reg, root);
            }

            return rootParentMapping;
        }
    }
}
