using Dna.Binary;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
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
        public PeepholeResult()
        {
            if (AdhocInstCombinePass.bar == 519)
            {
                StackTrace stackTrace = new StackTrace(1);
                StackFrame callingFrame = stackTrace.GetFrame(0);
                MethodBase callingMethod = callingFrame.GetMethod();
                string methodName = callingMethod.Name;
                Console.WriteLine($"Constructor was invoked by: {methodName}");
            }
        }

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

        public unsafe bool InstCombine(LLVMOpaqueValue* function, nint loopInfo, nint mssa, nint simplifyQuery)
        {
            //return false;
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);


            LLVMValueRef f = function;

            bool changed = true;
            changed = false;
            var sq = new SimplifyQuery(simplifyQuery);
            foreach (var inst in f.GetInstructions().ToList())
            {
                var curr = inst;
                while (curr != null)
                {
                    var peephole = PeepholeInst(curr, sq);
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

        public PeepholeResult PeepholeInst(LLVMValueRef inst, SimplifyQuery query)
        {

            Validate(inst.InstructionParent.Parent);
            var r = PeepholeInstInternal(inst, query);
            Validate(inst.InstructionParent.Parent);
            return r;
        }

        public PeepholeResult PeepholeInstInternal(LLVMValueRef inst, SimplifyQuery simplifyQuery)
        {
            if (inst.TypeOf.IntWidth > 64)
                return null;

            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;

            var opc = inst.InstructionOpcode;
            if (inst.InstructionOpcode == LLVMOpcode.LLVMStore || inst.InstructionOpcode == LLVMOpcode.LLVMLoad || inst.InstructionOpcode == LLVMOpcode.LLVMPHI)
                return null;


            PeepholeResult changed = null;

            //changed = TrySimplifyInstruction(inst);
            //if (changed != null)
            //    return changed;

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

            changed = TryDistributeTernarySelect(inst);
            if (changed != null)
                return changed;

            changed = TrySimplifySharedConditionSelect(inst);
            if (changed != null)
                return changed;


            changed = TrySimplifyDemandedBits(inst);
            if (changed != null)
                return changed;


            changed = TryKnownBitsFoldToSelect(inst, simplifyQuery);
            if (changed != null)
                return changed;

            changed = TryRewriteVmpShifts(inst);
            if (changed != null)
                return changed;

            /*
            changed = TryDistributeTruncation(inst);
            if (changed != null)
                return changed;
            */

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

        private static readonly LLVMOpcode[] UnaryOpcodes = { LLVMOpcode.LLVMTrunc, LLVMOpcode.LLVMCall };

        private static readonly string[] UnaryWhitelist = { "llvm.ctpop" };

        private static readonly LLVMOpcode[] BinaryOpcodes = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr, LLVMOpcode.LLVMCall };


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

        private static readonly LLVMOpcode[] kbFolds = { LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub, LLVMOpcode.LLVMMul, LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor, LLVMOpcode.LLVMShl, LLVMOpcode.LLVMLShr, LLVMOpcode.LLVMAShr };


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
        private PeepholeResult TrySimplifySharedConditionSelect(LLVMValueRef inst)
        {
            return null;
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
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
                        for (int j = i + 1; j < substList.Count; i++)
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
                        Debugger.Break();
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
                LLVMOpcode.LLVMXor => op0() | op1(),
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
            var select = builder.BuildSelect(inst.GetOperand(0), LLVMValueRef.CreateConstInt(type, ulong.MaxValue), LLVMValueRef.CreateConstInt(type, 0));
            ValidateSelect(select);
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
            if (constant.ConstIntZExt != 0)
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


            var newCmp = builder.BuildICmp(icmp.ICmpPredicate, trunc, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0));
            peephole.Add(trunc);
            peephole.Add(newCmp);

            return peephole;
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


        static HashSet<LLVMOpcode> opcodes = new();
        private PeepholeResult TrySimplifyInstruction(LLVMValueRef inst)
        {
            if (!inst.Is(LLVMValueKind.LLVMInstructionValueKind))
                return null;


            var simplified = ConstantFoldingAPI.TryConstantFold(inst);
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
