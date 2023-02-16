using Dna.Relocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using Unicorn;
using Unicorn.X86;

namespace Dna.Emulation
{
    public class UnicornEmulator : ICpuEmulatorState
    {
        private static Dictionary<register_e, Func<X86Emulator, ulong>> registerReadFunctions = new();

        private static Dictionary<register_e, Action<X86Emulator, ulong>> registerWriteFunctions = new();

        private readonly ICpuArchitecture architecture;

        private X86Emulator emulator;  

        static UnicornEmulator()
        {
            var registerProperties = typeof(X86Registers).GetProperties();

            var regNames = typeof(register_e)
                .GetEnumValues()
                .Cast<register_e>()
                .ToDictionary(x => x.ToString().ToUpper().Replace("ID_REG_X86_", ""), x => x);

            foreach(var property in registerProperties)
            {
                var name = property.Name;
                if(!regNames.ContainsKey(name))
                {
                    Console.WriteLine("Failed to map register: {0}", name);
                    continue;
                }

                var getRegister = (X86Emulator emu) =>
                {
                    return (ulong)property.GetValue(emu);
                };

                var setRegister = (X86Emulator emu, ulong value) =>
                {
                    property.SetValue(emu, value);
                };

                registerReadFunctions.Add(regNames[name], getRegister);
                registerWriteFunctions.Add(regNames[name], setRegister);
            }
        }

        public UnicornEmulator(ICpuArchitecture architecture)
        {
            emulator = new X86Emulator(X86Mode.b64);
            this.architecture = architecture;

            // Setup hooks.
            emulator.Hooks.Memory.Add(MemoryEventHookType.UnmappedFetch | MemoryEventHookType.UnmappedRead | MemoryEventHookType.UnmappedWrite, UnmappedMemoryHook, null);
            emulator.Hooks.Code.Add(CodeHook, null);
        }

        private void CodeHook(Emulator genericEmu, ulong address, int size, object userToken)
        {
            if (true)
            {
                Console.WriteLine("Code hook at addr: {0} with rip {1}", address.ToString("X"), GetRegister(register_e.ID_REG_X86_RIP));

                Console.WriteLine("RAX: 0x{0}", GetRegister(register_e.ID_REG_X86_RAX).ToString("X"));
            }
        }

        public ulong GetRegister(register_e regId)
        {
            // Return the register if it has a 1:1 mapping to unicorn's register list.
            bool found = registerReadFunctions.TryGetValue(regId, out var readRegister);
            if (found)
                return readRegister(emulator);

            // Throw if the register is not a flag bit.
            if (!architecture.IsFlagRegister(regId))
                throw new InvalidOperationException(String.Format("Cannot map register {0} to unicorn", regId));

            // Shift so that the specific bit(e.g. bit 7 for SF) is at index zero.
            var rflags = registerReadFunctions[register_e.ID_REG_X86_EFLAGS](emulator);
            var lowestBit = rflags >> GetFlagBitIndex(regId);

            // Zero out all other bits and return. 
            return lowestBit & 1;
        }

        public void SetRegister(register_e regId, ulong value)
        {
            // Return the register if it has a 1:1 mapping to unicorn's register list.
            bool found = registerWriteFunctions.TryGetValue(regId, out var writeRegister);
            if (found)
            {
                writeRegister(emulator, value);
                return;
            }

            // Throw if the register is not a flag bit.
            if (!architecture.IsFlagRegister(regId))
                throw new InvalidOperationException(String.Format("Cannot map register {0} to unicorn", regId));

            // Set or clear the specified rflags bits.
            var rflags = (ulong)emulator.Registers.EFLAGS;
            var bitIndex = GetFlagBitIndex(regId);
            if (value == 0)
            {
                rflags &= ~(1UL << bitIndex);
            }

            else
            {
                rflags |= 1UL << bitIndex;
            }

            // Update the flags register.
            emulator.Registers.EFLAGS = (long)rflags;
        }

        private int GetFlagBitIndex(register_e regId)
        {
            switch(regId)
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
                    throw new InvalidOperationException(String.Format("{0} does not belong to rflags.", regId));
            }
        }

        public T ReadMemory<T>(ulong addr)
        {
            var buffer = new byte[MarshalType<T>.Size];
            emulator.Memory.Read(addr, buffer, buffer.Length);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }

        public void WriteMemory<T>(ulong addr, T value)
        {
            var buffer = MarshalType<T>.ObjectToByteArray(value);
            WriteMemory(addr, buffer);
        }

        public void WriteMemory(ulong addr, byte[] buffer)
        {
            try
            {
                emulator.Memory.Write(addr, buffer, buffer.Length);
            }

            catch(Exception ex)
            {
                if (!ex.Message.Contains("UC_ERR_WRITE_UNMAPPED"))
                    throw;

                MapMemory(addr, buffer.Length);
                emulator.Memory.Write(addr, buffer, buffer.Length);
            }
        }

        public void Start(ulong addr, ulong untilAddr = long.MaxValue)
        {
            // Emulate a single instruction.
            SetRegister(register_e.ID_REG_X86_RIP, (ulong)addr);
            emulator.Start(addr, untilAddr);
        }

        /// <summary>
        /// Unicorn callback raised when unmapped memory is read or written.
        /// </summary>
        private static bool UnmappedMemoryHook(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            // TODO: Handle unmapped reads.
            if (type != MemoryType.ReadUnmapped)
            {
                throw new InvalidOperationException();
            }

            // Map the memory automatically.
            var newSize = (size / 0x1000) * 0x1000;
            emulator.Memory.Map(address, newSize == 0 ? 1024 : newSize, MemoryPermissions.All);
            return true;
        }

        private void MapMemory(ulong address, int size)
        {
            // Map the memory automatically.
            var newAddress = (address / 0x1000) * 0x1000;
            emulator.Memory.Map(newAddress, 2 * 1024 * 1024, MemoryPermissions.All);
        }
    }
}
