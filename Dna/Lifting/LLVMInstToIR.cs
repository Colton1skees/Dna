using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    public class LLVMInstToIR
    {
        private readonly LLVMModuleRef module;

        private readonly ICpuArchitecture architecture;

        private readonly Dictionary<LLVMValueRef, IOperand> llvmOperandMapping = new Dictionary<LLVMValueRef, IOperand>();

        public LLVMInstToIR(LLVMModuleRef module, ICpuArchitecture architecture)
        {
            this.module = module;
            this.architecture = architecture;
        }

        public IEnumerable<AbstractInst> LowerInstruction(LLVMValueRef inst)
        {
            var output = new List<AbstractInst>();
            var emit = (AbstractInst instruction) =>
            {
                output.Add(instruction);
            };

            var op1 = () =>
            {
                return GetOperand(inst.GetOperand(0));
            };

            var op2 = () =>
            {
                return GetOperand(inst.GetOperand(1));
            };

            var op3 = () =>
            {
                return GetOperand(inst.GetOperand(3));
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
                    emit(new InstCopy(dest(), op2()));
                    break;
            }

            return output;
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
            return null;
        }

        private IOperand GetOperand(LLVMValueRef operand)
        {
            if(operand.InstructionOpcode == LLVMOpcode.LLVMLoad)
            {

            }
            return llvmOperandMapping[operand];
        }
    }
}
