using Dna.DataStructures;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Passes
{
    public static class InstructionSlicer
    {
        public static OrderedSet<LLVMValueRef> SliceInst(LLVMValueRef inst)
        {
            var visited = new OrderedSet<LLVMValueRef>();
            RecursiveSlice(inst, visited);
            return visited;
        }

        private static void RecursiveSlice(LLVMValueRef inst, OrderedSet<LLVMValueRef> visited)
        {
            // Skip if we've already seen this operand, to avoid infinite recursion.
            // This will only happen with loops, which need special handling anyways.
            if (visited.Contains(inst))
                return;

            // Add the instruction / operand to the visited list.
            visited.Add(inst);

            // Don't slice the operands of global variables.
            if (inst.Kind == LLVMValueKind.LLVMGlobalVariableValueKind)
                return;

            for (uint i = 0; i < inst.OperandCount; i++)
            {
                // The first index to GEP will always be a global [memory] ptr in our lifted IR.
                // We don't want this in our slice, so we skip it.
                if (inst.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && i == 0)
                    continue;

                RecursiveSlice(inst.GetOperand(i), visited);
            }
        }
    }
}
