using Antlr4.Runtime;
using Dna.Synthesis.Miasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Parsing
{
    public static class ExpressionDatabaseParser
    {
        private static MiasmAstTranslationVisitor visitor = new MiasmAstTranslationVisitor();

        public static Expr ParseExpression(string discount)
        {
            discount = discount.Replace(" ", "");
            // Console.WriteLine(discount);
            var charStream = new AntlrInputStream(discount);
            var lexer = new MiasmLexer(charStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new MiasmParser(tokenStream);
            parser.BuildParseTree = true;
            var expr = parser.root();
            var result = visitor.VisitRoot(expr);
            return result;
        }
    }
}
