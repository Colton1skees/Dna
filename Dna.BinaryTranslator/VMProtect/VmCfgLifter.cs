using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.Runtime;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockMapping = System.Collections.Generic.IReadOnlyDictionary<Dna.ControlFlow.BasicBlock<Dna.BinaryTranslator.VMProtect.VmHandler>, LLVMSharp.Interop.LLVMBasicBlockRef>;

namespace Dna.BinaryTranslator.VMProtect
{
    public class VmCfgLifter
    {
        private readonly LLVMModuleRef module;

        private readonly RemillArch arch;

        private readonly VmpParameterizedStateStructure StateStruct;

        private readonly ControlFlowGraph<VmHandler> vmCfg;

        private readonly IReadOnlyDictionary<VmHandler, (LLVMValueRef func, BasicBlock<VmHandler> block)> blockToFunctionMapping;

        private readonly IReadOnlyDictionary<ulong, VmpJmpTable> jmpTables;

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;

        private readonly LLVMBuilderRef builder;

        private LLVMBasicBlockRef exitBlock;

        private IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping;


        private LLVMValueRef translatedFunction;

        public VmCfgLifter(LLVMModuleRef module, RemillArch arch, VmpParameterizedStateStructure stateStruct, ControlFlowGraph<VmHandler> vmCfg, IReadOnlyDictionary<VmHandler, (LLVMValueRef func, BasicBlock<VmHandler> block)> blockToFunctionMapping, IReadOnlyDictionary<ulong, VmpJmpTable> jmpTables, IReadOnlySet<ulong> vmexitHandlerRips)
        {
            this.module = module;
            this.arch = arch;
            this.StateStruct = stateStruct;
            this.vmCfg = vmCfg;
            this.blockToFunctionMapping = blockToFunctionMapping;
            this.jmpTables = jmpTables;
            this.vmexitHandlerRips = vmexitHandlerRips;
            builder = LLVMBuilderRef.Create(module.Context);
        }

        // TODO for tomorrow: You are allocating a local state structure and then accidentally failing to make your basic block invocations write to them.
        public LLVMValueRef Lift()
        {
          // module.PrintToFile("translatedFunction.ll");
            // Create a lifted function.
            var prototype = StateStruct.ParameterizedFunctionPrototype;
            translatedFunction = module.AddFunction($"PartialCfgFrom_{vmCfg.StartAddress.ToString("X")}", prototype);

            // Create the entry basic block.
            var entryBlock = translatedFunction.AppendBasicBlock("entry");

            // Allocate a local state structure as a mutable alloca for each general purpose register.
            // TODO: Refactor method out to a common helper class
            builder.PositionAtEnd(entryBlock);
            registerAllocaMapping = VmPartialBlockLifter.CreateLocalStateStruct(builder, StateStruct, translatedFunction);

            // Create an exit block.
            exitBlock = translatedFunction.AppendBasicBlock("exit");
            builder.PositionAtEnd(exitBlock);
            VmPartialBlockLifter.UpdateOutputRegisters(builder, translatedFunction, registerAllocaMapping, StateStruct);
            builder.BuildRetVoid();

            builder.PositionAtEnd(entryBlock);
            // For each basic block, create an empty `stub` block in the LLVM control flow graph.
            var blockMapping = CreateLlvmBlocks(translatedFunction);

            // Insert a branch from our artificial entry block to the VMCFG's entry block.
            builder.BuildBr(blockMapping[vmCfg.GetBlocks().First()]);

            // Lift each basic block.
            LiftBlocks(blockMapping, registerAllocaMapping);


            module.PrintToFile("translatedFunction.ll");

            // Inline all calls to each basic block. TODO: Delete.
            foreach (var func in blockToFunctionMapping.Values)
                LLVMCloning.InlineFunction(func.func);

            module.PrintToFile("translatedFunction.ll");
            //module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
  
            Console.WriteLine("lifted all blocks");
            return translatedFunction;
        }

        private BlockMapping CreateLlvmBlocks(LLVMValueRef function)
        {
            // Create LLVM blocks for each native basic block, then store a mapping between these two items.
            var blocks = vmCfg.GetBlocks();
            var blockMapping = new Dictionary<BasicBlock<VmHandler>, LLVMBasicBlockRef>();
            foreach (var block in blocks)
            {
                var llvmBlock = function.AppendBasicBlock($"bb_{block.Address.ToString("X")}");
                blockMapping.Add(block, llvmBlock);
            }

            return blockMapping.AsReadOnly();
        }

        private void LiftBlocks(BlockMapping blockMapping, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
        {
            foreach (var block in blockMapping)
            {
                // Lift the basic block into the LLVM IR function.
                LiftBlock(block.Key, blockMapping, registerAllocaMapping);
            }
        }

        private void LiftBlock(BasicBlock<VmHandler> block, BlockMapping blockMapping, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
        {
            // Fetch the corresponding LLVM block within our cfg.
            var handler = block.Instructions.First();
            var llvmBlock = blockMapping[block];
            builder.PositionAtEnd(llvmBlock);

            // Fetch the corresponding function.
            var blockFunction = blockToFunctionMapping[handler];

            // Call the block function.
            VmPartialBlockLifter.CallVmHandler(builder, translatedFunction, blockFunction.func, registerAllocaMapping, StateStruct);

            LiftBlockEdges(blockMapping, block);
        }

        private void LiftBlockEdges(BlockMapping blockMapping, BasicBlock<VmHandler> block)
        {
            // Fetch the corresponding LLVM block within our cfg.
            var handler = block.Instructions.First();
            var llvmBlock = blockMapping[block];

            // If there's a vmexit, stop processing.
            bool isVmexit = vmexitHandlerRips.Contains(handler.NativeRip);
            bool isComplete = IsBlockComplete(block);
            if ((isComplete && block.OutgoingEdges.Count == 0) || isVmexit)
            {
                Console.WriteLine("Encounted possible vmexit!");
                builder.BuildBr(exitBlock);
                //builder.BuildRetVoid();
                return;
                // throw new InvalidOperationException("TODO! Encountered possible vmexit");
            }

            // Load the indirect jump value.
            var int64Ty = module.Context.GetInt64Ty();
            var indirectPc = LoadBytecodePointer();

            var liftedCases = new HashSet<ulong>
            {
            };

            LLVMBasicBlockRef defaultBlock = null;

            var outgoingEdges = block.GetOutgoingEdges().Select(x => x.TargetBlock).ToList();
            var outgoingAddresses = outgoingEdges.Select(x => x.Address).ToList();

            // If the jump table is considered incomplete(we know some edges but potentially not all), then we lift the jump table as a switch statement
            // where the known values get their own 'case', and the default case points to an remill_jump intrinsic.
            if (!isComplete)
            {
                var jmpFromAddr = block.ExitInstruction.BytecodeRip;
                defaultBlock = translatedFunction.AppendBasicBlock($"reprove_new_edge_for_jmp_table_{jmpFromAddr.ToString("X")}");
                builder.PositionAtEnd(defaultBlock);
                AddCallToIndirectBranchIntrinsic(defaultBlock, jmpFromAddr);
                builder.PositionAtEnd(llvmBlock);
            }

            // If the jump table is considered complete(i.e. we are 100% confident that we know all possible outgoing values),
            // then we make the default case for the jump table point to a randomly selected(first) jump table outgoing block.
            else
            {
                // Select a random(the first) switch case block to be used as the default case.
                defaultBlock = blockMapping.Single(x => x.Key == outgoingEdges.First()).Value;
                // Keep track of the fact that we've already lifted this block, so that we don't lift it twice.
                liftedCases.Add(outgoingAddresses.First());
            }


            //  Console.WriteLine(defaultBlock);
            //  Console.WriteLine(indirectPc);
            module.PrintToFile("translatedFunction.ll");
            //  Console.ReadLine();
            builder.PositionAtEnd(llvmBlock);
            var swtch = builder.BuildSwitch(indirectPc, defaultBlock, (uint)outgoingAddresses.Count);
     
          //  Console.ReadLine();
            foreach (var target in outgoingAddresses)
            {
                if (liftedCases.Contains(target))
                    continue;

                liftedCases.Add(target);
                var targetBlock = blockMapping.Single(x => x.Key.Address == target).Value;
                swtch.AddCase(LLVMValueRef.CreateConstInt(module.Context.Int64Type, target), targetBlock);
            }
        }

        private bool IsBlockComplete(BasicBlock<VmHandler> block)
        {
            var unsolved = block.Instructions.Where(x => jmpTables[x.BytecodeRip].IsComplete == false).ToList();
            if (unsolved.Count == 0)
                return true;
            else if (unsolved.Count == 1 && unsolved.Single() == block.Instructions.Last())
                return false;
            else
                throw new InvalidOperationException($"A basic block may only have one unsolved exit, and that exit must be at the end of the block!");
        }

        private void AddCallToIndirectBranchIntrinsic(LLVMBasicBlockRef llvmBlock, ulong exitFromRip)
        {
            // Get or create the function.
            var int64Ty = module.Context.GetInt64Ty();
            var (prototype, intrinsicFunc) = GetOrCreateJmpIntrinsic();

            var args = new List<LLVMValueRef>();
            args.Add(LoadBytecodePointer());
            args.Add(LoadNativeInstructionPointer());
            args.Add((LLVMValueRef.CreateConstInt(int64Ty, exitFromRip)));
            var call = builder.BuildCall2(prototype, intrinsicFunc, args.ToArray());
            builder.BuildBr(exitBlock);
        }

        private LLVMValueRef LoadBytecodePointer()
        {
            // TODO: Stop hardcoding RDI as the bytecode pointer.
            var regPtr = registerAllocaMapping.Single(x => x.Key.Name.Contains("RSI")).Value;

            // Load and return the value of the "RDI"(bytecode ptr) local variable.
            var regValue = builder.BuildLoad2(LLVMTypeRef.Int64, regPtr, "bytecode_ptr");
            return regValue;
        }

        private LLVMValueRef LoadNativeInstructionPointer()
        {
            // TODO: Stop hardcoding RDI as the bytecode pointer.
            var regPtr = registerAllocaMapping.Single(x => x.Key.Name.Contains("RDX")).Value;

            // Load and return the value of the "RDI"(bytecode ptr) local variable.
            var regValue = builder.BuildLoad2(LLVMTypeRef.Int64, regPtr, "instruction_ptr");
            return regValue;
        }

        private (LLVMTypeRef prototype, LLVMValueRef function) GetOrCreateJmpIntrinsic()
        {
            var ptrType = module.Context.GetPtrType();
            var i64Type = LLVMTypeRef.Int64;
            var argTypes = new List<LLVMTypeRef>();
            argTypes.Add(i64Type);
            argTypes.Add(i64Type);
            argTypes.Add(i64Type);

            var prototype = LLVMTypeRef.CreateFunction(ptrType, argTypes.ToArray());

            var function = module.GetFunctions().SingleOrDefault(x => x.Name == "vmp_maybe_unsolved_jump");
            if (function != null)
                return (prototype, function);
            var func = module.AddFunction("vmp_maybe_unsolved_jump", prototype);
            return (prototype, func);
        }
    }
}
