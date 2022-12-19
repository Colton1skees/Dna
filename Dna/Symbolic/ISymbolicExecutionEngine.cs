using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Symbolic
{
    public interface ISymbolicExecutionEngine
    {
        /// <summary>
        /// Gets a readonly mapping of each operand's symbolic value.
        /// </summary>
        public IReadOnlyDictionary<IOperand, Expr> VariableDefinitions { get; }

        /// <summary>
        /// Gets a readonly mapping of each memory node's symbolic value.
        /// </summary>
        public IReadOnlyDictionary<MemoryNode, Expr> MemoryDefinitions { get; }

        public void ExecuteInstruction(AbstractInst inst);
    }
}
