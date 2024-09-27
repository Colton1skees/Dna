using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;

namespace Dna.BinaryTranslator.Unsafe
{
    /// <summary>
    /// Class for applying the fastcall ABI to functions
    /// </summary>
    public class FastcallAbiInserter
    {
        private readonly RemillArch arch;

        private readonly ILLVMRuntime runtime;

        private readonly LLVMValueRef function;

        private readonly ParameterizedStateStructure parameterizedStateStructure;

        private readonly LLVMBuilderRef builder;

        private readonly HashSet<RemillRegister> savedRegisters;

        private readonly HashSet<RemillRegister> clobberedRegisters;

        public static void Insert(RemillArch arch, ILLVMRuntime runtime, LLVMValueRef function, ParameterizedStateStructure parameterizedStateStructure)
        {
            var inserter = new FastcallAbiInserter(arch, runtime, function, parameterizedStateStructure);
            inserter.InsertFastcallAbi();
        }

        private FastcallAbiInserter(RemillArch arch, ILLVMRuntime runtime, LLVMValueRef function, ParameterizedStateStructure parameterizedStateStructure)
        {
            this.arch = arch;
            this.runtime = runtime;
            this.function = function;
            this.parameterizedStateStructure = parameterizedStateStructure;
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
            savedRegisters = GetSavedRegisters();
            clobberedRegisters = GetClobberedRegisters();
        }

        private HashSet<RemillRegister> GetSavedRegisters()
        {
            // Compute a list of registers which are potentially read but never clobbered(destroyed) by a function call.
            return new List<string>() { "RBX", "RBP", "RDI", "RSI", "RSP", "R12", "R13", "R14", "R15" }.Select(x => arch.GetRegisterByName(x)).ToHashSet();
        }

        private HashSet<RemillRegister> GetClobberedRegisters()
        {
            // Compute a list of registers which *may* be clobbered / discarded after a function call exits.
            return new List<string>() { "RAX", "RCX", "RDX", "R8", "R9", "R10", "R11" }.Select(x => arch.GetRegisterByName(x)).ToHashSet();
        }

        private void InsertFastcallAbi()
        {
            // Get the call intrinsic function. If it doesn't exist then we exit.
            var callIntrinsic = GetRemillCallIntrinsic();
            if (callIntrinsic == null)
                return;

            // Create a prototype for a fastcall intrinsic function.
            var funcPrototype = GetFastcallIntrinsicPrototype();

            // Create the function call.
            var dnaCallIntrinsic = function.GlobalParent.AddFunction("dna_fastcall_invoke", funcPrototype);
            dnaCallIntrinsic.Linkage = LLVMLinkage.LLVMExternalLinkage;

            // Compute a list of argument names.
            var names = new List<string>() { "addr", "memory" };
            names.AddRange(savedRegisters.Select(x => x.Name));
            names.AddRange(clobberedRegisters.Select(x => x.Name));
            if (names.Count != dnaCallIntrinsic.ParamsCount)
                throw new InvalidOperationException("Mismatched param count.");

            // Apply the names.
            for (int i = 0; i < names.Count; i++)
            {
                var param = dnaCallIntrinsic.GetParam((uint)i);
                param.Name = names[i];
            }

            var stateStruct = GetStateStructure(function);

            var clobberMapping = BuildAllocaClobberMapping(stateStruct);

            // Replace each call to @__remill_function_call with a custom intrinsic.
            // The intrinsic uses a combination of i64 and ptr arguments to capture
            // the fact that certain registers are callee saved or callee clobbered.
            foreach (var caller in RemillUtils.CallersOf(callIntrinsic.Value).Where(x => x.InstructionParent.Parent == function).ToList())
            {
                ApplyFastcallToRemillCall(stateStruct, clobberMapping, caller, dnaCallIntrinsic, funcPrototype);
            }
        }

        private LLVMValueRef? GetRemillCallIntrinsic()
        {
            return function.GlobalParent.GetFunctions().SingleOrDefault(x => x.Name == "__remill_function_call");
        }

        private LLVMTypeRef GetFastcallIntrinsicPrototype()
        {
            var i64Ty = function.GetFunctionCtx().Int64Type;
            var ptrTy = function.GetFunctionCtx().GetPtrType();

            // Create the list of argument types.
            var argTypes = new List<LLVMTypeRef>();
            argTypes.Add(i64Ty); // address
            argTypes.Add(ptrTy); // mem ptr
            argTypes.AddRange(savedRegisters.Select(x => i64Ty));
            argTypes.AddRange(clobberedRegisters.Select(x => ptrTy));

            // Create the function prototype.
            var funcPrototype = LLVMTypeRef.CreateFunction(ptrTy, argTypes.ToArray());
            return funcPrototype;
        }

        private LLVMValueRef GetStateStructure(LLVMValueRef function)
        {
            // Get the local state structure pointer.
            return function.GetInstructions()
                .Single(x => x.InstructionOpcode == LLVMOpcode.LLVMAlloca && x.ToString().Contains("= alloca %struct.State"));
        }

        /// <summary>
        /// Creates an 'alloca' in the entry basic block for each potentially clobbered register.
        /// </summary>
        /// <param name="statePtr"></param>
        /// <returns></returns>
        private IReadOnlyDictionary<RemillRegister, LLVMValueRef> BuildAllocaClobberMapping(LLVMValueRef statePtr)
        {
            // Position the builder at the start of the entry block.
            builder.PositionBefore(function.EntryBasicBlock.FirstInstruction);
            
            // For each register, allocate a local variable of the registers width.
            // This allocation is placed at the start of the entry block,
            // and used as a spill space during subsequent function calls.
            var output = new Dictionary<RemillRegister, LLVMValueRef>();
            foreach(var reg in clobberedRegisters)
            {
                var intTy = LLVMTypeRef.CreateInt((uint)reg.Size * 8);
                output.Add(reg, builder.BuildAlloca(intTy));
            }

            return output.AsReadOnly();
        }

        private void ApplyFastcallToRemillCall(LLVMValueRef statePtr, IReadOnlyDictionary<RemillRegister, LLVMValueRef> allocaClobberedRegisters, LLVMValueRef caller, LLVMValueRef newIntrinsic, LLVMTypeRef newIntrinsicPrototype)
        {
            // newIntrinsic.GlobalParent.WriteToLlFile("liftedFunction.ll");

            // Position the builder right before the original call.
            builder.PositionBefore(caller);

            // Create the initial argument list.
            var args = new List<LLVMValueRef>();
            var i64Ty = newIntrinsic.GetFunctionCtx().Int64Type;
            args.Add(caller.GetOperand(1)); // call address
            args.Add(builder.BuildLoad2(newIntrinsic.GetFunctionCtx().GetPtrType(), runtime.MemoryPointer.Value)); // mem ptr
            args.AddRange(savedRegisters.Select(x => builder.BuildLoad2(i64Ty, x.GetAddressOf(statePtr, builder)))); // i64 values loaded from the state ptr

            var clobbers = clobberedRegisters.OrderBy(x => parameterizedStateStructure.GetRegisterArgumentIndex(x));
            foreach (var clobberedReg in clobbers)
            {
                // Load the register value from the local state structure.
                var regValue = builder.BuildLoad2(i64Ty, clobberedReg.GetAddressOf(statePtr, builder));

                // Allocate a stack based variable to contain the value of the clobbered register.
                var alloca = allocaClobberedRegisters[clobberedReg];

                // Store the state structure register value to the noalias register pointer parameter.
                builder.BuildStore(regValue, alloca);

                // Add the ptr to the call argument list.
                args.Add(alloca);
            }

            // Finally we insert a call to our new 'dna_call` intrinsic.
            var newCall = builder.BuildCall2(newIntrinsicPrototype, newIntrinsic, args.ToArray(), "dna_call_ptr");

            // After the function call we need to restore the potentially clobbered register values(the noalias ptr params)
            // back into the state structure.
            foreach(var clobberedReg in clobbers)
            {
                // Get the noalias register pointer from the current function.
                var newCallFunc = newCall.GetFunction();
                var regPtr = allocaClobberedRegisters[clobberedReg];

                // Load the register value from the pointer parameter.
                var regValue = builder.BuildLoad2(i64Ty, regPtr);

                // Get the state structure pointer for the register.
                var registerStatePtr = clobberedReg.GetAddressOf(statePtr, builder);

                // Store the value back into the state structure.
                builder.BuildStore(regValue, registerStatePtr);
            }

            // Delete the old call now.
            caller.ReplaceAllUsesWith(newCall);
            caller.InstructionEraseFromParent();
        }
    }
}
