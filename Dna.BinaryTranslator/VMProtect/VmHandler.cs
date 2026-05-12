using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public struct VmHandler : IEquatable<VmHandler>
    {
        public ulong BytecodeRip { get; }

        public ulong NativeRip { get; }

        public VmHandler(ulong BytecodeRip, ulong NativeRip)
        {
            this.BytecodeRip = BytecodeRip;
            this.NativeRip = NativeRip;
        }

        public static bool operator == (VmHandler lhs, VmHandler rhs)
        {
            return lhs.BytecodeRip == rhs.BytecodeRip;
        }

        public static bool operator != (VmHandler lhs, VmHandler rhs)
        {
            return lhs.BytecodeRip != rhs.BytecodeRip;
        }

        public override int GetHashCode()
        {
            return BytecodeRip.GetHashCode();
            //return BytecodeRip.GetHashCode() + NativeRip.GetHashCode();
        }

        public override string ToString()
        {
            return $"0x{BytecodeRip.ToString("X")}";
        }

        public bool Equals(VmHandler other)
        {
            return this == other;
        }
    }
}
