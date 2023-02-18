using Dna.Extensions;
using Dna.Relocation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using Unicorn;
using Unicorn.X86;

namespace Dna.Emulation.Unicorn
{
    public class UnicornEmulator : ICpuEmulator
    {
        private readonly ICpuArchitecture architecture;

        private dgOnMemoryRead memReadCallback;

        private dgOnMemoryWrite memWriteCallback;

        public X86Emulator Emulator { get;}

        public UnicornEmulator(ICpuArchitecture architecture)
        {
            Emulator = new X86Emulator(X86Mode.b64);
            this.architecture = architecture;

            // Insert a hook to throw an exception when unmapped memory usages occur.
            Emulator.Hooks.Code.Add(CodeHook, null);
            Emulator.Hooks.Memory.Add(MemoryEventHookType.UnmappedFetch | MemoryEventHookType.UnmappedRead | MemoryEventHookType.UnmappedWrite, UnmappedMemoryHook, null);
            Emulator.Hooks.Memory.Add(MemoryHookType.Read, OnMemoryRead, null);
            Emulator.Hooks.Memory.Add(MemoryHookType.Write, OnMemoryWrite, null);
        }

        public ulong GetRegister(register_e regId)
        {
            // Return the register if it has a 1:1 mapping to unicorn's register enum.
            bool found = Emulator.TryReadRegister(regId, out ulong value);
            if (found)
                return value;

            // Throw if the register is not a flag bit.
            if (!architecture.IsFlagRegister(regId))
                throw new InvalidOperationException(string.Format("Cannot map register {0} to unicorn", regId));

            // Shift so that the specific bit(e.g. bit 7 for SF) is at index zero.
            var rflags = Emulator.ReadRegister(register_e.ID_REG_X86_EFLAGS);
            var lowestBit = rflags >> regId.GetFlagBitIndex();

            // Zero out all other bits and return. 
            return lowestBit & 1;
        }

        public void SetRegister(register_e regId, ulong value)
        {
            // Return if the register if it has a 1:1 mapping to unicorn's register list.
            bool found = Emulator.TryWriteRegister(regId, value);
            if (found)
                return;

            // Throw if the register is not a flag bit.
            if (!architecture.IsFlagRegister(regId))
                throw new InvalidOperationException(string.Format("Cannot map register {0} to unicorn", regId));

            // Set or clear the specified rflags bits.
            var rflags = (ulong)Emulator.Registers.EFLAGS;
            var bitIndex = regId.GetFlagBitIndex();
            if (value == 0)
                rflags &= ~(1UL << bitIndex);
            else
                rflags |= 1UL << bitIndex;

            // Update the flags register.
            Emulator.Registers.EFLAGS = (long)rflags;
        }

        public void MapMemory(ulong address, int size)
        {
            Emulator.Memory.Map(address, size, MemoryPermissions.All);
        }

        public byte[] ReadMemory(ulong addr, int size)
        {
            var buffer = new byte[size];
            Emulator.Memory.Read(addr, buffer, buffer.Length);
            return buffer;
        }

        public void WriteMemory(ulong addr, byte[] buffer)
        {
            Emulator.Memory.Write(addr, buffer, (ulong)buffer.Length);
        }

        public void Start(ulong addr, ulong untilAddr = long.MaxValue)
        {
            // Emulate a single instruction.
            Emulator.Start(addr, addr + 0x1000);
        }

        /// <summary>
        /// Unicorn callback raised when unmapped memory is read or written.
        /// </summary>
        private bool UnmappedMemoryHook(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            throw new InvalidOperationException($"Emulator accessed unmapped memory. (type: {type}), (address: {address})");
        }

        private void OnMemoryRead(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memReadCallback?.Invoke(address, size);
        }

        private void OnMemoryWrite(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memWriteCallback?.Invoke(address, size, value);
        }

        public void CodeHook(Emulator genericEmu, ulong address, int size, object userToken)
        {
            Console.WriteLine("executing {0}:", ((ulong)Emulator.Registers.RIP).ToString("X"));

            if (Emulator.Registers.RIP == 0x140001753)
            {
                Console.WriteLine("RAX: 0x{0}", Emulator.Registers.RAX.ToString("X"));
                Debugger.Break();
            }
        }

        public void SetMemoryReadCallback(dgOnMemoryRead callback)
        {
            memReadCallback = callback;   
        }

        public void SetMemoryWriteCallback(dgOnMemoryWrite callback)
        {
            memWriteCallback = callback;
        }
    }
}
