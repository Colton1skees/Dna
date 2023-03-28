using Dna.Binary;
using Dna.Extensions;
using Dna.Lifting;
using Dna.LLVMInterop.Passes.Matchers;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        private readonly Dictionary<ulong, byte> accessedBytes = new();

        private readonly HashSet<string> existingConcretizes = new();

        public ConstantConcretizationPass(LLVMValueRef function, LLVMBuilderRef builder, IBinary binary)
        {
            this.function = function;
            this.builder = builder;
            this.binary = binary;
        }

        public void Execute()
        {
            return;
            var instructions = function.GetInstructions();

            // Traverse each instruction and track a set of all bytes being accessed.
            foreach (var instruction in instructions)
            {
                if (instruction.InstructionOpcode == LLVMOpcode.LLVMLoad)
                    TrackSecionAccesses(instruction.GetOperand(0), instruction.TypeOf.IntWidth);
            }

            ConcretizeBinarySectionAccesses();
        }

        private void TrackSecionAccesses(LLVMValueRef value, uint bitWidth)
        {
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

            // If this is not a getelementptr, then it's a global variable, where no processing is needed.
            if (value.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return;

            // If this is not a binary section access, then it's not relevant.
            if (!BinaryAccessMatcher.IsBinarySectionAccess(value.GetOperand(1)))
                return;

            // Get the binary section offset.
            var sectionOffset = BinaryAccessMatcher.GetBinarySectionOffset(value.GetOperand(1));

            if (sectionOffset == 0x140043761)
            {
               // Debugger.Break();
            }

            var byteWidth = bitWidth / 8;
            for(ulong i = 0; i < byteWidth + 1; i++)
            {
                var offset = i + sectionOffset;
                if (accessedBytes.ContainsKey(offset))
                    continue;

                var data = (byte)readBytes(offset, 1);
                accessedBytes.Add(offset, data);
            }

            /*
            if (!accessedBytes.ContainsKey(sectionOffset))
            {

                accessedBytes.Add(sectionOffset, bitWidth);
            }

            else
            {
                var currentWidth = accessedBytes[sectionOffset];
                if (bitWidth > currentWidth)
                {
                    accessedBytes[sectionOffset] = bitWidth;
                }
            }
            */
            //accessedBytes.TryAdd(sectionOffset, bitWidth);

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

            //last = function.EntryBasicBlock.GetInstructions().First(x => x.ToString().Contains("%sub = add i64 "));

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


            var gsAccess = function.EntryBasicBlock.GetInstructions().First(x => x.OperandCount == 1 && x.GetOperand(0).Kind == LLVMValueKind.LLVMGlobalVariableValueKind
            && x.GetOperand(0).Name == "gs");

            last = gsAccess.NextInstruction;

            foreach (var address in accessedBytes)
            {
                // Since we're iterating in reverse, position the builder before
                // the previously added instruction.
                builder.PositionBefore(last);

                // Create a constant byte integer.
                var context = function.GlobalParent.Context;
                var valType = LLVMTypeRef.CreateInt(8);

   
                var constantInt = LLVMValueRef.CreateConstInt(valType, address.Value, false);

                if (address.Key >= 0x140043761 && address.Key <= 0x140043761 + 8)
                {
                 //   Debugger.Break();
                }
                // Store the constant byte to memory.
                var storeAddr = LLVMValueRef.CreateConstInt(context.Int64Type, address.Key, false);
                var storePtr = builder.BuildInBoundsGEP2(context.Int8Type, memoryPtr, new LLVMValueRef[] { storeAddr });
                //storePtr = builder.BuildBitCast(storePtr, LLVMTypeRef.CreatePointer(valType, 0));

                last = builder.BuildStore(constantInt, storePtr);
            }
        }
    }
}
