using Dna.Synthesis.Simplification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dna.Synthesis.Miasm
{
    public abstract class MiasmExpr
    {
        public uint Size { get; set; }

        public int Hash { get; set; }

        public MiasmExpr(uint size)
        {
            if (size == 0 || size > 64)
                throw new NotSupportedException();
            Size = size;
        }

        public override int GetHashCode()
        {
            return StringHasher.Hash(ExpressionFormatter.FormatExpression(this)).GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is MiasmExpr expr && expr.GetHashCode() == this.GetHashCode())
                return true;
            return base.Equals(obj);
        }
    }
}
