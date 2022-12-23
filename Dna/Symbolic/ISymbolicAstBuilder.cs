using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Expression;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Symbolic
{
    /// <summary>
    /// Class for building symbolic ASTs from intermediate language instructions.
    /// </summary>
    public interface ISymbolicAstBuilder
    {
        /// <summary>
        /// Gets a symbolic AST for the value of an instruction.
        /// </summary>
        /// <returns></returns>
        public (IOperand destination, AbstractNode source) GetAst(AbstractInst inst);

        /// <summary>
        /// Gets a symbolic AST for the destination and source of a store instruction.
        /// </summary>
        /// <param name="inst"></param>
        /// <returns></returns>
        public (MemoryNode destination, AbstractNode source) GetStoreAst(InstStore inst);
    }
}
