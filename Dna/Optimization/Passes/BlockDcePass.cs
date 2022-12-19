using Dna.ControlFlow;
using Dna.ControlFlow.DataStructures;
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
    // Class for recovering simplified expressions from complex ones.
    // Given a basic block, it uses forward symbolic execution
    // to compute an expression for each known destination.
    //
    // If it detects a simplified expression, then it updates
    // the control flow graph to contain the simplified expression.
    // This optimization pass is mainly meant to recover simple
    // representations of EFlag expressions(e.g. parity, isZero, etc.)

    // The optimization pass is guaranteed to recover simple expressions
    // for all whitelisted operations(short of any alas analysis issues).
    // Additionally, once the expressions are recovered,
    // there is *much* less pressure on the register allocator due to the reduction in temporaries.
    // This allows us to compile a control flow graph down to something very close to the original.
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
                var versionMapping = NumberBlock(block);
                DeleteDeadcode(block, versionMapping);
            }

            //var targetBlock = cfg.GetBlocks().First();
            //OptimizeBlock(targetBlock);
        }

        private Dictionary<IOperand, OrderedSet<SsaOperand>> NumberBlock(Block block)
        {
            Dictionary<IOperand, OrderedSet<SsaOperand>> versionMapping = new();
            foreach (var inst in block.Instructions)
            {
                for (int i = 0; i < inst.Operands.Count; i++)
                {
                    // Fetch the operand.
                    var operand = inst.Operands[i];

                    // Skip if the operand is immediate.
                    if (operand is ImmediateOperand)
                        continue;

                    // Fetch the list of operands.
                    versionMapping.TryAdd(operand, new OrderedSet<SsaOperand>());
                    var versions = versionMapping[operand];

                    // If the operand is not versioned(i.e. the definition exists in another block),
                    // then create a version for it.
                    if (!versions.Any())
                    {
                        var ssaOp = new SsaOperand(operand, null, 0);
                        versions.Add(ssaOp);
                    }

                    // Modify the instruction to utilize the ssa version.
                    var ssa = versions.Last();
                    ssa.Users.Add(inst);
                    inst.Operands[i] = ssa;
                }

                // Update the destination definition.
                if (inst.HasDestination)
                {
                    // Fetch the ssa operand mapping.
                    var dest = inst.Dest;
                    versionMapping.TryAdd(dest, new OrderedSet<SsaOperand>());
                    var versions = versionMapping[dest];

                    // Create and store a new definition for the variable.
                    int newId = !versions.Any() ? 0 : versions.Last().Version + 1;
                    var newDest = new SsaOperand(dest, inst, newId);
                    versionMapping[dest].Add(newDest);

                    // Update the instruction.
                    inst.Dest = newDest;
                }
            }

            return versionMapping;
        }

        private void DeleteDeadcode(Block block, Dictionary<IOperand, OrderedSet<SsaOperand>> versionMapping)
        {
            var symex = new SymbolicExecutionEngine();
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
