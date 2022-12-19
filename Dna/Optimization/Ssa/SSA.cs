using Dna.ControlFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Optimization.Ssa
{
    /*
     Generic class for static single assignment (SSA) transformation

     Handling of
     - variable generation
     - variable renaming
     - conversion of an IRCFG block into SSA
    
     Variables will be renamed to <variable>.<index>, whereby the
     index will be increased in every definition of <variable>.

     Memory expressions are stateless. The addresses are in SSA form,
     but memory aliasing will occur. For instance, if it holds
     that RAX == RBX.0 + (-0x8) and

     @64[RBX.0 + (-0x8)] = RDX
     RCX.0 = @64[RAX],

     then it cannot be tracked that RCX.0 == RDX.
    */
    public class SSA
    {
        private readonly ControlFlowGraph<AbstractInst> ircfg;

        public SSA(ControlFlowGraph<AbstractInst> ircfg)
        {
            this.ircfg = ircfg;
        }
    }
}
