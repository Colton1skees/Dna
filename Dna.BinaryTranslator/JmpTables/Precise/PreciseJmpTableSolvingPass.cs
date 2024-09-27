using Dna.Binary;
using Dna.BinaryTranslator.JmpTables.Slicing;
using Dna.BinaryTranslator.Unsafe;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.LLVMInterop.Passes;
using Dna.LLVMInterop.Passes.Matchers;
using Dna.Symbolic;
using Dna.Utilities;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    public class PreciseJumpTableSolvingPass
    {
        private static int count = 0;

        private readonly IBinary binary;

        public dgSolveJumpTableBounds PtrSolveBounds { get; }

        public dgTrySolveConstant PtrTrySolveConstant { get; }

        public List<JmpTable> SolvedTables = new();

        public unsafe PreciseJumpTableSolvingPass(IBinary binary)
        {
            PtrSolveBounds = new dgSolveJumpTableBounds(Solve);
            PtrTrySolveConstant = new dgTrySolveConstant(ConstantSolverWithCache.TrySolveConstant);
            this.binary = binary;
        }

        public unsafe void Solve(LLVMOpaqueValue* function, nint pLoopInfo, nint pMssa, nint lazyValueInfo, nint trySolveConstant) => SolveBounds(function, new LoopInfo(pLoopInfo), new MemorySSA(pMssa), lazyValueInfo, trySolveConstant);

        private void SolveBounds(LLVMValueRef function, LoopInfo loopInfo, MemorySSA mssa, nint lazyValueInfo, nint trySolveConstant)
        {
            var jmpIntrinsic = function.GlobalParent.GetFunctions().FirstOrDefault(x => x.Name.Contains("__remill_jump"));
            if (jmpIntrinsic == null)
            {
                Console.WriteLine("Found no jump tables to resolve.");
                return;
            }

            var jmpCalls = RemillUtils.CallersOf(jmpIntrinsic).Where(x => x.GetFunction() == function).ToList();
            foreach(var jmpCall in jmpCalls)
            {
                // Create a jump table solver.
                var jmpFromAddress = jmpCall.GetOperand(0).GetOperand(0).ConstIntZExt;
                var jmpFromBlock = jmpCall.InstructionParent;
                var jmpDestPtr = jmpCall.GetOperand(1);


                /*
                var solver = new PreciseJmpTableSolver(binary, jmpFromAddress, jmpFromBlock, jmpDestPtr, loopInfo);

                if(count == 1)
                {
                    jmpDestPtr = function.GetInstructions().Single(x => x.ToString().StartsWith("  %local_state_struct.sroa.172.0.ph = "));
                    //jmpDestPtr = function.GetInstructions().Single(x => x.ToString().Contains("%0 = add i6"));
                    //jmpDestPtr = function.GetParam(0);
                    Console.WriteLine(jmpDestPtr);
                    Console.WriteLine("Slicing!");

                    var souperSolver = new SouperJumpTableSolver(binary, jmpFromAddress, jmpCall, jmpDestPtr, loopInfo);
                    souperSolver.IterativelySolve();

                    //SlicingApi.Solve(function, jmpCall, jmpDestPtr, jmpFromBlock, lazyValueInfo, trySolveConstant);
                    //SlicingApi.SliceValue(function, jmpDestPtr);
                }
                

                // Solve the jump table.
                var solved = solver.IterativelySolve();

                SolvedTables.Add(solved);
                count++;
                */

                var souperSolver = new SouperJumpTableSolver(binary, jmpFromAddress, jmpCall, jmpDestPtr, loopInfo);
                SolvedTables.Add(souperSolver.IterativelySolve());
            }
        }
    }
}
