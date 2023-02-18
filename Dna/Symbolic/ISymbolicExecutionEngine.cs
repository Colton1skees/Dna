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
        public IReadOnlyDictionary<IOperand, AbstractNode> VariableDefinitions { get; }

        public IReadOnlyDictionary<MemoryNode, AbstractNode> MemoryDefinitions { get; }

        public void ExecuteInstruction(AbstractInst inst);

        public AbstractNode GetOperandDefinition(IOperand operand);

        public void StoreOperandDefinition(IOperand operand, AbstractNode value);

        public AbstractNode GetMemoryDefinition(MemoryNode memoryNode);

        public void StoreMemoryDefinition(MemoryNode memoryNode, AbstractNode value);
    }
}
