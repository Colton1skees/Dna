using Dna.Extensions;
using Dna.LLVMInterop.API;
using FASTER.core;
using LLVMSharp;
using Microsoft.Z3;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Dna.BinaryTranslator.JmpTables
{
    public class SolverResult
    {
        public bool IsSat { get; set; }

        public string Model { get; set; }
    }

    public static class ConstantSolverWithCache
    {
        // The settings for the z3 query cache.
        private static FasterKVSettings<string, string> settings = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "solvercache.txt"));

        // The z3 query cache.
        private static FasterKV<string, string> store = new(settings);

        // Pool of z3 contexts to be used across threads. This is necessary because creating a new z3 context alone may take up to 30ms.
        private static ConcurrentBag<Context> ContextPool = new();

        // Helper function to be used by native code for attempting to solve constant jump table values.
        // Given a string representation of a model, and a target variable name to extract from the (possibly) solved model, we try to extract out a constant solution.
        public static unsafe bool TrySolveConstant(sbyte* pModel, sbyte* pTarget, out ulong constant)
        {
            return TrySolveConstant(StringMarshaler.AcquireString(pModel), StringMarshaler.AcquireString(pTarget), out constant);
        }

        public static bool TrySolveConstant(string model, string targetVariable, out ulong constant)
        {
            var result = Solve(model);

            // If the model is not satisfiable then return false.
            if (!result.IsSat)
            {
                constant = 0;
                return false;
            }

            // Try to parse a constant out of the model.
            var lines = result.Model.Split(new[] { '\r', '\n' });
            // Not a fan of this approach, but for now we try to parse out the (hopefully) single satisfying assignment of a constant
            // to a variable with the name provided as the argument.
            var before = lines.Single(x => x.Contains(targetVariable));
            var index = lines.IndexOf(x => x == before);
            var target = lines.ElementAt(index + 1);
            // Turn "((ConstantVar1 #x000000014009886c))" into "000000014009886c".
            target = target.Replace("  #x", "").Replace(")", "");

            // Parse the hex number.
            constant = ulong.Parse(target, NumberStyles.HexNumber);
            return true;
        }

        public static SolverResult Solve(string model)
        {
            // Create a new sessiom.
            var session = store.NewSession(new SimpleFunctions<string, string>());

            // Try to read a cache entry.
            var status = session.Read(model, out string output);

            // If a cache entry exists then return a deserialized version.
            if(status.IsCompletedSuccessfully & status.Found)
                return JsonConvert.DeserializeObject<SolverResult>(output);

            // Otherwise we need to solve the query.
            var ctx = TakeContext();

            // Load the model into a solver.
            var sw = Stopwatch.StartNew();

            var tactics = ctx.AndThen(
                ctx.MkTactic("simplify"),
                ctx.MkTactic("propagate-values"),
                ctx.MkTactic("simplify"),
                ctx.MkTactic("elim-uncnstr"),
                ctx.MkTactic("qe-light"),
                ctx.MkTactic("simplify"),
                ctx.MkTactic("elim-uncnstr"),
                ctx.MkTactic("reduce-args"),
                ctx.MkTactic("qe-light"),
                ctx.MkTactic("smt")
                );

            var solver = ctx.MkSolver(tactics);
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} to make solver.");
            solver.FromString(model);
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} to load model.");

            // Create a result object and add it to the cache.
            bool isSat = solver.Check() == Microsoft.Z3.Status.SATISFIABLE;
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} to check model.");
            var result = new SolverResult();
            result.IsSat = isSat;
            result.Model = isSat ? solver.Model.ToString() : "model is not available";
            session.Upsert(model, JsonConvert.SerializeObject(result));
            
            // Save the result.
            var checkpoint = store.TakeHybridLogCheckpointAsync(CheckpointType.FoldOver).Result;

            // Add the z3 context back to the pool. TODO: Free the solvers?
            ContextPool.Add(ctx);

            return result;
        }

        private static Context TakeContext()
        {
            // If a z3 context is free from the pool, take it.
            if (ContextPool.TryTake(out var solver))
                return solver;

            // Otherwise create a new context and return it.
            // The caller is expected to add it back to the queue after they're done.
            return new Context();
        }
    }
}
