using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Passes.Matchers
{
    public static class StackAccessMatcher
    {
        public static bool IsStackAccess(LLVMValueRef gepIndex)
        {
            // Precisely match:
            //  %gep_INDEX = load i64, ptr @rsp
            if (IsLoadRSP(gepIndex))
                return true;

            // Precisely match:
            //  %gep_index = add [rsp], [wildcard]
            if (IsAddToRSP(gepIndex))
                return true;

            return false;
        }

        /// <summary>
        /// Gets whether the instruction is: load i64, ptr @rsp
        /// </summary>
        private static bool IsLoadRSP(LLVMValueRef value)
        {
            if (value.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return false;

            return IsRSPGlobalVariable(value.GetOperand(0));
        }

        /// <summary>
        /// Gets whether the instruction is: add i64 %rsp, [wildcard]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsAddToRSP(LLVMValueRef value)
        {
            // If the instruction is not an ADD, return false.
            if (value.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;

            // If the first operand of the ADD is a dereference to RSP,
            // then it is an add [rsp] instruction.
            if (IsLoadRSP(value.GetOperand(0)))
                return true;

            return false;
        }

        /// <summary>
        /// Gets whether the provided value is the RSP global variable.
        /// </summary>
        private static bool IsRSPGlobalVariable(LLVMValueRef gepIndex)
        {
            return gepIndex.Kind == LLVMValueKind.LLVMGlobalVariableValueKind && gepIndex.Name == "rsp";
        }
    }
}
