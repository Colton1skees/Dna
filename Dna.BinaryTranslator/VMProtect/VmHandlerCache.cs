using Dna.BinaryTranslator;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public class VmHandlerCache
    {
        private readonly Dictionary<ulong, FunctionWithStateStructure> handlerRipToLlvmFunction = new();

        public LLVMModuleRef CacheModule;

        public VmHandlerCache(LLVMContextRef context)
        {
            CacheModule = context.CreateModuleWithName("HandlerCache");
        }

        public bool ContainsHandler(ulong rip)
        {
            return handlerRipToLlvmFunction.ContainsKey(rip);
        }

        public FunctionWithStateStructure GetLiftedHandler(ulong rip)
        {
            return handlerRipToLlvmFunction[rip];
        }

        public LLVMValueRef CloneLiftedHandlerIntoModule(ulong rip, LLVMModuleRef outModule)
        {
            var memPtr = CacheModule.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(CacheModule.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }


            // Fetch the handler function.
            var handler = GetLiftedHandler(rip);

            // Add a new function with the exact same prototype as the handler function.
            var newHandler = CacheModule.AddFunction(handler.Function.Name + "_from_cache", handler.ParameterizedStateStructure.ParameterizedFunctionPrototype);

            // Build a call to the original handler function, followed by a RET.
            var builder = LLVMBuilderRef.Create(CacheModule.Context);
            var entryBb = newHandler.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBb);
            var call = builder.BuildCall2(handler.ParameterizedStateStructure.ParameterizedFunctionPrototype, handler.Function, newHandler.GetParams());
            builder.BuildRetVoid();

            // Inlines ALL calls to the handler function.
            // TODO: Inline only the call we just created. We just need to add a pinvoke import for this.
            LLVMCloning.InlineFunction(handler.Function);

            CacheModule.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // Move the newly created function into the target module.
            newHandler = FunctionIsolator.IsolateFunctionInto(outModule, newHandler);
            return newHandler;
        }

        public void AddFunction(ulong handlerRip, FunctionWithStateStructure function)
        {
            handlerRipToLlvmFunction.Add(handlerRip, function);
        }
      
    }
}
