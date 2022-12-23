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

        private Dictionary<IOperand, AbstractNode> variableDefinitions = new();

        private Dictionary<MemoryNode, AbstractNode> memoryDefinitions = new();

        public IReadOnlyDictionary<IOperand, AbstractNode> VariableDefinitions => variableDefinitions;

        public IReadOnlyDictionary<MemoryNode, AbstractNode> MemoryDefinitions => memoryDefinitions;

        public SymbolicExecutionEngine()
        {
            astBuilder = new SymbolicAstBuilder(GetAstFromSymbolicState);
        }

        private AbstractNode GetAstFromSymbolicState(IOperand operand)
        {
            if(variableDefinitions.TryGetValue(operand, out AbstractNode ast))
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

        public void ExecuteInstruction(AbstractInst inst)
        {
            if(inst is InstStore storeInst)
            {
                var ast = astBuilder.GetStoreAst(storeInst);
                memoryDefinitions[ast.destination] = ast.source;
                Console.WriteLine("{0} = {1}", ast.destination, ast.source);
            }

            else
            {
                var ast = astBuilder.GetAst(inst);
                variableDefinitions[ast.destination] = ast.source;
                var text = String.Format("{0} = {1}", ast.destination, ast.source);
                Console.WriteLine(text);

                // TODO: Delete this debugging code.
                if (text.Contains("Ite(Equal(Extract(0x1F:I32),0x0:I32),Bvsub(Reg(edx).0),Sx(0x0:I32),0xC8:I32)))),0x0:I32)),0x1:I1),0x0:I1))"))
                {
                    Console.WriteLine("Simplifying expression");

                    /*
                    var ctx = new Context();
                    var z3Builder = new Z3AstBuilder(ctx);

                    var expr = z3Builder.GetZ3Ast(ast.source);
                    Console.WriteLine("AST: {0}\n", expr.ToString());
                    Console.WriteLine("Simplified AST: {0}", expr.Simplify());
                    Console.WriteLine("Done");
                    */

                    IExpressionSimplifier exprSimplifier = new RewriteSimplifier();
                    exprSimplifier.SimplifyExpression(ast.source);
                }
            }
        }
    }
}
