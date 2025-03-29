using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes.Matchers
{
    public static class SegmentAccessMatcher
    {
        public static bool IsSegmentAccess(LLVMValueRef gepIndex)
        {
            // Precisely match:
            //  %gepIndex = load i64, ptr @gs
            if (IsLoadSegment(gepIndex))
                return true;

            // Precisely match:
            //  %gep_index = add [gs], [wildcard]
            if (IsAddToSegment(gepIndex))
                return true;

            return false;
        }

        /// <summary>
        /// Gets whether the instruction is: load i64, ptr @gs
        /// </summary>
        private static bool IsLoadSegment(LLVMValueRef value)
        {
            if (value.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return false;

            return IsSegmentGlobalVariable(value.GetOperand(0));
        }

        /// <summary>
        /// Gets whether the instruction is: add i64 %rsp, [wildcard]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsAddToSegment(LLVMValueRef value)
        {
            // If the instruction is not an ADD, return false.
            if (value.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;

            // If the first operand of the ADD is a dereference to gs,
            // then it is an add [gs] instruction.
            if (IsLoadSegment(value.GetOperand(0)))
                return true;

            return false;
        }

        private static bool IsSegmentGlobalVariable(LLVMValueRef value)
        {
            return value.Kind == LLVMValueKind.LLVMGlobalVariableValueKind && value.Name == "gs";
        }
    }
}
