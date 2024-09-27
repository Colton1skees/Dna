using Dna.Symbolic;
using Microsoft.Z3;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    public record struct AstSubstitution(AbstractNode from, AbstractNode to);

    public class AstEvaluator
    {
        private readonly Z3AstBuilder z3Translator = new Z3AstBuilder(new Microsoft.Z3.Context());

        public ulong? Evaluate(AbstractNode ast, AstSubstitution astSubstitution)
        {
            var dict = new Dictionary<AbstractNode, AbstractNode>()
            {
                [astSubstitution.from] = astSubstitution.to
            };

            return Evaluate(ast, dict);
        }

        public ulong? Evaluate(AbstractNode ast, IReadOnlyDictionary<AbstractNode, AbstractNode> variableNodeSubstitutions)
        {
            var isDefined = (AbstractNode src) => variableNodeSubstitutions.ContainsKey(src);
            var getVar = (AbstractNode src) => variableNodeSubstitutions[src];

            // Convert the abstract node to an ast.
            var z3Ast = z3Translator.GetZ3Ast(ast, isDefined, getVar);

            // Use z3's simplifier as an expression evaluator.
            // If the expression is resolvable to a constant, it will yield a `BitVecNum` type.
            // Otherwise it will yield some ast type structure.
            var evaluation = z3Ast.Simplify();

            return evaluation is BitVecNum bvNum ? bvNum.UInt64 : null;
        }
    }
}
