using Dna.Binary;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;
using static Dna.LLVMInterop.NativePassApi;
using static System.Net.Mime.MediaTypeNames;

namespace Dna.Passes
{
    public class PeepholeResult
    {
        // List of new values added by the peephole optimization
        // Invariant: The last instruction is the result value.
        public List<LLVMValueRef> Insts = new();

        public LLVMValueRef GetResult() => Insts[Insts.Count - 1];

        public void Add(params LLVMValueRef[] insts)
            => Insts.AddRange(insts);
    }

    // Peephole optimization pass for cases that InstCombine either cannot perform or choses not to due to global profitability
    public class AdhocInstCombinePass
    {
        private readonly bool debug = false;

        public LLVMBuilderRef builder;

        public dgAdhocInstCombinePass PtrToStoreLoadPropagation { get; }

        public unsafe AdhocInstCombinePass()
        {
            PtrToStoreLoadPropagation = new dgAdhocInstCombinePass(InstCombine);
        }

        public unsafe bool InstCombine(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);

            LLVMValueRef f = function;
            bool changed = true;
            changed = false;
            foreach (var inst in f.GetInstructions().ToList())
            {
                var curr = inst;
                while (curr != null)
                {
                    var peephole = PeepholeInst(curr);
                    if (peephole == null)
                    {
                        curr = null;
                        break;
                    }

                    changed = true;
                    curr.ReplaceAllUsesWith(peephole.GetResult());
                    curr = peephole.GetResult();
                }
            }

            return changed;
        }

        public PeepholeResult PeepholeInst(LLVMValueRef inst)
        {
            Debug.Assert(inst.Is(LLVMValueKind.LLVMInstructionValueKind));

            PeepholeResult changed = null;
            changed = TrySimplifyInstruction(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteTruncOfConstantSelect(inst);
            if (changed != null)
                return changed;

            changed = TryDistributeSelect(inst);
            if (changed != null)
                return changed;

            changed = TryKnownBitsFoldToSelect(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteVmpShifts(inst);
            if (changed != null)
                return changed;

            changed = TrySinkLoadOfSelect(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteSignExtI1(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteTruncAnd(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteZextTrunc(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteDisjointOr(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteInverseShifts(inst);
            if (changed != null)
                return changed;

            return null;
        }

        private static readonly LLVMOpcode[] Opcodes = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor,  LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr, LLVMOpcode.LLVMCall };

        private static readonly string[] Whitelist = { "llvm.bswap", "llvm.fshl" };

        private PeepholeResult TryDistributeSelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(Opcodes, opcode) == -1)
                return null;

            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!Whitelist.Any(x => name.StartsWith(x)))
                    return null;
            }

            // Get the operands
            var op1 = inst.GetOperand(0);
            var op2 = inst.GetOperand(1);

            // At least one operand must be a select of two constants
            if (!IsSelect(op1) && !IsSelect(op2))
                return null;

            var selectIndex = IsSelect(op1) ? 0 : 1;
            var selectOperand = selectIndex == 0 ? op1 : op2;
            var otherIndex = IsSelect(op1) ? 1 : 0;
            var otherOperand = otherIndex == 0 ? op1 : op2;

            var clone0 = Clone(inst);
            var clone1 = Clone(inst);

            clone0.SetOperand((uint)selectIndex, selectOperand.GetOperand(1));
            clone1.SetOperand((uint)selectIndex, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);

            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);
            return peephole;
        }

        private static readonly LLVMOpcode[] kbFolds = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr };

        private PeepholeResult TryKnownBitsFoldToSelect(LLVMValueRef inst)
        {
            if (!inst.Is(kbFolds))
                return null;

            var kb = NativeKnownBits.Get(inst, inst.GlobalParent);
            if (kb.GetUnknownBitCount() != 1)
                return null;

            var users = inst.GetUsers().ToList();
            if (users.All(x => x.Is(LLVMOpcode.LLVMICmp)))
                return null;

            // Clone the source instruction
            var clone = Clone(inst);

            builder.PositionBefore(inst.NextInstruction);

            var ty = inst.TypeOf;
            var values = kb.AllPossibleValues().Select(x => LLVMValueRef.CreateConstInt(ty, x)).ToList();
            var cmp = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, clone, values[0]);
            var select = builder.BuildSelect(cmp, values[0], values[1]);

            var peephole = new PeepholeResult();
            peephole.Add(clone, cmp, select);
            return peephole;
        }

        private PeepholeResult TryRewriteTruncOfConstantSelect(LLVMValueRef inst)
        {
            bool isTrunc = inst.InstructionOpcode == LLVMOpcode.LLVMTrunc;
            bool isZext = inst.InstructionOpcode == LLVMOpcode.LLVMZExt;
            if (!isTrunc && !isZext)
                return null;

            // Skip if the first operand is not a select.
            var select = inst.GetOperand(0);
            if (select.Kind != LLVMValueKind.LLVMInstructionValueKind || select.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return null;

            // If this is not a select between two constants, skip it.
            var (isConstant, selectOp1, selectOp2) = AsSelectOfTwoConstants(select);
            if (!isConstant)
                return null;

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

            var peephole = new PeepholeResult();
            peephole.Add(v1, v2, newSelect);
            return peephole;
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
        private PeepholeResult TryRewriteVmpShifts(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMMul && inst.InstructionOpcode != LLVMOpcode.LLVMShl)
                return null;

            var coeff = inst.GetOperand(1);
            if (coeff.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;

            if (inst.InstructionOpcode == LLVMOpcode.LLVMShl)
                coeff = LLVMValueRef.CreateConstInt(inst.TypeOf, 1ul << (ushort)coeff.ConstIntZExt);

            var andInst = inst.GetOperand(0);
            if (andInst.Kind != LLVMValueKind.LLVMInstructionValueKind || andInst.InstructionOpcode != LLVMOpcode.LLVMAnd)
                return null;
            var mask = andInst.GetOperand(1);
            if (mask.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;

            var shiftInst = andInst.GetOperand(0);
            if (shiftInst.Kind != LLVMValueKind.LLVMInstructionValueKind || shiftInst.InstructionOpcode != LLVMOpcode.LLVMLShr || shiftInst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;

            var shift = shiftInst.GetOperand(1);
            if ((coeff.ConstIntZExt & ModuloReducer.GetMask((uint)shift.ConstIntZExt)) != 0)
                return null;

            var newMask = mask.ConstIntZExt << (ushort)shift.ConstIntZExt;
            var newCoeff = coeff.ConstIntZExt >> (ushort)shift.ConstIntZExt;

            builder.PositionBefore(inst);
            var t0 = builder.BuildAnd(shiftInst.GetOperand(0), LLVMValueRef.CreateConstInt(shiftInst.TypeOf, newMask));
            var t1 = builder.BuildMul(t0, LLVMValueRef.CreateConstInt(t0.TypeOf, newCoeff));

            var peephole = new PeepholeResult();
            peephole.Add(t0, t1);
            return peephole;
        }

        // %154 = select i1 %124, i64 %2, i64 %3
        // %getelementptr3551 = getelementptr inbounds i8, ptr %load, i64 %154
        // %load3552 = load i64, ptr %getelementptr3551, align 8
        private PeepholeResult TrySinkLoadOfSelect(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return null;
            var gep = inst.GetOperand(0);
            if (gep.Kind != LLVMValueKind.LLVMInstructionValueKind || gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return null;

            var select = gep.GetOperand(1);
            if (select.Kind != LLVMValueKind.LLVMInstructionValueKind || select.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return null;


            builder.PositionBefore(inst);
            var ptr0 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(1)});
            var load0 = builder.BuildLoad2(inst.TypeOf, ptr0);

            var ptr1 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(2) });
            var load1 = builder.BuildLoad2(inst.TypeOf, ptr1);

            var replacement = builder.BuildSelect(select.GetOperand(0), load0, load1);

            var peephole = new PeepholeResult();
            peephole.Add(ptr0, load0, ptr1, load1, replacement);
            return peephole;
        }


        private PeepholeResult TryRewriteSignExtI1(LLVMValueRef inst)
        {
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSExt)
                return null;
            var type = inst.TypeOf;
            if (type.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                return null;
            if (inst.GetOperand(0).TypeOf.IntWidth != 1)
                return null;

            builder.PositionBefore(inst);
            var select = builder.BuildSelect(inst.GetOperand(0), LLVMValueRef.CreateConstInt(type, ulong.MaxValue), LLVMValueRef.CreateConstInt(type, 0));

            var peephole = new PeepholeResult();
            peephole.Add(select);
            return peephole;
        }

        // %163 = and i64 %3, 255
        // %164 = trunc i64 %163 to i8
        private PeepholeResult TryRewriteTruncAnd(LLVMValueRef trunc)
        {
            if (!trunc.Is(LLVMOpcode.LLVMTrunc))
                return null;
            var and = trunc.GetOperand(0);

            // and x, 255
            if (!and.Is(LLVMOpcode.LLVMAnd))
                return null;
            if (!and.GetOperand(1).TryGetConstant(out var andMask))
                return null;

            var truncWidth = trunc.TypeOf.IntWidth;
            var andWidth = BitOperations.TrailingZeroCount(~andMask);

            bool isSubset = truncWidth <= andWidth;
            if (!isSubset)
                return null;

            var src = and.GetOperand(0);
            //trunc.SetOperand(0, src);
            builder.PositionBefore(trunc);
            var replacement = builder.BuildTrunc(src, trunc.TypeOf);

            var peephole = new PeepholeResult();
            peephole.Add(replacement);
            return peephole;
        }


        //   %3581 = trunc i64 %3017 to i8
        // %3582 = zext i8 %3581 to i64
        private PeepholeResult TryRewriteZextTrunc(LLVMValueRef zext)
        {
            if (!zext.Is(LLVMOpcode.LLVMZExt))
                return null;
            var trunc = zext.GetOperand(0);
            if (!trunc.Is(LLVMOpcode.LLVMTrunc))
                return null;

            var src = trunc.GetOperand(0);
            if (src.TypeOf.IntWidth != zext.TypeOf.IntWidth)
                return null;

            var truncMask = ModuloReducer.GetMask(trunc.TypeOf.IntWidth);
            builder.PositionBefore(zext);
            var replacement = builder.BuildAnd(src, LLVMValueRef.CreateConstInt(zext.TypeOf, truncMask));
            //zext.ReplaceAllUsesWith(replacement);
            //return true;

            var peephole = new PeepholeResult();
            peephole.Add(replacement);
            return peephole;
        }

        private PeepholeResult TryRewriteDisjointOr(LLVMValueRef or)
        {
            if (!or.Is(LLVMOpcode.LLVMOr))
                return null;
            var children = new LLVMValueRef[] { or.GetOperand(0), or.GetOperand(1) };
            if (!children.All(x => x.Is(LLVMOpcode.LLVMAnd) && x.GetOperand(1).IsConstant()))
                return null;

            var src = children[0].GetOperand(0);
            if (src != children[1].GetOperand(0))
                return null;

            builder.PositionBefore(or);
            var combinedMask = children[0].GetOperand(1).ConstIntZExt | children[1].GetOperand(1).ConstIntZExt;
            var replacement = builder.BuildAnd(src, LLVMValueRef.CreateConstInt(src.TypeOf, combinedMask));
            //Replace(or, replacement);
            //return true;

            var peephole = new PeepholeResult();
            peephole.Add(replacement);
            return peephole;
        }

        // %206 = lshr i64 %6, 56
        // %207 = shl i64 %206, 56
        private PeepholeResult TryRewriteInverseShifts(LLVMValueRef shl)
        {
            if (!shl.Is(LLVMOpcode.LLVMShl))
                return null;
            var lshr = shl.GetOperand(0);
            if (!lshr.Is(LLVMOpcode.LLVMLShr))
                return null;

            var immediates = new LLVMValueRef[] { shl.GetOperand(1), lshr.GetOperand(1) };
            if (!immediates.All(x => x.IsConstant()))
                return null;

            if (immediates[0].ConstIntZExt != immediates[1].ConstIntZExt)
                return null;

            ulong mask = ModuloReducer.GetMask(shl.TypeOf.IntWidth);
            var shiftBy = (ushort)immediates[0].ConstIntZExt;
            mask >>= shiftBy;
            mask <<= shiftBy;

            builder.PositionBefore(shl);
            var replacement = builder.BuildAnd(lshr.GetOperand(0), LLVMValueRef.CreateConstInt(shl.TypeOf, mask));

            var peephole = new PeepholeResult();
            peephole.Add(replacement);
            return peephole;
        }

        private PeepholeResult TrySimplifyInstruction(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;

            var simplified = ConstantFoldingAPI.TrySimplify(inst);
            if (simplified.Handle == 0)
                return null;

            var peephole = new PeepholeResult();
            peephole.Add(simplified);
            //Replace(inst, simplified);
            return peephole;
        }

        private void Replace(LLVMValueRef from, LLVMValueRef to)
        {
            if(debug)
                Console.WriteLine($"Rewriting {from}\n=> To:\n{to}");

            from.ReplaceAllUsesWith(to);
            //from.InstructionEraseFromParent();
        }

        private LLVMValueRef Clone(LLVMValueRef inst)
        {
            var clone0 = inst.InstructionClone;
            builder.PositionBefore(inst.NextInstruction);
            builder.Insert(clone0);
            clone0.Name = inst.Name;
            return clone0;
        }
    }
}
