using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Symbolic
{
    public delegate void dgOnSymbolicVariableWrite(IOperand operand, AbstractNode value);

    public delegate void dgOnSymbolicMemoryWrite(MemoryNode memoryNode, AbstractNode value);

    public interface ISymbolicExecutionEngine
    {
        /// <summary>
        /// A mapping of each variable's symbolic value.
        /// </summary>
        public IReadOnlyDictionary<IOperand, AbstractNode> VariableDefinitions { get; }

        // A mapping of each memory node's symbolic value.
        public IReadOnlyDictionary<MemoryNode, AbstractNode> MemoryDefinitions { get; }

        /// <summary>
        /// Symbolically executes the provided instruction.
        /// </summary>
        /// <param name="inst"></param>
        public void ExecuteInstruction(AbstractInst inst);

        /// <summary>
        /// Sets a callback which is invoked whenever the symbolic executor assigns a new AST to a variable.
        /// Note that this callback is *not* when the user assigns an AST via <see cref="StoreOperandDefinition(IOperand, AbstractNode)"/>.
        /// It is only invoked when the symbolic executor assigns a new AST via <see cref="ExecuteInstruction(AbstractInst))"/>.
        /// </summary>
        public void SetSymbolicVariableWriteCallback(dgOnSymbolicVariableWrite callback);

        /// <summary>
        /// Sets a callback which is invoked whenever the symbolic executor assigns a new AST to a memory location.
        /// Note that this callback is *not* when the user assigns an AST via <see cref="StoreMemoryDefinition(MemoryNode, AbstractNode)"/>.
        /// It is only invoked when the symbolic executor assigns a new AST via <see cref="ExecuteInstruction(AbstractInst))"/>.
        /// </summary>
        public void SetSymbolicMemoryWriteCallback(dgOnSymbolicMemoryWrite callback);

        /// <summary>
        /// Gets the symbolic AST for the provided variable.
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public AbstractNode GetOperandDefinition(IOperand operand);

        /// <summary>
        /// Assigns the AST to the provided operand.
        /// </summary>
        public void StoreOperandDefinition(IOperand operand, AbstractNode value);

        /// <summary>
        /// Gets the symbolic ast for the provided memory node.
        /// </summary>
        public AbstractNode GetMemoryDefinition(MemoryNode memoryNode);

        /// <summary>
        /// Assigns the AST to the provided memory location.
        /// </summary>
        /// <param name="memoryNode"></param>
        /// <param name="value"></param>
        public void StoreMemoryDefinition(MemoryNode memoryNode, AbstractNode value);
    }
}
