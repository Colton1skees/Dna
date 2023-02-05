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

        public static MiasmExpr ParseExpression(string exprText)
        {
            exprText = exprText.Replace(" ", "");
            var charStream = new AntlrInputStream(exprText);
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
