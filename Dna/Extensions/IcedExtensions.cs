using Dna.Relocation;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extensions
{
    public static class IcedExtensions
    {
        private static List<OpKind> immediateKinds = new List<OpKind>()
        {
            OpKind.Immediate16,
            OpKind.Immediate32,
            OpKind.Immediate32to64,
            OpKind.Immediate64,
            OpKind.Immediate8,
            OpKind.Immediate8to16,
            OpKind.Immediate8to32,
            OpKind.Immediate8to64,
            OpKind.Immediate8_2nd,
        };

        private static List<OpKind> explicitImmediateKinds = new List<OpKind>()
        {
            OpKind.Immediate16,
            OpKind.Immediate32,
            OpKind.Immediate32to64,
            OpKind.Immediate64,
            OpKind.Immediate8,
            OpKind.Immediate8to16,
            OpKind.Immediate8to32,
            OpKind.Immediate8to64,
            OpKind.Immediate8_2nd,
        };

        private static List<List<Register>> registerMapping = new List<List<Register>>()
        {
            new List<Register>() {Register.RAX, Register.EAX, Register.AX, Register.AH, Register.AL },
            new List<Register>() {Register.RBX, Register.EBX, Register.BX, Register.BH, Register.BL },
            new List<Register>() {Register.RCX, Register.ECX, Register.CX, Register.CH, Register.CL },
            new List<Register>() {Register.RDX, Register.EDX, Register.DX, Register.DH, Register.DL },
            new List<Register>() {Register.RSI, Register.ESI, Register.SI, Register.None, Register.SIL },
            new List<Register>() {Register.RDI, Register.EDI, Register.DI, Register.None, Register.DIL },
            new List<Register>() {Register.RBP, Register.EBP, Register.BP, Register.None, Register.BPL },
            new List<Register>() {Register.RSP, Register.ESP, Register.SP, Register.None, Register.SPL },
            new List<Register>() {Register.R8, Register.R8D, Register.R8W, Register.None, Register.R8L },
            new List<Register>() {Register.R9, Register.R9D, Register.R9W, Register.None, Register.R9L },
            new List<Register>() {Register.R10, Register.R10D, Register.R10W, Register.None, Register.R10L },
            new List<Register>() {Register.R11, Register.R11D, Register.R11W, Register.None, Register.R11L },
            new List<Register>() {Register.R12, Register.R12D, Register.R12W, Register.None, Register.R12L },
            new List<Register>() {Register.R13, Register.R13D, Register.R13W, Register.None, Register.R13L },
            new List<Register>() {Register.R14, Register.R14D, Register.R14W, Register.None, Register.R14L },
            new List<Register>() {Register.R15, Register.R15D, Register.R15W, Register.None, Register.R15L },
        };

        public static string GetName(this Register register)
        {
            return register.ToString().ToLower();
        }

        public static int GetSizeInBits(this Register register)
        {
            return register.GetSize() * 8;
        }

        public static bool IsBranch(this FlowControl flowControl)
        {
            if (flowControl == FlowControl.UnconditionalBranch || flowControl == FlowControl.IndirectBranch || flowControl == FlowControl.ConditionalBranch)
                return true;

            return false;
        }

        public static bool IsCall(this FlowControl flowControl)
        {
            if (flowControl == FlowControl.Call || flowControl == FlowControl.IndirectCall)
                return true;

            return false;
        }

        public static bool IsRet(this FlowControl flowControl)
        {
            return flowControl == FlowControl.Return;
        }

        public static bool IsConditional(this FlowControl flowControl)
        {
            return flowControl == FlowControl.ConditionalBranch;
        }

        public static bool IsImmediate(this OpKind kind)
        {
            return immediateKinds.Contains(kind);
        }

        public static bool IsExplicitImmediate(this OpKind kind)
        {
            return explicitImmediateKinds.Contains(kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBranchOpKind(this OpKind kind)
        {
            switch (kind)
            {
                case OpKind.FarBranch16:
                case OpKind.FarBranch32:
                case OpKind.NearBranch16:
                case OpKind.NearBranch32:
                case OpKind.NearBranch64:
                    return true;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetBranchTarget(this Instruction instruction, OpKind kind)
        {
            switch (kind)
            {
                case OpKind.FarBranch16:
                    return instruction.FarBranch16;
                case OpKind.FarBranch32:
                    return instruction.FarBranch32;
                case OpKind.NearBranch16:
                    return instruction.NearBranch16;
                case OpKind.NearBranch32:
                    return instruction.NearBranch32;
                case OpKind.NearBranch64:
                    return instruction.NearBranch64;
                default:
                    throw new InvalidOperationException(String.Format("Operand {0} is not a branch.", kind));
            }
        }

        public static bool HasImmediateBranchTarget(this Instruction instruction)
        {
            switch (instruction.Op0Kind)
            {
                case OpKind.FarBranch16:
                case OpKind.FarBranch32:
                case OpKind.NearBranch16:
                case OpKind.NearBranch32:
                case OpKind.NearBranch64:
                case OpKind.Immediate16:
                case OpKind.Immediate32:
                case OpKind.Immediate32to64:
                case OpKind.Immediate64:
                case OpKind.Immediate8:
                case OpKind.Immediate8to16:
                case OpKind.Immediate8to32:
                case OpKind.Immediate8to64:
                case OpKind.Immediate8_2nd:
                    return true;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetImmediateBranchTarget(this Instruction instruction)
        {
            switch (instruction.Op0Kind)
            {
                case OpKind.FarBranch16:
                    return instruction.FarBranch16;
                case OpKind.FarBranch32:
                    return instruction.FarBranch32;
                case OpKind.NearBranch16:
                    return instruction.NearBranch16;
                case OpKind.NearBranch32:
                    return instruction.NearBranch32;
                case OpKind.NearBranch64:
                    return instruction.NearBranch64;
                case OpKind.Immediate16:
                case OpKind.Immediate32:
                case OpKind.Immediate32to64:
                case OpKind.Immediate64:
                case OpKind.Immediate8:
                case OpKind.Immediate8to16:
                case OpKind.Immediate8to32:
                case OpKind.Immediate8to64:
                case OpKind.Immediate8_2nd:
                    return instruction.GetImmediate(0);
                default:
                    throw new InvalidOperationException(String.Format("Operand kind {0} is not a branch.", instruction.Op0Kind));
            }
        }

        public static byte[] GetBytes(this Instruction instruction)
        {
            return InstructionEncoder.EncodeInstruction(instruction, instruction.IP);
        }
    }
}
