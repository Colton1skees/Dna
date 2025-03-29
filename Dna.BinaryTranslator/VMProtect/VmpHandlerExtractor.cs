using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Relocation;
using Iced.Intel;
using Rivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static Iced.Intel.AssemblerRegisters;

namespace Dna.BinaryTranslator.VMProtect
{
    public class VmpHandlerExtractor
    {
        private readonly IDna dna;

        public VmpHandlerExtractor(IDna dna)
        {
            this.dna = dna;
        }

        public IReadOnlyList<Instruction> Process(ulong address, bool isVmEnter = false)
        {
            if (isVmEnter)
                return ProcessVmEnter(address).GetBlocks().Single().Instructions.ToList();
            else
                return ProcessStandardHandler(address);
        }

        private ControlFlowGraph<Instruction> ProcessVmEnter(ulong addr)
        {
            // Disassemble the push and the call.
           // var push = dna.BinaryDisassembler.GetInstructionAt(addr);
            //var call = dna.BinaryDisassembler.GetInstructionAt(push.NextIP);

            var callCfg = dna.RecursiveDescent.ReconstructCfg(addr);
            Console.WriteLine(callCfg) ;
            // Create a new control flow graph, where all of the vmenter instructions are merged into.
            var fullCfg = new ControlFlowGraph<Instruction>(addr);
            var entryBb = fullCfg.CreateBlock(addr);


            // Add the push and the call.
            //  entryBb.Instructions.Add(push);
            //  entryBb.Instructions.Add(call);

            // Remove all branches from the sequence.
            var straightLineSequence = GetStraightLineSequence(callCfg).Where(x => (!x.FlowControl.IsBranch()) || x.ToString().Contains("jmp r"));

            // Remove the stack alignment and stack allocation sequence.
            straightLineSequence = ReplaceStackAlignmentAndAllocation(straightLineSequence.ToList());

            // Append the straight line sequence into the new basic block.
            entryBb.Instructions.AddRange(straightLineSequence);

            //var assembler = new Assembler(64);
            //assembler.push(AssemblerRegisters.r13);
            //var encodedInst = InstructionEncoder.RelocateInstructions(assembler.Instructions.ToList(), addr).Single();
            //entryBb.Instructions.Insert(0, encodedInst);


            return fullCfg;
        }
        private IReadOnlyList<Instruction> GetStraightLineSequence(ControlFlowGraph<Instruction> cfg, BasicBlock<Instruction>? ignore = null)
        {
            List<Instruction> output = new();

            var next = cfg.GetBlocks().First();
            while (next != null)
            {
                output.AddRange(next.Instructions);
                if (!next.OutgoingEdges.Any())
                    break;

                next = next.GetOutgoingEdges().First(e => e.TargetBlock != ignore).TargetBlock;
            }

            return output;
        }

        private IReadOnlyList<Instruction> ReplaceStackAlignmentAndAllocation(IReadOnlyList<Instruction> inputInstructions)
        {
            var subRsp = inputInstructions.Single(x => x.ToString().Contains("sub rsp,"));
            var andRsp = inputInstructions.Single(x => x.ToString().Contains("and rsp,"));

            var assembler = new Assembler(64);
            var output = new List<Instruction>();
            foreach (var inst in inputInstructions)
            {
                // Skip the AND RSP instruction.
                if (inst == andRsp)
                    continue;
                // Skip if this is not the sub rsp, 0x
                if (inst != subRsp)
                {
                    output.Add(inst);
                    continue;
                }

                // Allocate a massive stack space, then relocate the instruction to the correct address.
                assembler.sub(rsp, 12582912);
                var encodedInst = InstructionEncoder.RelocateInstructions(assembler.Instructions.ToList(), inst.IP).Single();
                output.Add(encodedInst);
            }

            return output;
        }

        private IReadOnlyList<Instruction> ProcessStandardHandler(ulong address)
        {
            var cfg = dna.RecursiveDescent.ReconstructCfg(address);


            // Get the next instruction guaranteed to sequentially execute after this one.
            // Returns the block, the index of the instruction within the list, and the instruction itself.
            // Importantly, this skips direct jumps; however, it will *not* skip conditional or indirect jumps.
            (BasicBlock<Instruction>?, int, Instruction) GetNext(BasicBlock<Instruction> block, int index)
            {
                if ((block.Instructions.Count - 1) == index)
                {
                    // Try to go to next block if we're at the end of the instruction list and can do so soundly.
                    if (block.Instructions[index].FlowControl is not FlowControl.Next or FlowControl.UnconditionalBranch)
                    {
                        return (null, -1, default);
                    }

                    // We should always have a successor for fallthrough edges or unconditional jumps.
                    index = 0;
                    block = (BasicBlock<Instruction>)block.GetSuccessors().First();
                }
                else
                {
                    // Just go to the next instruction if we can.
                    index++;
                }

                var instr = block.Instructions[index];
                while (block.Instructions[index].Mnemonic == Mnemonic.Cmc || block.Instructions[index].Mnemonic == Mnemonic.Bt)
                    index++;

                // Follow jumps (including blocks with only jumps in them) until we reach a fixpoint.
                // TODO: this can loop infinitely :(
                while (instr.IsJmpShortOrNear)
                {
                    block = (BasicBlock<Instruction>)block.GetSuccessors().First();

                    index = 0;
                    instr = block.Instructions[index];
                }

                return (block, index, block.Instructions[index]);
            }

            BasicBlock<Instruction> ignore = null;

            // Iterate through all instructions of all blocks and try to match the stack comparison pattern.
            // We assume there's only one per handler, so we bail as soon as we find one.
            foreach (var block in cfg.GetBlocks())
            {
                for (var i = 0; i < block.Instructions.Count; i++)
                {
                    /*
                     * Match:
                     * lea reg, [rsp + 0x140]
                     * cmp rbx, reg
                     * ja [dest]
                     */
                    var (block1, index1, inst1) = GetNext(block, i);
                    if (index1 == -1) break;

                    if (!(inst1.Code == Code.Lea_r64_m && inst1.MemoryBase == Register.RDI && inst1.MemoryDisplacement64 == 0xe0))
                        continue;

                    var (block2, index2, inst2) = GetNext(block1!, index1);
                    if (index2 == -1) break;

                    if (!((inst2.Code == Code.Cmp_r64_rm64 || inst2.Code == Code.Cmp_rm64_r64) && inst2.Op0Register == Register.RBP && inst2.Op1Register == inst1.Op0Register))
                        continue;

                    var (block3, index3, inst3) = GetNext(block2!, index2);
                    if (index3 == -1) break;

                    if (inst3.Code != Code.Ja_rel32_64)
                        continue;

                    // We want to ignore the fallthrough target when getting our new straight-line sequence, since it's the one with stack readjustment code.
                    ignore = (BasicBlock<Instruction>)cfg.Nodes[inst3.NextIP.ToString("X")];
                    break;
                }
            }

            // VMP handlers are all straight-line code so this should be okay.
            var result = GetStraightLineSequence(cfg, ignore).Where(x => !(x.IsJmpShortOrNear || x.IsJccShortOrNear)).ToList();

            var output = new List<Instruction>();
            foreach(var input in result)
            {
                // Special case: Remill does not support the MOVZX_GPRv_MEMw_16 variant of movzx.
                if (input.Mnemonic == Mnemonic.Movzx && input.Op1Kind == OpKind.Memory && input.MemorySize == MemorySize.UInt16)
                {
                    if (input.ToString() != "movzx ax,word ptr [rsi-2]")
                        throw new InvalidOperationException($"TODO: Handle this movzx instantiation!");
                    var assembler = new Assembler(64);
                    assembler.mov(rax, __word_ptr[rsi - 2]);
                    assembler.and(rax, 65535);

                    var relocations = InstructionEncoder.RelocateInstructions(assembler.Instructions.ToList(), input.IP);
                    output.AddRange(relocations);
                    continue;
                }
                output.Add(input);
            }

            return output;
        }
    }
}
