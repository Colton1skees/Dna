using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Lifting
{
    public class SehIntrinsicBuilder
    {
        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        public SehIntrinsicBuilder(LLVMModuleRef module, LLVMBuilderRef builder)
        {
            this.module = module;
            this.builder = builder;
        }

        public LLVMValueRef CreateMsvcPersonalityFunction()
        {
            // Create the function type.
            var fnType = LLVMTypeRef.CreateFunction(module.GetCtx().Int32Type, Array.Empty<LLVMTypeRef>(), true);

            // Create the function in the module.
            var func = GetOrCreate("__C_specific_handler", fnType);

            LLVMCloning.MakeDsoLocal(func, true);

            return func;
        }

        public LLVMValueRef EmitSehTryScopeBegin(LLVMBasicBlockRef tryBlock, LLVMBasicBlockRef catchBlock)
        {
            // Create the function type.
            var fnType = LLVMTypeRef.CreateFunction(module.GetCtx().VoidType, Array.Empty<LLVMTypeRef>(), false);

            // Create the function in the module.
            var func = GetOrCreate("llvm.seh.try.begin", fnType);

            return builder.BuildInvoke2(fnType, func, Array.Empty<LLVMValueRef>(), tryBlock, catchBlock);
        }

        public LLVMValueRef EmitSehTryScopeEnd(LLVMBasicBlockRef tryEndBlock, LLVMBasicBlockRef catchBlock)
        {
            // Create the function type.
            var fnType = LLVMTypeRef.CreateFunction(module.GetCtx().VoidType, Array.Empty<LLVMTypeRef>(), false);

            // Create the function in the module.
            var func = GetOrCreate("llvm.seh.try.end", fnType);

            return builder.BuildInvoke2(fnType, func, Array.Empty<LLVMValueRef>(), tryEndBlock, catchBlock);
        }

        public unsafe LLVMValueRef EmitCatchSwitch(LLVMBasicBlockRef catchPad)
        {
            var swtch = (LLVMValueRef)LLVM.BuildCatchSwitch(builder, null, null, 1, new MarshaledString("ctswitch"));
            LLVM.AddHandler(swtch, catchPad);
            return swtch;
        }

        public unsafe LLVMValueRef EmitCatchPad(LLVMValueRef catchPadValue, LLVMValueRef filterFunction)
        {
            var args = new LLVMValueRef[] { filterFunction };
            fixed (LLVMValueRef* pArgs = args)
            {
                return LLVM.BuildCatchPad(builder, catchPadValue, (LLVMOpaqueValue**)pArgs, (uint)args.Length, new MarshaledString("ctpad"));
            }
        }

        public unsafe LLVMValueRef EmitCatchRet(LLVMValueRef catchPadValue, LLVMBasicBlockRef exceptBlock)
        {
            return LLVM.BuildCatchRet(builder, catchPadValue, exceptBlock);
        }

        public LLVMValueRef EmitEhGetExceptionCode(LLVMValueRef token)
        {
            // Create the function type.
            var fnType = LLVMTypeRef.CreateFunction(module.GetCtx().Int32Type, new LLVMTypeRef[] { token.TypeOf }, false);

            // Create the function in the module.
            var func = GetOrCreate("llvm.eh.exceptioncode", fnType);

            return builder.BuildCall2(fnType, func, new LLVMValueRef[] { token});
        }

        public LLVMValueRef EmitSehLocalEscape(IEnumerable<LLVMValueRef> allocas)
        {
            // Create the function type.
            var fnType = LLVMTypeRef.CreateFunction(module.GetCtx().VoidType, Array.Empty<LLVMTypeRef>(), true);

            // Create the function in the module.
            var func = GetOrCreate("llvm.localescape", fnType);

            // Invoke localescape.
            return builder.BuildCall2(fnType, func, allocas.ToArray());
        }


        public LLVMValueRef EmitSehRecoverFp(LLVMValueRef parentFunction, LLVMValueRef framePtr)
        {
            // Create the function type.
            var ptrType = module.GetCtx().GetPtrType();
            var fnType = LLVMTypeRef.CreateFunction(ptrType, new LLVMTypeRef[] { ptrType, ptrType }, false);

            // Create the function in the module.
            var func = GetOrCreate("llvm.eh.recoverfp", fnType);

            // Invoke recoverfp.
            return builder.BuildCall2(fnType, func, new LLVMValueRef[] { parentFunction, framePtr });
        }

        public LLVMValueRef EmitSehLocalRecover(LLVMValueRef parentFunction, LLVMValueRef recoverfpResult, LLVMValueRef index)
        {
            // Create the function type.
            var ptrType = module.GetCtx().GetPtrType();
            var fnType = LLVMTypeRef.CreateFunction(ptrType, new LLVMTypeRef[] { ptrType, ptrType, module.GetCtx().Int32Type }, false);

            // Create the function in the module.
            var func = GetOrCreate("llvm.localrecover", fnType);

            // Invoke localrecover.
            return builder.BuildCall2(fnType, func, new LLVMValueRef[] { parentFunction, recoverfpResult, index });
        }

        private LLVMValueRef GetOrCreate(string name, LLVMTypeRef fnType)
        {
            var target = module.GetFunctions().SingleOrDefault(x => x.Name == name);
            if (target.Handle == nint.Zero)
                target = module.AddFunction(name, fnType);
            return target;
        }
    }
}
