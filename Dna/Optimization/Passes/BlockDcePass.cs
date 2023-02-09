using Dna.ControlFlow;
using Dna.DataStructures;
using Dna.Symbolic;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization.Passes
{
    /// <summary>
    /// Class for deleting deadcode within the scope of a basic block.
    /// </summary>
    public class BlockDcePass : IOptimizationPass
    {
        private readonly ControlFlowGraph<AbstractInst> cfg;

        public BlockDcePass(ControlFlowGraph<AbstractInst> cfg)
        {
            this.cfg = cfg;
        }

        public void Run()
        {
            foreach (var block in cfg.GetBlocks())
            {
                // Construct ssa form for the basic block.
                var versionMapping = BlockSsaConstructor.ConstructSsa(block);

                // Use the version mapping to identify and discard dead assignments.
                DeleteDeadcode(block, versionMapping);

                // Execute out of ssa transformation.
                BlockSsaConstructor.DestructSsa(block);
            }
        }

        private void DeleteDeadcode(Block block, Dictionary<IOperand, OrderedSet<SsaOperand>> versionMapping)
        {
            block.Instructions.Reverse();
            HashSet<AbstractInst> toDelete = new HashSet<AbstractInst>();
            foreach(var inst in block.Instructions)
            {
                // If the instruction does not write to anything, 
                // then it's a memory store.
                if (!inst.HasDestination)
                    continue;

                // Skip if the ssa operand is read at a later point.
                var ssaDest = (SsaOperand)inst.Dest;
                if (ssaDest.Users.Any())
                    continue;

                // While temporaries and flag registers are local to each basic block,
                // general purpose registers are not. Because of this, if this instruction
                // sets the last definition a register, then we need to preserve it.
                bool isGpr = ssaDest.BaseOperand is RegisterOperand regDest && regDest.Bitsize != 1;
                if(isGpr && versionMapping[ssaDest.BaseOperand].Last().Version == ssaDest.Version)
                {
                    continue;
                }

                // If anything reads from this instruction,
                // then we cannot delete it.
                if (ssaDest.Users.Any())
                    continue;

                // At this point, we have determined that the instruction is safe to discard.
                // For each input ssa operand, delete this instruction from it's user list.
                foreach(var ssaOp in inst.Operands.Where(x => x is SsaOperand).Cast<SsaOperand>())
                {
                    ssaOp.Users.Remove(inst);
                }

                // Finally, discard this definition and mark the instruction for deletion.
                versionMapping[ssaDest.BaseOperand].Remove(ssaDest);
                toDelete.Add(inst);
            }

            // Update the block instructions.
            block.Instructions.Reverse();
            block.Instructions = block.Instructions.Where(x => !toDelete.Contains(x)).ToList();
        }
    }
}
