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
    public class SymbolicExecutionEngine : BaseSymbolicExecutionEngine
    {
        private ISymbolicAstBuilder astBuilder;

        public SymbolicExecutionEngine()
        {
            astBuilder = new SymbolicAstBuilder(GetAstFromSymbolicState);
        }

        private AbstractNode GetAstFromSymbolicState(IOperand operand)
        {
            if(VariableDefinitions.TryGetValue(operand, out AbstractNode ast))
                return ast;
            return CreateOperandNode(operand);
        }

        private AbstractNode CreateOperandNode(IOperand operand)
        {
            if (operand is ImmediateOperand immOp)
                return new IntegerNode(immOp.Value, immOp.Bitsize);
            else if (operand is RegisterOperand regOp)
                return new RegisterNode(regOp.Register);
            else if (operand is TemporaryOperand tempOp)
                return new TemporaryNode(tempOp.Uid, tempOp.Bitsize);
            else if (operand is SsaOperand ssaOp)
                return new SsaVariableNode((VariableNode)CreateOperandNode(ssaOp.BaseOperand), ssaOp.Version);
            else
                throw new InvalidOperationException(String.Format("Cannot create operand node for type {0}", operand.GetType().FullName));
        }

        public override void ExecuteInstruction(AbstractInst inst)
        {
            if(inst is InstStore storeInst)
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
    }
}
