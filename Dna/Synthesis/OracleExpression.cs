using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Synthesis
{
    public class ExpressionIo
    {
        public ulong Output { get; set; }

        public Dictionary<TemporaryNode, ulong> Inputs { get; }

        public ExpressionIo()
        {
            Inputs = new Dictionary<TemporaryNode, ulong>();
        }

        public ExpressionIo(Dictionary<TemporaryNode, ulong> inputs)
        {
            Inputs = inputs;
        }
    }

    public class OracleExpression
    {
        public OracleExpression(AbstractNode expression, AbstractNode? simplifiedExpression)
        {
            Expression = expression;
            SimplifiedExpression = simplifiedExpression;
        }

        public AbstractNode Expression { get; }

        public AbstractNode? SimplifiedExpression { get; }

        public List<ExpressionIo> Io { get; } = new List<ExpressionIo>();
    }
}
