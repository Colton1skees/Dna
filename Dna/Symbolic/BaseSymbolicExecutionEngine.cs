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
    public abstract class BaseSymbolicExecutionEngine
    {
        /// <summary>
        /// Gets a readonly mapping of each operand's symbolic value.
        /// </summary>
        private Dictionary<IOperand, AbstractNode> variableDefinitions = new();

        /// <summary>
        /// Gets a readonly mapping of each memory node's symbolic value.
        /// </summary>
        private Dictionary<MemoryNode, AbstractNode> memoryDefinitions = new();

        /// <summary>
        /// Callback which is invoked whenever a variable(e.g. a register) is updated.
        /// </summary>
        private readonly Action<IOperand> onVariableUpdated;

        /// <summary>
        /// Callback which is invoked whenever a memory location is updated.
        /// </summary>
        private readonly Action<MemoryNode> onMemoryUpdated;

        public IReadOnlyDictionary<IOperand, AbstractNode> VariableDefinitions => variableDefinitions.AsReadOnly();

        public IReadOnlyDictionary<MemoryNode, AbstractNode> MemoryDefinitions => memoryDefinitions.AsReadOnly();

        public BaseSymbolicExecutionEngine(Action<IOperand> onVariableUpdated, Action<MemoryNode> onMemoryUpdated)
        {
            this.onVariableUpdated = onVariableUpdated;
            this.onMemoryUpdated = onMemoryUpdated;
        }

        public abstract void ExecuteInstruction(AbstractInst inst);

        public AbstractNode GetOperandDefinition(IOperand operand)
        {
            return variableDefinitions[operand];
        }

        public void StoreOperandDefinition(IOperand operand, AbstractNode value)
        {
            variableDefinitions[operand] = value;
            onVariableUpdated?.Invoke(operand);
        }

        public AbstractNode GetMemoryDefinition(MemoryNode memoryNode)
        {
            return memoryDefinitions[memoryNode];
        }

        public void StoreMemoryDefinition(MemoryNode memoryNode, AbstractNode value)
        {
            memoryDefinitions[memoryNode] = value;
            onMemoryUpdated?.Invoke(memoryNode);
        }
    }
}
