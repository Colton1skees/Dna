using Dna.ControlFlow;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Intermediate.Operands;
using TritonTranslator.Intermediate;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Dna.Lifting
{
    public class LLVMLifter2
    {
        private readonly LLVMModuleRef module;

        private readonly ICpuArchitecture architecture;

        private readonly LLVMBuilderRef builder;

        private InstToLLVMLifter lifter;

        // Mapping between <triton register, global variable>
        private readonly Dictionary<register_e, LLVMValueRef> liftedRegisterGlobalVariableMapping = new Dictionary<register_e, LLVMValueRef>();

        // Mapping between <TemporaryOperand, llvm local variable>
        private Dictionary<TemporaryOperand, LLVMValueRef> liftedTemporaryMapping = new Dictionary<TemporaryOperand, LLVMValueRef>();

        // Mapping between <triton register, per-function llvm local variable>
        private Dictionary<register_e, LLVMValueRef> liftedLocalRegisters = new Dictionary<register_e, LLVMValueRef>();

        // Mapping between <lifted block, llvm block>
        private Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef> liftedBlockMapping = new Dictionary<BasicBlock<AbstractInst>, LLVMBasicBlockRef>();

        // Current control flow graph to lift.
        private ControlFlowGraph<AbstractInst> irCfg;

        // Current llvm function to lift into.
        private LLVMValueRef llvmFunction;

        // LLVM module to lift into.
        public LLVMModuleRef Module => module;

        public LLVMLifter2(ICpuArchitecture architecture)
        {
            this.architecture = architecture;

            // Constrct an LLVM module and builder.
            module = LLVMModuleRef.CreateWithName("TritonTranslator");
            module.Target = "x86_64";
            builder = Module.Context.CreateBuilder();

            // Construct an LLVM lifter for translating individual instructions.
            lifter = new InstToLLVMLifter(module, builder, LoadSourceOperand, StoreToOperand);

            // Initialize x86 target info.
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            var engine = Module.CreateExecutionEngine();

            // For each parent register(e.g. RAX), construct and store a global variable.
            //var registers = X86Registers.RegisterMapping.Values;
            var registers = X86Registers.RegisterMapping.Values.Where(x => x.ParentId == x.Id);
            foreach (var reg in registers)
            {
                // Skip registers with a size > 64, since rellic does not support 
                // arbitrary bit widths.
                if (reg.BitSize > 64)
                    continue;

                // Construct the global variable.
                var ptrType = (LLVMTypeRef.CreateInt(reg.BitSize));
                var global = Module.AddGlobal(ptrType, reg.Name);
                global.Linkage = LLVMLinkage.LLVMCommonLinkage;

                // Set the default value to zero.
                var ptrNull = LLVMValueRef.CreateConstInt(ptrType, 0, false);
                global.Initializer = ptrNull;

                // Store the global variable.
                liftedRegisterGlobalVariableMapping.Add(reg.Id, global);
            }
        }

        public void Lift(ControlFlowGraph<AbstractInst> irCfg)
        {
            // Setup state.
            this.irCfg = irCfg;
            liftedTemporaryMapping.Clear();
            liftedBlockMapping.Clear();
            liftedLocalRegisters.Clear();

            // Create the empty LLVM function.
            llvmFunction = CreateFunction("SampleFunc");

            // For each lifted IR basic block, create an empty llvm block.
            // block. Then store a mapping between <ir block, LLVM block>.
            AllocateEmptyLlvmBlocks();

            // Position the builder at the entry block.
            builder.PositionAtEnd(llvmFunction.EntryBasicBlock);


            // At the entry point of the LLVM routine, copy each lifted register
            // into a local variable.
            //CopyRegistersToLocalVariables();

            // Create a lambda to take a given address(i.e. the destination of a direct jump)
            // and query which LLVM block it points to. This is necessary since our IR
            // uses immediates to represent block destinations.
            var irBlocks = irCfg.GetBlocks();
            var getBlockByAddress = (ulong addr) =>
            {
                return liftedBlockMapping.Single(x => x.Key.Address == addr).Value;
            };

            // Lift each block to LLVM IR.
            foreach(var block in irBlocks)
            {
                builder.PositionAtEnd(liftedBlockMapping[block]);
                foreach(var inst in block.Instructions)
                {
                    lifter.LiftInstructionToLLVM(inst, getBlockByAddress);
                }
            }
        }

        private LLVMValueRef CreateFunction(string name)
        {
            var function = llvmFunction = Module.AddFunction(
                name,
                LLVMTypeRef.CreateFunction(LLVMTypeRef.Void,
                new LLVMTypeRef[] { },
                false));

            // Set the function linkage.
            llvmFunction.Linkage = LLVMLinkage.LLVMExternalLinkage;

            return function;
        }

        private void AllocateEmptyLlvmBlocks()
        {
            var irBlocks = irCfg.GetBlocks().ToList();
            foreach (var block in irBlocks)
            {
                if (block.Address == irBlocks.First().Address)
                {
                    var lblk = llvmFunction.AppendBasicBlock("entry");
                    liftedBlockMapping.Add(block, lblk);

                }

                else
                {

                    var llvmBlock = llvmFunction.AppendBasicBlock(block.Name);
                    liftedBlockMapping.Add(block, llvmBlock);
                }
            }
        }

        private void CopyRegistersToLocalVariables()
        {
            foreach(var registerId in liftedRegisterGlobalVariableMapping.Keys)
            {
                // Construct an integer type of the register width.
                var valueType = LLVMTypeRef.CreateInt(architecture.GetRegister(registerId).BitSize);

                // Allocate a local variable of the register size.
                var alloca = builder.BuildAlloca(valueType, registerId.ToString());

                // Store a mapping of <register id, local variable.
                liftedLocalRegisters.Add(registerId, alloca);
            }
        }

        private void StoreToOperand(IOperand operand, LLVMValueRef result)
        {
            if (operand is SsaOperand ssaOP)
                operand = ssaOP.BaseOperand;

            if (operand is RegisterOperand regOperand)
            {
                // Fix up register sizing. TODO: Remove.
                var root = architecture.GetRootParentRegister(regOperand.Register.Id);
                var rootPointer = liftedRegisterGlobalVariableMapping[root.Id];
                if (regOperand.Register.Id != root.Id)
                {
                    var destType = LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateInt(regOperand.Bitsize), 0);
                    rootPointer = builder.BuildPointerCast(rootPointer, destType, "rootPtr");
                }

                builder.BuildStore(result, rootPointer);
            }

            else if (operand is TemporaryOperand tempOperand)
            {
                liftedTemporaryMapping[tempOperand] = result;
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
                var rootPointer = liftedRegisterGlobalVariableMapping[root.Id];

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
                var temporary = liftedTemporaryMapping[tempOperand];
                return temporary;
            }

            else
            {
                throw new InvalidOperationException(String.Format("Cannot load source operand: {0}", operand.ToString()));
            }
        }
    }
}
