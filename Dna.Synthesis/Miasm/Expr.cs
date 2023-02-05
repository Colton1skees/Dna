using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Miasm
{
    public abstract class Expr
    {
        public uint Size { get; set; }

        public int Hash { get; set; }

        public Expr(uint size)
        {
            if (size == 0 || size > 64)
                throw new NotSupportedException();
            Size = size;
        }
    }
}
