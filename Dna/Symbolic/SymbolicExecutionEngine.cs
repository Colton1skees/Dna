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
                // If we encounter a memory store, then first we need to compute symbolic
                // expressions for the source and destination.
                var ast = astBuilder.GetStoreAst(storeInst);

                // The symbolic execution engine allows users to specify a callback to intercept
                // and handle memory writes themselves. If this callback is provided, then we
                // allow a consumer to intercept and handle the memory write event.
                // If a callback is not provided, then we store a symbolic memory
                // write at the symbolic memory location. 
                if (memoryWriteCallback != null)
                    memoryWriteCallback.Invoke(ast.destination, ast.source);
                else
                    memoryDefinitions[ast.destination] = ast.source;
            }

            else
            {
                // Symbolic register assignments are handled in the same manner as memory 
                // stores - a callback exists for the consumer to overwrite and handle these events.
                var ast = astBuilder.GetAst(inst);
                if (variableWriteCallback != null)
                    variableWriteCallback.Invoke(inst.Dest, ast.source);
                else
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
