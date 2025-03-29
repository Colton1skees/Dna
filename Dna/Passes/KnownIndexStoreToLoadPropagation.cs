using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes
{
    public record AddBasePtrWithSelectOfTwoConstantIndices(LLVMValueRef BasePtr, LLVMValueRef SelectOfTwoConstantIndices);

    public static class KnownIndexStoreToLoadPropagation
    {
        public static AddBasePtrWithSelectOfTwoConstantIndices? GetAsBaseWithConstantSelect(LLVMValueRef loadInst)
        {
            // If we are not loading the result of a getelementptr instruction, return false.
            var gep = loadInst.GetOperand(0);
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return null;

            // If the gep index is not an add of two values, return false.
            var addPtr = gep.GetOperand(1);
            if (addPtr.Kind != LLVMValueKind.LLVMInstructionValueKind || addPtr.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return null;

            // Get the add operands.
            var lhs = addPtr.GetOperand(0);
            var rhs = addPtr.GetOperand(1);

            // If the operand at index zero is a select between two constant values, model the operand at index zero as the base.
            if (IsSelectOfTwoConstants(lhs))
                return new AddBasePtrWithSelectOfTwoConstantIndices(rhs, lhs);
            // Vice versa.
            if (IsSelectOfTwoConstants(rhs))
                return new AddBasePtrWithSelectOfTwoConstantIndices(lhs, rhs);

            return null;
        }

        private static bool IsSelectOfTwoConstants(LLVMValueRef inst)
        {
            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            // If either operand is not a constant, return false.
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind || inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }
    }
}
