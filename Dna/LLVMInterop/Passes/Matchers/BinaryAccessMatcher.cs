using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Passes.Matchers
{
    public static class BinaryAccessMatcher
    {
        public static bool IsBinarySectionAccess(LLVMValueRef value)
        {
            // Precisely match:
            //  %foo = getelementptr inbounds i8, ptr %0, i64 5368985441
            if (IsConstantWithinBinarySection(value))
                return true;

            // Precisely match:
            //  %offset = i64 [wildcard]
            //  %index = add i64 offset, [address within the range of a binary section]
            //  %foo = getelementptr inbounds i8, ptr %0, i64 %index
            if (IsAddToBinarySection(value))
                return true;
            return false;
        }

        private static bool IsAddToBinarySection(LLVMValueRef value)
        {
            // If the instruction is not an ADD, return false.
            if (value.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;

            // If the first operand of the ADD is a dereference to RSP,
            // then it is an add [rsp] instruction.
            if(IsConstantWithinBinarySection(value.GetOperand(1)))
                return true;

            return false;
        }

        private static bool IsConstantWithinBinarySection(LLVMValueRef value)
        {
            if (!IsConstantInt(value))
                return false;

            var constant = value.ConstIntZExt;
            return constant >= 0x140009000 && constant <= 0x14006C460;
        }

        public static bool IsConstantInt(LLVMValueRef value) => value.Kind == LLVMValueKind.LLVMConstantIntValueKind;
    }
}
