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
    public class BlockExpressionPrettifier : IOptimizationPass
    {
        private readonly ControlFlowGraph<AbstractInst> cfg;

        public BlockExpressionPrettifier(ControlFlowGraph<AbstractInst> cfg)
        {
            this.cfg = cfg;
        }

        public void Run()
        {
            foreach (var block in cfg.GetBlocks())
                NumberBlock(block);

            var targetBlock = cfg.GetBlocks().First();
            OptimizeBlock(targetBlock);
        }

        private void NumberBlock(Block block)
        {
            Dictionary<IOperand, OrderedSet<SsaOperand>> versionMapping = new();
            foreach(var inst in block.Instructions)
            {
                for(int i = 0; i < inst.Operands.Count; i++)
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
                    if(!versions.Any())
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
                if(inst.HasDestination)
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
        }

        private void OptimizeBlock(Block block)
        {
            var symex = new SymbolicExecutionEngine();
            foreach(var inst in block.Instructions.SkipWhile(x => !x.ToString().Contains("t74.0:32 = sx i32 0x0, i32 0xC8")))
            {
                // Skip if the instruction does not write to anything.
                if (!inst.HasDestination)
                    continue;

                symex.ExecuteInstruction(inst);

                var z3Expr = symex.VariableDefinitions[inst.Dest];
                var definitions = BreadthFirstSearch(inst, symex);
                

                if(inst.ToString().Contains("Reg(zf).0:1 = select t69.0, i1 0x1, i1 0x0"))
                {
                    foreach(var def in definitions.Where(x => x.Key.ToString().Contains("t72.0")))
                    {
                        symex.CompareExpression((int)inst.Dest.Bitsize, (BitVecExpr)z3Expr, def.Key);
                    }
                    Console.WriteLine("Returning.");
                    return;
                }
            }
        }

        private Dictionary<IOperand, Expr> BreadthFirstSearch(AbstractInst inst, SymbolicExecutionEngine symex)
        {
            Dictionary<IOperand, Expr> result = new();

            HashSet<AbstractInst> newInsns = new HashSet<AbstractInst>();
            newInsns.Add(inst);

            while (true)
            {
                HashSet<AbstractInst> insnsToDelete = new HashSet<AbstractInst>();

                foreach (var insn in newInsns)
                {
                    foreach (var operand in insn.Operands)
                    {
                        if (operand is not SsaOperand ssaOp)
                            continue;

                        if (!symex.VariableDefinitions.ContainsKey(operand))
                            continue;

                        result.Add(operand, symex.VariableDefinitions[operand]);
                        if (ssaOp.Definition != null)
                            insnsToDelete.Add(ssaOp.Definition);
                    }

                }

                newInsns.Clear();
                foreach (var idk in insnsToDelete)
                    newInsns.Add(idk);

                insnsToDelete.Clear();

                if (newInsns.Count == 0)
                    break;

            }

            return result;
        }
    }
}
