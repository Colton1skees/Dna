using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public class ExprId : Expr
    {
        public string Name { get; }

        public ExprId(string name, uint size) : base(size)
        {
            Name = name;
        }
    }
}
