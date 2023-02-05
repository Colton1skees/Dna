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

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is ExprId exprId && exprId.Name == Name)
                return true;
            return base.Equals(obj);
        }
    }
}
