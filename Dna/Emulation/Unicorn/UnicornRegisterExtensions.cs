using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using Unicorn.X86;

namespace Dna.Emulation.Unicorn
{
    internal static class UnicornRegisterExtensions
    {
        private static readonly Dictionary<register_e, Func<X86Emulator, ulong>> registerReadFunctions = new();

        private static readonly Dictionary<register_e, Action<X86Emulator, ulong>> registerWriteFunctions = new();

        static UnicornRegisterExtensions()
        {
            var registerProperties = typeof(X86Registers).GetProperties();

            var regNames = typeof(register_e)
                .GetEnumValues()
                .Cast<register_e>()
                .ToDictionary(x => x.ToString().ToUpper().Replace("ID_REG_X86_", ""), x => x);

            foreach (var property in registerProperties)
            {
                var name = property.Name;
                if (!regNames.ContainsKey(name))
                {
                    Console.WriteLine("Failed to map register: {0}", name);
                    continue;
                }

                var getRegister = (X86Emulator emu) =>
                {
                    var rip = emu.Registers.RIP;
                    Console.WriteLine(rip);
                    var value = property.GetValue(emu.Registers);
                    return (ulong)(long)value;
                };

                var setRegister = (X86Emulator emu, ulong value) =>
                {
                    property.SetValue(emu.Registers, (long)value);
                };

                registerReadFunctions.Add(regNames[name], getRegister);
                registerWriteFunctions.Add(regNames[name], setRegister);
            }
        }

        public static ulong ReadRegister(this X86Emulator emulator, register_e registerId)
        {
            bool found = TryReadRegister(emulator, registerId, out ulong value);
            if (!found)
                throw new InvalidOperationException($"Cannot read register {registerId} from unicorn state. A mapping does not exist.");

            return value;
        }

        public static bool TryReadRegister(this X86Emulator emulator, register_e registerId, out ulong value)
        {
            bool found = registerReadFunctions.TryGetValue(registerId, out var readRegister);
            if (!found)
            {
                value = 0;
                return false;
            }

            value = readRegister(emulator);
            return true;
        }

        public static void WriteRegister(this X86Emulator emulator, register_e registerId, ulong value)
        {
            bool mappingExists = TryWriteRegister(emulator, registerId, value);
            if (!mappingExists)
                throw new InvalidOperationException($"Cannot write register {registerId} from unicorn state. A mapping does not exist.");
        }

        public static bool TryWriteRegister(this X86Emulator emulator, register_e registerId, ulong value)
        {
            bool found = registerWriteFunctions.TryGetValue(registerId, out var writeRegister);
            if (!found)
                return false;

            writeRegister(emulator, value);
            return true;
        }
    }
}
