using Dna.Binary;
using Dna.Extensions;
using Dna.LLVMInterop.Passes.Matchers;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Passes
{
    public class ConstantConcretizationPass
    {
        private readonly LLVMValueRef function;

        private readonly LLVMBuilderRef builder;

        private readonly IBinary binary;

        private readonly Dictionary<ulong, uint> accessedBytes = new();

        public ConstantConcretizationPass(LLVMValueRef function, LLVMBuilderRef builder, IBinary binary)
        {
            this.function = function;
            this.builder = builder;
            this.binary = binary;
        }

        public void Execute()
        {
            // Get all GEP instructions.
            var instructions = function.GetInstructions();

            // Traverse each instruction and track a set of all bytes being accessed.
            foreach(var instruction in instructions)
            {
                if(instruction.InstructionOpcode == LLVMOpcode.LLVMLoad)
                    TrackSecionAccesses(instruction.GetOperand(0), instruction.TypeOf.IntWidth);
                else if(instruction.InstructionOpcode == LLVMOpcode.LLVMStore)
                    TrackSecionAccesses(instruction.GetOperand(1), instruction.GetOperand(0).TypeOf.IntWidth);
            }

            ConcretizeBinarySectionAccesses();
        }

        private void TrackSecionAccesses(LLVMValueRef value, uint bitWidth)
        {
            // If this is not a getelementptr, then it's a global variable, where no processing is needed.
            if (value.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return;

            // If this is not a binary section access, then it's not relevant.
            if(!BinaryAccessMatcher.IsBinarySectionAccess(value.GetOperand(1))) 
                return;

            // Get the binary section offset.
            var sectionOffset = BinaryAccessMatcher.GetBinarySectionOffset(value.GetOperand(1));

            accessedBytes.TryAdd(sectionOffset, bitWidth);

            /*
            for (ulong i = sectionOffset; i < sectionOffset + bitWidth; i++)
            {
                accessedBytes.Add(i);
            }
            */
        }

        private void ConcretizeBinarySectionAccesses()
        {
            // Sort the byte addresses in ascending order.
            var byteAddresses = accessedBytes.OrderBy(x => x.Key);
            

            // Get the memory ptr.
            var memoryPtr = function.FirstBasicBlock.FirstInstruction;
            if (!memoryPtr.ToString().Contains("%0 = load ptr, ptr @memo"))
                throw new InvalidOperationException();

            var last = memoryPtr.NextInstruction;
            byteAddresses.Reverse();

            last = function.EntryBasicBlock.GetInstructions().First(x => x.ToString().Contains("%sub = add i64 "));

            var readBytes = (ulong address, uint size) =>
            {
                var bytes = binary.ReadBytes(address, (int)size);
                var value = size switch
                {
                    1 => bytes[0],
                    2 => BitConverter.ToUInt16(bytes),
                    4 => BitConverter.ToUInt32(bytes),
                    8 => BitConverter.ToUInt64(bytes),
                    _ => throw new InvalidOperationException()
                };
                return (ulong)value;
            };

            foreach (var address in byteAddresses)
            {
                // Since we're iterating in reverse, position the builder before
                // the previously added instruction.
                builder.PositionBefore(last);

                // Create a constant byte integer.
                var context = function.GlobalParent.Context;
                var valType = LLVMTypeRef.CreateInt(address.Value);
                var constantInt = LLVMValueRef.CreateConstInt(valType, readBytes(address.Key, address.Value / 8), false);

                // Store the constant byte to memory.
                var storeAddr = LLVMValueRef.CreateConstInt(context.Int64Type, address.Key, false);
                var storePtr = builder.BuildInBoundsGEP2(context.Int8Type, memoryPtr, new LLVMValueRef[] { storeAddr });
                storePtr = builder.BuildBitCast(storePtr, LLVMTypeRef.CreatePointer(valType, 0));

                last = builder.BuildStore(constantInt, storePtr);
            }
        }
    }
}
