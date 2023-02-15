/*
using Dna.ControlFlow;
using Dna.ControlFlow.DataStructures;
using Dna.DataStructures;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    enum CondCode : uint
    {
        kO             = 0x00u,       //!<         OF==1
        kNO            = 0x01u,       //!<         OF==0
        kC             = 0x02u,       //!< CF==1
        kB             = 0x02u,       //!< CF==1          (unsigned < )
        kNAE           = 0x02u,       //!< CF==1          (unsigned < )
        kNC            = 0x03u,       //!< CF==0
        kAE            = 0x03u,       //!< CF==0          (unsigned >=)
        kNB            = 0x03u,       //!< CF==0          (unsigned >=)
        kE             = 0x04u,       //!<         ZF==1  (any_sign ==)
        kZ             = 0x04u,       //!<         ZF==1  (any_sign ==)
        kNE            = 0x05u,       //!<         ZF==0  (any_sign !=)
        kNZ            = 0x05u,       //!<         ZF==0  (any_sign !=)
        kBE            = 0x06u,       //!< CF==1 | ZF==1  (unsigned <=)
        kNA            = 0x06u,       //!< CF==1 | ZF==1  (unsigned <=)
        kA             = 0x07u,       //!< CF==0 & ZF==0  (unsigned > )
        kNBE           = 0x07u,       //!< CF==0 & ZF==0  (unsigned > )
        kS             = 0x08u,       //!<         SF==1  (is negative)
        kNS            = 0x09u,       //!<         SF==0  (is positive or zero)
        kP             = 0x0Au,       //!< PF==1
        kPE            = 0x0Au,       //!< PF==1
        kPO            = 0x0Bu,       //!< PF==0
        kNP            = 0x0Bu,       //!< PF==0
        kL             = 0x0Cu,       //!<         SF!=OF (signed < )
        kNGE           = 0x0Cu,       //!<         SF!=OF (signed < )
        kGE            = 0x0Du,       //!<         SF==OF (signed >=)
        kNL            = 0x0Du,       //!<         SF==OF (signed >=)
        kLE            = 0x0Eu,       //!< ZF==1 | SF!=OF (signed <=)
        kNG            = 0x0Eu,       //!< ZF==1 | SF!=OF (signed <=)
        kG             = 0x0Fu,       //!< ZF==0 & SF==OF (signed > )
        kNLE           = 0x0Fu,       //!< ZF==0 & SF==OF (signed > )

        kZero          = kZ,          //!< Zero flag.
        kNotZero       = kNZ,         //!< Non-zero flag.

        kSign          = kS,          //!< Sign flag.
        kNotSign       = kNS,         //!< No sign flag.

        kNegative      = kS,          //!< Sign flag.
        kPositive      = kNS,         //!< No sign flag.

        kOverflow      = kO,          //!< Overflow (signed).
        kNotOverflow   = kNO,         //!< Not overflow (signed).

        kEqual         = kE,          //!< `a == b` (equal).
        kNotEqual      = kNE,         //!< `a != b` (not equal).

        kSignedLT      = kL,          //!< `a <  b` (signed).
        kSignedLE      = kLE,         //!< `a <= b` (signed).
        kSignedGT      = kG,          //!< `a >  b` (signed).
        kSignedGE      = kGE,         //!< `a >= b` (signed).

        kUnsignedLT    = kB,          //!< `a <  b` (unsigned).
        kUnsignedLE    = kBE,         //!< `a <= b` (unsigned).
        kUnsignedGT    = kA,          //!< `a >  b` (unsigned).
        kUnsignedGE    = kAE,         //!< `a >= b` (unsigned).

        kParityEven    = kP,          //!< Even parity flag.
        kParityOdd     = kPO,         //!< Odd parity flag.
        };

    public class X86Lowerer
    {
        private readonly ICpuArchitecture architecture;

        private Assembler assembler = new Assembler(64);

        private Dictionary<BasicBlock<AbstractInst>, ManagedLabel> blockLabelMapping = new();

        private int stackHeight = 0;

        private X86Compiler compiler = new X86Compiler();

        private Dictionary<TemporaryOperand, ManagedGp> tempMapping = new();

        public X86Lowerer(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
        }

        public void CompileIrCfg(ControlFlowGraph<AbstractInst> cfg)
        {
            // Create a label for each block.
            foreach(var block in cfg.GetBlocks())
            {
                var label = compiler.CreateLabel();
                blockLabelMapping[block] = label;
            }

            // Compile each block
            foreach(var block in cfg.GetBlocks())
                CompileBlock(block);
        }

        private void CompileBlock(BasicBlock<AbstractInst> block)
        {
            // Update the assembler to the current block.
            var label = blockLabelMapping[block];
            compiler.Bind(label);
            foreach(var instruction in block.Instructions)
            {
                CompileInstruction(instruction);
            }
        }

        private void CompileInstruction(AbstractInst instruction)
        {
            switch (instruction)
            {
                case InstAdd inst:
                    break;
                default:
                    break;

            }
        }

        private void FromAdd(InstAdd inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.add(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromAnd(InstAnd inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.and_(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromAshr(InstAshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.sar(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromLshr(InstLshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.shr(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromMul(InstMul inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.mul(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromNeg(InstNeg inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = compiler.neg(op1);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromNot(InstNot inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = compiler.not_(op1);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromOr(InstOr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.or_(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromRol(InstRol inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            var result = compiler.rol(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromRor(InstRor inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            var result = compiler.ror(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromSdiv(InstSdiv inst)
        {
            // Note: The signed division implementation might be wrong here.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.idiv(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromCond(InstCond inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var temp = AllocateGp((int)inst.Dest.Bitsize);
            compiler.mov(temp, op1);
            CondCode? predicate = null;
            switch (inst.CondType)
            {
                case CondType.Eq:
                    predicate = CondCode.kNotEqual;
                    break;
                case CondType.Sge:
                    predicate = CondCode.kSignedGE;
                    break;
                case CondType.Sgt:
                    predicate = CondCode.kSignedGT;
                    break;
                case CondType.Sle:
                    predicate = CondCode.kSignedLE;
                    break;
                case CondType.Slt:
                    predicate = CondCode.kSignedLT;
                    break;
                case CondType.Uge:
                    predicate = CondCode.kUnsignedGE;
                    break;
                case CondType.Ugt:
                    predicate = CondCode.kUnsignedGT;
                    break;
                case CondType.Ule:
                    predicate = CondCode.kUnsignedLE;
                    break;
                case CondType.Ult:
                    predicate = CondCode.kUnsignedLT;
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Cond type {0} is invalid.", inst.CondType));
            }

            var result = compiler.cmov((byte)predicate.Value, op1, op2);
            StoreToOperand(inst.Dest, op1);
        }


        private void FromSmod(InstSmod inst)
        {
            // TODO: Validate.
            // This is WRONG.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            compiler.idiv(op1, op2);
            var srem = LoadSourceOperand(inst.Op2);
            var added = compiler.add(op2, srem);
            var result = LoadSourceOperand(inst.Op2);
            compiler.idiv(op2, result);
            StoreToOperand(inst.Dest, result);
        }

        private void FromSrem(InstSrem inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.idiv(op1, op2);
            StoreToOperand(inst.Dest, op2);
        }

        private void FromSub(InstSub inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.sub(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromUdiv(InstUdiv inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.div(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }


        private void FromUrem(InstUrem inst)
        {
            // This is WRONG.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.div(op1, op2);
            StoreToOperand(inst.Dest, op2);
        }

        private void FromXor(InstXor inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = compiler.xor_(op1, op2);
            StoreToOperand(inst.Dest, op1);
        }

        private void FromConcat(InstConcat inst)
        {
            throw new Exception("Concat is not supported.");
        }

        private void FromExtract(InstExtract inst)
        {
            var low = (ImmediateOperand)inst.Op2;
            var value = LoadSourceOperand(inst.Op3);
            var destGp = AllocateGp((int)inst.Bitsize);

            if (low.Value == 0)
            {
                var truncated = compiler.mov(destGp, value);
                StoreToOperand(inst.Dest, destGp);
                return;
            }

            var lowOp = LoadSourceOperand(low);
            var shifted = compiler.shr(value, lowOp);


            var destGp2 = AllocateGp((int)inst.Bitsize);
            compiler.mov(destGp2, value);
            StoreToOperand(inst.Dest, destGp2);
        }

        private void FromSelect(InstSelect inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var op3 = LoadSourceOperand(inst.Op3);

            var destOp = AllocateGp((int)inst.Bitsize);
            compiler.mov(destOp, op2);

            // TODO: Sign extend all zero bit size ops to
            var imm = AllocateGp(8);
            compiler.mov(imm, new ManagedImm(1));
            compiler.cmp(imm, op1);
            compiler.cmov((byte)CondCode.kNotEqual, destOp, op3);

            StoreToOperand(inst.Dest, destOp);
        }

        private void FromSx(InstSx inst)
        {
            throw new Exception();
        }

        private void FromZx(InstZx inst)
        {
            var destType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var input = LoadSourceOperand(inst.InputOperand);
            var result = compiler.ZExt(input, destType, "zx");
            StoreToOperand(inst.Dest, op1);
        }

        private void FromCopy(InstCopy inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);

            var pointer = compiler.Alloca(LLVMTypeRef.CreateInt(inst.Bitsize), "copy");

            compiler.Store(op1, pointer);

            var load = compiler.Load(pointer, "copy");

            StoreToOperand(inst.Dest, load);
        }

        private void FromLoad(InstLoad inst)
        {
            // Load the source address.
            var op1 = LoadSourceOperand(inst.Op1);

            // Cast the address to a pointer.
            var ptrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateInt(inst.Bitsize), 0);
            var pointer = compiler.IntToPtr(op1, ptrType, "load");

            // Dereference the pointer.
            var value = compiler.Load(pointer, "load");

            StoreToOperand(inst.Dest, value);
        }

        private void FromJmp(InstJmp inst)
        {
            // Load the source address.
            var thenAddr = inst.Op1 as ImmediateOperand;
            var thenBlock = blockMapping.Single(x => x.Key.Address == thenAddr.Value).Value;

            compiler.Br(thenBlock);
        }

        private void FromJcc(InstJcc inst)
        {
            // Load the source address.
            var cond = LoadSourceOperand(inst.Op1);

            // Load the source address.
            var thenAddr = inst.Op2 as ImmediateOperand;
            var thenBlock = blockMapping.Single(x => x.Key.Address == thenAddr.Value).Value;

            var elseAddr = inst.Op3 as ImmediateOperand;
            var elseBlock = blockMapping.Single(x => x.Key.Address == elseAddr.Value).Value;

            compiler.CondBr(cond, thenBlock, elseBlock);
        }

        private void FromRet(InstRet inst)
        {
            compiler.RetVoid(builder);
        }

        private ManagedGp LoadSourceOperand(IOperand operand)
        {
            if (operand is ImmediateOperand immOperand)
            {
                var imm = new ManagedImm(immOperand.Value);

                var gp = AllocateGp((int)operand.Bitsize);

                compiler.mov(gp, imm);

                return gp;
            }

            else if (operand is RegisterOperand regOperand)
            {
                var items = Enum.GetValues(typeof(RegId)).Cast<RegId>();

                var target = items.Single(x => x.ToString().ToLower() == regOperand.Name.ToLower());

                var reg = compiler.GetRegister(target);

                var newGp = AllocateGp((int)regOperand.Bitsize);

                compiler.mov(newGp, reg);

                return newGp;
            }

            else if (operand is TemporaryOperand tempOperand)
            {
                var temp = GetTemporary(tempOperand);

                var newTemp = AllocateGp((int)tempOperand.Bitsize);

                compiler.mov(newTemp, temp);

                return newTemp;
            }

            else
            {
                throw new InvalidOperationException(String.Format("Cannot load source operand: {0}", operand.ToString()));
            }
        }

        private void StoreToOperand(IOperand operand, ManagedGp result)
        {
            if (operand is RegisterOperand regOperand)
            {
                var items = Enum.GetValues(typeof(RegId)).Cast<RegId>();

                var target = items.Single(x => x.ToString().ToLower() == regOperand.Name.ToLower());

                var destReg = compiler.GetRegister(target);

                compiler.mov(destReg, result);
            }

            else if (operand is TemporaryOperand tempOperand)
            {
                var temp = GetTemporary(tempOperand);
                compiler.mov(temp, result);
            }

            else
            {
                throw new InvalidOperationException(String.Format("Cannot store to operand: {0}", operand.ToString()));
            }
        }

        private ManagedGp AllocateGp(int bitSize)
        {
            return null;
        }

        private ManagedGp GetTemporary(TemporaryOperand operand)
        {
            if(tempMapping.ContainsKey(operand))
                return tempMapping[operand];

            var temp = AllocateGp((int)operand.Bitsize);
            tempMapping[operand] = temp;
            return temp;
        }
    }
}
*/