using Dna.Binary;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.SEH
{
    public static class UnwindCodeParser
    {
        private static Register[] registers =
        {
            Register.RAX,
            Register.RCX,
            Register.RDX,
            Register.RBX,
            Register.RSP,
            Register.RBP,
            Register.RSI,
            Register.RDI,
            Register.R8,
            Register.R9,
            Register.R10,
            Register.R11,
            Register.R12,
            Register.R13,
            Register.R14,
            Register.R15
        };

        public static IReadOnlyList<UnwindCode> ParseUnwindCode(IBinary binary, ulong offset, int countOfCodes, byte version)
        {
            countOfCodes = (countOfCodes + 1) & ~1;
            var codes = new List<UnwindCode>();
            uint advanceBy = 0;
            uint i = 0;
            while(true)
            {
                if (i >= countOfCodes)
                    break;

                var (code, increment) = Parse(binary, offset + advanceBy);
                codes.Add(code);
                advanceBy += increment;
                i += increment;
            }

            return codes;
        }

        private static (UnwindCode code, uint advanceBy) Parse(IBinary binary, ulong offset)
        {
            // Get the first slot.
            var uc = binary.ReadUint16(offset);

            // Parse all data out of the first slot.
            var codeOffset = (byte)(uc & 0xff);
            var unwindOp = (UnwindOpType)((uc & 0xf00) >> 8);
            var opInfo = (byte)((uc & 0xf000) >> 12);

            switch (unwindOp)
            {
                case UnwindOpType.UwOpPushNonVol:
                    var push = new UwOpPushNonVol(codeOffset, opInfo, registers[opInfo]);
                    return (push, 2);
                case UnwindOpType.UwOpAllocLarge:
                    if (opInfo == 0)
                    {
                        var size = (binary.ReadUint16(offset + 2) * 8);
                        var alloc = new UwOpAllocLarge(codeOffset, opInfo, size);
                        return (alloc, 2);
                    }

                    else
                    {
                        var size = BitConverter.ToUInt32(binary.ReadBytes(offset + 2, 4));
                        size = size << ((byte)16);
                        var alloc = new UwOpAllocLarge(codeOffset, opInfo, (int)size);
                        return (alloc, 3);
                    }
                case UnwindOpType.UwOpAllocSmall:
                    var allocSmall = new UwOpAllocSmall(codeOffset, opInfo, opInfo * 8 + 8);
                    return (allocSmall, 2);
                case UnwindOpType.UwOpSetFpReg:
                    var setFpReg = new UwOpSetFpReg(codeOffset, opInfo, registers[opInfo]);
                    return (setFpReg, 1);
                case UnwindOpType.UwOpSaveNonVol:
                {
                    var frameOffset = binary.ReadUint16(offset + 2);
                    var saveNonVol = new UwOpSaveNonVol(codeOffset, opInfo, registers[opInfo], frameOffset);
                    return (saveNonVol, 4);
                }
                case UnwindOpType.UwOpSaveNonVolFar:
                {
                    var fo = binary.ReadUint32(offset + 2);
                    var frameOffset = (ushort)(fo * 8);
                    var saveNonVol = new UwOpSaveNonVol(codeOffset, opInfo, registers[opInfo], frameOffset);
                    return (saveNonVol, 3);
                }
                case UnwindOpType.UwOpEpilog:
                    throw new InvalidOperationException("TODO: Handle deprecated UwOpEpilog");
                case UnwindOpType.UwOpSpareCode:
                    throw new InvalidOperationException("TODO: Handle deprecated UwOpSpareCode");
                case UnwindOpType.UwOpSaveXmm128:
                {
                    var frameOffset = binary.ReadUint16(offset + 2);
                    return (new UwOpSaveXmm128(codeOffset, opInfo, registers[opInfo], frameOffset), 2);
                }
                case UnwindOpType.UwOpSaveXmm128Far:
                {
                    throw new InvalidOperationException($"TODO: {unwindOp}");
                }
                case UnwindOpType.UwOpPushMachFrame:
                    throw new InvalidOperationException($"TODO: {unwindOp}");
                    break;
                case UnwindOpType.UwOpSetFpRegLarge:
                    throw new InvalidOperationException("TODO: Handle .NET only UwOpSetFpRegLarge");
                    break;
            }

            throw new InvalidOperationException($"TODO: {unwindOp}");
        }
    }
}
