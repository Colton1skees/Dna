using Dna.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.Symbolic
{
    public static class OracleBuilder
    {
        /// <summary>
        /// Gets a mapping of all expressions where the child node list contains one of our input operands.
        /// </summary>
        public static Dictionary<TemporaryNode, List<AbstractNode>> GetInputUsers(AbstractNode node)
        {
            // Collect all users of each input node.
            Dictionary<TemporaryNode, List<AbstractNode>> users = new();
            var worklist = new Stack<AbstractNode>();
            worklist.Push(node);
            while (true)
            {
                if (!worklist.Any())
                    break;

                var visited = worklist.Pop();
                foreach (var child in visited.Children)
                {
                    // If child is one of our expression inputs, then add the parent to it's user list.
                    if (child is TemporaryNode tempOp)
                    {
                        users.TryAdd(tempOp, new List<AbstractNode>());
                        users[tempOp].Add(visited);
                    }

                    // Otherwise, traverse the child subtrees.
                    else
                    {
                        worklist.Push(child);
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Gets a set of 50 pseudo-random IO pairs.
        /// </summary>
        public static IEnumerable<ExpressionIo> GetRandIo(OracleExpression expression, Dictionary<TemporaryNode, List<AbstractNode>> userMapping)
        {
            // Get all uses of each input operand.
            var inputs = new List<ExpressionIo>();

            // Generate 50 pseudo-random sets of expression input.
            var rand = new Random(623459435);
            for (int randIter = 0; randIter < 50; randIter++)
            {
                var io = new ExpressionIo();
                foreach (var tempOp in userMapping.Keys)
                {
                    io.Inputs.Add(tempOp, (ulong)rand.NextInt64());
                }

                inputs.Add(io);
            }

            return inputs;
        }

        /// <summary>
        /// Gets a set of heuristically selected IO pairs.
        /// </summary>
        /// <param name="inputNodes"></param>
        /// <returns></returns>
        public static IEnumerable<ExpressionIo> GetHeuristicIo(IEnumerable<TemporaryNode> inputNodes)
        {
            // Get an IO pair where all inputs are zero.
            var ioZero = new ExpressionIo();
            foreach (var node in inputNodes)
            {
                ioZero.Inputs.Add(node, 0);
            }

            var ioMax = new ExpressionIo();
            foreach (var node in inputNodes)
            {
                ioZero.Inputs.Add(node, MathUtility.GetMaxValue(node.BitSize));
            }

            var ioMin = new ExpressionIo();
            foreach (var node in inputNodes)
            {
                ioZero.Inputs.Add(node, MathUtility.GetMinValue(node.BitSize));
            }

            var ioHalf = new ExpressionIo();
            foreach (var node in inputNodes)
            {
                ioZero.Inputs.Add(node, MathUtility.GetMaxValue(node.BitSize) / 2);
            }

            var output = new List<ExpressionIo>();
            output.Add(ioZero);
            output.Add(ioMin);
            output.Add(ioMax);
            output.Add(ioHalf);
            return output;
        }
    }
}
