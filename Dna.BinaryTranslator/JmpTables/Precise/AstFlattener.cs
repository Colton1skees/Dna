using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    public static class AstFlattener
    {
        public static HashSet<AbstractNode> GetInputVariables(AbstractNode source)
        {
            return FlattenAst(source).Where(x => x is TemporaryNode).ToHashSet();
        }

        public static HashSet<AbstractNode> FlattenAst(AbstractNode source)
        {
            var seen = new HashSet<AbstractNode>();

            var worklist = new HashSet<AbstractNode>();
            worklist.Add(source);
            while(worklist.Any())
            {
                var ast = worklist.First();
                worklist.Remove(ast);

                seen.Add(ast);
                foreach(var child in ast.Children)
                {
                    if (!seen.Contains(child))
                        worklist.Add(child);
                }
            }

            return seen;
        }

        public static AbstractNode ReplaceAstItem(AbstractNode parentAst, AbstractNode from, AbstractNode to)
        {
            if (parentAst == from)
                return to;

            var flattened = FlattenAst(parentAst);
            foreach(var subtree in flattened)
            {
                var children = subtree.Children;
                for(int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child == from)
                        children[i] = to;
                }
            }

            return parentAst;
        }
    }
}
