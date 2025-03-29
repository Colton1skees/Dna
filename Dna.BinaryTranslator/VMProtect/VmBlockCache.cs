using Dna.BinaryTranslator;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public record PartialLiftedBlock(BasicBlock<VmHandler> Block, LLVMValueRef Function);

    public class VmBlockCache
    {
        public readonly Dictionary<VmHandler, PartialLiftedBlock> vmBlockLlvmCache = new();

        public LLVMModuleRef CacheModule { get; }

        public VmBlockCache(LLVMContextRef ctx)
        {
            CacheModule = ctx.CreateModuleWithName("VmBlockCache");
        }

        public VmBlockCache(LLVMContextRef ctx, LLVMModuleRef module)
        {
            CacheModule = module;
        }

        public void AddBlock(BasicBlock<VmHandler> block, LLVMValueRef function)
        {
            var handler = block.Instructions.First();
            vmBlockLlvmCache.Add(handler, new PartialLiftedBlock(block, function));
        }

        public bool ContainsBlock(VmHandler handler)
        {
            return vmBlockLlvmCache.ContainsKey(handler);
        }

        public PartialLiftedBlock GetBlock(VmHandler handler)
        {
            return vmBlockLlvmCache[handler];
        }

        public PartialLiftedBlock ClonePartialBlockIntoModule(VmHandler handler, LLVMModuleRef outModule)
        {
            CacheModule.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
            // Fetch the handler function.
            var partialBlock = GetBlock(handler);

            Debug.Assert(partialBlock.Function.GlobalParent.Handle == CacheModule.Handle);

            // Add a new function with the exact same prototype as the handler function.
            var newFunction = CacheModule.AddFunction(partialBlock.Function.Name + "_from_cache", partialBlock.Function.GetFunctionPrototype());

            // Build a call to the original handler function, followed by a RET.
            var builder = LLVMBuilderRef.Create(CacheModule.Context);
            var entryBb = newFunction.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBb);
            var call = builder.BuildCall2(partialBlock.Function.GetFunctionPrototype(), partialBlock.Function, newFunction.GetParams());
            builder.BuildRetVoid();

            // Inlines ALL calls to the handler function.
            // TODO: Inline only the call we just created. We just need to add a pinvoke import for this.
            LLVMCloning.InlineFunction(partialBlock.Function);

            CacheModule.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // Move the newly created function into the target module.
            newFunction = FunctionIsolator.IsolateFunctionInto(outModule, newFunction);
            return new PartialLiftedBlock(partialBlock.Block, newFunction);
        }
    }
}
