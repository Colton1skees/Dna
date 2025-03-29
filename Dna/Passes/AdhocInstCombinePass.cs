using Dna.Binary;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.Passes
{
    // Peephole optimization pass for cases that InstCombine either cannot perform or choses not to due to global profitability
    public class AdhocInstCombinePass
    {
        private readonly bool debug = false;

        private LLVMBuilderRef builder;

        public dgAdhocInstCombinePass PtrToStoreLoadPropagation { get; }

        public unsafe AdhocInstCombinePass()
        {
            PtrToStoreLoadPropagation = new dgAdhocInstCombinePass(InstCombine);
        }

        private unsafe bool InstCombine(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);
            return Run(function);
        }

        private bool Run(LLVMValueRef function)
        {
            bool changed = false;
            foreach(var inst in function.GetInstructions().ToList())
            {
                changed |= TryRewriteBinaryOperator(inst);
                changed |= TryRewriteConstantShiftOfConstantSelect(inst);
                changed |= TryRewriteTruncOfConstantSelect(inst);
            }

            return changed;
        }

        private bool TryRewriteBinaryOperator(LLVMValueRef inst)
        {
            bool isOr = inst.InstructionOpcode == LLVMOpcode.LLVMOr;
            bool isXor = inst.InstructionOpcode == LLVMOpcode.LLVMXor;
            bool isAnd = inst.InstructionOpcode == LLVMOpcode.LLVMAnd;
            bool isAdd = inst.InstructionOpcode == LLVMOpcode.LLVMAdd;
            bool isSub = inst.InstructionOpcode == LLVMOpcode.LLVMSub;
            if (!isOr && !isXor && !isAnd && !isAdd && !isSub)
                return false;

            // Get the operators being ORed.
            var op1 = inst.GetOperand(0);
            var op2 = inst.GetOperand(1);

            Func<LLVMValueRef, LLVMValueRef, LLVMValueRef> lambda = null;
            if (isOr)
                lambda = (op1, op2) => builder.BuildOr(op1, op2);
            else if (isXor)
                lambda = (op1, op2) => builder.BuildXor(op1, op2);
            else if (isAnd)
                lambda = (op1, op2) => builder.BuildAnd(op1, op2);
            else if (isAdd)
                lambda = (op1, op2) => builder.BuildAdd(op1, op2);
            else if (isSub)
                lambda = (op1, op2) => builder.BuildSub(op1, op2);
            else
                throw new InvalidOperationException($"Unknown opkind in TryRewriteBinaryOperator!");


            // If the second operand is a select between two constants:
            if (IsSelectOfTwoConstants(op2) && op1.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                // Get the constants being selected from.
                var (ignore, c1, c2) = AsSelectOfTwoConstants(op2);
                builder.PositionBefore(inst);

                var evaluated1 = lambda(op1, c1);
                var evaluated2 = lambda(op1, c2);
                var select = builder.BuildSelect(op2.GetOperand(0), evaluated1, evaluated2);
                Replace(inst, select);
                return true;
            }

            // If vice versa, swap the operands.
            else if(IsSelectOfTwoConstants(op1) && op2.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                // Get the constants being selected from.
                var (ignore, c1, c2) = AsSelectOfTwoConstants(op1);
                builder.PositionBefore(inst);

                var evaluated1 = lambda(op2, c1);
                var evaluated2 = lambda(op2, c2);
                var select = builder.BuildSelect(op1.GetOperand(0), evaluated1, evaluated2);
                Replace(inst, select);
                return true;
            }

            // Early exit.
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Try to match the pattern of:
        ///     %v1 = select i1 %t0, i64 64, i64 0
        ///     %v2 = lshr i64 %v1, 3
        ///     
        /// and rewrite it as:
        ///     %v1 = lshr i64 64, i64 3
        ///     %v2 = lshr i64 0, 3
        ///     %v3 = select i1 %t0, i64 %v1, i64%v2
        private bool TryRewriteConstantShiftOfConstantSelect(LLVMValueRef inst)
        {
            // Skip if this is not a logical shift right.
            bool isLshr = inst.InstructionOpcode == LLVMOpcode.LLVMLShr;
            bool isAshr = inst.InstructionOpcode == LLVMOpcode.LLVMAShr;
            bool isShl = inst.InstructionOpcode == LLVMOpcode.LLVMShl;
            bool isAdd = inst.InstructionOpcode == LLVMOpcode.LLVMAdd;
            bool isOr = false;
            //bool isAdd = false;
            if (!isLshr && !isAshr && !isShl && !isAdd && !isOr)
                return false;

            // Skip if we are not shifting by a constant amount.
            var constantShiftBy = inst.GetOperand(1);
            if (constantShiftBy.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            // Skip if the first operand is not a select.
            var select = inst.GetOperand(0);
            if (select.Kind != LLVMValueKind.LLVMInstructionValueKind || select.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            // If this is not a select between two constants, skip it.
            var (isConstant, selectOp1, selectOp2) = AsSelectOfTwoConstants(select);
            if(!isConstant)
                return false;

            // We found a match, rewrite it.
            builder.PositionBefore(inst);

            Func<LLVMValueRef, LLVMValueRef, LLVMValueRef> lambda = null;
            if (isLshr)
                lambda = (op1, op2) => builder.BuildLShr(op1, op2);
            else if (isAshr)
                lambda = (op1, op2) => builder.BuildAShr(op1, op2);
            else if (isShl)
                lambda = (op1, op2) => builder.BuildShl(op1, op2);
            else if (isAdd)
                lambda = (op1, op2) => builder.BuildAdd(op1, op2);
            else if (isOr)
                lambda = (op1, op2) => builder.BuildOr(op1, op2);
            else
                throw new InvalidOperationException($"Unknown opkind in TryRewriteConstantShiftOfConstantSelect!");

            var v1 = lambda(selectOp1, constantShiftBy);
            var v2 = lambda(selectOp2, constantShiftBy);
            var newSelect = builder.BuildSelect(select.GetOperand(0), v1, v2);

            Replace(inst, newSelect);

            return true;
        }

        private bool TryRewriteTruncOfConstantSelect(LLVMValueRef inst)
        {
            bool isTrunc = inst.InstructionOpcode == LLVMOpcode.LLVMTrunc;
            bool isZext = inst.InstructionOpcode == LLVMOpcode.LLVMZExt;
            if (!isTrunc && !isZext)
                return false;

            // Skip if the first operand is not a select.
            var select = inst.GetOperand(0);
            if (select.Kind != LLVMValueKind.LLVMInstructionValueKind || select.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            // If this is not a select between two constants, skip it.
            var (isConstant, selectOp1, selectOp2) = AsSelectOfTwoConstants(select);
            if (!isConstant)
                return false;

            // We found a match, rewrite it.
            builder.PositionBefore(inst);

            Func<LLVMValueRef, LLVMValueRef> lambda = null;
            if (isTrunc)
                lambda = (op1) => builder.BuildTrunc(op1, inst.TypeOf);
            else if(isZext)
                lambda = (op1) => builder.BuildZExt(op1, inst.TypeOf);
            else
                throw new InvalidOperationException($"Unknown opkind in TryRewriteConstantShiftOfConstantSelect!");

            var v1 = lambda(selectOp1);
            var v2 = lambda(selectOp2);
            var newSelect = builder.BuildSelect(select.GetOperand(0), v1, v2);

            Replace(inst, newSelect);

            return true;
        }

        private (bool isConstant, LLVMValueRef selectOp1, LLVMValueRef selectOp2) AsSelectOfTwoConstants(LLVMValueRef select)
        {
            // If this is not a select between two constants, skip it.
            var selectOp1 = select.GetOperand(1);
            var selectOp2 = select.GetOperand(2);
            bool isConstant = selectOp1.Kind == LLVMValueKind.LLVMConstantIntValueKind && selectOp2.Kind == LLVMValueKind.LLVMConstantIntValueKind;
            return (isConstant, selectOp1, selectOp2);
        }

        private static bool IsSelectOfTwoConstants(LLVMValueRef inst)
        {
            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            // If either operand is not a constant, return false.
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind || inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        private void Replace(LLVMValueRef from, LLVMValueRef to)
        {
            if(debug)
                Console.WriteLine($"Rewriting {from}\n=> To:\n{to}");

            from.ReplaceAllUsesWith(to);
            //from.InstructionEraseFromParent();
        }
    }
}
