using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Simplification
{
    public class RewriteRules
    {
        AstContext astCtxt = new AstContext();

        public Dictionary<AbstractNode, AbstractNode> GetRewriteRules()
        {
            var rules = new Dictionary<AbstractNode, AbstractNode>();
            for(uint i = 8; i <= 64; i *= 2)
            {
                var op1 = new TemporaryNode(0, i);
                rules.Add(GetZfExpression(op1), new ZeroNode(op1));
            }
            return rules;
        }

        private AbstractNode GetZfExpression(TemporaryNode op1)
        {
            var node = this.astCtxt.ite(
                          this.astCtxt.equal(
                            op1,
                            this.astCtxt.bv(0, op1.BitSize)
                          ),
                          this.astCtxt.bv(1, 1),
                          this.astCtxt.bv(0, 1)
                        );

            return node;
        }
    }
}
