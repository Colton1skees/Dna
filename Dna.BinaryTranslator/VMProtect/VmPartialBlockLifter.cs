using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.Runtime;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public class VmPartialBlockLifter
    {
        private readonly LLVMContextRef ctx;

        private readonly RemillArch arch;

        private readonly ControlFlowGraph<VmHandler> vmCfg;

        private readonly VmHandlerCache handlerCache;

        private readonly VmBlockCache partialBlockCache;

        private readonly IReadOnlyDictionary<ulong, VmpJmpTable> jmpTables;

        private readonly LLVMBuilderRef builder;

        public LLVMModuleRef module;

        // Wrapper class for reading and writing to registers based off the prototype of function that we specified.
        private readonly VmpParameterizedStateStructure StateStruct;

        public VmPartialBlockLifter(LLVMContextRef ctx, RemillArch arch, ControlFlowGraph<VmHandler> vmCfg, VmHandlerCache handlerCache, VmBlockCache partialBlockCache, IReadOnlyDictionary<ulong, VmpJmpTable> jmpTables)
        {
            this.ctx = ctx;
            this.arch = arch;
            this.vmCfg = vmCfg;
            this.handlerCache = handlerCache;
            this.partialBlockCache = partialBlockCache;
            this.jmpTables = jmpTables;
            builder = LLVMBuilderRef.Create(ctx);
            module = ctx.CreateModuleWithName("PartialBlocks");
            StateStruct = handlerCache.GetLiftedHandler(vmCfg.GetBlocks().First().Address).ParameterizedStateStructure;
        }

        public IReadOnlyDictionary<VmHandler, (LLVMValueRef func, BasicBlock<VmHandler> block)> Lift()
        {
            // Note that these two dictionaries are not to be confused.
            // Blocktollvm = mapping between each basic block(marked by it's starting bytecode ptr) and it's corresponding llvm func
            // Handlertollvm = 
            Dictionary<VmHandler, (LLVMValueRef func, BasicBlock<VmHandler> block)> blockToLlvmFunc = new();
            Dictionary<VmHandler, LLVMValueRef> handlerToLlvmFunc = new();
            foreach (var block in vmCfg.GetBlocks())
            {
                var handler = block.EntryInstruction;
                var liftedBlock = LiftBlock(block, handlerToLlvmFunc);
                blockToLlvmFunc.Add(handler, (liftedBlock, block));
            }

            // Inline and delete all of the VMProtect handler functions.
            foreach (var func in handlerToLlvmFunc.Values)
            {
                LLVMCloning.InlineFunction(func);
                func.DeleteFunction();
            }

            module.PrintToFile("translatedFunction.ll");
            return blockToLlvmFunc;
        }

        private LLVMValueRef LiftBlock(BasicBlock<VmHandler> block, Dictionary<VmHandler, LLVMValueRef> handlerToLlvmFunc)
        {
            var memPtr = module.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(module.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }

            module.PrintToFile("translatedFunction.ll");
            module.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // Create the function
            LLVMValueRef outFunction = module.AddFunction($"block_{block.Address.ToString("X")}", StateStruct.ParameterizedFunctionPrototype);
            var entryBb = outFunction.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBb);

            var registerAllocaMapping = CreateLocalStateStruct(builder, StateStruct, outFunction);

            // Try to model the block as a cached partial block + a sequence of new instructions that need to be appended to the end.
            var maybeAsPartialBlock = TryModelAsPartialBlock(block);

            // If a partial block exists, clone it into our module. and call it.
            var blockHandler = block.EntryInstruction;
            PartialLiftedBlock clonedPartialBlock = null;
            if (maybeAsPartialBlock != null)
            {
                // Clone the partial function into our module.
                clonedPartialBlock = partialBlockCache.ClonePartialBlockIntoModule(blockHandler, module);

                // Call the partial vm block.
                CallVmHandler(builder, outFunction, clonedPartialBlock.Function, registerAllocaMapping, StateStruct);
            }

            // Fetch the set of instructions to be lifted.
            var instsToLift = maybeAsPartialBlock == null ? block.Instructions : maybeAsPartialBlock.Value.instructionsNotInPartialBlock;
            foreach (var inst in instsToLift)
            {
                // Fetch the handler. If we have not already cloned this handler into our module, do it.
                if (!handlerToLlvmFunc.ContainsKey(inst))
                    handlerToLlvmFunc.Add(inst, handlerCache.CloneLiftedHandlerIntoModule(inst.NativeRip, module));

                // Invoke the native VM handler.
                var handlerFunction = handlerToLlvmFunc[inst];
                var call = CallVmHandler(builder, outFunction, handlerFunction, registerAllocaMapping, StateStruct);
            }

            var ret = builder.BuildRetVoid();


            ret.InstructionEraseFromParent();

            // Write the local state struct contents back into the output argument pointers.
            UpdateOutputRegisters(builder, outFunction, registerAllocaMapping, StateStruct);
            // Return nothing.
            builder.BuildRetVoid();


            memPtr = module.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(module.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }
            outFunction.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // If a partial block was invoked, inline the call and then delete the function.
            if (clonedPartialBlock != null)
            {
               // outFunction.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
               // outFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                var callersOf = RemillUtils.CallersOf(clonedPartialBlock.Function);
                LLVMCloning.InlineFunction(clonedPartialBlock.Function);
                clonedPartialBlock.Function.DeleteFunction();
            }

            // Delete any lingering calls to @vmp_maybe_unsolved_jmp.
            // The code that stitches the partial blocks together will handle inserting the intrinsics correctly.
            var calls = outFunction.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMCall && x.GetOperand((uint)x.OperandCount - 1).ToString().Contains("vmp_maybe_unsolved_jump")).ToList();
            foreach (var call in calls)
                call.InstructionEraseFromParent();

            return outFunction;
        }

        public static IReadOnlyDictionary<RemillRegister, LLVMValueRef> CreateLocalStateStruct(LLVMBuilderRef builder, VmpParameterizedStateStructure stateStruct, LLVMValueRef function)
        {
            // For each register, create a local alloca.
            var registerAllocaMapping = new Dictionary<RemillRegister, LLVMValueRef>();
            foreach (var reg in stateStruct.OrderedRegisterArguments)
            {
                // Allocate a local variable for the register.
                var regType = LLVMTypeRef.CreateInt((uint)reg.Size * 8);
                var alloca = builder.BuildAlloca(regType);

                // Store the input register value to our local register variable.
                var regValue = stateStruct.GetRegParam(reg, function);
                builder.BuildStore(regValue, alloca);

                // Add this to the list.
                registerAllocaMapping.Add(reg, alloca);
            }

            return registerAllocaMapping;
        }

        private (PartialLiftedBlock partialLiftedBlock, List<VmHandler> instructionsNotInPartialBlock)? TryModelAsPartialBlock(BasicBlock<VmHandler> block)
        {
            // Return null if no partial version of this block exists in the cache.
            var handler = block.EntryInstruction;
            if (!partialBlockCache.ContainsBlock(handler))
                return null;

            // Fetch the partial basic block and partial function.
            var (partialBlock, partialFunction) = partialBlockCache.GetBlock(handler);

            // Throw if any of the partial block instructions are considered incomplete(aka we don't know their precise set of outgoing edges)
            if (partialBlock.Instructions.Any(x => !jmpTables[x.BytecodeRip].IsComplete))
                return null;
                //throw new InvalidOperationException($"Error: Attempted to lift partial block when one of the instructions has a potentially unknown set of outgoing edges!");

            // Throw is the partial block is larger than the block it's supposed to be inside of
            if (partialBlock.Instructions.Count > block.Instructions.Count)
                throw new InvalidOperationException($"Partial block is larger than the parent block! This should not happen for now.");

            // Split the block instructions into two lists: 
            //  (1) the list of instructions contained in the partial block
            //  (2) the list of instructions not included in the partial block
            var inPartialBlock = block.Instructions.Take(partialBlock.Instructions.Count).ToList();
            var outsideOfPartialBlock = block.Instructions.Skip(partialBlock.Instructions.Count).ToList();

            // Throw if the shared instructions are not exactly identical.
            if (!inPartialBlock.SequenceEqual(partialBlock.Instructions))
                throw new InvalidOperationException($"Partial block contains non-matching sequence of instructions!");

            return (new PartialLiftedBlock(partialBlock, partialFunction), outsideOfPartialBlock);
        }

        public static LLVMValueRef CallVmHandler(LLVMBuilderRef builder, LLVMValueRef parentFunction, LLVMValueRef handlerFunction, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, VmpParameterizedStateStructure StateStruct)
        {
            // For each input register, pass the local register value as a by-register argument.
            var args = new List<LLVMValueRef>();
            foreach (var reg in StateStruct.OrderedRegisterArguments)
            {
                var regType = LLVMTypeRef.CreateInt((uint)reg.Size * 8);
                var regValue = builder.BuildLoad2(regType, registerAllocaMapping[reg], reg.Name);
                args.Add(regValue);
            }

            // Gotcha: If an optional memory pointer is used, it is always placed between the input and output register arguments.
            if (StateStruct.addMemoryPtr)
                args.Add(parentFunction.GetParam((uint)StateStruct.RegisterArgumentIndices.Count + 1));

            // For each output register argument, pass in our local state structure argument pointer.
            foreach (var (reg, index) in StateStruct.RegisterOutputArgumentIndices.OrderBy(x => x.Value))
            {
                var regPtr = registerAllocaMapping[reg];
                args.Add(regPtr);
            }

            var call = builder.BuildCall2(StateStruct.ParameterizedFunctionPrototype, handlerFunction, args.ToArray());
            return call;
        }

        public static void UpdateOutputRegisters(LLVMBuilderRef builder, LLVMValueRef function, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, VmpParameterizedStateStructure StateStruct)
        {
            // For each input register, fetch the register value from the local state structure.
            var args = new List<LLVMValueRef>();
            foreach (var reg in StateStruct.OrderedRegisterArguments)
            {
                var regType = LLVMTypeRef.CreateInt((uint)reg.Size * 8);
                var regValue = builder.BuildLoad2(regType, registerAllocaMapping[reg], reg.Name);
                args.Add(regValue);
            }

            // For each output register argument, store the register value to the parameter.
            int i = 0;
            foreach (var (reg, index) in StateStruct.RegisterOutputArgumentIndices.OrderBy(x => x.Value))
            {
                var regPtr = function.GetParam((uint)index);
                var regValue = args[i];
                i++;

                builder.BuildStore(regValue, regPtr);
            }
        }
    }
}
