using Dna.ControlFlow;
using Rivers.Analysis.Traversal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

using Block = Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>;

namespace Dna.Optimization
{
    /// <summary>
    /// This is an adapted implementation of:
    ///     - Simple and Efficient Construction of Static Single Assignment Form (2013) Braun, M., et al.
    ///     - https://pp.info.uni-karlsruhe.de/uploads/publikationen/braun13cc.pdf
    ///     
    /// 
    /// </summary>
    public class SSAConstructor
    {
        /*
        private ControlFlowGraph<AbstractInst> cfg;

        private Dictionary<IOperand, Dictionary<Block, AbstractInst>> currentDef = new();

        private HashSet<Block> sealedBlocks = new();

        private Dictionary<Block, Dictionary<IOperand, AbstractInst>> incompletePhis = new();

        private Dictionary<Block, HashSet<AbstractInst>> blockPhis;

        public void Construct(ControlFlowGraph<AbstractInst> cfg)
        {
            // Compute a reverse post order list of basic blocks.
            this.cfg = cfg;
            var traversal = new BreadthFirstTraversal();
            var recorder = new TraversalRecorder(traversal);
            traversal.Run(cfg.Nodes.First());
            var orderedBlocks = recorder.Traversal.Cast<Block>().Reverse();

            // Populate blockPhis with a hashset for each entry.
            blockPhis = orderedBlocks
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => new HashSet<AbstractInst>());

            foreach (var block in orderedBlocks)
            {
                Console.WriteLine(block.Address.ToString("X"));
                foreach (var inst in block.Instructions)
                {
                    // Process reads of variables.
                    ProcessReads(inst, block);

                    // Process writes to variables.
                    ProcessWrites(inst, block);
                }

                SealBlock(block);
            }

            Console.WriteLine("Handling block phis: {0}", blockPhis.Sum(x => x.Value.Count));
            foreach(var idk in blockPhis)
            {
                Console.WriteLine("Block: {0}", idk.Key.Address.ToString("X"));
                if (idk.Key.Address == 0x14000103B)
                    Debugger.Break();
                foreach(var i in idk.Value)
                {
                    if (i.Id != InstructionId.Phi)
                        continue;
                    Console.WriteLine(i);
                }
            }
            Console.WriteLine("Done.");
        }

        private void ProcessReads(AbstractInst inst, Block block)
        {
            foreach (var op in inst.Operands)
            {
                // Don't construct SSA form for immediate operands.
                if (op is ImmediateOperand)
                    continue;

                // Otherwise, process the operand.
                var phi = ReadVariable(op, block);
                blockPhis[block].Add(phi);
            }
        }

        private void ProcessWrites(AbstractInst inst, Block block)
        {
            // Skip if the instruction does not write to anything.
            if (!inst.HasDestination)
                return;

            // Otherwise, process the write.
            WriteVariable(inst.Dest, block, inst);
        }

        private AbstractInst ReadVariable(IOperand variable, Block block)
        {
            // Try to get a mapping for the current variable.
            var isFound = currentDef.TryGetValue(variable, out var defMapping);

            // If a map is found, then try to find a definition from the current block.
            // Otherwise, do nothing.
            AbstractInst defPhi = null;
            isFound = isFound ? defMapping.TryGetValue(block, out defPhi) : false;

            // If a phi definition is found in the provided block, the return it.
            // Otherwise, recursively iterate backwards to compute a definition for the variable.
            return isFound ? defPhi : ReadVariableRecursive(variable, block);
        }

        private AbstractInst ReadVariableRecursive(IOperand variable, Block block)
        {
            AbstractInst result = null;

            // In the case of an incomplete cfg, build a placeholder phi.
            if (!sealedBlocks.Contains(block))
            {
                // Create an empty phi.
                result = new InstPhi(variable, block);

                // Insert a self-reference, so that this doesn't look like a completely dead PHI.
                result.Sources.Add(result);

                incompletePhis.TryAdd(block, new Dictionary<IOperand, AbstractInst>());
                incompletePhis[block][variable] = result;
            }

            // In the common case of a single predecessor, no PHI is needed.
            else if (block.IncomingEdges.Count == 1)
            {
                result = ReadVariable(variable, block);
            }

            // In the case of loops, avoid infinite recursion.
            else
            {
                // Create an empty phi.
                result = new InstPhi(variable, block);

                // Insert a self-reference, so that this doesn't look like a completely dead PHI.
                result.Sources.Add(result);

                WriteVariable(variable, block, result);
                result = AddPhiOperands(variable, result as InstPhi);
            }

            WriteVariable(variable, block, result);
            return result;
        }

        private void WriteVariable(IOperand operand, Block block, AbstractInst value)
        {
            // Create a nested dictionary item if it does not exist.
            currentDef.TryAdd(operand, new Dictionary<Block, AbstractInst>());

            // Store the variable definition.
            currentDef[operand][block] = value;
        }


        private InstPhi AddPhiOperands(IOperand variable, InstPhi phi)
        {
            foreach (var pred in phi.Block.GetIncomingEdges())
            {
                var source = ReadVariable(variable, pred.SourceBlock);
                phi.Sources.Add(source);
                source.Users.Add(phi);
            }

            return TryRemoveTrivialPhi(phi);
        }

        private InstPhi TryRemoveTrivialPhi(InstPhi phi)
        {
            InstPhi same = null;
            foreach (var op in phi.Sources)
            {
                // Handle unique value or self-reference.
                if (op == same || op == phi)
                    continue;

                // The phi merges at least two values: not trivial.
                if (same == null)
                    continue;

                same = (InstPhi)op;
            }

            // If the phi is unreachable or in the start block, undefine it.
            if (same == null)
                same = new InstPhi(phi.Dest, phi.Block);

            // Remember all users except the phi itself.
            phi.Users.Remove(phi);
            

            // Reroute all uses of phi to same and remove phi
            foreach (var u in phi.Users)
                phi.Users.Add(u);

            ReplacePhi(phi, same);
            blockPhis[phi.Block].Remove(phi);
            blockPhis[phi.Block].Add(same);

            // Try to recursively remove all phi users, which might have become trivial.
            foreach (var use in phi.Users)
            {
                if(use is not InstPhi phiUse)
                {
                    Console.WriteLine("Use is not a phi.");
                    continue;
                }
                        
                TryRemoveTrivialPhi(phiUse);
            }

            return same;
        }

        private void ReplacePhi(InstPhi old, InstPhi newPhi)
        {
            foreach (var user in old.Users)
            {
                // If the old phi is not used, something is out of sync.
                if (!user.Sources.Contains(old))
                {
                    continue;
                    throw new Exception("Phi is not used");
                }

                user.Sources.Remove(old);
                user.Sources.Add(newPhi);
            }
        }

        private void SealBlock(Block block)
        {
            foreach (var variable in incompletePhis[block])
            {
                AddPhiOperands(variable.Key, (InstPhi)variable.Value);
            }

            sealedBlocks.Add(block);
        }

        */
    }
}
