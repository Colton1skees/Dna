using Dna.Binary;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using Dna.Passes.Mba;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;
using static Antlr4.Runtime.Atn.SemanticContext;
using static Dna.LLVMInterop.NativePassApi;
using static System.Net.Mime.MediaTypeNames;

namespace Dna.Passes
{
    public class PeepholeResult
    {
        // List of new values added by the peephole optimization
        // Invariant: The last instruction is the result value.
        public List<LLVMValueRef> Insts = new();

        public LLVMValueRef GetResult() => Insts.Last();

        public void Add(params LLVMValueRef[] insts)
            => Insts.AddRange(insts);

        public void Add(IReadOnlyList<LLVMValueRef> insts)
           => Insts.AddRange(insts);
    }

    // Peephole optimization pass for cases that InstCombine either cannot perform or choses not to due to global profitability
    public class AdhocInstCombinePass
    {
        public static int bar = 0;

        public static void Validate(LLVMValueRef function)
        {
            if (bar != 519)
                return;

            function.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            foreach (var select in function.GetInstructions())
            {
                if (!select.Is(LLVMOpcode.LLVMSelect))
                    continue;

                var op0 = select.GetOperand(1);
                if (!op0.IsConstant() || op0.ConstIntZExt != 68)
                    continue;
                var op1 = select.GetOperand(2);
                if (!op1.IsConstant() || op1.ConstIntZExt != 0)
                    continue;

                if (select.ToString().Contains("%454 = select i1 %455, i64 68, i64 0"))
                    Debugger.Break();
            }
        }

        public static void ValidateSelect(LLVMValueRef select)
        {
            if (bar != 519)
                return;

            if (!select.Is(LLVMOpcode.LLVMSelect))
                return;

            var op0 = select.GetOperand(1);
            if (!op0.IsConstant() || op0.ConstIntZExt != 68)
                return;
            var op1 = select.GetOperand(2);
            if (!op1.IsConstant() || op1.ConstIntZExt != 0)
                return;

            if (select.ToString().Contains("%454 = select i1 %455, i64 68, i64 0"))
                Debugger.Break();
        }



        private readonly bool debug = false;

        public LLVMBuilderRef builder;

        public dgAdhocInstCombinePass PtrToStoreLoadPropagation { get; }

        public unsafe AdhocInstCombinePass()
        {
            PtrToStoreLoadPropagation = new dgAdhocInstCombinePass(InstCombine);
        }

        public unsafe bool InstCombine(LLVMOpaqueValue* function, nint loopInfo, nint domTree, nint mssa, nint simplifyQuery)
        {
            //return false;
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);


            LLVMValueRef f = function;

            bool changed = true;
            changed = false;
            var sq = new SimplifyQuery(simplifyQuery);
            var dt = new DominatorTree(domTree);
            foreach (var inst in f.GetInstructions().ToList())
            {
                var curr = inst;
                while (curr != null)
                {
                    var peephole = PeepholeInst(curr, dt, sq);
                    if (peephole == null)
                    {
                        curr = null;
                        break;
                    }

                    changed = true;
                    f.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
                    curr.ReplaceAllUsesWith(peephole.GetResult());
                    f.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
                    curr = peephole.GetResult();
                }
            }

            return changed;
        }

        public PeepholeResult PeepholeInst(LLVMValueRef inst, DominatorTree domTree, SimplifyQuery query)
        {

            Validate(inst.InstructionParent.Parent);
            var r = PeepholeInstInternal(inst, domTree, query);
            Validate(inst.InstructionParent.Parent);
            return r;
        }

        public PeepholeResult PeepholeInstInternal(LLVMValueRef inst, DominatorTree domTree, SimplifyQuery simplifyQuery)
        {
            if (inst.TypeOf.IntWidth > 64)
                return null;

            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;

            var opc = inst.InstructionOpcode;
            if (inst.InstructionOpcode == LLVMOpcode.LLVMStore || inst.InstructionOpcode == LLVMOpcode.LLVMLoad || inst.InstructionOpcode == LLVMOpcode.LLVMPHI)
                return null;


            PeepholeResult changed = null;

            /*
            changed = TrySimplifyInstruction(inst);
            if (changed != null)
                return changed;
            */

            changed = TryFoldConstantGepChain(inst);
            if (changed != null)
                return changed;

            return changed;

            //return null;

            /*
            if (opc == LLVMOpcode.LLVMShl || opc == LLVMOpcode.LLVMAShr || opc == LLVMOpcode.LLVMShl)
            {
                var rhs = inst.GetOperand(1);

                if (rhs.IsConstant())
                {
                    Debug.Assert(rhs.ConstIntZExt < inst.TypeOf.IntWidth);
                }

                if (!rhs.Is(LLVMOpcode.LLVMAnd) && !rhs.IsConstant())
                {
                    var safeMask = inst.TypeOf.IntWidth - 1;

                    builder.PositionBefore(inst);
                    var and = builder.BuildAnd(rhs, LLVMValueRef.CreateConstInt(inst.TypeOf, safeMask));
                    inst.SetOperand(1, and);
                }
            }
            */

            changed = TryRewriteTruncOfConstantSelect(inst);
            if (changed != null)
                return changed;


            changed = TryDistributeUnarySelect(inst);
            if (changed != null)
                return changed;


            changed = TryDistributeBinarySelect(inst);
            if (changed != null)
                return changed;

            //return changed;

            /*
            changed = TryCollapseRedundantSelectRound(inst);
            if (changed != null)
                return changed;
            */


            changed = TryDistributeTernarySelect(inst);
            if (changed != null)
                return changed;


            changed = TrySimplifyDemandedBits(inst);
            if (changed != null)
                return changed;
            
            /*
            changed = TryKnownBitsFoldToSelect(inst, simplifyQuery);
            if (changed != null)
                return changed;
            */

            changed = TryRewriteVmpShifts(inst);
            if (changed != null)
                return changed;

            changed = TrySimplifyCmp(inst);
            if (changed != null)
                return changed;

            /*
            changed = TryDistributeTruncation(inst);
            if (changed != null)
                return changed;
            */

            // WORKS HERE

            changed = TryRewriteCmpAnd(inst);
            if (changed != null)
                return changed;

            changed = TryEliminateTrunc(inst);
            if (changed != null)
                return changed;

 
            changed = TrySinkLoadOfSelect(inst);
            if (changed != null)
                return changed;

      
            changed = TryRewriteSignExtI1(inst);
            if (changed != null)
                return changed;

            return changed;

            changed = TrySimplifySelectIdentity(inst);
            if (changed != null)
                return changed;


            // STOPS WORKING HERE

            /*
         changed = TryRewriteTruncAnd(inst);
         if (changed != null)
             return changed;

         changed = TryRewriteZextTrunc(inst);
         if (changed != null)
             return changed;
         */
            changed = TryRewriteDisjointOr(inst);
            if (changed != null)
                return changed;


            changed = TryRewriteInverseShifts(inst);
            if (changed != null)
                return changed;

            /*
            changed = TryCanonicalizeInverseCmp(inst);
            if (changed != null)
                return changed;
            */

            changed = TrySimplifyCmp(inst);
            if (changed != null)
                return changed;

            /*
            changed = TryCreateBranchAssumptions(domTree, inst);
            if (changed != null)
                return changed;


            changed = TryCreateBranchAssumptions2(domTree, inst);
            if (changed != null)
                return changed;
            */

            return changed;


            /*
            changed = TryRewriteInverseCmp(inst);
            if (changed != null)
                return changed;

            changed = TryRewriteInverseXor(inst);
            if (changed != null)
                return changed;
            */


            return null;
        }

        private unsafe PeepholeResult TryFoldConstantGepChain(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMGetElementPtr) || inst.OperandCount != 2)
                return null;

            var innerGep = inst.GetOperand(0);
            if (!innerGep.Is(LLVMOpcode.LLVMGetElementPtr) || innerGep.OperandCount != 2)
                return null;

            var outerIndex = inst.GetOperand(1);
            var innerIndex = innerGep.GetOperand(1);
            if (outerIndex.Kind != LLVMValueKind.LLVMConstantIntValueKind ||
                innerIndex.Kind != LLVMValueKind.LLVMConstantIntValueKind ||
                outerIndex.TypeOf.Handle != innerIndex.TypeOf.Handle)
                return null;

            var outerElementType = new LLVMTypeRef((IntPtr)LLVM.GetGEPSourceElementType(inst));
            var innerElementType = new LLVMTypeRef((IntPtr)LLVM.GetGEPSourceElementType(innerGep));

            if (innerIndex.TypeOf.IntWidth != 64 || outerIndex.TypeOf.IntWidth != 64)
                Debugger.Break();

            var combinedIndex = LLVMValueRef.CreateConstInt(
                outerIndex.TypeOf,
                unchecked(innerIndex.ConstIntZExt + outerIndex.ConstIntZExt));

            builder.PositionBefore(inst);
            var isInBounds = LLVM.IsInBounds(inst) != 0 && LLVM.IsInBounds(innerGep) != 0;
            var replacement = isInBounds
                ? builder.BuildInBoundsGEP2(outerElementType, innerGep.GetOperand(0), new[] { combinedIndex })
                : builder.BuildGEP2(outerElementType, innerGep.GetOperand(0), new[] { combinedIndex });

            var peephole = new PeepholeResult();
            peephole.Add(replacement);
            return peephole;
        }

        private static readonly LLVMOpcode[] UnaryOpcodes = { LLVMOpcode.LLVMTrunc, LLVMOpcode.LLVMZExt, LLVMOpcode.LLVMSExt, LLVMOpcode.LLVMCall };

        private static readonly string[] UnaryWhitelist = { "llvm.ctpop" };

        private static readonly LLVMOpcode[] BinaryOpcodes = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr, LLVMOpcode.LLVMCall, LLVMOpcode.LLVMGetElementPtr };


        private static readonly string[] BinaryWhitelist = { "llvm.bswap", "llvm.fshl" };
        /*
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

var selectTrue = selectOperand.GetOperand(1);
var selectFalse = selectOperand.GetOperand(2);
var selectCond = selectOperand.GetOperand(0);

builder.PositionBefore(inst);

// Build the two branch instructions from scratch (instead of cloning `inst`) so we
// don't inherit any per-instruction state (poison flags, metadata, call attribute
// lists, etc.) that could cause a miscompile after we mutate operands.
LLVMValueRef BuildBranch(LLVMValueRef selectBranch)
{
    var lhs = selectIndex == 0 ? selectBranch : otherOperand;
    var rhs = selectIndex == 1 ? selectBranch : otherOperand;
    return opcode switch
    {
        LLVMOpcode.LLVMAdd => builder.BuildAdd(lhs, rhs),
        LLVMOpcode.LLVMSub => builder.BuildSub(lhs, rhs),
        LLVMOpcode.LLVMMul => builder.BuildMul(lhs, rhs),
        LLVMOpcode.LLVMAnd => builder.BuildAnd(lhs, rhs),
        LLVMOpcode.LLVMOr => builder.BuildOr(lhs, rhs),
        LLVMOpcode.LLVMXor => builder.BuildXor(lhs, rhs),
        LLVMOpcode.LLVMShl => builder.BuildShl(lhs, rhs),
        LLVMOpcode.LLVMLShr => builder.BuildLShr(lhs, rhs),
        LLVMOpcode.LLVMAShr => builder.BuildAShr(lhs, rhs),
        LLVMOpcode.LLVMCall => BuildIntrinsicCall(inst, selectBranch),
        _ => throw new InvalidOperationException($"Unsupported opcode in TryDistributeSelect: {opcode}"),
    };
}

var v1 = BuildBranch(selectTrue);
var v2 = BuildBranch(selectFalse);
var res = builder.BuildSelect(selectCond, v1, v2);

var peephole = new PeepholeResult();
peephole.Add(v1, v2, res);
return peephole;
}
*/

        private PeepholeResult TryDistributeUnarySelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(UnaryOpcodes, opcode) == -1)
                return null;

            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!UnaryWhitelist.Any(x => name.StartsWith(x)))
                    return null;
            }

            // Get the operands
            var selectOperand = inst.GetOperand(0);
            if (!selectOperand.Is(LLVMOpcode.LLVMSelect))
                return null;


            builder.PositionBefore(inst);
            var clone0 = Clone(inst);
            builder.PositionBefore(inst);
            var clone1 = Clone(inst);


            builder.PositionBefore(inst);
            clone0.SetOperand(0, selectOperand.GetOperand(1));
            clone1.SetOperand(0, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);
            ValidateSelect(res);
            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);
            return peephole;
        }


        private PeepholeResult TryDistributeBinarySelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(BinaryOpcodes, opcode) == -1)
                return null;



            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!BinaryWhitelist.Any(x => name.StartsWith(x)))
                    return null;
            }

            if (opcode == LLVMOpcode.LLVMGetElementPtr && inst.OperandCount != 2)
                return null;


            // Get the operands
            var op1 = inst.GetOperand(0);
            var op2 = inst.GetOperand(1);

            // At least one operand must be a select of two constants
            var bothConstant = inst.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr;
            if (!IsSelect(op1, bothConstant) && !IsSelect(op2, bothConstant))
                return null;

            var selectIndex = IsSelect(op1, bothConstant) ? 0 : 1;
            var selectOperand = selectIndex == 0 ? op1 : op2;
            var otherIndex = IsSelect(op1, bothConstant) ? 1 : 0;
            var otherOperand = otherIndex == 0 ? op1 : op2;

            builder.PositionBefore(inst);
            var clone0 = Clone(inst);
            builder.PositionBefore(inst);
            builder.PositionBefore(inst);
            var clone1 = Clone(inst);

            builder.PositionBefore(inst);

            clone0.SetOperand((uint)selectIndex, selectOperand.GetOperand(1));
            clone1.SetOperand((uint)selectIndex, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);
            ValidateSelect(res);

            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);

            if (inst.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr)
            {
                //inst.ReplaceAllUsesWith(res);
                //inst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
                //Debugger.Break();
            }
            return peephole;
        }
        // Restrict this to expressions which are guaranteed to fold once their children are
        // visited. It preserves the useful `sext(select(c, K0, K1))` -> add case without making
        // an arbitrary non-constant arm profitable for select distribution.
        private static bool IsConstantLike(LLVMValueRef value)
        {
            if (value.IsConstant())
                return true;
            if (!value.Is(LLVMValueKind.LLVMInstructionValueKind))
                return false;

            return value.InstructionOpcode switch
            {
                LLVMOpcode.LLVMTrunc or LLVMOpcode.LLVMZExt or LLVMOpcode.LLVMSExt
                    => IsConstantLike(value.GetOperand(0)),
                LLVMOpcode.LLVMAdd or LLVMOpcode.LLVMSub or LLVMOpcode.LLVMMul or
                LLVMOpcode.LLVMAnd or LLVMOpcode.LLVMOr or LLVMOpcode.LLVMXor or
                LLVMOpcode.LLVMShl or LLVMOpcode.LLVMLShr or LLVMOpcode.LLVMAShr
                    => IsConstantLike(value.GetOperand(0)) && IsConstantLike(value.GetOperand(1)),
                _ => false,
            };
        }



        /*
        private PeepholeResult TryDistributeBinarySelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(BinaryOpcodes, opcode) == -1)
                return null;

            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!BinaryWhitelist.Any(x => name.StartsWith(x)))
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

            builder.PositionBefore(inst);
            var clone0 = Clone(inst);
            builder.PositionBefore(inst);
            builder.PositionBefore(inst);
            var clone1 = Clone(inst);

            builder.PositionBefore(inst);

            clone0.SetOperand((uint)selectIndex, selectOperand.GetOperand(1));
            clone1.SetOperand((uint)selectIndex, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);
            ValidateSelect(res);

            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);
            return peephole;
        }
        */

        // Merges a binary op of two selects that share the exact same condition, e.g.:
        //   xor(select(c, a, b), select(c, d, e)) => select(c, xor(a, d), xor(b, e))
        // Unlike TryDistributeBinarySelect, this does NOT go through IsSelect()/require either
        // select to have a constant arm: the transform is unconditionally sound whenever both
        // operands are selects gated by the identical condition value, regardless of what their
        // arms are (loads, truncs, etc.), since we're not distributing across a non-select
        // operand - we're just re-associating two selects that already share a branch.
        private PeepholeResult TryMergeSameConditionBinarySelect(LLVMValueRef inst)
        {
            var opcode = inst.InstructionOpcode;
            if (Array.IndexOf(BinaryOpcodes, opcode) == -1)
                return null;

            if (opcode == LLVMOpcode.LLVMCall)
            {
                var name = inst.GetCallInstTarget().Name;
                if (!BinaryWhitelist.Any(x => name.StartsWith(x)))
                    return null;
            }

            var op1 = inst.GetOperand(0);
            var op2 = inst.GetOperand(1);

            if (!op1.Is(LLVMOpcode.LLVMSelect) || !op2.Is(LLVMOpcode.LLVMSelect))
                return null;

            // Both selects must be gated by the exact same condition value.
            if (op1.GetOperand(0) != op2.GetOperand(0))
                return null;

            builder.PositionBefore(inst);
            var clone0 = Clone(inst);
            builder.PositionBefore(inst);
            var clone1 = Clone(inst);

            builder.PositionBefore(inst);
            clone0.SetOperand(0, op1.GetOperand(1));
            clone0.SetOperand(1, op2.GetOperand(1));
            clone1.SetOperand(0, op1.GetOperand(2));
            clone1.SetOperand(1, op2.GetOperand(2));

            var res = builder.BuildSelect(op1.GetOperand(0), clone0, clone1);
            ValidateSelect(res);

            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);
            return peephole;
        }

        // Matches the recurrence shape:
        //   %A = select c1, K1, %X
        //   %B = select c1, %X, K2
        //   %C = select c2, %A, %B
        // i.e. f(X) = select(c2, select(c1,K1,X), select(c1,X,K2)).
        private static bool TryMatchSelectRound(LLVMValueRef value, out LLVMValueRef c1, out LLVMValueRef c2, out LLVMValueRef k1, out LLVMValueRef k2, out LLVMValueRef x)
        {
            c1 = c2 = k1 = k2 = x = null;
            if (!value.Is(LLVMOpcode.LLVMSelect))
                return false;

            c2 = value.GetOperand(0);
            var a = value.GetOperand(1);
            var b = value.GetOperand(2);
            if (!a.Is(LLVMOpcode.LLVMSelect) || !b.Is(LLVMOpcode.LLVMSelect))
                return false;

            c1 = a.GetOperand(0);
            if (b.GetOperand(0) != c1)
                return false;

            var aTrue = a.GetOperand(1);
            var aFalse = a.GetOperand(2);
            var bTrue = b.GetOperand(1);
            var bFalse = b.GetOperand(2);

            // a = select(c1, K1, X); b = select(c1, X, K2) - X must be the exact same value on both sides.
            if (!aTrue.IsConstant() || !bFalse.IsConstant())
                return false;
            if (aFalse != bTrue)
                return false;

            k1 = aTrue;
            k2 = bFalse;
            x = aFalse;
            return true;
        }

        // f(x) = select(c2, select(c1,K1,x), select(c1,x,K2)) is idempotent for ANY x:
        // whichever of the 4 (c1,c2) combinations is taken, the result is either a fixed
        // constant (independent of x) or x itself unchanged. So f(f(y)) == f(y) always,
        // regardless of what y is. This collapses each redundant repeated "round" of an
        // unrolled fixed-point recurrence (same c1/c2/K1/K2 reapplied to its own previous
        // output) down to the first application - purely an identity, no constant-arm
        // requirement beyond the pattern's own literal K1/K2, and IsSelect() is untouched.
        private PeepholeResult TryCollapseRedundantSelectRound(LLVMValueRef inst)
        {
            if (!TryMatchSelectRound(inst, out var c1, out var c2, out var k1, out var k2, out var x))
                return null;

            if (!TryMatchSelectRound(x, out var innerC1, out var innerC2, out var innerK1, out var innerK2, out _))
                return null;

            if (innerC1 != c1 || innerC2 != c2)
                return null;
            if (innerK1.TypeOf.Handle != k1.TypeOf.Handle || innerK2.TypeOf.Handle != k2.TypeOf.Handle)
                return null;
            if (innerK1.ConstIntZExt != k1.ConstIntZExt || innerK2.ConstIntZExt != k2.ConstIntZExt)
                return null;

            var peephole = new PeepholeResult();
            peephole.Add(x);
            return peephole;
        }

        private PeepholeResult TryDistributeTernarySelect(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMCall))
                return null;

            var name = inst.GetCallInstTarget().Name;
            if (!name.StartsWith("llvm.fshl"))
                return null;

            var selectOperand = inst.GetOperand(1);
            if (!selectOperand.Is(LLVMOpcode.LLVMSelect))
                return null;

            builder.PositionBefore(inst);
            var clone0 = Clone(inst);
            builder.PositionBefore(inst);
            builder.PositionBefore(inst);
            var clone1 = Clone(inst);
            builder.PositionBefore(inst);
            clone0.SetOperand((uint)1, selectOperand.GetOperand(1));
            clone1.SetOperand((uint)1, selectOperand.GetOperand(2));

            var res = builder.BuildSelect(selectOperand.GetOperand(0), clone0, clone1);
            ValidateSelect(res);
            var peephole = new PeepholeResult();
            peephole.Add(clone0, clone1, res);
            return peephole;

            return null;
        }

        private static readonly LLVMOpcode[] kbFolds = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr, LLVMOpcode.LLVMZExt };


        private LLVMValueRef BuildIntrinsicCall(LLVMValueRef originalCall, LLVMValueRef newArg0)
        {
            // Only single-argument whitelisted intrinsics (e.g. llvm.bswap) are supported.
            var callee = originalCall.GetCallInstTarget();
            var paramTypes = new LLVMTypeRef[] { originalCall.TypeOf };
            var functionType = LLVMTypeRef.CreateFunction(originalCall.TypeOf, paramTypes);
            return builder.BuildCall2(functionType, callee, new LLVMValueRef[] { newArg0 });
        }

        // bar ? (cond ? a : b) : (cond ? c : d)
        //  =>
        // bar ?
        public PeepholeResult TrySimplifySharedConditionSelect(LLVMValueRef inst)
        {
            return null;
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return null;

            if (inst.GetOperand(1).IsConstant() && inst.GetOperand(2).IsConstant())
                return null;
            if (inst.ToString().Contains("%336 = select i1 %298, i32 %334, i32 %335"))
                Debugger.Break();
            else
                return null;
            // Get the chain of selects
            Stack<LLVMValueRef> stack = new();
            OrderedSet<LLVMValueRef> seen = new();
            stack.Push(inst);
            while (stack.Count != 0)
            {
                var pop = stack.Pop();
                if (pop.IsConstant())
                    continue;
                if (!seen.Add(pop))
                    continue;


                if (!pop.Is(LLVMOpcode.LLVMSelect))
                    return null;

                stack.Push(pop.GetOperand(1));
                stack.Push(pop.GetOperand(2));
            }



            HashSet<LLVMValueRef> substitutions = new();
            Queue<LLVMValueRef> workQueue = new();
            HashSet<LLVMValueRef> visited = new();

            foreach (var select in seen)
            {
                var cond = select.GetOperand(0);
                if (!IsSupported(cond.InstructionOpcode))
                    substitutions.Add(cond);


                if (visited.Add(cond))
                    workQueue.Enqueue(select.GetOperand(0));
            }



            //if (inst.GetOperand(1).ToString().Contains("10737722993") && inst.GetOperand(1).Is(LLVMOpcode.LLVMSelect))
            //{
            //    inst.InstructionParent.Parent.GlobalParent.PrintToFile("translatedFunction.ll");
            //    Debugger.Break();
            //}
            // Problem: Unsupported instruction..


            inst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
            Dictionary<LLVMValueRef, ulong> valueMap = new();
            while (workQueue.Count > 0)
            {
                //Console.WriteLine($"Visiting {inst}");



                // Emulate the
                var variables = workQueue.Concat(substitutions).Distinct().ToArray();
                var numVars = variables.Length;
                if (numVars <= 5)
                {
                    // Assign a value to each variable
                    int numEntries = 1 << 5;
                    var resultVector = new ulong[numEntries];
                    for (var resultVecIdx = 0; resultVecIdx < numEntries; resultVecIdx++)
                    {
                        valueMap.Clear();

                        for (ushort varIdx = 0; varIdx < numVars; varIdx++)
                            valueMap[variables[varIdx]] = (resultVecIdx & (1u << varIdx)) != 0 ? 1ul : 0;

                        var r = Emulate(inst, valueMap); ;
                        resultVector[resultVecIdx] = r;
                        //Console.WriteLine($"Got {r}");
                    }



                    //Console.WriteLine("");

                    var substList = substitutions.ToList();
                    for (int i = 0; i < substList.Count; i++)
                    {
                        var op0 = substList[i];
                        if (!op0.Is(LLVMOpcode.LLVMICmp))
                            continue;
                        for (int j = i + 1; j < substList.Count; j++)
                        {
                            var op1 = substList[j];
                            if (!op1.Is(LLVMOpcode.LLVMICmp))
                                continue;

                            //Debugger.Break();
                        }
                    }

                    var uniqueValues = resultVector.ToHashSet();
                    if (uniqueValues.Count <= 2 && !IsSelectOfTwoConstants(inst))
                    {
                        //Debugger.Break();
                    }

                }


                var current = workQueue.Dequeue();

                foreach (var operand in current.GetOperands())
                {
                    if (!operand.Is(LLVMValueKind.LLVMInstructionValueKind))
                        continue;

                    // Skip operands that are not i1
                    var size = operand.TypeOf.IntWidth;
                    if (size != 1)
                        continue;

                    switch (operand.InstructionOpcode)
                    {
                        case LLVMOpcode.LLVMAnd:
                        case LLVMOpcode.LLVMOr:
                        case LLVMOpcode.LLVMXor:
                        case LLVMOpcode.LLVMSelect:
                            if (visited.Add(operand))
                                workQueue.Enqueue(operand);
                            break;
                        default:
                            substitutions.Add(operand);
                            break;

                    }

                }
            }

            return null;
        }

        private static bool IsSupported(LLVMOpcode opcode)
        {
            return opcode switch
            {
                LLVMOpcode.LLVMAnd or LLVMOpcode.LLVMOr or LLVMOpcode.LLVMXor or LLVMOpcode.LLVMSelect => true,
                _ => false,
            };
        }

        private ulong Emulate(LLVMValueRef value, Dictionary<LLVMValueRef, ulong> valueMap)
        {
            //Console.WriteLine($"Getting {value}");
            if (value.IsConstant())
                return value.ConstIntZExt;

            if (valueMap.TryGetValue(value, out var existing))
                return existing;

            var op0 = () => Emulate(value.GetOperand(0), valueMap);
            var op1 = () => Emulate(value.GetOperand(1), valueMap);
            var op2 = () => Emulate(value.GetOperand(2), valueMap);

            var opc = value.InstructionOpcode;
            var result = opc switch
            {
                LLVMOpcode.LLVMAnd => op0() & op1(),
                LLVMOpcode.LLVMOr => op0() | op1(),
                LLVMOpcode.LLVMXor => op0() ^ op1(),
                LLVMOpcode.LLVMSelect => op0() != 0 ? op1() : op2(),
                _ => throw new InvalidOperationException($"Unimplemented instruction: {opc}")
            };

            valueMap[value] = result;
            return result;
        }

        // Adhoc demanded bits simplifications. TODO: Generalize
        // %134 = select i1 %129, i64 64, i64 0
        // %135 = select i1 %129, i64 192, i64 128
        // %136 = select i1 %125, i64 %135, i64 %134
        // %146 = and i64 %136, 64
        private PeepholeResult TrySimplifyDemandedBits(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMAnd))
                return null;

            var constant = inst.GetOperand(1);
            if (!constant.IsConstant())
                return null;

            var baseSelect = inst.GetOperand(0);
            if (!baseSelect.Is(LLVMOpcode.LLVMSelect))
                return null;


            var select0 = baseSelect.GetOperand(1);
            var select1 = baseSelect.GetOperand(2);
            if (!select0.Is(LLVMOpcode.LLVMSelect) || !select1.Is(LLVMOpcode.LLVMSelect))
                return null;

            if (!IsSelectOfTwoConstants(select0) || !IsSelectOfTwoConstants(select1))
                return null;

            if (select0.GetOperand(0) != select1.GetOperand(0))
                return null;

            var mask = constant.ConstIntZExt;
            var imms0 = select0.GetOperands().Skip(1).Select(x => x.ConstIntZExt & mask).ToArray();
            var imms1 = select1.GetOperands().Skip(1).Select(x => x.ConstIntZExt & mask).ToArray();
            if (imms0[0] != imms1[0])
                return null;
            if (imms0[1] != imms1[1])
                return null;

            builder.PositionBefore(inst);
            var ty = inst.TypeOf;
            var result = builder.BuildSelect(select0.GetOperand(0), LLVMValueRef.CreateConstInt(ty, imms0[0]), LLVMValueRef.CreateConstInt(ty, imms0[1]));
            ValidateSelect(result);
            var peephole = new PeepholeResult();
            peephole.Add(result);
            return peephole;
        }

        private static readonly LLVMOpcode[] distributableOpcodes
            = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor };

        private LLVMValueRef Create(LLVMOpcode opcode, LLVMValueRef[] operands)
        {
            var op0 = () => operands[0];
            var op1 = () => operands[1];
            var op2 = () => operands[2];
            return opcode switch
            {
                LLVMOpcode.LLVMAdd => builder.BuildAdd(op0(), op1()),
                LLVMOpcode.LLVMSub => builder.BuildSub(op0(), op1()),
                LLVMOpcode.LLVMMul => builder.BuildMul(op0(), op1()),
                LLVMOpcode.LLVMAnd => builder.BuildAnd(op0(), op1()),
                LLVMOpcode.LLVMOr => builder.BuildOr(op0(), op1()),
                LLVMOpcode.LLVMXor => builder.BuildXor(op0(), op1()),
                LLVMOpcode.LLVMShl => builder.BuildShl(op0(), op1()),
            };
        }

        // trunc(a+b) => trunc(a) + trunc(b)
        private PeepholeResult TryDistributeTruncation(LLVMValueRef trunc)
        {
            if (!trunc.Is(LLVMOpcode.LLVMTrunc))
                return null;

            var inst = trunc.GetOperand(0);
            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;

            if (!inst.Is(distributableOpcodes))
                return null;

            builder.PositionBefore(inst);
            var children = inst.GetOperands().Select(x => builder.BuildTrunc(x, trunc.TypeOf)).ToArray();

            builder.PositionBefore(inst);
            /*
            var clone = Clone(inst);
            for(int i = 0; i < children.Length; i++)
            {
                clone.SetOperand((uint)i, children[i]);
            }
            */

            var result = Create(inst.InstructionOpcode, children);

            //var result = builder.BuildTrunc(clone, trunc.TypeOf);

            var peephole = new PeepholeResult();
            peephole.Add(children);
            peephole.Add(result);

            return peephole;

        }

        // trunc(trunc, a)
        private PeepholeResult TryEliminateTrunc(LLVMValueRef trunc)
        {
            if (!trunc.Is(LLVMOpcode.LLVMTrunc))
                return null;

            var inst = trunc.GetOperand(0);
            if (!inst.Is(LLVMOpcode.LLVMTrunc))
                return null;

            if (inst.TypeOf.Handle != trunc.TypeOf.Handle)
                return null;

            var peephole = new PeepholeResult();
            peephole.Add(inst);
            return peephole;

        }

        private PeepholeResult TryKnownBitsFoldToSelect(LLVMValueRef inst, SimplifyQuery simplifyQuery)
        {
            if (!inst.Is(kbFolds))
                return null;

            var isI1 = inst.TypeOf.IntWidth <= 1;
            //var isAnd1 = inst.Is(LLVMOpcode.LLVMAnd) && inst.GetOperand(1).IsConstant() && inst.GetOperand(1).ConstIntZExt == 1;
            if (inst.TypeOf.IntWidth <= 1)
                return null;

            var kb = NativeKnownBits.Get(inst, simplifyQuery);
            if (kb.GetUnknownBitCount() != 1)
                return null;

            var users = inst.GetUsers().ToList();
            if (users.All(x => x.Is(LLVMOpcode.LLVMICmp)))
                return null;

            // Clone the source instruction
            builder.PositionBefore(inst.NextInstruction);
            var clone = Clone(inst);



            var ty = inst.TypeOf;
            var values = kb.AllPossibleValues().Select(x => LLVMValueRef.CreateConstInt(ty, x)).ToList();
            var cmp = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, clone, values[0]);
            var select = builder.BuildSelect(cmp, values[0], values[1]);
            ValidateSelect(select);
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
            else if (isZext)
                lambda = (op1) => builder.BuildZExt(op1, inst.TypeOf);
            else
                throw new InvalidOperationException($"Unknown opkind in TryRewriteConstantShiftOfConstantSelect!");

            var v1 = lambda(selectOp1);
            var v2 = lambda(selectOp2);
            var newSelect = builder.BuildSelect(select.GetOperand(0), v1, v2);
            ValidateSelect(newSelect);
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
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind && inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        private static bool IsRealSelect(LLVMValueRef inst)
        {

            if (inst.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;

            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;

            return true;
        }

        private static bool IsSelect(LLVMValueRef inst, bool bothConstants = true)
        {
            return bothConstants ? IsSelectOfTwoConstants(inst) : inst.InstructionOpcode == LLVMOpcode.LLVMSelect;
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
            var ptr0 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(1) });
            var load0 = builder.BuildLoad2(inst.TypeOf, ptr0);

            var ptr1 = builder.BuildInBoundsGEP2(gep.TypeOf, gep.GetOperand(0), new LLVMValueRef[] { select.GetOperand(2) });
            var load1 = builder.BuildLoad2(inst.TypeOf, ptr1);

            var replacement = builder.BuildSelect(select.GetOperand(0), load0, load1);
            ValidateSelect(replacement);

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
            var extended = builder.BuildZExt(inst.GetOperand(0), type);
            var negated = builder.BuildSub(LLVMValueRef.CreateConstInt(type, 0), extended);
            var peephole = new PeepholeResult();
            peephole.Add(extended, negated);
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


        // %3581 = trunc i64 %3017 to i8
        // %3582 = zext i8 %3581 to i64
        // =>
        // (x & 255)
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

        //   %133 = and i64 %9, 4294967295
        // %134 = icmp eq i64 % 133, 0
        //
        // =>
        // trunc(%9, i32)
        // %9 == 0
        private PeepholeResult TryRewriteCmpAnd(LLVMValueRef icmp)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp))
                return null;
            var kind = icmp.ICmpPredicate;
            if (kind != LLVMIntPredicate.LLVMIntNE && kind != LLVMIntPredicate.LLVMIntEQ)
                return null;

            var constant = icmp.GetOperand(1);
            if (!constant.IsConstant())
                return null;
            if (constant.ConstIntZExt > 4294967295)
                return null;

            var andInst = icmp.GetOperand(0);
            if (!andInst.Is(LLVMOpcode.LLVMAnd))
                return null;

            var andMask = andInst.GetOperand(1);
            if (!andMask.IsConstant() || andMask.ConstIntZExt > 4294967295)
                return null;
            if (andMask.TypeOf.IntWidth != 64)
                return null;

            builder.PositionBefore(andInst);
            var peephole = new PeepholeResult();
            var trunc = builder.BuildTrunc(andInst.GetOperand(0), LLVMTypeRef.Int32);
            peephole.Add(trunc);
            if (andMask.ConstIntZExt != 4294967295)
            {
                trunc = builder.BuildAnd(trunc, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, andMask.ConstIntZExt));
                peephole.Add(trunc);
            }


            var newCmp = builder.BuildICmp(icmp.ICmpPredicate, trunc, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, constant.ConstIntZExt));
            peephole.Add(trunc);
            peephole.Add(newCmp);

            return peephole;
        }

        private PeepholeResult TrySimplifySelectIdentity(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMSelect))
                return null;
            if (inst.TypeOf.IntWidth != 1)
                return null;

            if (!inst.GetOperand(1).IsConstant() && !inst.GetOperand(2).IsConstant())
                return null;

            var cond = inst.GetOperand(0);
            var then = inst.GetOperand(1);
            var other = inst.GetOperand(2);
            builder.PositionBefore(inst);
            var peephole = new PeepholeResult();
            if (other.IsConstant(0))
            {
                peephole.Add(builder.BuildAnd(cond, then));
                return peephole;
            }

            if (then.IsConstant(1))
            {
                peephole.Add(builder.BuildOr(cond, other));
                return peephole;
            }

            return null;

        }

        // TODO: Canonicalize this
        /*
        
         %371 = and i64 %phiofops.in, 4294967295

          %372 = trunc i64 %phiofops.in to i32

          %418 = icmp eq i32 %372, 23423235

          %419 = icmp eq i64 %371, 23423235
        */
        //  %371 = and i64 %phiofops.in, 4294967295
        //  %419 = icmp eq i64 %371, 23423235
        // =>
        //  %372 = trunc i64 %phiofops.in to i32
        //  %419.canon = icmp eq i32 % 372, 23423235
        private PeepholeResult TryRewriteCmpAndOld(LLVMValueRef icmp)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp))
                return null;
            if (icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntEQ && icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntNE)
                return null;

            //if (icmp.ToString().Contains("%136 = icmp eq i64 %13"))
            //    Debugger.Break();

            var cmpImm = icmp.GetOperand(1);
            if (!cmpImm.IsConstant())
                return null;
            var cmpMask = cmpImm.ConstIntZExt;
            var cmpWidth = (ulong)cmpImm.TypeOf.IntWidth - (ulong)BitOperations.LeadingZeroCount(cmpImm.ConstIntZExt);
            cmpWidth = NearestPow2(cmpWidth);
            if (cmpWidth >= cmpImm.TypeOf.IntWidth)
                return null;

            //  %371 = and i64 %phiofops.in, 4294967295
            //  %419 = icmp eq i64 %371, 23423235
            var and = icmp.GetOperand(0);
            if (!and.Is(LLVMOpcode.LLVMAnd))
                return null;
            var andImm = and.GetOperand(1);
            if (!andImm.IsConstant())
                return null;
            var andMask = andImm.ConstIntZExt;
            var andWidth = BitOperations.TrailingZeroCount(~andMask);
            if (andWidth >= cmpImm.TypeOf.IntWidth)
                return null;

            if (BitOperations.PopCount((uint)andWidth) != 1)
                return null;

            builder.PositionBefore(and);
            var t0 = builder.BuildTrunc(and.GetOperand(0), LLVMTypeRef.CreateInt((uint)andWidth));
            var t1 = builder.BuildICmp(icmp.ICmpPredicate, t0, LLVMValueRef.CreateConstInt(t0.TypeOf, cmpMask));

            var peephole = new PeepholeResult();
            peephole.Add(t0);
            peephole.Add(t1);
            return peephole;

            /*
            var truncWidth = and.TypeOf.IntWidth;
            var andWidth = BitOperations.TrailingZeroCount(~andMask);
            if (andWidth >= cmpImm.TypeOf.IntWidth)
                return null;
            */



            return null;
        }

        public static ulong NearestPow2(ulong value)
        {
            if (value <= 1)
                return 1;

            if (value >= 0x8000000000000000)
                return 0x8000000000000000;

            int leadingZeros = BitOperations.LeadingZeroCount(value);
            ulong prevPower = 1UL << (63 - leadingZeros);
            ulong nextPower = prevPower << 1;

            return (value - prevPower < nextPower - value) ? prevPower : nextPower;
        }

        // %419 = icmp ne i32 %417, 2
        // %420 = icmp eq i32 % 417, 2
        private PeepholeResult TryRewriteInverseCmp(LLVMValueRef icmp)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp))
                return null;
        https://open.spotify.com/playlist/2OfZT7teUPaGjHWRgGqMta
            if (icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntEQ && icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntNE)
                return null;

            var inversePredicate = icmp.ICmpPredicate == LLVMIntPredicate.LLVMIntEQ
                ? LLVMIntPredicate.LLVMIntNE
                : LLVMIntPredicate.LLVMIntEQ;


            int depth = 0;
            var curr = icmp.NextInstruction;
            while (curr.Handle != 0 && curr.InstructionParent == icmp.InstructionParent && depth < 10)
            {
                //if (curr.Is(LLVMOpcode.LLVMICmp) && curr.ICmpPredicate == inversePredicate && HasSameCommutativeOperands(icmp, curr))
                if (AreInverseComparisons(icmp, curr))
                {
                    var insertBefore = curr.NextInstruction;
                    if (insertBefore.Handle == 0)
                        return null;

                    builder.PositionBefore(insertBefore);
                    var not = builder.BuildXor(curr, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1));

                    var peephole = new PeepholeResult();
                    peephole.Add(not);
                    //icmp.InstructionParent.Parent.VerifyFunction(LLVMVerifierFailureAction.LLVMAbortProcessAction);
                    return peephole;
                }

                curr = curr.NextInstruction;
                depth++;
            }

            return null;
        }

        private static bool AreInverseComparisons(LLVMValueRef icmp, LLVMValueRef other)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp) || !other.Is(LLVMOpcode.LLVMICmp))
                return false;

            if (icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntEQ && icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntNE)
                return false;

            var inversePredicate = icmp.ICmpPredicate == LLVMIntPredicate.LLVMIntEQ
                ? LLVMIntPredicate.LLVMIntNE
                : LLVMIntPredicate.LLVMIntEQ;

            return other.ICmpPredicate == inversePredicate && HasSameCommutativeOperands(icmp, other);
        }

        private static bool HasSameCommutativeOperands(LLVMValueRef a, LLVMValueRef b)
        {
            var a0 = a.GetOperand(0);
            var a1 = a.GetOperand(1);
            var b0 = b.GetOperand(0);
            var b1 = b.GetOperand(1);

            return (a0.Handle == b0.Handle && a1.Handle == b1.Handle) ||
                   (a0.Handle == b1.Handle && a1.Handle == b0.Handle);
        }

        // TODO: Validate correctness
        private PeepholeResult TryRewriteInverseXor(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMXor))
                return null;

            // Walk the previous up to 10 instructions looking for an earlier xor that is
            // the inverse of `inst` (AreInverseXor(curr, inst)). Searching backward (rather
            // than forward, as this used to) means `curr` always dominates `inst` - and
            // therefore dominates every existing use of `inst` too, since those uses are
            // themselves dominated by `inst`. Building NOT(curr) right before `inst` is
            // therefore always safe, unlike searching forward for a later partner, which
            // can be positioned after some of `inst`'s own uses and break dominance.
            int depth = 0;
            var curr = inst.PreviousInstruction;
            while (curr.Handle != 0 && curr.InstructionParent == inst.InstructionParent && depth < 10)
            {
                if (AreInverseXor(curr, inst))
                {
                    builder.PositionBefore(inst);
                    var not = builder.BuildXor(curr, LLVMValueRef.CreateConstInt(curr.TypeOf, ulong.MaxValue));

                    var peephole = new PeepholeResult();
                    peephole.Add(not);
                    return peephole;
                }

                curr = curr.PreviousInstruction;
                depth++;
            }

            return null;
        }

        // %476 = xor i1 %467, %420
        // %477 = xor i1 %467, %419
        //
        // where %420 = xor i1 %419, true
        private static bool AreInverseXor(LLVMValueRef xor1, LLVMValueRef xor2)
        {
            if (!xor1.Is(LLVMOpcode.LLVMXor) || !xor2.Is(LLVMOpcode.LLVMXor))
                return false;

            if (xor1.GetOperand(0) != xor2.GetOperand(0))
                return false;

            var t420 = xor1.GetOperand(1);
            if (!t420.Is(LLVMOpcode.LLVMXor))
                return false;
            if (t420.GetOperand(0) != xor2.GetOperand(1))
                return false;

            var trueOp = t420.GetOperand(1);
            if (!trueOp.IsConstant())
                return false;
            if (trueOp.ConstIntZExt != ModuloReducer.GetMask(xor1.TypeOf.IntWidth))
                return false;

            return true;
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

        // icmp eq (and X, 1), 0 -> xor(trunc X to i1), true
        private PeepholeResult TrySimplifyCmp(LLVMValueRef icmp)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp))
                return null;
            if (icmp.ICmpPredicate != LLVMIntPredicate.LLVMIntEQ)
                return null;

            var lhs = icmp.GetOperand(0);
            var rhs = icmp.GetOperand(1);

            // Equality is commutative, so canonicalize the zero to the RHS.
            if (lhs.IsConstant(0))
                (lhs, rhs) = (rhs, lhs);
            if (!rhs.IsConstant(0))
                return null;

            if (!lhs.Is(LLVMOpcode.LLVMAnd))
                return null;

            var x = lhs.GetOperand(0);
            var mask = lhs.GetOperand(1);

            // `and` is commutative, so accept either operand order.
            if (x.IsConstant(1))
                (x, mask) = (mask, x);
            if (!mask.IsConstant(1))
                return null;

            builder.PositionBefore(icmp);
            var peephole = new PeepholeResult();

            LLVMValueRef lowBit;
            if (x.TypeOf.IntWidth == 1)
            {
                lowBit = x;
            }
            else
            {
                lowBit = builder.BuildTrunc(x, LLVMTypeRef.Int1);
                peephole.Add(lowBit);
            }

            var not = builder.BuildXor(lowBit, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1));
            peephole.Add(not);
            return peephole;

        }

        private PeepholeResult TryCanonicalizeInverseCmp(LLVMValueRef icmp)
        {
            if (!icmp.Is(LLVMOpcode.LLVMICmp))
                return null;

            var predicate = icmp.ICmpPredicate;
            if (!IsComplementablePredicate(predicate))
                return null;

            var curr = icmp.PreviousInstruction;
            int depth = 0;
            while (curr.Handle != 0 &&
                   curr.InstructionParent == icmp.InstructionParent &&
                   depth < 100)
            {
                // Don't count calls in the walk limit. Sometimes we insert a lot of intrinsic calls
                if (!curr.Is(LLVMOpcode.LLVMCall))
                    depth++;

                if (!AreComplementaryComparisons(curr, icmp))
                {
                    curr = curr.PreviousInstruction;
                    continue;
                }

                // curr is earlier in the same block and therefore dominates icmp.
                builder.PositionBefore(icmp);
                var not = builder.BuildXor(curr, LLVMValueRef.CreateConstInt(curr.TypeOf, 1));
                var peephole = new PeepholeResult();
                peephole.Add(not);
                return peephole;
            }

            return null;
        }

        private static bool IsComplementablePredicate(LLVMIntPredicate predicate)
            => predicate is LLVMIntPredicate.LLVMIntEQ or LLVMIntPredicate.LLVMIntNE or
               LLVMIntPredicate.LLVMIntUGT or LLVMIntPredicate.LLVMIntUGE or
               LLVMIntPredicate.LLVMIntULT or LLVMIntPredicate.LLVMIntULE or
               LLVMIntPredicate.LLVMIntSGT or LLVMIntPredicate.LLVMIntSGE or
               LLVMIntPredicate.LLVMIntSLT or LLVMIntPredicate.LLVMIntSLE;

        // Covers exact complements such as `eq`/`ne`, `slt`/`sge`, and `ugt`/`ule`,
        // plus the adjacent-boundary forms emitted by the flag reconstruction:
        // `sgt x, K - 1` <=> `not (slt x, K)` and `ugt x, K` <=> `not (ult x, K + 1)`.
        private static bool AreComplementaryComparisons(LLVMValueRef first, LLVMValueRef second)
        {
            if (!first.Is(LLVMOpcode.LLVMICmp) || !second.Is(LLVMOpcode.LLVMICmp))
                return false;

            if (HasSameSignFlag(first) != HasSameSignFlag(second))
                return false;

            bool inversePredicates = AreInversePredicates(first.ICmpPredicate, second.ICmpPredicate);
            bool equalityPair = first.ICmpPredicate is LLVMIntPredicate.LLVMIntEQ or LLVMIntPredicate.LLVMIntNE;
            bool sameOperands = equalityPair
                ? HasSameCommutativeOperands(first, second)
                : first.GetOperand(0) == second.GetOperand(0) && first.GetOperand(1) == second.GetOperand(1);
            if (sameOperands && inversePredicates)
            {
                return true;
            }

            return AreAdjacentRangeComplements(first, second, HasSameSignFlag(first));
        }

        private static bool AreInversePredicates(LLVMIntPredicate first, LLVMIntPredicate second)
            => (first, second) is
                (LLVMIntPredicate.LLVMIntEQ, LLVMIntPredicate.LLVMIntNE) or
                (LLVMIntPredicate.LLVMIntNE, LLVMIntPredicate.LLVMIntEQ) or
                (LLVMIntPredicate.LLVMIntUGT, LLVMIntPredicate.LLVMIntULE) or
                (LLVMIntPredicate.LLVMIntULE, LLVMIntPredicate.LLVMIntUGT) or
                (LLVMIntPredicate.LLVMIntUGE, LLVMIntPredicate.LLVMIntULT) or
                (LLVMIntPredicate.LLVMIntULT, LLVMIntPredicate.LLVMIntUGE) or
                (LLVMIntPredicate.LLVMIntSGT, LLVMIntPredicate.LLVMIntSLE) or
                (LLVMIntPredicate.LLVMIntSLE, LLVMIntPredicate.LLVMIntSGT) or
                (LLVMIntPredicate.LLVMIntSGE, LLVMIntPredicate.LLVMIntSLT) or
                (LLVMIntPredicate.LLVMIntSLT, LLVMIntPredicate.LLVMIntSGE);

        private static bool AreAdjacentRangeComplements(LLVMValueRef first, LLVMValueRef second, bool sameSign)
        {
            LLVMValueRef greater;
            LLVMValueRef less;
            bool signed;
            if ((first.ICmpPredicate is LLVMIntPredicate.LLVMIntUGT or LLVMIntPredicate.LLVMIntSGT) &&
                (second.ICmpPredicate is LLVMIntPredicate.LLVMIntULT or LLVMIntPredicate.LLVMIntSLT))
            {
                greater = first;
                less = second;
                signed = first.ICmpPredicate == LLVMIntPredicate.LLVMIntSGT;
            }
            else if ((second.ICmpPredicate is LLVMIntPredicate.LLVMIntUGT or LLVMIntPredicate.LLVMIntSGT) &&
                     (first.ICmpPredicate is LLVMIntPredicate.LLVMIntULT or LLVMIntPredicate.LLVMIntSLT))
            {
                greater = second;
                less = first;
                signed = second.ICmpPredicate == LLVMIntPredicate.LLVMIntSGT;
            }
            else
                return false;

            if ((signed && less.ICmpPredicate != LLVMIntPredicate.LLVMIntSLT) ||
                (!signed && less.ICmpPredicate != LLVMIntPredicate.LLVMIntULT))
            {
                return false;
            }

            var lowerBound = greater.GetOperand(1);
            var upperBound = less.GetOperand(1);
            if (greater.GetOperand(0) != less.GetOperand(0) ||
                !lowerBound.IsConstant() ||
                !upperBound.IsConstant() ||
                lowerBound.TypeOf.Handle != upperBound.TypeOf.Handle)
            {
                return false;
            }

            var bitWidth = lowerBound.TypeOf.IntWidth;
            if (bitWidth == 0 || bitWidth > 64)
                return false;

            var maxValue = ModuloReducer.GetMask(bitWidth);
            var lower = lowerBound.ConstIntZExt & maxValue;
            var upper = upperBound.ConstIntZExt & maxValue;
            if (upper != ((lower + 1) & maxValue))
                return false;

            if (signed)
            {
                var signedMax = (1UL << ((int)bitWidth - 1)) - 1;
                if (lower == signedMax)
                    return false;
            }
            else if (lower == maxValue)
            {
                return false;
            }

            // `samesign` asserts both operands share the same sign bit at every dominated use.
            // That assumption is only observable at the sign-bit boundary (e.g. 0x7FFF.. ->
            // 0x8000..), so adjacent bounds crossing it cannot be folded under the flag; ordinary
            // in-range bounds like 2047/2048 don't cross it and remain safe to fold.
            if (sameSign)
            {
                var signBit = 1UL << ((int)bitWidth - 1);
                if ((lower & signBit) != (upper & signBit))
                    return false;
            }

            return true;
        }

        private static bool HasSameSignFlag(LLVMValueRef icmp)
            => LLVMUtilApi.HasSameSign(icmp);
        // => icmp.ToString().Contains("icmp samesign ", StringComparison.Ordinal);


        private static bool IsICmp(
        LLVMValueRef value,
        LLVMIntPredicate expectedPredicate,
        out LLVMValueRef lhs,
        out LLVMValueRef rhs)
        {
            lhs = null;
            rhs = null;

            if (value.Kind != LLVMValueKind.LLVMInstructionValueKind ||
                value.InstructionOpcode != LLVMOpcode.LLVMICmp ||
                value.ICmpPredicate != expectedPredicate)
            {
                return false;
            }

            lhs = value.GetOperand(0);
            rhs = value.GetOperand(1);
            return true;
        }

        // If a conditional branch's block is the sole gateway into one of its successors
        // (i.e. it dominates that successor), then every execution reaching the successor
        // must have taken that specific edge. Record that fact via an `llvm.assume` at the
        // top of the successor so later folds/simplifications can make use of it.
        private PeepholeResult TryCreateBranchAssumptions(DominatorTree domTree, LLVMValueRef branchInst)
        {
            if (!branchInst.Is(LLVMOpcode.LLVMBr))
                return null;
            if (branchInst.OperandCount != 3)
                return null;

            var block = branchInst.InstructionParent;
            var cond = branchInst.GetOperand(0);

            for (uint i = 0; i < 2; i++)
            {
                // GetSuccessor(0) is the true destination, GetSuccessor(1) is the false destination.
                var succ = branchInst.GetSuccessor(i);
                bool isTrueEdge = i == 0;

                if (!domTree.ProperlyDominates(block, succ))
                    continue;

                // Don't insert a redundant assume if an equivalent one already exists.
                if (BlockHasLeadingAssumeOf(succ, cond, isTrueEdge))
                    continue;

                var insertBefore = GetFirstNonPhiInstruction(succ);
                if (insertBefore.Handle == 0)
                    continue;

                builder.PositionBefore(insertBefore);
                var peephole = new PeepholeResult();

                var assumedCond = cond;
                if (!isTrueEdge)
                {
                    assumedCond = builder.BuildXor(cond, LLVMValueRef.CreateConstInt(cond.TypeOf, 1));
                    peephole.Add(assumedCond);
                }

                var assumeFn = GetAssumeIntrinsic(branchInst.GetFunction().GlobalParent);
                var call = builder.BuildCall2(assumeFn.GetFunctionPrototype(), assumeFn, new LLVMValueRef[] { assumedCond });
                peephole.Add(call);

                // Do not return the peephole because there's nothing to replace. We only added new facts
                return null;
            }

            return null;
        }

        // If every predecessor of a block reaches it by enforcing the exact same condition
        // (e.g. every predecessor branches on `icmp eq %x, 0`, always taking the edge into
        // this block on the same truth value), then that fact holds unconditionally at the
        // top of the block - regardless of which predecessor was actually taken. Unlike
        // TryCreateBranchAssumptions, this does not require a single dominating branch: it
        // instead requires unanimous agreement across all incoming edges.
        private PeepholeResult TryCreateBranchAssumptions2(DominatorTree domTree, LLVMValueRef inst)
        {
            var block = inst.InstructionParent;
            if (block.Handle == 0)
                return null;

            // This is a block-level fact, not an instruction-level one. Only evaluate it once
            // per block, when visiting the first instruction eligible to host the assume.
            if (inst != GetFirstNonPhiInstruction(block))
                return null;

            var predecessors = block.GetPredecessors();
            if (predecessors.Count == 0)
                return null;

            LLVMValueRef canonicalCond = null;
            bool canonicalWantTrue = false;

            foreach (var pred in predecessors)
            {
                var terminator = pred.LastInstruction;
                if (!terminator.Is(LLVMOpcode.LLVMBr) || terminator.OperandCount != 3)
                    return null;

                // GetSuccessor(0) is the true destination, GetSuccessor(1) is the false destination.
                var trueDest = terminator.GetSuccessor(0);
                var falseDest = terminator.GetSuccessor(1);
                if (trueDest == falseDest)
                    return null;

                bool wantTrue;
                if (trueDest == block)
                    wantTrue = true;
                else if (falseDest == block)
                    wantTrue = false;
                else
                    return null;

                var cond = terminator.GetOperand(0);
                if (canonicalCond == null)
                {
                    canonicalCond = cond;
                    canonicalWantTrue = wantTrue;
                    continue;
                }

                if (wantTrue != canonicalWantTrue || !AreSameCondition(canonicalCond, cond))
                    return null;
            }

            if (canonicalCond == null)
                return null;

            // Don't insert a redundant assume if an equivalent one already exists.
            if (BlockHasLeadingAssumeOf(block, canonicalCond, canonicalWantTrue))
                return null;

            builder.PositionBefore(inst);
            var peephole = new PeepholeResult();

            var assumedCond = canonicalCond;
            if (!canonicalWantTrue)
            {
                assumedCond = builder.BuildXor(canonicalCond, LLVMValueRef.CreateConstInt(canonicalCond.TypeOf, 1));
                peephole.Add(assumedCond);
            }

            var assumeFn = GetAssumeIntrinsic(block.Parent.GlobalParent);
            var call = builder.BuildCall2(assumeFn.GetFunctionPrototype(), assumeFn, new LLVMValueRef[] { assumedCond });
            peephole.Add(call);

            // Do not return the peephole because there's nothing to replace. We only added new facts
            return null;
        }

        // True if `a` and `b` are guaranteed to evaluate to the same boolean result: either the
        // exact same value, or structurally identical icmp instructions (same predicate, and -
        // respecting commutativity only for eq/ne - the same operands).
        private static bool AreSameCondition(LLVMValueRef a, LLVMValueRef b)
        {
            if (a == b)
                return true;

            if (!a.Is(LLVMOpcode.LLVMICmp) || !b.Is(LLVMOpcode.LLVMICmp))
                return false;

            if (a.ICmpPredicate != b.ICmpPredicate)
                return false;

            bool equalityPair = a.ICmpPredicate is LLVMIntPredicate.LLVMIntEQ or LLVMIntPredicate.LLVMIntNE;
            return equalityPair
                ? HasSameCommutativeOperands(a, b)
                : a.GetOperand(0) == b.GetOperand(0) && a.GetOperand(1) == b.GetOperand(1);
        }

        private static LLVMValueRef GetFirstNonPhiInstruction(LLVMBasicBlockRef block)
        {
            var inst = block.FirstInstruction;
            while (inst.Handle != 0 && inst.Is(LLVMOpcode.LLVMPHI))
                inst = inst.NextInstruction;

            return inst;
        }

        // Checks whether `block` already begins with an `llvm.assume` that asserts the same
        // fact we're about to insert (either `cond` itself, or its logical negation).
        private static bool BlockHasLeadingAssumeOf(LLVMBasicBlockRef block, LLVMValueRef cond, bool wantTrue)
        {
            var inst = GetFirstNonPhiInstruction(block);
            while (inst.Handle != 0 && IsAssumeCall(inst))
            {
                var arg = inst.GetOperand(0);
                if (wantTrue ? arg == cond : IsLogicalNotOf(arg, cond))
                    return true;

                inst = inst.NextInstruction;
            }

            return false;
        }

        private static bool IsAssumeCall(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMOpcode.LLVMCall))
                return false;

            var target = inst.GetCallInstTarget();
            return target.Kind == LLVMValueKind.LLVMFunctionValueKind && target.Name == "llvm.assume";
        }

        private static bool IsLogicalNotOf(LLVMValueRef value, LLVMValueRef cond)
        {
            if (!value.Is(LLVMOpcode.LLVMXor))
                return false;

            var op0 = value.GetOperand(0);
            var op1 = value.GetOperand(1);
            return (op0 == cond && op1.IsConstant(1)) || (op1 == cond && op0.IsConstant(1));
        }

        private LLVMValueRef GetAssumeIntrinsic(LLVMModuleRef module)
        {
            var assume = module.GetNamedFunction("llvm.assume");
            if (assume.Handle != 0)
                return assume;

            var ctx = module.GetCtx();
            var prototype = LLVMTypeRef.CreateFunction(ctx.VoidType, new LLVMTypeRef[] { ctx.Int1Type });
            return module.AddFunction("llvm.assume", prototype);
        }

        static HashSet<LLVMOpcode> opcodes = new();
        private PeepholeResult TrySimplifyInstruction(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;


            var simplified = ConstantFoldingAPI.TrySimplify(inst);
            if (simplified.Handle == 0)
                return null;


            //opcodes.Add(inst.InstructionOpcode);

            //foreach (var opc in opcodes)
            //    Console.WriteLine(opc);

            var peephole = new PeepholeResult();
            peephole.Add(simplified);
            //Console.WriteLine($"Replacing {inst} with {simplified}");
            //Replace(inst, simplified);
            return peephole;
        }

        private void Replace(LLVMValueRef from, LLVMValueRef to)
        {
            if (debug)
                Console.WriteLine($"Rewriting {from}\n=> To:\n{to}");

            from.ReplaceAllUsesWith(to);
            //from.InstructionEraseFromParent();
        }

        private LLVMValueRef Clone(LLVMValueRef inst)
        {
            var clone0 = inst.InstructionClone;
            //builder.PositionBefore(inst.NextInstruction);
            builder.Insert(clone0);
            clone0.Name = inst.Name;
            return clone0;
        }
    }
}
