using Dna.Simplification;
using LLVMSharp;
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

        private dgOnSymbolicVariableWrite variableWriteCallback;

        private dgOnSymbolicMemoryWrite memoryWriteCallback;

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

        public SymbolicExecutionEngine(Func<IOperand, AbstractNode> symbolicAstEvaluator)
        {
            if (symbolicAstEvaluator == null)
                throw new ArgumentNullException(nameof(symbolicAstEvaluator));
            astBuilder = new SymbolicAstBuilder(symbolicAstEvaluator);
        }

        public void ExecuteInstruction(AbstractInst inst)
        {
            if (inst is InstStore storeInst)
            {
                // Note: Users are optionally allowed 
                var ast = astBuilder.GetStoreAst(storeInst);
                if(memoryWriteCallback?.Invoke(ast.destination, ast.source) == true)
                    memoryDefinitions[ast.destination] = ast.source;
            }

            else
            {
                var ast = astBuilder.GetAst(inst);
                if(variableWriteCallback?.Invoke(inst.Dest, ast.source) == true)
                    variableDefinitions[inst.Dest] = ast.source;
            }
        }

        public void SetSymbolicVariableWriteCallback(dgOnSymbolicVariableWrite callback)
        {
            variableWriteCallback = callback;
        }

        public void SetSymbolicMemoryWriteCallback(dgOnSymbolicMemoryWrite callback)
        {
            memoryWriteCallback = callback;
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
