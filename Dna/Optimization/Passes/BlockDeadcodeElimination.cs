using Dna.ControlFlow.DataStructures;
using Dna.DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Optimization.Passes
{
    public enum UseKind
    {
        Read = 0,
        Write = 1,
    }

    public class OperandUsage
    {
        public OrderedSet<int> ReadIndices { get; } = new OrderedSet<int>();

        public OrderedSet<int> WriteIndices { get; } = new OrderedSet<int>();
    }

    public class BlockDeadcodeElimination : IOptimizationPass
    {
        private readonly ICpuArchitecture architecture;

        private readonly List<AbstractInst> instructions;

        public BlockDeadcodeElimination(ICpuArchitecture architecture, List<AbstractInst> instructions)
        {
            this.architecture = architecture;
            this.instructions = instructions;
        }

        public void Run()
        {
            var useMapping = GetRegisterUseMapping();

            RemoveDeadAssignments(useMapping);
        }

        /// <summary>
        /// Gets a collection of all reads and writes to a register.
        /// </summary>
        private Dictionary<IOperand, OperandUsage> GetRegisterUseMapping()
        {
            // Create the output mapping.
            var usageMapping = new Dictionary<IOperand, OperandUsage>();

            // Collect all reads and writes to an operand.
            for (int i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                if (inst.Dest != null)
                {
                    if (!usageMapping.ContainsKey(inst.Dest))
                        usageMapping[inst.Dest] = new OperandUsage();
                    usageMapping[inst.Dest].WriteIndices.Add(i);
                }

                foreach (var operand in inst.Operands)
                {
                    if (operand is RegisterOperand regOperand)
                    {
                        if (!usageMapping.ContainsKey(regOperand))
                            usageMapping[regOperand] = new OperandUsage();
                        usageMapping[regOperand].ReadIndices.Add(i);
                        continue;
                    }

                    if (operand is TemporaryOperand tempOperand)
                    {
                        if (!usageMapping.ContainsKey(tempOperand))
                            usageMapping[tempOperand] = new OperandUsage();
                        usageMapping[tempOperand].ReadIndices.Add(i);
                    }
                }
            }

            return usageMapping;
        }

        private void RemoveDeadAssignments(Dictionary<IOperand, OperandUsage> useMapping)
        {
            var reversedInstructions = instructions.ToList();
            reversedInstructions.Reverse();

            OrderedSet<int> indicesToDelete = new OrderedSet<int>();


            Dictionary<IOperand, int> latestWrites = new Dictionary<IOperand, int>();
            Dictionary<IOperand, int> latestReads = new Dictionary<IOperand, int>();


            for (int i = 0; i < reversedInstructions.Count; i++)
            {
                // Get the current instruction.
                var inst = reversedInstructions[i];

             //   if (inst.ToString().Contains("Reg(rip):64 ="))
                //    Debugger.Break();

                int iCopy = reversedInstructions.Count - i - 1;

                // Get the instruction destination.
                var instDest = inst.Dest;

                // Get the latest read and write index.
                var destUses = useMapping[instDest];
                var lastWrite = destUses.WriteIndices.Any() ? (destUses.WriteIndices.Last()) : -1;
                var lastRead = destUses.ReadIndices.Any() ? ( destUses.ReadIndices.Last()) : -1;

                // If the write destination is never overwritten, then we can't discard it.
                bool isOverwritten = lastWrite != -1 && lastWrite > iCopy;

                // If the destination is read before something overwrites it, then it is not a dead store.
                bool isReadBeforeWritten = lastRead == -1 ? false : lastRead > iCopy && lastRead < lastWrite;

                // Update the hashset indices.
              //  if (instDest.Name.ToLower().Contains("af"))
                 //   Debugger.Break();
                if (destUses.WriteIndices.Last() != iCopy)
                {
                    destUses.WriteIndices.Remove(destUses.WriteIndices.Last());
                }

                foreach(var operand in inst.Operands)
                {
                    if (!useMapping.ContainsKey(operand))
                        continue;

                    var opUses = useMapping[operand];
                    if(opUses.ReadIndices.Last() != iCopy)
                    {
                        opUses.ReadIndices.Remove(opUses.ReadIndices.Last());
                    }
                }

                // Propagate the latest reads and writes downwards.
                if (isOverwritten && !isReadBeforeWritten)
                {
                    // If assignment is overwritten before it is read, then we discard it and update the usage mapping.
                    indicesToDelete.Add(iCopy);
                    foreach (var operand in inst.Operands)
                    {
                        if (!useMapping.ContainsKey(operand))
                            continue;

                        var operandUsages = useMapping[operand];
                        operandUsages.ReadIndices.Remove(iCopy);
                    }
                }
            }


            var newList = new List<AbstractInst>(instructions.Count - indicesToDelete.Count);
            for(int i = 0; i < instructions.Count; i++)
            {
                if (indicesToDelete.Contains(i))
                    continue;

                newList.Add(instructions[i]);
            }

            instructions.Clear();
            instructions.AddRange(newList);
        }
    }
}
