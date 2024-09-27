using Dna.Extensions;
using Dna.BinaryTranslator.Runtime;
using Dna.BinaryTranslator.Unsafe;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    public class SafeTrivialRuntimeImplementer
    {
        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        public static ILLVMRuntime Implement(LLVMModuleRef module)
        {
            // Implement the runtime.
            var implementer = new SafeTrivialRuntimeImplementer(module);
            implementer.Insert();

            return null;
        }

        private unsafe SafeTrivialRuntimeImplementer(LLVMModuleRef module)
        {
            this.module = module;
            builder = module.Context.CreateBuilder();
        }

        private void Insert()
        {
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

                // Implement the write intrinsics.
                if (function.Name.Contains(writePrefix))
                    ImplementMemWrite(function);
                // Implement the write intrinsic.
                else if (function.Name.Contains(readPrefix))
                    ImplementMemRead(function);
                else
                    throw new InvalidOperationException($"Cannot implement memory intrinsic: {function}");

                // Mark the function for inlining.
                LLVMCloning.InlineFunction(function);
            }
        }

        private void ImplementMemWrite(LLVMValueRef function)
        {
            // Use GEP to create an i8* pointer to memory[address].
            var storeAddr = function.Params[1];
            var storePointer = builder.BuildIntToPtr(storeAddr, module.GetPtrType());

            // Bitcast the i8* pointer to the type of the value being stored.
            var storeValue = function.Params[2];

            // Store the value to memory.
         //   EmitMemoryReorderBarrier(builder);
            var store = builder.BuildStore(storeValue, storePointer);
            store.Volatile = true;
          //  EmitMemoryReorderBarrier(builder);

            // Return a meaningless ptr.
            builder.BuildRet(function.GetParam(0));
        }

        private void ImplementMemRead(LLVMValueRef function)
        {
            // Use GEP to create an i8* pointer to memory[address].
            var loadPointer = builder.BuildIntToPtr(function.GetParam(1), module.GetPtrType());

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
           // EmitMemoryReorderBarrier(builder);
            var loadValue = builder.BuildLoad2(valueType, loadPointer);
            loadValue.Volatile = true;
          //  EmitMemoryReorderBarrier(builder);
            builder.BuildRet(loadValue);
        }

        /// <summary>
        /// Creates an LLVM IR function call to inline assembly which essentially prevents the reordering of memory accesses.
        /// </summary>
        /// <param name="builder"></param>
        private void EmitMemoryReorderBarrier(LLVMBuilderRef builder)
        {
            var inlineAssembly = LLVMValueRef.CreateConstInlineAsm(GetInlineAsmPrototype(), "", "~{memory},~{dirflag},~{fpsr},~{flags}", true, false);
            builder.BuildCall2(GetInlineAsmPrototype(), inlineAssembly, new LLVMValueRef[] { });
        }

        private LLVMTypeRef GetInlineAsmPrototype()
        {
            var ctx = module.GetCtx();
            var types = new List<LLVMTypeRef>();
            return LLVMTypeRef.CreateFunction(ctx.VoidType, types.ToArray());
        }
    }
}
