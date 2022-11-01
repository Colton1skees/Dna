using Dna;
using Dna.ControlFlow;
using Dna.Extensions;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extraction
{
    /// <inheritdoc cref="IFunctionExtractor" />
    public class FunctionExtractor : IFunctionExtractor
    {
        private readonly IDna dna;

        private readonly Dictionary<ulong, IExtractedFunction> extractedFunctions = new Dictionary<ulong, IExtractedFunction>();

        public FunctionExtractor(IDna dna)
        {
            this.dna = dna;
        }

        /// <inheritdoc />
        public IExtractedFunction ExtractFunction(ulong start, ulong? end = null)
        {
            var functionGraph = dna.RecursiveDescent.ReconstructCfg(start);
            var graphEnd = functionGraph.GetInstructions().Max(x => x.IP);
            if (end.HasValue && graphEnd > end.Value)
                throw new Exception(String.Format("Failed to extract function at 0x{0}. The provided end address was less than the calculated end address.", start.ToString("X")));

            if (!IsFunctionExtractable(functionGraph))
                throw new Exception(String.Format("Function at 0x{0} is not extractable.", start.ToString("X")));

            // Initialize the extracted function.
            ExtractedFunction extractedFunction = new ExtractedFunction();
            extractedFunction.Graph = functionGraph;
            extractedFunctions.Add(start, extractedFunction);

            // Recursively parse all callees, while avoiding functions which have already been extracted.
            var callees = new List<IExtractedFunction>();
            var calleeAddresses = GetFunctionCalleeAddresses(functionGraph).Distinct();
            foreach(var calleeAddress in calleeAddresses)
            {
                // If we have already extracted this function, then grab the cached result.
                if (extractedFunctions.ContainsKey(calleeAddress))
                    callees.Add(extractedFunctions[calleeAddress]);

                // Otherwise, extract the function.
                else
                    callees.Add(ExtractFunction(calleeAddress));
            }
            extractedFunction.Callees = callees;
            return extractedFunction;
        }

        /// <summary>
        /// Gets whether a function is extractable.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        private bool IsFunctionExtractable(ControlFlowGraph<Instruction> functionGraph)
        {
            if (HasIndirectBranches(functionGraph))
                return false;
            return true;
        }

        /// <summary>
        /// Gets whether a function has any branching instructions(e.g call, jmp, etc) where the destination cannot be resolved statically.
        /// </summary>
        /// <param name="functionGraph"></param>
        /// <returns></returns>
        private bool HasIndirectBranches(ControlFlowGraph<Instruction> functionGraph)
        {
            var instructions = functionGraph.GetInstructions().ToList();
            var branchingInstructions = functionGraph.GetInstructions().Where(x => x.FlowControl != FlowControl.Next);
            var unresolvableBranches = branchingInstructions.Where(x =>
                !x.Op0Kind.IsImmediate()  // An unresolvable branch is classified as any branching instruction whose operand is not an immediate value(e.g jmp eax)
                && x.Mnemonic != Mnemonic.Ret // RETs are technically classified as an unresolvable branch, but we obviously don't want to attempt to resolve them.
            );

            if (unresolvableBranches.Any())
                throw new Exception("Failed to extract function. Encountered a branching instruction with an unresolvable destination.");

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the address of all functions which are called by the provided function.
        /// </summary>
        /// <param name="functionGraph"></param>
        /// <returns></returns>
        private IEnumerable<ulong> GetFunctionCalleeAddresses(ControlFlowGraph<Instruction> functionGraph)
        {
            var immediateCalls = functionGraph.GetInstructions().Where(x => x.Mnemonic == Mnemonic.Call && x.Op0Kind.IsImmediate());
            return immediateCalls.Select(x => x.NearBranch64);
        }
    }
}
