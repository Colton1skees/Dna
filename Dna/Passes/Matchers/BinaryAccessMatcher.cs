using Dna.Binary;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes.Matchers
{
    public static class BinaryAccessMatcher
    {
        public static ulong GetBinarySectionOffset(IBinary bin, LLVMValueRef value)
        {
            if (IsConstantWithinBinarySection(bin, value))
            {
                var constant = value.ConstIntZExt;
                return constant;
            }

            if (IsAddToBinarySection(bin, value))
            {
                var constant = value.GetOperand(1).ConstIntZExt;
                return constant;
            }

            else if (IsLoopBinaryAccess(bin, value))
            {
                var constant = value.GetOperand(0).ConstIntZExt;
                return constant;
            }

            throw new InvalidOperationException($"Cannot identify constant binary section access for: {value}");
        }

        public static bool IsBinarySectionAccess(IBinary bin, LLVMValueRef value)
        {
            // Precisely match:
            //  %foo = getelementptr inbounds i8, ptr %0, i64 5368985441
            if (IsConstantWithinBinarySection(bin, value))
                return true;

            // Precisely match:
            //  %offset = i64 [wildcard]
            //  %index = add i64 offset, [address within the range of a binary section]
            //  %foo = getelementptr inbounds i8, ptr %0, i64 %index
            if (IsAddToBinarySection(bin, value))
                return true;

            // Precisely match this slice:
            //  %foo = add i64 %phiIndex, 8
            //  %phiIndex = phi i64 [ 5369023239, %entry ], [ %foo, %"140015B1D" ]
            //  %gep_index = getelementptr inbounds i8, ptr %0, i64 %phiIndex
            if (IsLoopBinaryAccess(bin, value))
                return true;
            return false;
        }


        private static bool IsLoopBinaryAccess(IBinary bin, LLVMValueRef value)
        {
            // Precisely match:
            //  %phiIndex = phi i64 [ 5369023239, %entry ], [ %foo, %"140015B1D" ]
            if (value.InstructionOpcode != LLVMOpcode.LLVMPHI)
                return false;

            // For now we only match PHIs with two operands.
            if (value.OperandCount != 2)
                return false;

            // Assume that the first phi value must be a constant.
            var phiVal = value.GetOperand(0);
            if (!IsConstantWithinBinarySection(bin, value.GetOperand(0)))
                return false;

            // Return false if the second PHI value is not an add.
            var otherPhiValue = value.GetOperand(1);
            if (otherPhiValue.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;

            // Return true if this is semantically equivalent to:
            //  %foo = add i64 %phiIndex, 8
            if (otherPhiValue.GetOperand(0) == value && IsConstantInt(otherPhiValue.GetOperand(1)))
                return true;

            return false;
        }


        private static bool IsAddToBinarySection(IBinary bin, LLVMValueRef value)
        {
            // If the instruction is not an ADD, return false.
            if (value.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;

            // If the first operand of the ADD is a dereference to RSP,
            // then it is an add [rsp] instruction.
            if (IsConstantWithinBinarySection(bin, value.GetOperand(1)))
                return true;

            return false;
        }

        public static bool IsConstantWithinBinarySection(IBinary bin, LLVMValueRef value)
        {
            if (!IsConstantInt(value))
                return false;

            return bin.IsConstantData(value.ConstIntZExt);
        }

        public static bool IsConstantInt(LLVMValueRef value) => value.Kind == LLVMValueKind.LLVMConstantIntValueKind;
    }
}
