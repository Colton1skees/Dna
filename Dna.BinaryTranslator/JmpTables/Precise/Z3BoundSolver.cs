using Dna.Symbolic;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    public static class Z3BoundSolver
    {
        public static bool HasAnySolution(AbstractNode expression, HashSet<AbstractNode> constraints, bool forceReasonableRange = true)
        {
            var z3Translator = new Z3AstBuilder(new Context());
            var z3IndexAst = z3Translator.GetZ3Ast(expression);

            var solver = z3Translator.Ctx.MkSolver("smt");
            foreach (var constraint in constraints)
                solver.Add(MakeConstraint(z3Translator, constraint));

            var check = solver.Check();
            if (check == Status.UNSATISFIABLE)
                return false;

            var model = solver.Model;
            var evaluation = model.Eval(z3IndexAst);
            if (evaluation is not BitVecNum num)
                return false;

            if(forceReasonableRange)
            {
                // Create a construction thats basically "does a solution exist thats either less than (evaluation - 1000) or greater than (evaluation + 1000)".
                //var concrete = new IntegerNode(num.UInt64, expression.BitSize);
                var min = new IntegerNode(num.UInt64 - 1000, expression.BitSize);
                var max = new IntegerNode(num.UInt64 + 1000, expression.BitSize);
                var ule = new BvuleNode(expression, min);
                var uge = new BvugeNode(expression, max);
                var ored = new BvorNode(ule, uge);
                var final = new BvandNode(ored, constraints.Single());

                // If this clause is true then it's either a very large jump table, or it's a unbounded.
                // For now we assume it's unbounded.
                constraints = new();
                constraints.Add(final);
                var unreasonableSolution = HasAnySolution(expression, constraints, false);
                Console.WriteLine($"Found unreasonable solution: {unreasonableSolution} to {constraints.Single()}");
                if (unreasonableSolution)
                    return false;
            }

            return true;
        }

        public static IReadOnlyList<ulong> GetSolutions(AbstractNode expression, HashSet<AbstractNode> constraints)
        {
            expression = new TemporaryNode(44545, 64);

            var z3Translator = new Z3AstBuilder(new Context());
            var z3IndexAst = z3Translator.GetZ3Ast(expression);

            var solver = z3Translator.Ctx.MkSolver();
            foreach (var constraint in constraints)
                solver.Add((BoolExpr)(MakeConstraint(z3Translator, constraint).Simplify()));

            Console.WriteLine(solver);
            // Solve for all possible integer solutions.
            // If the equation is unbounded(or if it may be equal to anything),
            // then z3 will return no valid solutions.
            List<ulong> solutions = new();
            while (true)
            {
                var sw = Stopwatch.StartNew();
                var check = solver.Check();
                sw.Stop();
                Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms");
                if (check == Status.UNSATISFIABLE)
                    break;

                var model = solver.Model;
                var evaluation = model.Eval(z3IndexAst);
                if (evaluation is not BitVecNum num)
                    break;

                solutions.Add(num.UInt64);
                solver.Add(z3Translator.Ctx.MkNot(z3Translator.Ctx.MkEq(z3IndexAst, z3Translator.Ctx.MkBV(num.UInt64, expression.BitSize))));
            }

            solutions = solutions.OrderByDescending(x => x).Reverse().ToList();
            return solutions;
        }

        private static BoolExpr MakeConstraint(Z3AstBuilder z3Translator, AbstractNode constraint)
        {
            // Convert the constraint expression to a z3 ast.
            var z3Constraint = z3Translator.GetZ3Ast(constraint);

            // Add a constraint that requires the source expression to be true.
            var bvTrue = z3Translator.Ctx.MkBV(1, 1);
            var bvConstraint = z3Translator.Ctx.MkEq(z3Constraint, bvTrue);
            return bvConstraint;
        }
    }
}
