using Dna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Optimization.Passes
{
    /// <summary>
    /// Optimization pass for discarding all unnecessary temporaries.
    /// Note: This pass assumes that all temporaries have a single assignment.
    /// </summary>
    public class BlockTemporaryPropagationPass : IOptimizationPass
    {
        private readonly List<AbstractInst> instructions;

        public BlockTemporaryPropagationPass(List<AbstractInst> instructions)
        {
            this.instructions = instructions;
        }

        public void Run()
        {
            // Identify the instruction where each temporary is defined.
            var tempDefs = GetTemporaryDefinitions();
            
            // Propagate all redundant copied values to their usages.
            PropagateTemporaryClones(tempDefs);

            // Recompute all temporary definitions.
            tempDefs = GetTemporaryDefinitions();

            // Propagate all assignments from a temporary to it's definition.
            PropgateTemporariesToDestination(tempDefs);

            // Recompute all temporary definitions.
            tempDefs = GetTemporaryDefinitions();

            // Discard all unused temporaries.
            RemoveUselessTemporaries(tempDefs);
        }

        private Dictionary<IOperand, AbstractInst> GetTemporaryDefinitions()
        {
            var definitionMapping = new Dictionary<IOperand, AbstractInst>();
            foreach(var inst in instructions)
            {
                // Filter to only include instructions which write to a temporary.
                if (inst.Dest == null || inst.Dest is not TemporaryOperand || inst is InstStore)
                    continue;

                // Since temporaries can only be written to once, we throw an
                // exception if the temporary is defined in two places.
                if (definitionMapping.ContainsKey(inst.Dest))
                    throw new InvalidOperationException(String.Format("Temporary {0} has more than one definition.", inst.Dest));

                definitionMapping[inst.Dest] = inst;
            }

            return definitionMapping;
        }

        private void PropagateTemporaryClones(Dictionary<IOperand, AbstractInst> tempDefs)
        {
            // Identify all redundant copies to temporaries.
            var redundantTemps = new Dictionary<IOperand, InstCopy>(tempDefs
                .Where(x => x.Value.Id == InstructionId.Copy)
                .Select(x => new KeyValuePair<IOperand, InstCopy>(x.Key, (InstCopy)x.Value)));

            foreach(var instruction in instructions)
            {
                // Create a predicate for selecting the copied value.
                var getReplacement = (IOperand inputOperand) => { return redundantTemps[inputOperand].Op1; };

                // Replace all usages of the redundant temporaries with their true value.
                instruction.Operands.ReplaceAll(x => redundantTemps.ContainsKey(x), getReplacement);
            }
        }

        private void PropgateTemporariesToDestination(Dictionary<IOperand, AbstractInst> tempDefs)
        {
            // Get a list of all cases where a temporary is used.
            var temporaryUses = GetAllTemporaryUses();

            // Transform this:
            //      t2:64 = select t0, t1
            //      Reg(rip):64 = copy t2
            // To this:
            //      Reg(rip):64 = select t0, t1
            //      Discard: Reg(rip):64 = copy t2
            HashSet<AbstractInst> instructionsToDelete = new HashSet<AbstractInst>();
            foreach(var inst in instructions)
            {
                // Skip the instruction if it is not a copy.
                if (inst is not InstCopy)
                    continue;

                // Skip the instruction if it is not copying from a temporary.
                if (inst.Op1 is not TemporaryOperand)
                    continue;

                // If a temporary is used in multiple places, then we cannot propagate it from use to def.
                if (temporaryUses.ContainsKey(inst.Op1) && temporaryUses[inst.Op1].Count > 1)
                    continue;

                // Otherwise, we are free to discard the temporary altogether
                // and overwrite the definition destination.
                var tempDef = tempDefs[inst.Op1];
                tempDef.Dest = inst.Dest;
                instructionsToDelete.Add(inst);
            }

            // Discard all old assignments.
            var newList = new List<AbstractInst>(instructions.Count - instructionsToDelete.Count);
            foreach (var inst in instructions)
            {
                if (instructionsToDelete.Contains(inst))
                    continue;

                newList.Add(inst);
            }

            instructions.Clear();
            instructions.AddRange(newList);
        }

        private Dictionary<IOperand, HashSet<AbstractInst>> GetAllTemporaryUses()
        {
            var temporaryUses = new Dictionary<IOperand, HashSet<AbstractInst>>();
            foreach (var instruction in instructions)
            {
                foreach (var operand in instruction.Operands)
                {
                    // Skip the operand if it is not a temporary.
                    if (operand is not TemporaryOperand)
                        continue;

                    // Create a hashset if it does not already exist.
                    if (!temporaryUses.ContainsKey(operand))
                        temporaryUses[operand] = new HashSet<AbstractInst>();

                    // Store the use of the temporary.
                    temporaryUses[operand].Add(instruction);
                }
            }

            return temporaryUses;
        }

        private void RemoveUselessTemporaries(Dictionary<IOperand, AbstractInst> tempDefs)
        {
            var instructionsToDelete = new HashSet<AbstractInst>();
            var temporaryUses = GetAllTemporaryUses();
            foreach (var tempDef in tempDefs.Reverse())
            {
                // Get a mapping of all usages of the current temporary.
                var uses = temporaryUses.ContainsKey(tempDef.Key) ? temporaryUses[tempDef.Key] : new HashSet<AbstractInst>();

                // Discard all usages which have been already been killed.
                if (uses.Count > 0)
                    uses.RemoveWhere(x => instructionsToDelete.Contains(x));

                // Skip if the temporary has legitimate usages.
                if (uses.Count > 0)
                    continue;

                // Mark the instruction for deletion.
                // instructionsToDelete.Add(tempDef.Value);
            }

            // Discard all old assignments.
            var newList = new List<AbstractInst>(instructions.Count - instructionsToDelete.Count);
            foreach (var inst in instructions)
            {
                if (instructionsToDelete.Contains(inst))
                    continue;

                newList.Add(inst);
            }

            instructions.Clear();
            instructions.AddRange(newList);
        }
    }
}
