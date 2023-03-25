using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extensions
{
    public static class LLVMExtensions
    {
        public static IEnumerable<LLVMValueRef> GetGlobals(this LLVMModuleRef module)
        {
            // Get the first global within the module.
            var next = module.FirstGlobal;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                // Setup the next global for iteration.
                next = next.NextGlobal;
            }
        }

        public static IEnumerable<LLVMValueRef> GetInstructions(this LLVMBasicBlockRef block)
        {
            // Get the first global within the module.
            var next = block.FirstInstruction;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                if (next == block.LastInstruction)
                    yield break;

                // Setup the next global for iteration.
                next = next.NextInstruction;

            }
        }

        public static IEnumerable<LLVMValueRef> GetOperands(this LLVMValueRef value)
        {
            for(uint i = 0; i < value.OperandCount; i++)
            {
                yield return value.GetOperand(i);
            }
        }
    }
}
