using Dna.Binary;
using Dna.BinaryTranslator.JmpTables.Precise;
using Dna.BinaryTranslator.JmpTables;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO;
using Dna.LLVMInterop.API.LLVMBindings.Transforms;
using Dna.LLVMInterop;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Unsafe
{
    /// <summary>
    /// Class for solving the set of values that may be used in an indirect jump(lifted as remill_jump intrinsic invocations in the control flow graph).
    /// </summary>
    public static class JmpTableSolver
    {
        /// <summary>
        /// Find all remill_jump intrinsics in the control flow graph and solve the set of possible values.
        /// </summary>
        public static IReadOnlyList<JmpTable> SolveJumpTables(IBinary binary, LLVMValueRef function)
        {
            // Make the CFG reducible, remove switch statements, enforce that all loops have dedicated exits.
            CanonicalizeCFG(function);

            var fpm = new FunctionPassManager();
            var pmb = new PassManagerBuilder();
            var moduleManager = new PassManager();

            var jmpTablePass = new PreciseJumpTableSolvingPass(binary);
            var nativeJmpTablePass = PassApi.CreateJumpTableAnalysisPass(jmpTablePass.PtrSolveBounds, jmpTablePass.PtrTrySolveConstant);
            fpm.Add(nativeJmpTablePass);

            pmb.PopulateFunctionPassManager(fpm);
            pmb.PopulateModulePassManager(moduleManager);

            fpm.DoInitialization();
            fpm.Run(function);
            fpm.DoFinalization();

            return jmpTablePass.SolvedTables;
        }

        /// <summary>
        /// Use LLVM passes to put the control flow graph into a canonical form that's easier for the jump table solver to handle.
        /// Switch statements and irreducible control flow graphs are removed, loops have dedicated exits, and loop closed SSA form is used.
        /// </summary>
        /// <param name="function"></param>
        private static void CanonicalizeCFG(LLVMValueRef function)
        {
            var fpm = new FunctionPassManager();
            var pmb = new PassManagerBuilder();
            var moduleManager = new PassManager();

            // Remove all switches. This simplifies analysis since we don't need to handle
            // cases where more than two case predecessors exist.
            fpm.Add(PassApi.CreateUnSwitchPass());
            // Remove irreducible control flow. Thus we only work with sane loops.
            fpm.Add(UtilsPasses.CreateFixIrreduciblePass());
            //fpm.Add(PassApi.CreateControlledNodeSplittingPass());
            // Canonicalize the loop. Make sure all loops have dedicated exits(that is, no exit block for the loop has a predecessor
            // that is outside the loop. This implies that all exit blocks are dominated by the loop header.)
            fpm.Add(UtilsPasses.CreateLoopSimplifyPass());

            fpm.Add(UtilsPasses.CreateLCSSAPass());

            pmb.PopulateFunctionPassManager(fpm);
            pmb.PopulateModulePassManager(moduleManager);

            fpm.DoInitialization();
            fpm.Run(function);
            fpm.DoFinalization();
        }
    }
}
