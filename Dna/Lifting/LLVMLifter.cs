using Dna.ControlFlow;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    public class LLVMLifter
    {
        private readonly string opName = "op";

        private readonly ICpuArchitecture architecture;

        LLVMModuleRef module = LLVM.ModuleCreateWithName("TritonTranslator");

        LLVMBuilderRef builder = LLVM.CreateBuilder();

        private Dictionary<register_e, LLVMValueRef> registerGlobals = new Dictionary<register_e, LLVMValueRef>();

        private Dictionary<TemporaryOperand, LLVMValueRef> temporaryVariables = new Dictionary<TemporaryOperand, LLVMValueRef>();

        private Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef> blockMapping = new Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef>();

        public LLVMLifter(ICpuArchitecture architecture)
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            if (LLVM.CreateExecutionEngineForModule(out var engine, module, out var errorMessage).Value == 1)
            {
                throw new Exception(errorMessage);
            }

            var parentRegs = X86Registers.RegisterMapping.Values.Where(x => x.ParentId == x.Id);
            foreach (var parentReg in parentRegs)
            {
                Console.WriteLine(parentReg.Name);
                var ptrType = (LLVM.IntType(parentReg.BitSize));
                var global = LLVM.AddGlobal(module, ptrType, parentReg.Name);
                global.SetLinkage(LLVMLinkage.LLVMCommonLinkage);
                //var ptrNull = LLVM.ConstPointerNull(LLVM.PointerType(ptrType, 0));
                var ptrNull = LLVM.ConstInt(ptrType, 0, new LLVMBool(0));
                global.SetInitializer(ptrNull);
                registerGlobals.Add(parentReg.Id, global);
            }

            this.architecture = architecture;
        }

        public void Lift(ControlFlowGraph<AbstractInst> irCfg)
        {
            var function = LLVM.AddFunction(
                module,
                "SampleFunc",
                LLVM.FunctionType(LLVM.VoidType(),
                new LLVMTypeRef[] { },
                false));

            LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);

            var entryBlock = LLVM.AppendBasicBlock(function, "entry");
            LLVM.PositionBuilderAtEnd(builder, entryBlock);
            var irBlocks = irCfg.GetBlocks();
            blockMapping.Add(irBlocks.First(), entryBlock);
            foreach (var block in irBlocks.Skip(1))
            {
                var llvmBlock = LLVM.AppendBasicBlock(function, block.Name);
                blockMapping.Add(block, llvmBlock);
            }

            foreach (var block in irBlocks)
            {
                LLVM.PositionBuilderAtEnd(builder, blockMapping[block]);
                foreach(var inst in block.Instructions)
                {
                    CompileInstruction(inst);
                }
            }
        }

        private void CompileInstruction(AbstractInst instruction)
        {
            switch (instruction)
            {
                case InstAdd inst:
                    FromAdd(inst);
                    break;
                case InstAnd inst:
                    FromAnd(inst);
                    break;
                case InstAshr inst:
                    FromAshr(inst);
                    break;
                case InstLshr inst:
                    FromLshr(inst);
                    break;
                case InstMul inst:
                    FromMul(inst);
                    break;
                case InstNeg inst:
                    FromNeg(inst);
                    break;
                case InstNot inst:
                    FromNot(inst);
                    break;
                case InstOr inst:
                    FromOr(inst);
                    break;
                case InstRol inst:
                    FromRol(inst);
                    break;
                case InstRor inst:
                    FromRor(inst);
                    break;
                case InstSdiv inst:
                    FromSdiv(inst);
                    break;
                case InstCond inst:
                    FromCond(inst);
                    break;
                case InstSmod inst:
                    FromSmod(inst);
                    break;
                case InstSrem inst:
                    FromSrem(inst);
                    break;
                case InstSub inst:
                    FromSub(inst);
                    break;
                case InstUdiv inst:
                    FromUdiv(inst);
                    break;
                case InstUrem inst:
                    FromUrem(inst);
                    break;
                case InstXor inst:
                    FromXor(inst);
                    break;
                case InstConcat inst:
                    FromConcat(inst);
                    break;
                case InstExtract inst:
                    FromExtract(inst);
                    break;
                case InstSelect inst:
                    FromSelect(inst);
                    break;
                case InstSx inst:
                    FromSx(inst);
                    break;
                case InstZx inst:
                    FromZx(inst);
                    break;
                case InstCopy inst:
                    FromCopy(inst);
                    break;
                case InstLoad inst:
                    FromLoad(inst);
                    break;
                case InstJmp inst:
                    FromJmp(inst);
                    break;
                case InstJcc inst:
                    FromJcc(inst);
                    break;
                case InstRet inst:
                    FromRet(inst);
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Node type {0} is not supported.", instruction.Id));
            }
        }

        private void FromAdd(InstAdd inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildAdd(builder, op1, op2, "add");
            StoreToOperand(inst.Dest, result);
        }

        private void FromAnd(InstAnd inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildAnd(builder, op1, op2, "and");
            StoreToOperand(inst.Dest, result);
        }

        private void FromAshr(InstAshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildAShr(builder, op1, op2, "ashr");
            StoreToOperand(inst.Dest, result);
        }

        private void FromLshr(InstLshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildLShr(builder, op1, op2, "lshr");
            StoreToOperand(inst.Dest, result);
        }

        private void FromMul(InstMul inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildMul(builder, op1, op2, "mul");
            StoreToOperand(inst.Dest, result);
        }

        private void FromNeg(InstNeg inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = LLVM.BuildNeg(builder, op1, "neg");
            StoreToOperand(inst.Dest, result);
        }

        private void FromNot(InstNot inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = LLVM.BuildNot(builder, op1, "not");
            StoreToOperand(inst.Dest, result);
        }

        private void FromOr(InstOr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildOr(builder, op1, op2, "or");
            StoreToOperand(inst.Dest, result);
        }

        private void FromRol(InstRol inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            // Create the intrinsic.
            var intType = LLVM.IntType(inst.Bitsize);
            var fnType = LLVM.FunctionType(intType, new LLVMTypeRef[] { intType, intType, intType }, false);
            var intrinsicFunc = LLVM.AddFunction(module, "llvm.fshl.i" + inst.Bitsize, fnType);

            var result = LLVM.BuildCall(builder, intrinsicFunc, new LLVMValueRef[] { op1, op1, op2 }, "rol");
            StoreToOperand(inst.Dest, result);
        }

        private void FromRor(InstRor inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            // Create the intrinsic.
            var intType = LLVM.IntType(inst.Bitsize);
            var fnType = LLVM.FunctionType(intType, new LLVMTypeRef[] { intType, intType, intType }, false);
            var intrinsicFunc = LLVM.AddFunction(module, "llvm.fshr.i" + inst.Bitsize, fnType);

            // Invoke the intrinsic and store the result.
            var result = LLVM.BuildCall(builder, intrinsicFunc, new LLVMValueRef[] { op1, op1, op2 }, "ror");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSdiv(InstSdiv inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildSDiv(builder, op1, op2, "sdiv");
            StoreToOperand(inst.Dest, result);
        }

        private void FromCond(InstCond inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            LLVMIntPredicate? predicate = null;
            switch (inst.CondType)
            {
                case CondType.Eq:
                    predicate = LLVMIntPredicate.LLVMIntEQ;
                    break;
                case CondType.Sge:
                    predicate = LLVMIntPredicate.LLVMIntSGE;
                    break;
                case CondType.Sgt:
                    predicate = LLVMIntPredicate.LLVMIntSGT;
                    break;
                case CondType.Sle:
                    predicate = LLVMIntPredicate.LLVMIntSLE;
                    break;
                case CondType.Slt:
                    predicate = LLVMIntPredicate.LLVMIntSLT;
                    break;
                case CondType.Uge:
                    predicate = LLVMIntPredicate.LLVMIntUGE;
                    break;
                case CondType.Ugt:
                    predicate = LLVMIntPredicate.LLVMIntUGT;
                    break;
                case CondType.Ule:
                    predicate = LLVMIntPredicate.LLVMIntULE;
                    break;
                case CondType.Ult:
                    predicate = LLVMIntPredicate.LLVMIntULT;
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Cond type {0} is invalid.", inst.CondType));
            }

            var result = LLVM.BuildICmp(builder, predicate.Value, op1, op2, "cond");
            StoreToOperand(inst.Dest, result);
        }


        private void FromSmod(InstSmod inst)
        {
            // TODO: Validate.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var srem = LLVM.BuildSRem(builder, op1, op2, opName);
            var added = LLVM.BuildAdd(builder, srem, op2, opName);
            var result = LLVM.BuildSRem(builder, added, op2, "smod");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSrem(InstSrem inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildSRem(builder, op1, op2, "srem");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSub(InstSub inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildSub(builder, op1, op2, "sub");
            StoreToOperand(inst.Dest, result);
        }

        private void FromUdiv(InstUdiv inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildUDiv(builder, op1, op2, "udiv");
            StoreToOperand(inst.Dest, result);
        }


        private void FromUrem(InstUrem inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildURem(builder, op1, op2, "urem");
            StoreToOperand(inst.Dest, result);
        }

        private void FromXor(InstXor inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = LLVM.BuildXor(builder, op1, op2, "xor");
            StoreToOperand(inst.Dest, result);
        }

        private void FromConcat(InstConcat inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var intType = LLVM.IntType(inst.Bitsize);
            var result = LLVM.BuildZExt(builder, op1, intType, "concat");

            foreach (var operand in inst.Operands.Skip(1))
            {
                var shiftN = LLVM.ConstInt(intType, operand.Bitsize, new LLVMBool(0));
                result = LLVM.BuildShl(builder, result, shiftN, "concat");
                var extended = LLVM.BuildZExt(builder, LoadSourceOperand(operand), intType, opName);
                result = LLVM.BuildOr(builder, result, extended, "concat");
            }

            StoreToOperand(inst.Dest, result);
        }

        private void FromExtract(InstExtract inst)
        {
            var low = (ImmediateOperand)inst.Op2;
            var value = LoadSourceOperand(inst.Op3);
            var intType = LLVM.IntType(inst.Bitsize);

            if (low.Value == 0)
            {
                var truncated = LLVM.BuildTrunc(builder, value, intType, opName);
                StoreToOperand(inst.Dest, truncated);
                return;
            }

            var shifted = LLVM.BuildLShr(builder, value, LoadSourceOperand(low), opName);
            var result = LLVM.BuildTrunc(builder, shifted, intType, "extract");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSelect(InstSelect inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var op3 = LoadSourceOperand(inst.Op3);
            var result = LLVM.BuildSelect(builder, op1, op2, op3, "select");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSx(InstSx inst)
        {
            var destType = LLVM.IntType(inst.Bitsize);
            var input = LoadSourceOperand(inst.InputOperand);
            var result = LLVM.BuildSExt(builder, input, destType, "sx");
            StoreToOperand(inst.Dest, result);
        }

        private void FromZx(InstZx inst)
        {
            var destType = LLVM.IntType(inst.Bitsize);
            var input = LoadSourceOperand(inst.InputOperand);
            var result = LLVM.BuildZExt(builder, input, destType, "zx");
            StoreToOperand(inst.Dest, result);
        }

        private void FromCopy(InstCopy inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);

            var pointer = LLVM.BuildAlloca(builder, LLVM.IntType(inst.Bitsize), "copy");

            LLVM.BuildStore(builder, op1, pointer);

            var load = LLVM.BuildLoad(builder, pointer, "copy");

            StoreToOperand(inst.Dest, load);
        }

        private void FromLoad(InstLoad inst)
        {
            // Load the source address.
            var op1 = LoadSourceOperand(inst.Op1);

            // Cast the address to a pointer.
            var ptrType = LLVM.PointerType(LLVM.IntType(inst.Bitsize), 0);
            var pointer = LLVM.BuildIntToPtr(builder, op1, ptrType, "load");

            // Dereference the pointer.
            var value = LLVM.BuildLoad(builder, pointer, "load");

            StoreToOperand(inst.Dest, value);
        }

        private void FromJmp(InstJmp inst)
        {
            // Load the source address.
            var thenAddr = inst.Op1 as ImmediateOperand;
            var thenBlock = blockMapping.Single(x => x.Key.Address == thenAddr.Value).Value;

            LLVM.BuildBr(builder, thenBlock);
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

            LLVM.BuildCondBr(builder, cond, thenBlock, elseBlock);
        }

        private void FromRet(InstRet inst)
        {
            LLVM.BuildRetVoid(builder);
        }

        private void StoreToOperand(IOperand operand, LLVMValueRef result)
        {
            if (operand is SsaOperand ssaOP)
                operand = ssaOP.BaseOperand;

            if (operand is RegisterOperand regOperand)
            {
                // Fix up register sizing. TODO: Remove.
                var root = architecture.GetRootParentRegister(regOperand.Register.Id);
                var rootPointer = registerGlobals[root.Id];
                if (regOperand.Register.Id != root.Id)
                {
                    var destType = LLVM.PointerType(LLVM.IntType(regOperand.Bitsize), 0);
                    rootPointer = LLVM.BuildPointerCast(builder, rootPointer, destType, opName);
                }

                LLVM.BuildStore(builder, result, rootPointer);
            }

            else if (operand is TemporaryOperand tempOperand)
            {
                temporaryVariables[tempOperand] = result;
            }

            else
            {
                throw new InvalidOperationException(String.Format("Cannot store to operand: {0}", operand.ToString()));
            }
        }

        private LLVMValueRef LoadSourceOperand(IOperand operand)
        {
            if (operand is SsaOperand ssaOP)
                operand = ssaOP.BaseOperand;

            if (operand is ImmediateOperand immOperand)
            {
                var intType = LLVM.IntType(operand.Bitsize);

                var alloca = LLVM.BuildAlloca(builder, intType, "imm");

                LLVM.BuildStore(builder, LLVM.ConstInt(intType, immOperand.Value, new LLVMBool(0)), alloca);

                var load = LLVM.BuildLoad(builder, alloca, "imm");

                return load;
            }

            else if (operand is RegisterOperand regOperand)
            {
                var root = architecture.GetRootParentRegister(regOperand.Register.Id);
                var rootPointer = registerGlobals[root.Id];
                if (regOperand.Register.Id != root.Id)
                {
                    var destType = LLVM.PointerType(LLVM.IntType(regOperand.Bitsize), 0);
                    rootPointer = LLVM.BuildPointerCast(builder, rootPointer, destType, "reg");
                }

                var load = LLVM.BuildLoad(builder, rootPointer, "reg");
                return load;
            }

            else if (operand is TemporaryOperand tempOperand)
            {
                var temporary = temporaryVariables[tempOperand];
                return temporary;
            }

            else
            {
                throw new InvalidOperationException(String.Format("Cannot load source operand: {0}", operand.ToString()));
            }
        }

        public void DumpModule()
        {
            LLVM.DumpModule(module);
        }
    }
}
