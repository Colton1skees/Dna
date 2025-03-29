using Dna.Extensions;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public class VmpContextRemovalPass
    {
        // Local context struct ranges from offset -8 to offset 0xC8
        // TODO: Stop hardcoding this!
        private const long ctxHeight = 0xC8;

        private const long virtualStackHeight = 0xC00000;

        private readonly LLVMValueRef function;

        private readonly LLVMValueRef memPtr;

        private readonly LLVMBuilderRef builder;

        public static void Run(LLVMValueRef function) => new VmpContextRemovalPass(function).Run();

        private VmpContextRemovalPass(LLVMValueRef function)
        {
            this.function = function;
            var global = function.GlobalParent.GetNamedGlobal("memory");
            this.memPtr = function.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == global);
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
        }

        private void Run()
        {
            // Replace the vmp context structure with a local allocation.
            RemoveContextStruct();

            // Replace the virtual stack with a local allocation
            RemoveVirtualStack();
        }

        private void RemoveContextStruct()
        {
            // Position the builder immediately after we dereference the global memory ptr.
            builder.PositionBefore(memPtr.NextInstruction);

            // Allocate an i8 array for the vmp context structure.
            var arrayTy = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)ctxHeight);
            var localCtx = builder.BuildAlloca(arrayTy, "vmp_ctx_struct");

            // Create a constant in that represents the end of the allocation. 
            // We use the end because the stack grows downwards(meaning that the context struct is at say rsp - 8 and downward).
            var arrayEndPtr = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)ctxHeight);

            // Lambda for checking if a stack offset is within the context structure.

            var getIsWithinRange = (long offset) => offset < 0 && offset >= -ctxHeight;

            var geps = GetStackPtrsWithinRange(function, memPtr, getIsWithinRange);
            foreach(var (gep, offset) in geps)
            {
                // Position the builder before the GetElementPtr instruction
                builder.PositionBefore(gep);

                // Calculate a new array index that can be used with our local allocation.
                var newIndex = builder.BuildAdd(arrayEndPtr, offset, $"ctx_offset_{offset}");

                // Create a new GEP instruction that is indexing into our local allocation.
                var newGep = builder.BuildInBoundsGEP2(gep.TypeOf, localCtx, new LLVMValueRef[] { newIndex });

                gep.ReplaceAllUsesWith(newGep);
                gep.InstructionEraseFromParent();
            }
        }

        private void RemoveVirtualStack()
        {
            // Position the builder immediately after we dereference the global memory ptr.
            builder.PositionBefore(memPtr.NextInstruction);

            // Allocate an i8 array for the vmp context structure.
            var arrayTy = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)virtualStackHeight);
            var localCtx = builder.BuildAlloca(arrayTy, "vmp_virtual_stack");

            // Create a constant in that represents the end of the allocation. 
            // We use the end because the stack grows downwards(meaning that the context struct is at say rsp - 8 and downward).
            var arrayEndPtr = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)virtualStackHeight);

            // Lambda for checking if a stack offset is within the context structure.

            var getIsWithinRange = (long offset) => offset < 0 && offset <= (-virtualStackHeight + 8);

            var geps = GetStackPtrsWithinRange(function, memPtr, getIsWithinRange);
            foreach (var (gep, offset) in geps)
            {
                // Position the builder before the GetElementPtr instruction
                builder.PositionBefore(gep);

                // Calculate a new array index that can be used with our local allocation.
                var newIndex = builder.BuildAdd(arrayEndPtr, offset, $"vsp_offset_{offset}");

                // Create a new GEP instruction that is indexing into our local allocation.
                var newGep = builder.BuildInBoundsGEP2(gep.TypeOf, localCtx, new LLVMValueRef[] { newIndex });

                gep.ReplaceAllUsesWith(newGep);
                gep.InstructionEraseFromParent();
            }
        }

        private static IReadOnlyList<(LLVMValueRef gep, LLVMValueRef constIntOffset)> GetStackPtrsWithinRange(LLVMValueRef function, LLVMValueRef memPtr, Func<long, bool> getIsWithinRange)
        {
            // Collect all getelementptrs into the global memory array.
            var geps = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && x.GetOperand(0) == memPtr).ToList();

            
            var rsp = function.GetParam(0);
            List<(LLVMValueRef ptr, LLVMValueRef constIntOffset)> ptrs = new();
            foreach(var gep in geps)
            {
                // Skip if we are not adding an offset to something.
                var ptr = gep.GetOperand(1);
                if (ptr.Kind != LLVMValueKind.LLVMInstructionValueKind || ptr.InstructionOpcode != LLVMOpcode.LLVMAdd)
                    continue;
                // Skip if we are not adding a constant to rsp.
                if (ptr.GetOperand(0) != rsp || ptr.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    continue;

                // Fetch the offset.
                var constantInt = ptr.GetOperand(1);
                var offset = (long)constantInt.ConstIntZExt;

                // Skip if the GEP is not within the vmp context struct.
                bool isInsideCtx = getIsWithinRange(offset);
                if (!isInsideCtx)
                    continue;

                ptrs.Add((gep, constantInt));
            }

            return ptrs;
        }

       // private static bool IsWithinVmpCtxStruct()
    }
}
