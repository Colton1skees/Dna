using Dna.Simplification;
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
    public class SymbolicExecutionEngine : ISymbolicExecutionEngine
    {
        private ISymbolicAstBuilder astBuilder;

        /// <summary>
        /// A mapping of each operand's symbolic value.
        /// </summary>
        private Dictionary<IOperand, AbstractNode> variableDefinitions = new();

        /// <summary>
        /// A mapping of each memory node's symbolic value.
        /// </summary>
        private Dictionary<MemoryNode, AbstractNode> memoryDefinitions = new();

        public IReadOnlyDictionary<IOperand, AbstractNode> VariableDefinitions => variableDefinitions.AsReadOnly();

        public IReadOnlyDictionary<MemoryNode, AbstractNode> MemoryDefinitions => memoryDefinitions.AsReadOnly();

        public SymbolicExecutionEngine(dgGetAstFromSymbolicState getAstFromSymbolicState)
        {
            astBuilder = new SymbolicAstBuilder(getAstFromSymbolicState);
        }

        public void ExecuteInstruction(AbstractInst inst)
        {
            if (inst is InstStore storeInst)
            {
                var ast = astBuilder.GetStoreAst(storeInst);
                StoreMemoryDefinition(ast.destination, ast.source);
            }

            else
            {
                var ast = astBuilder.GetAst(inst);
                StoreOperandDefinition(ast.destination, ast.source);
            }
        }

        public AbstractNode GetMemoryDefinition(MemoryNode memoryNode)
        {
            return memoryDefinitions[memoryNode];
        }

        public void StoreMemoryDefinition(MemoryNode memoryNode, AbstractNode value)
        {
            memoryDefinitions[memoryNode] = value;
        }

        public AbstractNode GetOperandDefinition(IOperand operand)
        {
            return variableDefinitions[operand];
        }

        public void StoreOperandDefinition(IOperand operand, AbstractNode value)
        {
            variableDefinitions[operand] = value;
        }
    }
}
