using Dna.BinaryTranslator.X86;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Unsafe
{
    /// <summary>
    /// Class for converting remill's state structure parameter to a 
    /// set of integer parameters.
    /// 
    /// Note: For now we only add general purpose registers as arguments.
    /// TODO: Add all potentially necessary arguments.
    /// </summary>
    public class ParameterizedStateStructure
    {
        private readonly RemillArch arch;

        private readonly bool addMemoryPtr;

        private readonly LLVMBuilderRef builder;

        private readonly IReadOnlySet<RemillRegister> rootRegisters;

        /// <summary>
        /// A mapping of (remill register, zero based index of the position in the prototype argument list).
        /// </summary>
        public IReadOnlyDictionary<RemillRegister, int> RegisterArgumentIndices { get; }

        public IReadOnlyList<RemillRegister> OrderedRegisterArguments => RegisterArgumentIndices.OrderBy(x => x.Value).Select(x => x.Key).ToList().AsReadOnly();

        /// <summary>
        /// The parameterized LLVM IR function prototype. 
        /// This is basically a typedef of void @func(i64 rcx, i64 rdx, ... general purpose registers ..., optional memory pointer).
        /// The memory pointer is 'optional' since it is only added to the prototype if specified in the utility method for creating
        /// the parameterized state structure.
        /// </summary>
        public LLVMTypeRef ParameterizedFunctionPrototype { get; }

        /// <summary>
        /// The new LLVM IR function with the updated prototype.
        /// </summary>
        public LLVMValueRef OutputFunction { get; private set; }

        public static ParameterizedStateStructure CreateFromFunction(RemillArch arch, LLVMValueRef function, bool addMemoryPtr = true)
        {
            var output = new ParameterizedStateStructure(arch, function, addMemoryPtr);
            output.Create(function);
            return output;
        }

        public ParameterizedStateStructure(RemillArch arch, LLVMValueRef function, bool addMemoryPtr)
        {
            this.arch = arch;
            this.addMemoryPtr = addMemoryPtr;
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
            rootRegisters = ArchRegisters.GetRootGprs(arch);
            RegisterArgumentIndices = ApplyParameterOrderingToRegisters(rootRegisters);

            // Create a prototype for the new function we are creating.
            // This takes all registers as integers, aswell as an optional
            // single ptr(last argument) for the remill memory pointer.
            ParameterizedFunctionPrototype = CreateParameterizedPrototype(function.GetFunctionCtx(), RegisterArgumentIndices, addMemoryPtr);
        }

        private static IReadOnlyDictionary<RemillRegister, int> ApplyParameterOrderingToRegisters(IReadOnlySet<RemillRegister> unorderedRegisters)
        {
            // Build a mapping of <register, argument index>.
            var argumentIndices = new Dictionary<RemillRegister, int>();

            // Apply a deterministic ordering to the registers.
            var orderedRegisters = unorderedRegisters.OrderBy(x => x.Name).ToList();

            // Ensure that RSP is always the first argument.
            var rsp = orderedRegisters.Single(x => x.Name == "RSP");
            var rspIndex = orderedRegisters.IndexOf(rsp);
            if(rspIndex != 0)
            {
                // Move the register at index zero into RSP's old position.
                orderedRegisters[rspIndex] = orderedRegisters[0];

                // Move RSP into index zero.
                orderedRegisters[0] = rsp;
            }

            // Assign a parameter index to each register.
            for (int i = 0; i < orderedRegisters.Count; i++)
            {
                var reg = orderedRegisters.ElementAt(i);
                argumentIndices.Add(reg, i);
            }

            return argumentIndices.AsReadOnly();
        }

        private void Create(LLVMValueRef inputFunction)
        {
            // Create the new function.
            var newFunc = CreateNewFunction(inputFunction.GlobalParent, ParameterizedFunctionPrototype, inputFunction.Name);
            newFunc.AppendBasicBlock("entry_block");

            // Allocate a local state structure within the newly created function.
            var localStateStruct = CreateLocalStateStructure(newFunc);

            // At this point the new function we created has a `noalias` ptr argument for each root(e.g. RAX) register.
            // Now we must copy the value of each register into the local state structure.
            CopyParameterizedStateIntoStructure(newFunc, localStateStruct);

            // Insert a call to the original function. This takes the local state structure 
            // we created as an argument.
            var call = CreateCallToOriginalFunction(inputFunction, newFunc, localStateStruct);
            builder.BuildRetVoid();

            // Inline the call to the original function.
            inputFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");

            LLVMCloning.InlineFunction(inputFunction);

            // Finally delete the function.
            inputFunction.DeleteFunction();

            // Set the output function.
            OutputFunction = newFunc;
        }

        public static LLVMTypeRef CreateParameterizedPrototype(LLVMContextRef ctx, IReadOnlyDictionary<RemillRegister, int> registerArgumentIndices, bool addMemoryPtr)
        {
            // Create a list of function argument types.
            // The last index is the remill memory ptr.
            var ptrTy = ctx.GetPtrType();
            var paramTypes = new List<LLVMTypeRef>();

            // Convert the dictionary to an order list of registers.
            var orderedRegisters = registerArgumentIndices.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            // Assign a parameter index to each register.
            for (int i = 0; i < orderedRegisters.Count; i++)
            {
                var reg = orderedRegisters[i];
                paramTypes.Add(ctx.GetIntTy((uint)(reg.Size * 8)));
            }

            // Add the last argument: the remill memory ptr.
            if(addMemoryPtr)
                paramTypes.Add(ptrTy);

            // Create the LLVM prototype for our new function.
            var prototype = LLVMTypeRef.CreateFunction(ctx.VoidType, paramTypes.ToArray());
            return prototype;
        }

        private LLVMValueRef CreateNewFunction(LLVMModuleRef module, LLVMTypeRef funcPrototype, string namePostfix)
        {
            // Create a new function with the prototype we created.
            var newFunction = module.AddFunction($"Parameterized_{namePostfix}", funcPrototype);

            // Assign readable names to each parameter.
            // TODO: Make things `noalias`.
            if (addMemoryPtr)
            {
                var memParam = newFunction.GetParams().Last();
                memParam.Name = "memory";
                LLVMCloning.AddParamAttr(newFunction, newFunction.ParamsCount - 1, AttrKind.NoAlias);
            }

            foreach (var indexMapping in RegisterArgumentIndices)
            {
                var regParam = newFunction.GetParams()[indexMapping.Value];
                regParam.Name = indexMapping.Key.Name;
            }

            return newFunction;
        }

        private LLVMValueRef CreateLocalStateStructure(LLVMValueRef function)
        {
            // Position the builder at the very start of the function.
            var entryBlock = function.EntryBasicBlock;
            builder.Position(entryBlock, entryBlock.FirstInstruction);

            // Allocate a local state structure at the start of the function.
            var stateStructType = arch.StateStructType;
            var localStateStruct = builder.BuildAlloca(stateStructType, "local_state_struct");
            return localStateStruct;
        }

        private void CopyParameterizedStateIntoStructure(LLVMValueRef function, LLVMValueRef statePtr)
        {
            // For each register ptr argument, copy it's value into the local state structure.
            // Note: The builder should be correctly positioned before this method is called.
            var registerValueMap = new Dictionary<RemillRegister, LLVMValueRef>();
            foreach(var (reg, argIndex) in RegisterArgumentIndices.OrderBy(x => x.Value))
            {
                // Get the register value.
                var regValue = GetRegParam(reg, function);

                // Copy the register value to the corresponding position in our newly allocated state structure.
                builder.BuildStore(regValue, reg.GetAddressOf(statePtr, builder));
            }
        }

        public uint GetRegisterArgumentIndex(RemillRegister reg) => (uint)RegisterArgumentIndices[reg];

        public LLVMValueRef GetRegParam(RemillRegister reg, LLVMValueRef func) => func.GetParam((uint)RegisterArgumentIndices[reg]);

        private LLVMValueRef GetMemoryParamPtr(LLVMValueRef function)
        {
            if (addMemoryPtr == false)
                throw new InvalidOperationException($"Memory pointer was not requested.");
            return function.GetParams().Last();
        }

        private LLVMValueRef CreateCallToOriginalFunction(LLVMValueRef originalFunc, LLVMValueRef newFunc, LLVMValueRef localStateStruct)
        {
            // Create the list of arguments.
            var callArgs = new List<LLVMValueRef>();

            // Create the list of arguments: %local_state_struct, %program_counter, and %memory.
            callArgs.Add(localStateStruct);
            callArgs.Add(GetRegParam(rootRegisters.Single(x => x.Name == "RIP"), newFunc));
            callArgs.Add(addMemoryPtr ? GetMemoryParamPtr(newFunc) : LLVMValueRef.CreateConstPointerNull(newFunc.GetPtrType()));

            // Call the original function.
            return builder.BuildCall2(arch.LiftedFunctionType, originalFunc, callArgs.ToArray(), "call_original");
        }

    }
}
