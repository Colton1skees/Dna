using Dna.ControlFlow.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate.Operands;
using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization
{
    public static class BlockSsaConstructor
    {
        /// <summary>
        /// In the scope of a single block, apply value numbering such that:
        ///     rax = 5 + 0
        ///     rcx = 5
        ///     rax = rcx + 8
        /// 
        /// Becomes:
        ///     rax.0 = 5 + 0
        ///     rcx.1 = 5
        ///     rax.1 = rcx.0 + 8  
        /// </summary>
        public static Dictionary<IOperand, OrderedSet<SsaOperand>> ConstructSsa(Block block)
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

        /// <summary>
        /// In the scope of a single block, apply ssa destruction such that:
        ///     rax.0 = 5 + 0
        ///     rcx.1 = 5
        ///     rax.1 = rcx.0 + 8  
        ///     
        /// Becomes:
        ///     rax = 5 + 0
        ///     rcx = 5
        ///     rax = rcx + 8
        /// </summary>
        public static void DestructSsa(Block block)
        {
            foreach(var instruction in block.Instructions)
            {
                if (instruction.HasDestination && instruction.Dest is SsaOperand ssaDest)
                    instruction.Dest = ssaDest.BaseOperand;

                // If an ssa operand exists(e.g. rax.3),
                // replace it with the base operand(e.g. rax).
                for(int i = 0; i < instruction.Operands.Count; i++)
                {
                    var op = instruction.Operands[i];
                    if (op is SsaOperand ssaOp)
                        instruction.Operands[i] = ssaOp.BaseOperand;
                }
            }
        }
    }
}
