using Dna.BinaryTranslator.Runtime;
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

namespace Dna.BinaryTranslator.Unsafe
{
    /// <summary>
    /// Class for inserting implementations to basic remill LLVM intrinsics(e.g. remill_compute_parity).
    /// </summary>
    public class UnsafeRuntimeImplementer
    {
        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private LLVMValueRef memoryPtr;

        public static ILLVMRuntime Implement(LLVMModuleRef module)
        {
            // Implement the runtime.
            var implementer = new UnsafeRuntimeImplementer(module);
            implementer.Insert();

            return new UnsafeRuntime(implementer.memoryPtr);
        }

        private unsafe UnsafeRuntimeImplementer(LLVMModuleRef module)
        {
            this.module = module;
            builder = module.Context.CreateBuilder();
        }

        private void Insert()
        {
            // Create an i64* pointer to store memory.
            var memoryPtrType = module.Context.GetPtrType();
            memoryPtr = module.AddGlobal(memoryPtrType, "memory");
            memoryPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
            var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(memoryPtrType);
            memoryPtr.Initializer = memoryPtrNull;

            // Insert concrete implementations for remill flag and comparison intrinsics.
            SharedRuntimeImplementer.ImplementFlagAndCmpIntrinsics(module, builder);

            // Insert concrete implementations for remill memory intrinsics.
            ImplementMemoryIntrinsics();

            // Quick hack to implement undef values.
            SharedRuntimeImplementer.ImplementUndef8(module, builder);
        }

        private unsafe void ImplementMemoryIntrinsics()
        {
            var writePrefix = "__remill_write_memory_";
            var readPrefix = "__remill_read_memory";

            var memFunctions = module.GetFunctions()
                .Where(x => x.Name.Contains(writePrefix) || x.Name.Contains(readPrefix))
                .ToList();

            foreach (var function in memFunctions)
            {
                // Set the function linkage to internal.
                LLVM.SetLinkage(function, LLVMLinkage.LLVMInternalLinkage);

                // Create a single basic block for the implementation.
                var block = function.AppendBasicBlock("entry");
                builder.PositionAtEnd(block);

                // Load the memory pointer into a local variable.
                var localMemPtr = builder.BuildLoad2(module.Context.GetPtrType(), memoryPtr);

                // Implement the write intrinsics.
                if (function.Name.Contains(writePrefix))
                    ImplementMemWrite(function, localMemPtr);
                // Implement the write intrinsic.
                else if (function.Name.Contains(readPrefix))
                    ImplementMemRead(function, localMemPtr);
                else
                    throw new InvalidOperationException($"Cannot implement memory intrinsic: {function}");

                // Mark the function for inlining.
                LLVMCloning.InlineFunction(function);
            }
        }

        private void ImplementMemWrite(LLVMValueRef function, LLVMValueRef memPtr)
        {
            // Use GEP to create an i8* pointer to memory[address].
            var storeAddr = function.GetParams()[1];
            var storePointer = builder.BuildInBoundsGEP2(module.Context.GetInt8Ty(), memPtr, new LLVMValueRef[] { storeAddr });

            // Bitcast the i8* pointer to the type of the value being stored.
            var storeValue = function.GetParams()[2];

            // Store the value to memory.
            builder.BuildStore(storeValue, storePointer);

            // Return a meaningless ptr.
            builder.BuildRet(function.GetParam(0));
        }

        private void ImplementMemRead(LLVMValueRef function, LLVMValueRef memPtr)
        {
            // Use GEP to create an i8* pointer to memory[address].
            var loadPointer = builder.BuildInBoundsGEP2(module.Context.GetInt8Ty(), memPtr, new LLVMValueRef[] { function.GetParam(1) });

            // Note: This is a hack required to get the return type using the APIs we have access to.
            LLVMTypeRef valueType = function.Name.Replace("__remill_read_memory_", "") switch
            {
                "8" => module.Context.GetIntTy(8),
                "16" => module.Context.GetIntTy(16),
                "32" => module.Context.GetIntTy(32),
                "64" => module.Context.GetIntTy(64),
                "f32" => module.Context.FloatType,
                "f64" => module.Context.DoubleType,
                "f80" => module.Context.X86FP80Type,
                _ => throw new InvalidOperationException($"Memory intrinsic not supported: {function}")
            };

            // Dereference the pointer and return the value.
            var loadValue = builder.BuildLoad2(valueType, loadPointer);
            builder.BuildRet(loadValue);
        }
    }
}
