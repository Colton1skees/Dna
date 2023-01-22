using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Simplification
{
    public class AstMatchCandidates
    {
        /// <summary>
        /// The AST being simplified.
        /// </summary>
        public AbstractNode SourceAst { get; }

        /// <summary>
        /// Worklist of child nodes to visit.
        /// </summary>
        public Queue<AbstractNode> ChildQueue { get; } = new();

        /// <summary>
        /// A worklist of candidates being pattern matched.
        /// </summary>
        public HashSet<AstMatch> MatchCandidates { get; } = new HashSet<AstMatch>();

        public AstMatchCandidates(AbstractNode sourceAst)
        {
            SourceAst = sourceAst;
        }
    }

    public class AstMatch
    {
        /// <summary>
        /// The AST being searched for.
        /// </summary>
        public AbstractNode Pattern { get; set; }

        /// <summary>
        ///  A worklist of child nodes.
        /// </summary>
        public Queue<AbstractNode> ChildQueue { get; } = new();

        public AstMatch(AbstractNode pattern)
        {
            Pattern = pattern;
        }
    }

    public class RewriteSimplifier : IExpressionSimplifier
    {
        private RewriteRules rules = new RewriteRules();

        Dictionary<AbstractNode, AbstractNode> simplificationMapping;

        Dictionary<AstType, List<AbstractNode>> nodeStartMapping = new();

        public RewriteSimplifier()
        {
            simplificationMapping = rules.GetRewriteRules();
            foreach(var inputExpression in simplificationMapping)
            {
                nodeStartMapping.TryAdd(inputExpression.Key.Type, new List<AbstractNode>());
                nodeStartMapping[inputExpression.Key.Type].Add(inputExpression.Key);
            }
        }

        public AbstractNode? SimplifyExpression(AbstractNode expression)
        {
            Console.WriteLine("Before simplification: {0}", expression);
            var simplified = SimplifyAstRedundancies(expression);
            Console.WriteLine("After simplificaiton: {0}", simplified);

            throw new NotImplementedException();
        }

        private AbstractNode SimplifyAstRedundancies(AbstractNode node)
        {
            // If the root node of the expression is unnecessary(i.e. a size extension of zero),
            // then traverse downwards until we find a non trivial root node to start simplifying.
            while(true)
            {
                var skipped = SkipRedundancies(node);
                if (skipped == null)
                    break;
                node = skipped;
            }

            // Breadth first search while simplifying redundancies in child nodes.
            var worklist = new Queue<AbstractNode>();
            worklist.Enqueue(node);
            while(worklist.Any())
            {
                var visiting = worklist.Dequeue();
                for(int i = 0; i < visiting.Children.Count; i++)
                {
                    // Simplify out any child nodes with redundancies.
                    var childNode = visiting.Children[i];
                    var skipped = SkipRedundancies(childNode);
                    if (skipped != null)
                        visiting.Children[i] = SkipRedundancies(childNode);

                    // Add the child to the worklist.
                    worklist.Enqueue(visiting.Children[i]);
                }
            }

            return node;
        }

        private AbstractNode? SkipRedundancies(AbstractNode node)
        {
            if (node is SxNode sxNode && sxNode.SizeExt.Value == 0)
                return sxNode.Source;
            else if (node is ZxNode zxNode && zxNode.SizeExt.Value == 0)
                return zxNode.Source;
            else if(node is ExtractNode extractNode && extractNode.BitSize == extractNode.Source.BitSize)
                return extractNode.Source;

            return null;
        }

        private AbstractNode RewriteExpression(AbstractNode node)
        {
            Dictionary<AbstractNode, AstMatchCandidates> candidateMapping = new();
            var worklist = new Queue<AbstractNode>();
            worklist.Enqueue(node);
            while(worklist.Any())
            {
                var visiting = worklist.Dequeue();
                candidateMapping.Add(visiting, GetNodeAstMatch(visiting));

                foreach(var candidate in candidateMapping.ToList())
                {
                    // Step 1: Delete all cases with no match candidates.
                    if (!candidate.Value.MatchCandidates.Any())
                    {
                        candidateMapping.Remove(candidate.Key);
                        continue;
                    }

                    // Step 2: Delete all sub-candidates where the number of child nodes does not match our AST.
                    foreach(var subAst in candidate.Value.ChildQueue.ToList())
                    {

                    }
                }
            }

            return null;
        }

        private AstMatchCandidates GetNodeAstMatch(AbstractNode node)
        {
            var rootMatch = new AstMatchCandidates(node);
            foreach(var child in node.Children)
                rootMatch.ChildQueue.Enqueue(child);
            bool found = nodeStartMapping.TryGetValue(node.Type, out List<AbstractNode> candidates);
            if(!found)
                return rootMatch; 

            foreach (var matchCandidate in nodeStartMapping[node.Type])
            {
                var candidate = new AstMatch(matchCandidate);
                foreach(var child in matchCandidate.Children)
                    candidate.ChildQueue.Enqueue(child);
            }
            return rootMatch;
        }
    }
}
