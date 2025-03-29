using Dna.BinaryTranslator.Unsafe;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    public class SafeExecutableRuntimeImplementer
    {
        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly ParameterizedStateStructure parameterizedStateStructure;

        private readonly IReadOnlyDictionary<ulong, ulong> callKeyToStubOffsetMapping;

        HashSet<Instruction> callInsts;

        private dgIsInstructionInsideOfTryStatement isInstructionInAnyTryStatement;

        private readonly LLVMBuilderRef builder;

        private LLVMTypeRef prototype;

        public LLVMValueRef OutputFunction { get; private set; }

        public uint DispatcherArgIndex { get; }

        public uint ImageBaseArgIndex { get; }

        public static SafeExecutableRuntimeImplementer Implement(RemillArch arch, LLVMValueRef function, ParameterizedStateStructure parameterizedStateStructure, IReadOnlyDictionary<ulong, ulong> callKeyToStubOffsetMapping, HashSet<Instruction> callInsts, dgIsInstructionInsideOfTryStatement getIsInstructionInAnyTryStatement)
            => new SafeExecutableRuntimeImplementer(arch, function, parameterizedStateStructure, callKeyToStubOffsetMapping, callInsts, getIsInstructionInAnyTryStatement).Implement(function);

        private SafeExecutableRuntimeImplementer(RemillArch arch, LLVMValueRef function, ParameterizedStateStructure parameterizedStateStructure, IReadOnlyDictionary<ulong, ulong> callKeyToStubOffsetMapping, HashSet<Instruction> callInsts, dgIsInstructionInsideOfTryStatement getIsInstructionInAnyTryStatement)
        {
            ctx = function.GetFunctionCtx();
            this.arch = arch;
            this.parameterizedStateStructure = parameterizedStateStructure;
            this.callKeyToStubOffsetMapping = callKeyToStubOffsetMapping;
            this.callInsts = callInsts;
            this.isInstructionInAnyTryStatement = getIsInstructionInAnyTryStatement;
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());

            var startIndex = (uint)parameterizedStateStructure.OrderedRegisterArguments.Count;
            DispatcherArgIndex = startIndex;
            ImageBaseArgIndex = startIndex + 1;
        }

        private SafeExecutableRuntimeImplementer Implement(LLVMValueRef function)
        {
            // Create a new function.
            function = Create(function);


            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            ImplementCall(function, function.GlobalParent);
            ImplementReturn(function, function.GlobalParent);
            ImplementError(function.GlobalParent);
            ImplementVirtualDispatch(function);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            OutputFunction = function;
            return this;
        }

        private LLVMValueRef Create(LLVMValueRef inputFunction)
        {
            // Create the final, executable lifted function prototype.
            this.prototype = CreatePrototype();

            // Create a new function with the newly created prototype.
            var newFunc = inputFunction.GlobalParent.AddFunction($"Executable_{inputFunction.Name.Replace("@", "")}", prototype);
            newFunc.AppendBasicBlock("entry_block");
            builder.PositionAtEnd(newFunc.EntryBasicBlock);

            // Call the original lifted function, while passing in all register arguments.
            var call = CreateCallToOriginalFunction(inputFunction, newFunc);

            // Ret void.
            builder.BuildRetVoid();

            LLVMCloning.InlineFunction(inputFunction);

            // Finally delete the function.
            inputFunction.DeleteFunction();

            foreach (var (reg, index) in parameterizedStateStructure.RegisterArgumentIndices)
            {
                var p = newFunc.GetParam((uint)index);
                p.Name = reg.Name;
            }

            var dispatchParam = newFunc.GetParam(DispatcherArgIndex);
            dispatchParam.Name = "dispatcher_key";

            var ImageBaseParam = newFunc.GetParam(ImageBaseArgIndex);
            ImageBaseParam.Name = "image_base";

            newFunc.GlobalParent.WriteToLlFile("translatedFunction.ll");

            return newFunc;
        }

        private LLVMTypeRef CreatePrototype()
        {
            // We start off the argument list with a set of all registers arguments as integers.
            var argTypes = new List<LLVMTypeRef>();
            foreach (var reg in parameterizedStateStructure.OrderedRegisterArguments)
            {
                argTypes.Add(ctx.GetIntTy(reg.Size * 8));
            }

            var i64Ty = ctx.GetInt64Ty();
            // Next we add an i64 for the virtual dispatcher key.
            argTypes.Add(i64Ty);
            // Next we add an i64 for the image base.
            argTypes.Add(i64Ty);

            // Create and return the function prototype.
            var prototype = LLVMTypeRef.CreateFunction(ctx.VoidType, argTypes.ToArray());
            return prototype;
        }

        private LLVMValueRef CreateCallToOriginalFunction(LLVMValueRef originalFunc, LLVMValueRef newFunc)
        {
            // Create the list of arguments, which is just all registers passed by value here.
            var callArgs = parameterizedStateStructure.OrderedRegisterArguments.Select(x => parameterizedStateStructure.GetRegInputParam(x, newFunc));

            // Call the original function.
            return builder.BuildCall2(parameterizedStateStructure.ParameterizedFunctionPrototype, originalFunc, callArgs.ToArray(), "");
        }


        private void ImplementCall(LLVMValueRef newFunc, LLVMModuleRef module)
        {
            var callIntrinsic = module.GetFunctions().SingleOrDefault(x => x.Name.Contains("remill_function_call"));
            if (callIntrinsic == null)
                return;

            Call(newFunc, callIntrinsic);
        }

        private void ImplementReturn(LLVMValueRef newFunc, LLVMModuleRef module)
        {
            var errorIntrinsic = module.GetFunctions().SingleOrDefault(x => x.Name.Contains("remill_function_return"));
            if (errorIntrinsic == null)
                return;

            Ret(newFunc, errorIntrinsic);
            //var retStubParam = newFunc.GetParam(ImageBaseArgIndex);
            //ImplementMethod(module, errorIntrinsic, retStubParam, "soteria_return");
        }

        private void ImplementError(LLVMModuleRef module)
        {
            var errorIntrinsic = module.GetFunctions().SingleOrDefault(x => x.Name.Contains("__remill_error"));
            if (errorIntrinsic == null)
                return;

            foreach (var caller in RemillUtils.CallersOf(errorIntrinsic))
            {
                var key = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0xDEAD);
                var ptr = LLVMValueRef.CreateConstIntToPtr(key, module.GetCtx().GetPtrType());
                caller.ReplaceAllUsesWith(ptr);
                caller.InstructionEraseFromParent();
            }
        }

        private unsafe void Call(LLVMValueRef newFunc, LLVMValueRef callIntrinsic)
        {
            foreach (var blk in newFunc.GetBlocks().ToList())
            {
                var instructions = blk.GetInstructions().ToList();
                bool shouldDelete = false;
                for (int i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    // If we encounter a call to @__remill_function_call:
                    if (inst.InstructionOpcode == LLVMOpcode.LLVMCall && inst.GetOperand((uint)inst.OperandCount - 1) == callIntrinsic)
                    {
                        // Since we are turning __remill_function_call into a terminating tail call, mark all successors for deletion.
                        shouldDelete = true;
                        // Insert a `ret void` immediately following the call.
                        builder.PositionBefore(instructions[i + 1]);
                        builder.BuildRetVoid();
                        continue;
                    }

                    if (shouldDelete)
                        inst.InstructionEraseFromParent();
                }
            }

            //var prototype = parameterizedStateStructure.ParameterizedFunctionPrototype;
            foreach (var caller in RemillUtils.CallersOf(callIntrinsic))
            {
                // Fetch the local state structure.
                var statePtr = GetLocalStateStructurePtr(caller.GetFunction());

                // Fetch the u64 vm re-entry key from the call intrinsic invocation.
                var ptrCallFromAddress = caller.GetOperand(2);
                if (ptrCallFromAddress.ConstOpcode != LLVMOpcode.LLVMIntToPtr)
                    throw new InvalidOperationException($"Key for call instruction {caller} was not set. Expected inttoptr (constant key).");

                // Fetch the 'call key', which is the ip of the next instruction that is supposed to execute after the call.
                var callFromAddr = ptrCallFromAddress.GetOperand(0).ConstIntZExt;
                var callKey = callInsts.Single(x => x.IP == callFromAddr).NextIP;

                // Load all registers values as integers.
                builder.PositionBefore(caller);
                var regValues = new List<LLVMValueRef>();
                foreach (var register in parameterizedStateStructure.OrderedRegisterArguments)
                {
                    // Compute the register width type.
                    var regType = callIntrinsic.GetFunctionCtx().GetIntTy((uint)(register.Size * 8));

                    // Load the register value from the local state structure.
                    var regValue = builder.BuildLoad2(regType, register.GetAddressOf(statePtr, builder));

                    regValues.Add(regValue);
                }

                // Clang requires that tail calls have exactly the same number of arguments.
                // To support this, we append two bogus values to the register value list,
                // bumping our argument count from 17(musttail target) to 19(# of arguments used by the lifted function).
                regValues.Add(regValues.Last());
                regValues.Add(regValues.Last());

                // Compute the vm call stub address.
                var i64Key = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, callKeyToStubOffsetMapping[callKey]);
                var addr = builder.BuildAdd(caller.GetFunction().GetParam(ImageBaseArgIndex), i64Key);

                // Dereference the function pointer from the binary section.
                var funcPtr = builder.BuildIntToPtr(addr, ctx.GetPtrType());
                funcPtr = builder.BuildLoad2(ctx.GetInt64Ty(), funcPtr);
                funcPtr = builder.BuildAdd(funcPtr, caller.GetFunction().GetParam(ImageBaseArgIndex));

                funcPtr = builder.BuildIntToPtr(funcPtr, ctx.GetPtrType());

                var callResult = builder.BuildCall2(prototype, funcPtr, regValues.ToArray(), "");
                caller.ReplaceAllUsesWith(LLVMValueRef.CreateConstPointerNull(callIntrinsic.GetPtrType()));
                caller.InstructionEraseFromParent();

                // If the call instruction is not inside of a TRY statement then we make it a terminating tail call.
                // Note that we cannot use a terminating tail calls if the call from address is inside of a try statement,
                // because that breaks LLVM's SEH implementation.
                if (isInstructionInAnyTryStatement(callFromAddr))
                    Debugger.Break();
                else
                    LLVMCloning.MakeMustTail(callResult);
            }
        }

        private unsafe void Ret(LLVMValueRef newFunc, LLVMValueRef callIntrinsic)
        {
            foreach (var blk in newFunc.GetBlocks().ToList())
            {
                var instructions = blk.GetInstructions().ToList();
                bool shouldDelete = false;
                for (int i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    // If we encounter a call to @__remill_function_call:
                    if (inst.InstructionOpcode == LLVMOpcode.LLVMCall && inst.GetOperand((uint)inst.OperandCount - 1) == callIntrinsic)
                    {
                        // Since we are turning __remill_function_call into a terminating tail call, mark all successors for deletion.
                        shouldDelete = true;
                        // Insert a `ret void` immediately following the call.
                        builder.PositionBefore(instructions[i + 1]);
                        builder.BuildRetVoid();
                        continue;
                    }

                    if (shouldDelete)
                        inst.InstructionEraseFromParent();
                }
            }

            //var prototype = parameterizedStateStructure.ParameterizedFunctionPrototype;
            foreach (var caller in RemillUtils.CallersOf(callIntrinsic))
            {

                // Fetch the local state structure.
                var statePtr = GetLocalStateStructurePtr(caller.GetFunction());

                // Load all registers values as integers.
                builder.PositionBefore(caller);
                var regValues = new List<LLVMValueRef>();
                foreach (var register in parameterizedStateStructure.OrderedRegisterArguments)
                {
                    // Compute the register width type.
                    var regType = callIntrinsic.GetFunctionCtx().GetIntTy((uint)(register.Size * 8));

                    // Load the register value from the local state structure.
                    var regValue = builder.BuildLoad2(regType, register.GetAddressOf(statePtr, builder));

                    regValues.Add(regValue);
                }

                // Clang requires that tail calls have exactly the same number of arguments.
                // To support this, we append two bogus values to the register value list,
                // bumping our argument count from 17(musttail target) to 19(# of arguments used by the lifted function).
                regValues.Add(regValues.Last());
                regValues.Add(regValues.Last());

                // Compute the vm call stub address.
                var max = callKeyToStubOffsetMapping.Values.Max();
                var i64Key = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, max + 8);
                var addr = builder.BuildAdd(caller.GetFunction().GetParam(ImageBaseArgIndex), i64Key);

                // Dereference the function pointer from the binary section.
                var funcPtr = builder.BuildIntToPtr(addr, ctx.GetPtrType());
                funcPtr = builder.BuildLoad2(ctx.GetInt64Ty(), funcPtr);
                funcPtr = builder.BuildAdd(funcPtr, caller.GetFunction().GetParam(ImageBaseArgIndex));

                funcPtr = builder.BuildIntToPtr(funcPtr, ctx.GetPtrType());

                var callResult = builder.BuildCall2(prototype, funcPtr, regValues.ToArray(), "");
                caller.ReplaceAllUsesWith(LLVMValueRef.CreateConstPointerNull(callIntrinsic.GetPtrType()));
                caller.InstructionEraseFromParent();

                // Turn this call into a terminating tail call.
                LLVMCloning.MakeMustTail(callResult);
            }
        }

        private LLVMValueRef GetLocalStateStructurePtr(LLVMValueRef function)
        {
            // Get the local state structure pointer.
            return function.GetInstructions()
                .Single(x => x.InstructionOpcode == LLVMOpcode.LLVMAlloca && x.ToString().Contains("= alloca %struct.State"));
        }

        private void ImplementVirtualDispatch(LLVMValueRef function)
        {
            // Get the global dispatcher key.
            var globalKey = function.GlobalParent.GetGlobals().Single(x => x.Name.Contains("virtual_dispatch_key"));

            var loads = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == globalKey).ToList();
            foreach (var load in loads)
            {
                load.ReplaceAllUsesWith(function.GetParam(DispatcherArgIndex));
                load.InstructionEraseFromParent();
            }

            globalKey.DeleteGlobal();
        }
    }
}
