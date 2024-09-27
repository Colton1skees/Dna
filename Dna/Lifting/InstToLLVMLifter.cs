using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using static LLVMSharp.CmpInst;

namespace Dna.Lifting
{
    public class InstToLLVMLifter
    {
        private const string opName = "op";

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly Func<LLVMValueRef> getLocalMemVariable;

        private readonly Func<IOperand, LLVMValueRef> load;

        private readonly Action<IOperand, LLVMValueRef> storeToOperand;

        public InstToLLVMLifter(LLVMModuleRef module, LLVMBuilderRef builder, Func<LLVMValueRef> getLocalMemVariable, Func<IOperand, LLVMValueRef> load, Action<IOperand, LLVMValueRef> storeToOperand)
        {
            this.module = module;
            this.builder = builder;
            this.getLocalMemVariable = getLocalMemVariable;
            this.load = load;
            this.storeToOperand = storeToOperand;
        }

        public void LiftInstructionToLLVM(AbstractInst instruction, Func<ulong, LLVMBasicBlockRef> getBlockByAddress)
        {
            LLVMValueRef? one = null;
            LLVMValueRef? two = null;
            LLVMValueRef? three = null;

            var op1 = () => {
                one ??= load(instruction.Op1);
                return one.Value;
            };

            var op2 = () => 
            {
                two ??= load(instruction.Op2);
                return two.Value;
            };

            var op3 = () => {
                three ??= load(instruction.Op3);
                return three.Value;
            };
            var store = (LLVMValueRef value) => { storeToOperand(instruction.Dest, value); };

            switch (instruction)
            {
                case InstAdd inst:
                    var add = builder.BuildAdd(op1(), op2(), "add");
                    store(add);
                    break;
                case InstAnd inst:
                    var and = builder.BuildAnd(op1(), op2(), "and");
                    store(and);
                    break;
                case InstAshr inst:
                    var ashr = builder.BuildAShr(op1(), op2(), "ashr");
                    store(ashr);
                    break;
                case InstLshr inst:
                    var lshr = builder.BuildLShr(op1(), op2(), "lshr");
                    store(lshr);
                    break;
                case InstMul inst:
                    var mul = builder.BuildMul(op1(), op2(), "mul");
                    store(mul);
                    break;
                case InstNeg inst:
                    var neg = builder.BuildNeg(op1(), "neg");
                    store(neg);
                    break;
                case InstNot inst:
                    var not = builder.BuildNot(op1(), "not");
                    store(not);
                    break;
                case InstOr inst:
                    var or = builder.BuildOr(op1(), op2(), "or");
                    store(or);
                    break;
                // TODO: Fshl/fshr are not semantically correct.
                case InstRol inst:
                    // Create the fshl intrinsic.
                    var rolType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var rolFnType = LLVMTypeRef.CreateFunction(rolType, new LLVMTypeRef[] { rolType, rolType, rolType }, false);
                    var rolIntrinsicFunc = module.AddFunction("llvm.fshl.i" + inst.Bitsize, rolFnType);

                    // Execute and store the result.
                    var rol = builder.BuildCall2(rolFnType, rolIntrinsicFunc, new LLVMValueRef[] { op1(), op1(), op2() }, "rol");
                    store(rol);
                    break;
                case InstRor inst:
                    // Create the intrinsic.
                    var rorType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var rorFnType = LLVMTypeRef.CreateFunction(rorType, new LLVMTypeRef[] { rorType, rorType, rorType }, false);
                    var rorIntrinsicFunc = module.AddFunction("llvm.fshr.i" + inst.Bitsize, rorFnType);

                    // Invoke the intrinsic and store the result.
                    var ror = builder.BuildCall2(rorFnType, rorIntrinsicFunc, new LLVMValueRef[] { op1(), op1(), op2() }, "ror");
                    store(ror);
                    break;
                case InstSdiv inst:
                    var sdiv = builder.BuildSDiv(op1(), op2(), "sdiv");
                    store(sdiv);
                    break;
                case InstShl inst:
                    var shl = builder.BuildShl(op1(), op2(), "shl");
                    store(shl);
                    break;
                case InstCond inst:
                    var predicateType = GetLlvmPredicateType(inst.CondType);
                    var icmp = builder.BuildICmp(predicateType, op1(), op2(), "cond");
                    store(icmp);
                    break;
                case InstSmod inst:
                    var srem = builder.BuildSRem(op1(), op2(), opName);
                    var added = builder.BuildAdd(srem, op2(), opName);
                    var smod = builder.BuildSRem(added, op2(), "smod");
                    store(smod);
                    break;
                case InstSrem inst:
                    var srem_ = builder.BuildSRem(op1(), op2(), "srem");
                    store(srem_);
                    break;
                case InstSub inst:
                    var sub = builder.BuildSub(op1(), op2(), "sub");
                    store(sub);
                    break;
                case InstUdiv inst:
                    var udiv = builder.BuildUDiv(op1(), op2(), "udiv");
                    store(udiv);
                    break;
                case InstUrem inst:
                    var urem = builder.BuildURem(op1(), op2(), "urem");
                    store(urem);
                    break;
                case InstXor inst:
                    var xor = builder.BuildXor(op1(), op2(), "xor");
                    store(xor);
                    break;
                case InstConcat inst:
                    var concatType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var concat = builder.BuildZExt(op1(), concatType, "concat");

                    foreach (var operand in inst.Operands.Skip(1))
                    {
                        var shiftN = LLVMValueRef.CreateConstInt(concatType, operand.Bitsize, false);
                        concat = builder.BuildShl(concat, shiftN, "concat");
                        var extended = builder.BuildZExt(load(operand), concatType, opName);
                        concat = builder.BuildOr(concat, extended, "concat");
                    }

                    store(concat);
                    break;
                case InstExtract inst:
                    var low = (ImmediateOperand)inst.Op2;
                    var value = op3();
                    var intType = LLVMTypeRef.CreateInt(inst.Bitsize);

                    if (low.Value == 0)
                    {
                        var truncated = builder.BuildTrunc(value, intType, opName);
                        store(truncated);
                        break;
                    }

                    var shifted = builder.BuildLShr(value, load(low), opName);
                    var result = builder.BuildTrunc(shifted, intType, "extract");
                    store(result);
                    break;
                case InstSelect inst:
                    var select = builder.BuildSelect(op1(), op2(), op3(), "select");
                    store(select);
                    break;
                case InstSx inst:
                    var sxType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var sx = builder.BuildSExt(load(inst.InputOperand), sxType, "sx");
                    store(sx);
                    break;
                case InstZx inst:
                    var zxType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var zx = builder.BuildZExt(load(inst.InputOperand), zxType, "zx");
                    store(zx);
                    break;
                case InstCopy inst:
                    op1();
                    var valType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    var pointer = builder.BuildAlloca(valType, "copy");
                    builder.BuildStore(op1(), pointer);
                    var loaded = builder.BuildLoad2(valType, pointer, "copy");
                    store(loaded);
                    break;
                case InstLoad inst:
                    // Cast the address to a pointer.
                    //var loadValType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    //var ptrType = LLVMTypeRef.CreatePointer(loadValType, 0);
                    //var loadPointer = builder.BuildInBoundsGEP2(ptrType, op1(), new LLVMValueRef[] {});
                    //var loadPointer = builder.BuildIntToPtr(op1(), ptrType, "loadPtr");

                    var loadValType = LLVMTypeRef.CreateInt(inst.Bitsize);
                    //builder.BuildInBoundsGEP2(,)
                    var loadPointer = builder.BuildInBoundsGEP2(LLVMTypeRef.CreateInt(8), getLocalMemVariable(), new LLVMValueRef[] { op1()});
                    loadPointer = builder.BuildBitCast(loadPointer, LLVMTypeRef.CreatePointer(loadValType, 0));
                    // Dereference the pointer.

                    var loadValue = builder.BuildLoad2(loadValType, loadPointer, "load");
                    store(loadValue);
                    break;
                case InstStore inst:
                    // Get the address to store to.
                    var storeAddr = load(inst.Dest);

                    // Get the value being stored.
                    var storeValue = load(inst.Op1);

                    /*
                    // Cast the destination address to a pointer of the source value width.
                    var storeIntType = LLVMTypeRef.CreateInt(inst.Op1.Bitsize);
                    var storePtrType = LLVMTypeRef.CreatePointer(storeIntType, 0);
                    var storePtr = builder.BuildIntToPtr(storeAddr, storePtrType, "storePtr");
                    */

                    var storeIntType = LLVMTypeRef.CreateInt(inst.Op1.Bitsize);
                    var storePointer = builder.BuildInBoundsGEP2(LLVMTypeRef.CreateInt(8), getLocalMemVariable(), new LLVMValueRef[] { storeAddr });

                    // Write to memory.
                    storePointer = builder.BuildBitCast(storePointer, LLVMTypeRef.CreatePointer(storeIntType, 0));
                    builder.BuildStore(storeValue, storePointer);
                    break;
                case InstJmp inst:
                    var jmpAddr = inst.Op1 as ImmediateOperand;
                    var jmpBlock = getBlockByAddress(jmpAddr.Value);
                    builder.BuildBr(jmpBlock);
                    break;
                case InstJcc inst:
                    var jmpCond = op1();
                    var jccThenBlock = getBlockByAddress(inst.ThenOp.Value);
                    var jccElseBlock = getBlockByAddress(inst.ElseOp.Value);
                    builder.BuildCondBr(jmpCond, jccThenBlock, jccElseBlock);
                    break;
                case InstRet inst:
                    builder.BuildRetVoid();
                    break;
                default:
                    throw new InvalidOperationException($"Cannot lift instruction {instruction} to llvm.");
            }

        }

        private static LLVMIntPredicate GetLlvmPredicateType(CondType type)
        {
            switch (type)
            {
                case CondType.Eq:
                    return LLVMIntPredicate.LLVMIntEQ;
                case CondType.Sge:
                    return LLVMIntPredicate.LLVMIntSGE;
                case CondType.Sgt:
                    return LLVMIntPredicate.LLVMIntSGT;
                case CondType.Sle:
                    return LLVMIntPredicate.LLVMIntSLE;
                case CondType.Slt:
                    return LLVMIntPredicate.LLVMIntSLT;
                case CondType.Uge:
                    return LLVMIntPredicate.LLVMIntUGE;
                case CondType.Ugt:
                    return LLVMIntPredicate.LLVMIntUGT;
                case CondType.Ule:
                    return LLVMIntPredicate.LLVMIntULE;
                case CondType.Ult:
                    return LLVMIntPredicate.LLVMIntULT;
                default:
                    throw new InvalidOperationException(String.Format("Cond type {0} is invalid.", type));
            }
        }
    }
}
