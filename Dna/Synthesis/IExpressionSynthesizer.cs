using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Synthesis
{
    public interface IExpressionSynthesizer
    {
        public void SynthesizeExpression(AbstractNode node);
    }
}
