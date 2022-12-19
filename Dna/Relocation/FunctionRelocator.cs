using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Extraction;
using Dna.Reinsertion;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Note: This was ported over from a very old project of mine
// TODO: Refactor.
namespace Dna.Relocation
{
    /// <inheritdoc cref="IFunctionRelocator"/>
    public class FunctionRelocator : IFunctionRelocator
    {
        private readonly IDna dna;

        private readonly Dictionary<ulong, IExtractedFunction> relocatedFunctions = new Dictionary<ulong, IExtractedFunction>();

        private readonly Dictionary<ulong, ulong> relocatedFunctionAddresses = new Dictionary<ulong, ulong>();

        private readonly Dictionary<ulong, RelocatedBlock> relocationMapping = new Dictionary<ulong, RelocatedBlock>();

        private bool isRootFunction = true;

        public FunctionRelocator(IDna dna)
        {
            this.dna = dna;
        }

        /// <inheritdoc cref="IFunctionRelocator.RelocateFunction(IExtractedFunction, ulong)"/>
        public byte[] RelocateFunction(IExtractedFunction function, ulong relocRip, out ulong endRip)
        {
            bool isRoot = isRootFunction;
            isRootFunction = false;

            // Sequentially reserve space for each basic block.
            relocatedFunctions.Add(function.Graph.StartAddress, function);
            var relocatedBlocks = RelocateBasicBlocks(function, relocRip, out endRip);

            // Build a mapping between old and relocated basic blocks.
            foreach(var block in relocatedBlocks)
                relocationMapping.Add(block.OriginalRip, block);

            // Allocate space for each function
            foreach(var callee in function.Callees)
            {
                if (relocatedFunctionAddresses.ContainsKey(callee.Graph.StartAddress))
                    continue;

                relocatedFunctionAddresses.Add(callee.Graph.StartAddress, endRip);
                RelocateFunction(callee, endRip, out endRip);
            }

            // Temporary hack to avoid needless computation. TODO: Refactor out.
            if (isRoot == false)
                return new byte[] { };

            foreach(var pair in relocationMapping)
            {
                if (pair.Value.HasBeenRewritten)
                    continue;

                pair.Value.HasBeenRewritten = true;
                RelocateBlock(pair.Key, pair.Value);
            }

            var finalBlocks = relocationMapping.Select(x => x.Value);
            var startRip = finalBlocks.Min(x => x.RelocatedRip);
            var finalEndRip = finalBlocks.Max(x => x.RelocatedRip + (ulong)x.RelocatedSize);
            var finalBytes = Enumerable.Repeat<byte>(0x90, (int)(finalEndRip - startRip)).ToList();
            foreach(var relocBlock in relocationMapping.Values)
            {
                var bytes = InstructionRelocator.EncodeInstructions(relocBlock.RelocatedInstructions, relocBlock.RelocatedRip, out ulong encodedEnd);
                if (encodedEnd > relocBlock.RelocatedRip + (ulong)relocBlock.RelocatedSize)
                    throw new Exception("Block is too big....");

                int offset = (int)(relocBlock.RelocatedRip - startRip);
                for(int i = 0; i < bytes.Length; i++)
                    finalBytes[offset + i] = bytes[i];
            }

            return finalBytes.ToArray();
        }

        private List<RelocatedBlock> RelocateBasicBlocks(IExtractedFunction function, ulong relocRip, out ulong endRip)
        {
            // Allocate each basic block sequentially.
            List<RelocatedBlock> output = new List<RelocatedBlock>();
            endRip = relocRip;
            foreach(var inputBlock in function.Graph.GetBlocks())
            {
                // Compute the maximum size, which leaves padding for variable length instructions.
                var size = GetMaximumBlockPadding(inputBlock);

                // Create a placeholder for the relocated block.
                var relocatedBlock = new RelocatedBlock(inputBlock, inputBlock.Address, endRip, size);
                output.Add(relocatedBlock);
                endRip += (ulong)size;
            }

            return output;
        }

        /// <summary>
        /// Gets the maximum amount of space that may be taken up by a basic block when relocated.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private int GetMaximumBlockPadding(BasicBlock<Instruction> block)
        {
            int maxJccSize = 8;

            // TODO: Implement properly.
            var maxSize = InstructionRelocator.EncodeInstructions(block.Instructions, block.Address + ushort.MaxValue, out ulong endRip).Length;
            if (block.ExitInstruction.FlowControl == FlowControl.ConditionalBranch)
                maxSize += maxJccSize;

            return maxSize;
        }

        private void RelocateBlock(ulong oldAddress, RelocatedBlock relocatedBlock)
        {
            var block = relocatedBlock.InputBlock;
            ulong rip = relocatedBlock.RelocatedRip;
            foreach(var inst in block.Instructions.Take(block.Instructions.Count - 1))
            {
                // Assemble custom instructions to call relocated functions.
                if(inst.FlowControl == FlowControl.Call && inst.Op0Kind.IsImmediate())
                {
                    ulong imm = inst.Op0Kind.IsExplicitImmediate() ? inst.GetImmediate(0) : inst.NearBranchTarget;
                    if (!relocatedFunctions.ContainsKey(imm))
                        throw new Exception(String.Format("Cannot find function {0}.", imm));
                    imm = relocatedFunctionAddresses[imm];

                    var assembler = new Assembler(64);
                    assembler.call(imm);
                    var call = assembler.Instructions.Single();
                    relocatedBlock.RelocatedInstructions.Add(call);
                    rip += (ulong)call.Length;
                    continue;
                }

                // If the function is not a branch or call, we can safely relocate it.
                var relocatedInst = InstructionRelocator.RelocateInstructions(new List<Instruction>() { inst }, rip).Single();
                relocatedBlock.RelocatedInstructions.Add(relocatedInst);
                rip += (ulong)relocatedInst.Length;
            }

            // If the exit instruction is a return, then we simply relocate it.
            var exitInstruction = block.ExitInstruction;
            if (exitInstruction.FlowControl == FlowControl.Return)
            {
                var relocatedInst = InstructionRelocator.RelocateInstructions(new List<Instruction>() { exitInstruction }, rip).Single();
                relocatedBlock.RelocatedInstructions.Add(relocatedInst);
                return;
            }

            else if (exitInstruction.FlowControl == FlowControl.UnconditionalBranch)
            {
                // If the jmp is indirect, then all we can do is relocate it.
                if (!exitInstruction.Op0Kind.IsImmediate())
                {
                    var relocatedInst = InstructionRelocator.RelocateInstructions(new List<Instruction>() { exitInstruction }, rip).Single();
                    relocatedBlock.RelocatedInstructions.Add(relocatedInst);
                    return;
                }

                // If the jmp destination is an immediate, then we try to re-encode it with a fixed destination.
                ulong imm = exitInstruction.Op0Kind.IsExplicitImmediate() ? exitInstruction.GetImmediate(0) : exitInstruction.NearBranchTarget;
                imm = relocationMapping[imm].RelocatedRip;
                var assembler = new Assembler(64);
                assembler.jmp(imm);
                var jmp = assembler.Instructions.Single();
                relocatedBlock.RelocatedInstructions.Add(jmp);
                return;
            }

            else if (exitInstruction.FlowControl == FlowControl.ConditionalBranch)
            {
                // If the jmp destination is an immediate, then we try to re-encode it with a fixed destination.
                ulong imm = exitInstruction.Op0Kind.IsExplicitImmediate() ? exitInstruction.GetImmediate(0) : exitInstruction.NearBranchTarget;
                imm = relocationMapping[imm].RelocatedRip;
                var assembler = new Assembler(64);
                var methodAssembleJCC = typeof(Assembler).GetMethods().Single(x => x.Name == exitInstruction.Mnemonic.ToString().ToLower() && x.GetParameters()[0].ParameterType == typeof(ulong));
                methodAssembleJCC.Invoke(assembler, new object[] { imm });

                var otherBlock = relocationMapping[exitInstruction.NextIP].RelocatedRip;
                assembler.jmp(otherBlock);
                relocatedBlock.RelocatedInstructions.AddRange(assembler.Instructions);
                var idk = assembler.CreateLabel("adssa");
            }

            else
            {
                throw new InvalidOperationException(String.Format("Control flow of type {0} is not handled.", exitInstruction.FlowControl));
            }
        }
    }
}
