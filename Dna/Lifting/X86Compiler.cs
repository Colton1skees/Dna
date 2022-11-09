using Dna.ControlFlow;
using Dna.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    public class X86Compiler
    {
        private readonly ICpuArchitecture architecture;

        private Iced.Intel.Assembler assembler = new Iced.Intel.Assembler(64);

        public X86Compiler(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
        }

        public void CompileIrCfg(ControlFlowGraph<AbstractInst> cfg)
        {
            foreach(var block in cfg.GetBlocks())
                CompileBlock(block);
        }

        private void CompileBlock(BasicBlock<AbstractInst> block)
        {
            int numLiveTemporaries = 0;
            var temporaryUses = GetAllTemporaryUses(block.Instructions);
            foreach(var instruction in block.Instructions)
            {
                
            }
        }

        private Dictionary<IOperand, OrderedSet<AbstractInst>> GetAllTemporaryUses(List<AbstractInst> instructions)
        {
            var temporaryUses = new Dictionary<IOperand, OrderedSet<AbstractInst>>();
            foreach (var instruction in instructions)
            {
                foreach (var operand in instruction.Operands)
                {
                    // Skip the operand if it is not a temporary.
                    if (operand is not TemporaryOperand)
                        continue;

                    // Create a hashset if it does not already exist.
                    if (!temporaryUses.ContainsKey(operand))
                        temporaryUses[operand] = new OrderedSet<AbstractInst>();

                    // Store the use of the temporary.
                    temporaryUses[operand].Add(instruction);
                }
            }

            return temporaryUses;
        }

        private List<Iced.Intel.Instruction> CompileInstruction(AbstractInst instruction)
        {
            var output = new List<Iced.Intel.Instruction>();  
            switch (instruction)
            {
                case InstAdd inst:
                    break;
                default:
                    break;

            }

            return output;
        }
    }
}
