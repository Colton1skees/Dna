using Dna.Extensions;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    public class LLVMInstToIR
    {
        private readonly LLVMModuleRef module;

        private readonly ICpuArchitecture architecture;

        private readonly Dictionary<LLVMValueRef, IOperand> llvmOperandMapping = new();

        private readonly Dictionary<LLVMValueRef, RegisterOperand> registerMapping = new();

        private readonly LLVMValueRef memoryPtr;

        public List<AbstractInst> Output = new List<AbstractInst>();

        public LLVMInstToIR(LLVMModuleRef module, ICpuArchitecture architecture)
        {
            this.module = module;
            this.architecture = architecture;
            foreach(var global in module.GetGlobals())
            {
                // Store the memory ptr if found.
                var name = global.Name;
                if(name == "memory")
                {
                    memoryPtr = global;
                    continue;
                }

                // Skip if the global is not a memory ptr or a known register.
                bool success = X86Registers.RegisterNameMapping.TryGetValue(name, out Register reg);
                if (!success)
                    continue;

                // Store the mapping between <global, register> if the global corresponds to a register.
                registerMapping.Add(global, new RegisterOperand(reg));
            }
        }

        public IEnumerable<AbstractInst> LowerInstruction(LLVMValueRef inst)
        {
            var emit = (AbstractInst instruction) =>
            {
                Output.Add(instruction);
            };

            var op1 = () =>
            {
                var val = GetOperand(inst.GetOperand(0));
                return val;
            };

            var op2 = () =>
            {
                var val = GetOperand(inst.GetOperand(1));
                return val;
            };

            var op3 = () =>
            {
                var val = GetOperand(inst.GetOperand(2));
                return val;
            };

            var dest = () =>
            {
                return CreateOperand(inst);
            };

            switch (inst.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                    emit(new InstAdd(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAnd:
                    emit(new InstAnd(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAShr:
                    emit(new InstAshr(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMLShr:
                    emit(new InstLshr(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMMul:
                    emit(new InstMul(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMOr:
                    emit(new InstOr(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSDiv:
                    emit(new InstSdiv(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMShl:
                    emit(new InstShl(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMICmp:
                    // Note: Since SMTLIB2 does not have an `NEQ` type node,
                    // we need to utilize an EQ node & swap the order of the ite input nodes.
                    if (inst.ICmpPredicate == LLVMIntPredicate.LLVMIntNE)
                    {
                        emit(new InstCond(CondType.Eq, dest(), op2(), op1()));
                        break;
                    }
                    var predicate = GetCondType(inst.ICmpPredicate);
                    emit(new InstCond(predicate, dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSRem:
                    emit(new InstAshr(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSub:
                    emit(new InstSub(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMUDiv:
                    emit(new InstUdiv(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMURem:
                    emit(new InstUrem(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMXor:
                    emit(new InstXor(dest(), op1(), op2()));
                    break;
                case LLVMOpcode.LLVMTrunc:
                    var destTy = inst.TypeOf;
                    emit(new InstExtract(dest(), (uint)destTy.IntWidth - 1u, 0, op1()));
                    break;
                case LLVMOpcode.LLVMSelect:
                    emit(new InstSelect(dest(), op1(), op2(), op3()));
                    break;
                case LLVMOpcode.LLVMSExt:
                    var sxTy = inst.TypeOf;
                    var srcTy = inst.GetOperand(0).TypeOf;
                    emit(new InstSx(dest(), new ImmediateOperand(sxTy.IntWidth - srcTy.IntWidth, sxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMZExt:
                    var zxTy = inst.TypeOf;
                    var srcZxTy = inst.GetOperand(0).TypeOf;
                    emit(new InstZx(dest(), new ImmediateOperand(zxTy.IntWidth - srcZxTy.IntWidth, zxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMGetElementPtr:
                    var d = dest();
                    var input = op2();
                    emit(new InstCopy(d, input));
                    break;
                case LLVMOpcode.LLVMLoad:
                    // Get the load address operand.
                    var loadSource = inst.GetOperand(0);

                    // If the load operand is a ptr to the global memory variable, then we do nothing.
                    // This is because the %memory pointer is only used as a sortof identifier into GEP, so we don't care.
                    if(loadSource == memoryPtr)
                    {
                        break;
                    }

                    // If we are loading from a register, then we simply want to copy the register to the destination variable.
                    else if(registerMapping.TryGetValue(loadSource, out RegisterOperand regOp))
                    {
                        emit(new InstCopy(dest(), regOp));
                        break;
                    }

                    // Otherwise, this is a legitimate load from an unknown memory address.
                    else
                    {
                        var loadAddr = op1();
                        var loadWidth = inst.TypeOf.IntWidth;
                        emit(new InstLoad(dest(), loadAddr, new ImmediateOperand(loadWidth, 64)));
                        break;
                    }

                case LLVMOpcode.LLVMStore:
                    var storeValue = inst.GetOperand(0);
                    var storePtr = inst.GetOperand(1);

                    // If we are storing to a register, then model this as a 'reg = copy {storevalue}`.
                    if(registerMapping.TryGetValue(storePtr, out RegisterOperand regPtr))
                    {
                        emit(new InstCopy(regPtr, op1()));
                        break;
                    }

                    // Otherwise this is a legitimate store to memory.
                    var storeDest = GetOperand(storePtr);
                    var storeIrValue = GetOperand(storeValue);
                    emit(new InstStore(storeDest, storeIrValue));
                    break;
                case LLVMOpcode.LLVMRet:
                    emit(new InstRet());
                    break;
                default:
                    throw new InvalidOperationException($"Cannot lower inst: {inst}");
            }

            return Output;
        }

        private CondType GetCondType(LLVMIntPredicate type)
        {
            return type switch
            {
                LLVMIntPredicate.LLVMIntEQ => CondType.Eq,
                LLVMIntPredicate.LLVMIntSGE => CondType.Sge,
                LLVMIntPredicate.LLVMIntSGT => CondType.Sgt,
                LLVMIntPredicate.LLVMIntSLE => CondType.Sle,
                LLVMIntPredicate.LLVMIntSLT => CondType.Slt,
                LLVMIntPredicate.LLVMIntUGE => CondType.Uge,
                LLVMIntPredicate.LLVMIntUGT => CondType.Ugt,
                LLVMIntPredicate.LLVMIntULE => CondType.Ule,
                LLVMIntPredicate.LLVMIntULT => CondType.Ult,
                _ => throw new InvalidOperationException($"Cond type {type} cannot be converted to smtlib cond.")
            };
        }

        private IOperand CreateOperand(LLVMValueRef destination)
        {
            var width = destination.TypeOf.IntWidth;
            if (destination.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                width = 64;

            var temporary = new TemporaryOperand(architecture.GetUniqueTemporaryId(), width);
            llvmOperandMapping[destination] = temporary;
            return temporary;
        }

        private IOperand GetOperand(LLVMValueRef operand)
        {
            if (operand.InstructionOpcode == LLVMOpcode.LLVMLoad)
            {
                var llvmOperand = operand.GetOperand(0);
                bool isReg = registerMapping.TryGetValue(llvmOperand, out RegisterOperand regOp);
                if (isReg)
                    return regOp;
                
            }

            if(operand.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                var width = operand.TypeOf.IntWidth;
                var valueStr = operand.ToString().Split(' ').Last();
                return new ImmediateOperand((ulong)Convert.ToInt64(valueStr), width);
            }


            var idk = operand.IsAConstantInt;

            var ty = operand.TypeOf;
            //var idk = operand.OperandCount;
            return llvmOperandMapping[operand];
        }
    }
}
