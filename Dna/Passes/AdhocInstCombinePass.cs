using Dna.Binary;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;
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

        public unsafe bool InstCombine(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
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
                changed |= TryDistributeSelect(inst);
                changed |= TryRewriteVmpShifts(inst);
                changed |= TrySinkLoadOfSelect(inst);
                changed |= TryRewriteSignExtI1(inst);
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

        private static readonly LLVMOpcode[] Opcodes = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr, LLVMOpcode.LLVMCall };

        private static readonly string[] Whitelist = { "llvm.bswap" };

        private bool TryDistributeSelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(Opcodes, opcode) == -1)
                return false;

            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!Whitelist.Any(x => name.StartsWith(x)))
                    return false;
            }

            // Get the operands
            var op1 = inst.GetOperand(0);
            var op2 = inst.GetOperand(1);

            // At least one operand must be a select of two constants
            if (!IsSelect(op1) && !IsSelect(op2))
                return false;

            var selectIndex = IsSelect(op1) ? 0 : 1;
            var selectOperand = selectIndex == 0 ? op1 : op2;
            var otherIndex = IsSelect(op1) ? 1 : 0;
            var otherOperand = otherIndex == 0 ? op1 : op2;

            var clone0 = inst.InstructionClone;
            builder.PositionBefore(inst.NextInstruction);
            builder.Insert(clone0);
            clone0.Name = inst.Name;

            var clone1 = inst.InstructionClone;
            builder.PositionBefore(clone0.NextInstruction);
            builder.Insert(clone1);
            clone1.Name = clone0.Name;

            clone0.SetOperand((uint)selectIndex, selectOperand.GetOperand(1));
            clone1.SetOperand((uint)selectIndex, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);
            Replace(inst, res);
            return true;
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
            if (inst.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;

            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            // If either operand is not a constant, return false.
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind || inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        private static bool IsSelect(LLVMValueRef inst)
        {
            return IsSelectOfTwoConstants(inst);

            if (inst.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;

            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            return true;
        }

        // %188 = lshr i64 %load3552, 8
        // %189 = and i64 %188, 255
        // %196 = mul nuw nsw i64 %189, 1099511628032
        private bool TryRewriteVmpShifts(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMMul && inst.InstructionOpcode != LLVMOpcode.LLVMShl)
                return false;

            var coeff = inst.GetOperand(1);
            if (coeff.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            if (inst.InstructionOpcode == LLVMOpcode.LLVMShl)
                coeff = LLVMValueRef.CreateConstInt(inst.TypeOf, 1ul << (ushort)coeff.ConstIntZExt);

            var andInst = inst.GetOperand(0);
            if (andInst.Kind != LLVMValueKind.LLVMInstructionValueKind || andInst.InstructionOpcode != LLVMOpcode.LLVMAnd)
                return false;
            var mask = andInst.GetOperand(1);
            if (mask.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            var shiftInst = andInst.GetOperand(0);
            if (shiftInst.Kind != LLVMValueKind.LLVMInstructionValueKind || shiftInst.InstructionOpcode != LLVMOpcode.LLVMLShr || shiftInst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            var shift = shiftInst.GetOperand(1);
            if ((coeff.ConstIntZExt & ModuloReducer.GetMask((uint)shift.ConstIntZExt)) != 0)
                return false;

            var newMask = mask.ConstIntZExt << (ushort)shift.ConstIntZExt;
            var newCoeff = coeff.ConstIntZExt >> (ushort)shift.ConstIntZExt;

            builder.PositionBefore(inst);
            var result = builder.BuildAnd(shiftInst.GetOperand(0), LLVMValueRef.CreateConstInt(shiftInst.TypeOf, newMask));
            result = builder.BuildMul(result, LLVMValueRef.CreateConstInt(result.TypeOf, newCoeff));
            Replace(inst, result);
            return true;
        }

        // %154 = select i1 %124, i64 %2, i64 %3
        // %getelementptr3551 = getelementptr inbounds i8, ptr %load, i64 %154
        // %load3552 = load i64, ptr %getelementptr3551, align 8
        private bool TrySinkLoadOfSelect(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return false;
            var gep = inst.GetOperand(0);
            if (gep.Kind != LLVMValueKind.LLVMInstructionValueKind || gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return false;

            var select = gep.GetOperand(1);
            if (select.Kind != LLVMValueKind.LLVMInstructionValueKind || select.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;


            builder.PositionBefore(inst);
            var ptr0 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(1)});
            var load0 = builder.BuildLoad2(inst.TypeOf, ptr0);

            var ptr1 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(2) });
            var load1 = builder.BuildLoad2(inst.TypeOf, ptr1);

          
            var replacement = builder.BuildSelect(select.GetOperand(0), load0, load1);
            Replace(inst, replacement);

            return true;
        }


        private bool TryRewriteSignExtI1(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSExt)
                return false;
            var type = inst.TypeOf;
            if (type.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                return false;
            if (inst.GetOperand(0).TypeOf.IntWidth != 1)
                return false;

            builder.PositionBefore(inst);
            var select = builder.BuildSelect(inst.GetOperand(0), LLVMValueRef.CreateConstInt(type, ulong.MaxValue), LLVMValueRef.CreateConstInt(type, 0));
            Replace(inst, select);

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
