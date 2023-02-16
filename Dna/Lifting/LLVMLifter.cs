using Dna.ControlFlow;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        private LLVMModuleRef module;

        private readonly string opName = "op";

        private readonly ICpuArchitecture architecture;

        private readonly LLVMBuilderRef builder;

        private Dictionary<register_e, LLVMValueRef> registerGlobals = new Dictionary<register_e, LLVMValueRef>();

        private Dictionary<TemporaryOperand, LLVMValueRef> temporaryVariables = new Dictionary<TemporaryOperand, LLVMValueRef>();

        private Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef> blockMapping = new Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef>();

        public LLVMModuleRef Module => module;

        public LLVMLifter(ICpuArchitecture architecture)
        {
            module = LLVMModuleRef.CreateWithName("TritonTranslator");
            module.Target = "x86_64";
            builder = Module.Context.CreateBuilder();

            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();

            var engine = Module.CreateExecutionEngine();

            var parentRegs = X86Registers.RegisterMapping.Values.Where(x => x.ParentId == x.Id);
            foreach (var parentReg in parentRegs)
            {
                // Skip registers with a size > 64, since rellic does not support 
                // arbitrary bit widths.
                if (parentReg.BitSize > 64)
                    continue;

                var ptrType = (LLVMTypeRef.CreateInt(parentReg.BitSize));
                var global = Module.AddGlobal(ptrType, parentReg.Name);
                global.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var ptrNull = LLVMValueRef.CreateConstInt(ptrType, 0, false);
                global.Initializer = ptrNull;
                registerGlobals.Add(parentReg.Id, global);
            }

            this.architecture = architecture;
        }

        public void Lift(ControlFlowGraph<AbstractInst> irCfg)
        {
            var function = Module.AddFunction(
                "SampleFunc",
                LLVMTypeRef.CreateFunction(LLVMTypeRef.Void,
                new LLVMTypeRef[] { },
                false));


            function.Linkage = LLVMLinkage.LLVMExternalLinkage;

            var entryBlock = function.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBlock);
            var irBlocks = irCfg.GetBlocks();
            blockMapping.Add(irBlocks.First(), entryBlock);
            foreach (var block in irBlocks.Skip(1))
            {
                var llvmBlock = function.AppendBasicBlock(block.Name);
                blockMapping.Add(block, llvmBlock);
            }

            var translator = new InstToLLVMLifter(module, builder, LoadSourceOperand, StoreToOperand);

            foreach (var block in irBlocks)
            {
                builder.PositionAtEnd(blockMapping[block]);
                foreach(var inst in block.Instructions)
                {
                    CompileInstruction(inst);
                    /*
                    translator.LiftInstructionToLLVM(inst, (ulong address) =>
                    {
                        return blockMapping.Single(x => x.Key.Address == address).Value;
                    });
                    */
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
            var result = builder.BuildAdd(op1, op2, "add");
            StoreToOperand(inst.Dest, result);
        }

        private void FromAnd(InstAnd inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildAnd(op1, op2, "and");
            StoreToOperand(inst.Dest, result);
        }

        private void FromAshr(InstAshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildAShr(op1, op2, "ashr");
            StoreToOperand(inst.Dest, result);
        }

        private void FromLshr(InstLshr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildLShr(op1, op2, "lshr");
            StoreToOperand(inst.Dest, result);
        }

        private void FromMul(InstMul inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildMul(op1, op2, "mul");
            StoreToOperand(inst.Dest, result);
        }

        private void FromNeg(InstNeg inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = builder.BuildNeg(op1, "neg");
            StoreToOperand(inst.Dest, result);
        }

        private void FromNot(InstNot inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var result = builder.BuildNot(op1, "not");
            StoreToOperand(inst.Dest, result);
        }

        private void FromOr(InstOr inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildOr(op1, op2, "or");
            StoreToOperand(inst.Dest, result);
        }

        private void FromRol(InstRol inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            // Create the intrinsic.
            var intType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var fnType = LLVMTypeRef.CreateFunction(intType, new LLVMTypeRef[] { intType, intType, intType }, false);
            var intrinsicFunc = Module.AddFunction("llvm.fshl.i" + inst.Bitsize, fnType);

            var result = builder.BuildCall(intrinsicFunc, new LLVMValueRef[] { op1, op1, op2 }, "rol");
            StoreToOperand(inst.Dest, result);
        }

        private void FromRor(InstRor inst)
        {
            // Load the operands.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);

            // Create the intrinsic.
            var intType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var fnType = LLVMTypeRef.CreateFunction(intType, new LLVMTypeRef[] { intType, intType, intType }, false);
            var intrinsicFunc = Module.AddFunction("llvm.fshr.i" + inst.Bitsize, fnType);

            // Invoke the intrinsic and store the result.
            var result = builder.BuildCall(intrinsicFunc, new LLVMValueRef[] { op1, op1, op2 }, "ror");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSdiv(InstSdiv inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildSDiv(op1, op2, "sdiv");
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

            var result = builder.BuildICmp(predicate.Value, op1, op2, "cond");
            StoreToOperand(inst.Dest, result);
        }


        private void FromSmod(InstSmod inst)
        {
            // TODO: Validate.
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var srem = builder.BuildSRem(op1, op2, opName);
            var added = builder.BuildAdd(srem, op2, opName);
            var result = builder.BuildSRem(added, op2, "smod");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSrem(InstSrem inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildSRem(op1, op2, "srem");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSub(InstSub inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildSub(op1, op2, "sub");
            StoreToOperand(inst.Dest, result);
        }

        private void FromUdiv(InstUdiv inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildUDiv(op1, op2, "udiv");
            StoreToOperand(inst.Dest, result);
        }


        private void FromUrem(InstUrem inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildURem(op1, op2, "urem");
            StoreToOperand(inst.Dest, result);
        }

        private void FromXor(InstXor inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = builder.BuildXor(op1, op2, "xor");
            StoreToOperand(inst.Dest, result);
        }

        private void FromConcat(InstConcat inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var intType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var result = builder.BuildZExt(op1, intType, "concat");

            foreach (var operand in inst.Operands.Skip(1))
            {
                var shiftN = LLVMValueRef.CreateConstInt(intType, operand.Bitsize, false);
                result = builder.BuildShl(result, shiftN, "concat");
                var extended = builder.BuildZExt(LoadSourceOperand(operand), intType, opName);
                result = builder.BuildOr(result, extended, "concat");
            }

            StoreToOperand(inst.Dest, result);
        }

        private void FromExtract(InstExtract inst)
        {
            var low = (ImmediateOperand)inst.Op2;
            var value = LoadSourceOperand(inst.Op3);
            var intType = LLVMTypeRef.CreateInt(inst.Bitsize);

            if (low.Value == 0)
            {
                var truncated = builder.BuildTrunc(value, intType, opName);
                StoreToOperand(inst.Dest, truncated);
                return;
            }

            var shifted = builder.BuildLShr(value, LoadSourceOperand(low), opName);
            var result = builder.BuildTrunc(shifted, intType, "extract");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSelect(InstSelect inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var op3 = LoadSourceOperand(inst.Op3);
            var result = builder.BuildSelect(op1, op2, op3, "select");
            StoreToOperand(inst.Dest, result);
        }

        private void FromSx(InstSx inst)
        {
            var destType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var input = LoadSourceOperand(inst.InputOperand);
            var result = builder.BuildSExt(input, destType, "sx");
            StoreToOperand(inst.Dest, result);
        }

        private void FromZx(InstZx inst)
        {
            var destType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var input = LoadSourceOperand(inst.InputOperand);
            var result = builder.BuildZExt(input, destType, "zx");
            StoreToOperand(inst.Dest, result);
        }

        private void FromCopy(InstCopy inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);

            var valType = LLVMTypeRef.CreateInt(inst.Bitsize);

            var pointer = builder.BuildAlloca(valType, "copy");

            builder.BuildStore(op1, pointer);

            var load = builder.BuildLoad2(valType, pointer, "copy");

            StoreToOperand(inst.Dest, load);
        }

        private void FromLoad(InstLoad inst)
        {
            // Load the source address.
            var op1 = LoadSourceOperand(inst.Op1);

            // Cast the address to a pointer.
            var valType = LLVMTypeRef.CreateInt(inst.Bitsize);
            var ptrType = LLVMTypeRef.CreatePointer(valType, 0);
            var pointer = builder.BuildIntToPtr(op1, ptrType, "loadPtr");

            // Dereference the pointer.
            var value = builder.BuildLoad2(valType, pointer, "load");

            StoreToOperand(inst.Dest, value);
        }

        private void FromJmp(InstJmp inst)
        {
            // Load the source address.
            var thenAddr = inst.Op1 as ImmediateOperand;
            var thenBlock = blockMapping.Single(x => x.Key.Address == thenAddr.Value).Value;

            builder.BuildBr(thenBlock);
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

            builder.BuildCondBr(cond, thenBlock, elseBlock);
        }

        private void FromRet(InstRet inst)
        {
            builder.BuildRetVoid();
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
                    var destType = LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateInt(regOperand.Bitsize), 0);
                    rootPointer = builder.BuildPointerCast(rootPointer, destType, opName);
                }

                builder.BuildStore(result, rootPointer);
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
                var intType = LLVMTypeRef.CreateInt(operand.Bitsize);

                var alloca = builder.BuildAlloca(intType, "imm");

                builder.BuildStore(LLVMValueRef.CreateConstInt(intType, immOperand.Value, false), alloca);

                var load = builder.BuildLoad2(intType, alloca, "imm");

                return load;
            }

            else if (operand is RegisterOperand regOperand)
            {
                // Get a pointer to the root register(e.g. RAX)
                var root = architecture.GetRootParentRegister(regOperand.Register.Id);
                var rootPointer = registerGlobals[root.Id];

                var valueType = LLVMTypeRef.CreateInt(regOperand.Bitsize);

                // If the input register is not the root parent(e.g. if it is EAX, but not RAX),
                // then we cast the pointer for truncation.
                // So if we currently have an i64*, and we are reading reg EAX, then we truncate the
                // pointer to i32*.
                if (regOperand.Register.Id != root.Id)
                {
                    var destType = LLVMTypeRef.CreatePointer(valueType, 0);
                    rootPointer = builder.BuildPointerCast(rootPointer, destType, "reg");
                }

                var load = builder.BuildLoad2(valueType, rootPointer, "reg");
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
            Module.Dump();
        }

        public void WriteToBitcodeFile(string path)
        {
            Module.WriteBitcodeToFile(path);
        }
        
        public unsafe byte[] Serialize()
        {
            var bufferRef = LLVM.WriteBitcodeToMemoryBuffer(Module);
            var serialized = GetLlvmBytes(bufferRef);
            LLVM.DisposeMemoryBuffer(bufferRef);
            return serialized;
        }
        
        private unsafe byte[] GetLlvmBytes(LLVMOpaqueMemoryBuffer* bufferRef)
        {
            var start = LLVM.GetBufferStart(bufferRef);
            var size = LLVM.GetBufferSize(bufferRef);
            byte[] copy = new byte[size];
            Marshal.Copy((nint)start, copy, 0, (int)size);
            return copy;
        }
    }
}
