using Dna.Synthesis.Miasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Parsing
{
    public enum Token
    {
        Negate,
        ExprOp,
        ExprId,

    }

    public static class AstParser
    {
        private static string quotation = new string(new char[] { '"' });

        private static string opStart = new string(new char[] { '(', '"' });

        public static Expr ParseAst(string text)
        {
            text = text.Replace(" ", "");
            text.Replace(opStart, "((");
            text.Replace(quotation, ")");


            return null;
        }
    }
}
