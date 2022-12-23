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

    // The optimization pass is should recover simple representations
    // of all expressions, assuming the dataflow graph does not reach
    // beyond the maximum number of nodes.

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
                BlockSsaConstructor.ConstructSsa(block);

            var targetBlock = cfg.GetBlocks().First();
            OptimizeBlock(targetBlock);
        }

        
        private void OptimizeBlock(Block block)
        {
            var symex = new SymbolicExecutionEngine();
            foreach(var inst in block.Instructions.Take(block.Instructions.Count - 1))
            {
                symex.ExecuteInstruction(inst);
            }
        }

        /*
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
        */
    }
}
