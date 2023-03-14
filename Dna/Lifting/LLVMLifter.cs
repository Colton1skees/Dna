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
    public class LLVMLifter
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

        private LLVMValueRef memoryPtr;

        private LLVMValueRef localMemVariable;

        // Current control flow graph to lift.
        private ControlFlowGraph<AbstractInst> irCfg;

        // Current llvm function to lift into.
        public LLVMValueRef llvmFunction;

        // LLVM module to lift into.
        public LLVMModuleRef Module => module;

        public LLVMLifter(ICpuArchitecture architecture)
        {
            this.architecture = architecture;

            // Constrct an LLVM module and builder.
            module = LLVMModuleRef.CreateWithName("TritonTranslator");
            module.Target = "wasm64";
            builder = Module.Context.CreateBuilder();

            // Create an i64* pointer to store memory.
            var memoryPtrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateInt(8), 0);
            memoryPtr = Module.AddGlobal(memoryPtrType, "memory");
            memoryPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
            var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(memoryPtrType);
            memoryPtr.Initializer = memoryPtrNull;

            // Construct an LLVM lifter for translating individual instructions.
            var getLocalMemVariable = () =>
            {
                return localMemVariable;
            };

            lifter = new InstToLLVMLifter(module, builder, getLocalMemVariable, LoadSourceOperand, StoreToOperand);

            // Initialize x86 target info.
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            var engine = Module.CreateExecutionEngine();

            // For each parent register(e.g. RAX), construct and store a global variable.
            //var registers = X86Registers.RegisterMapping.Values;
            var registers = X86Registers.RegisterMapping.Values;
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

            // Create a variable for local memory ptr.
            localMemVariable = builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateInt(64), 0), memoryPtr);

            // At the entry point of the LLVM routine, copy each lifted register
            // into a local variable.
            CopyRegistersToLocalVariables();

            // Create a lambda to take a given address(i.e. the destination of a direct jump)
            // and query which LLVM block it points to. This is necessary since our IR
            // uses immediates to represent block destinations.
            var irBlocks = irCfg.GetBlocks();
            var getBlockByAddress = (ulong addr) =>
            {
                return liftedBlockMapping.Single(x => x.Key.Address == addr).Value;
            };

            // Lift each block to LLVM IR.
            foreach (var block in irBlocks)
            {
                builder.PositionAtEnd(liftedBlockMapping[block]);
                foreach (var inst in block.Instructions)
                {
                    lifter.LiftInstructionToLLVM(inst, getBlockByAddress);
                }
            }

            // Finally, at the end of each lifted block, restore certain registers
            // back to global variables. Typically this will only be
            // RAX + callee saved registers.
            // https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170
            RestoreRegistersToGlobalVariables();
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
            foreach (var registerId in liftedRegisterGlobalVariableMapping.Keys)
            {
                // Construct an integer type of the register width.
                var valueType = LLVMTypeRef.CreateInt(architecture.GetRegister(registerId).BitSize);

                // Allocate a local variable of the register size.
                var alloca = builder.BuildAlloca(valueType, registerId.ToString());

                // Store a mapping of <register id, local variable.
                liftedLocalRegisters.Add(registerId, alloca);

                // Load the register value from it's global variable.
                var value = builder.BuildLoad2(valueType, liftedRegisterGlobalVariableMapping[registerId]);

                builder.BuildStore(value, alloca);
            }
        }

        private void RestoreRegistersToGlobalVariables()
        {
            // Assume the function is fastcall.
            var savedRegisters = new HashSet<register_e>()
            {
                register_e.ID_REG_X86_RAX,
                register_e.ID_REG_X86_EAX,
                register_e.ID_REG_X86_AX,
                register_e.ID_REG_X86_AL,
                register_e.ID_REG_X86_AH,

                register_e.ID_REG_X86_R11,
                register_e.ID_REG_X86_RCX,

                register_e.ID_REG_X86_RIP,
            };

            foreach (var block in liftedBlockMapping.Where(x => x.Key.OutgoingEdges.Count == 0).Select(x => x.Value))
            {
                // Assume that the last instruction is a RET.
                builder.PositionBefore(block.LastInstruction);
                if (!block.LastInstruction.ToString().ToLower().Contains("ret"))
                    throw new InvalidOperationException("Basic block must exit with a RET.");

                foreach (var register in liftedLocalRegisters.Keys)
                {
                    var rootParent = architecture.GetRootParentRegister(register);
                    if (rootParent.Id != register_e.ID_REG_X86_RIP)
                        continue;
                    // Get a triton register operand.
                    var regOperand = architecture.GetRegister(register);

                    // Use our helper function to load the register local
                    // variable value.
                    var value = LoadSourceOperand(new RegisterOperand(regOperand));

                    // Store the local variable value to our register global variable.
                    builder.BuildStore(value, liftedRegisterGlobalVariableMapping[register]);
                }
            }
        }

        private void StoreToOperand(IOperand operand, LLVMValueRef result)
        {
            if (operand is SsaOperand ssaOP)
                operand = ssaOP.BaseOperand;

            if (operand is RegisterOperand regOperand)
            {
                var pointer = liftedLocalRegisters[regOperand.Register.Id];
                builder.BuildStore(result, pointer);
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
                // // Get a pointer to the local variable representing the input register.
                var regPointer = liftedLocalRegisters[regOperand.Register.Id];

                // Constrct an integer type of the register width.
                var valueType = LLVMTypeRef.CreateInt(regOperand.Bitsize);

                // Store to the register pointer.
                var load = builder.BuildLoad2(valueType, regPointer, "reg");
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
