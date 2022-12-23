using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Simplification
{
    public interface IExpressionSimplifier
    {
        public AbstractNode? SimplifyExpression(AbstractNode expression);
    }
}
