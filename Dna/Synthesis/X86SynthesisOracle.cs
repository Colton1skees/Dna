using Dna.Symbolic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch.X86;
using TritonTranslator.Ast;
using TritonTranslator.Expression;

namespace Dna.Synthesis
{
    /// <summary>
    /// Synthesis oracle for x86 flag computations(e.g. parity).
    /// </summary>
    public class X86SynthesisOracle : ISynthesisOracle
    {
        private const uint bvSize = 64;

        private readonly X86CpuArchitecture cpuArchitecture;

        private readonly X86Semantics semantics;

        private readonly AstContext astCtxt = new AstContext();

        public X86SynthesisOracle(X86CpuArchitecture architecture)
        {
            this.cpuArchitecture = architecture;
            this.semantics = new X86Semantics(architecture);
        }

        public IEnumerable<OracleExpression> GetOracleExpressions()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of template oracle expressions.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<OracleExpression> GetExpressions()
        {
            // Get a list of oracle expressions at bit sizes: 8, 16, 32, 64.
            List<OracleExpression> expressions = new List<OracleExpression>();
            for(uint i = 8; i <= 64; i *= 2)
            {
                var op1 = () => new TemporaryNode(0, i);
                var op2 = () => new TemporaryNode(1, i);
                var op3 = () => new TemporaryNode(2, i);

                expressions.Add(new OracleExpression(GetZfExpression(op1()), null));
            }

            return expressions;
        }

        private AbstractNode GetZfExpression(TemporaryNode op1)
        {
            var node = this.astCtxt.ite(
                          this.astCtxt.equal(
                            op1,
                            this.astCtxt.bv(0, op1.BitSize)
                          ),
                          this.astCtxt.bv(1, 1),
                          this.astCtxt.bv(0, 1)
                        );

            return node;
        }

        private AbstractNode GetOfSubExpression(TemporaryNode op1, TemporaryNode op2, TemporaryNode op3)
        {
            /*
             * Create the semantic.
             * of = high:bool((op1 ^ op2) & (op1 ^ regDst))
             */
            var node = this.astCtxt.extract(bvSize - 1, bvSize - 1,
                          this.astCtxt.bvand(
                            this.astCtxt.bvxor(op1, op2),
                            this.astCtxt.bvxor(op1, op3)
                          )
                        );

            return node;
        }

        private IEnumerable<ExpressionIo> BuildExpressionIo(OracleExpression expression)
        {
            // Get all uses of each input operand.
            var userMapping = OracleBuilder.GetInputUsers(expression.Expression);
            var inputs = new List<ExpressionIo>();

            // Generate 50 pseudo-random sets of expression input.
            inputs.AddRange(OracleBuilder.GetRandIo(expression, userMapping));

            // Heuristically select a set of IO pairs with diverse output.
            inputs.AddRange(OracleBuilder.GetHeuristicIo(userMapping.Keys));
            return inputs;
        }


        static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) 
                return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
