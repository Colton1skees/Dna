using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Runtime
{
    public static class SharedRuntimeImplementer
    {
        public unsafe static void ImplementFlagAndCmpIntrinsics(LLVMModuleRef module, LLVMBuilderRef builder)
        {
            // Get all flag computation intrinsic functions.
            var functions = module.GetFunctions()
                .Where(x => x.Name.Contains("remill_flag_") || x.Name.Contains("__remill_compare_"))
                .ToList();

            foreach (var function in functions)
            {
                // All intrinsics included in this list take the return value as the first argument.
                // Thus we just add a single block which returns the first argument.
                var block = function.AppendBasicBlock("entry");
                builder.PositionAtEnd(block);
                builder.BuildRet(function.Params.First());

                // Mark the intrinsic function for inlining.
                LLVM.SetLinkage(function, LLVMLinkage.LLVMInternalLinkage);
                LLVMCloning.InlineFunction(function);
            }
        }

        public static unsafe void ImplementUndef8(LLVMModuleRef module, LLVMBuilderRef builder)
        {
            var targetFunc = module.GetFunctions().SingleOrDefault(x => x.Name.Contains("__remill_undefined_8"));
            if (targetFunc == null)
                return;

            // Use 0 for undef values. TODO: Provide an actual implementation.
            var callers = RemillUtils.CallersOf(targetFunc);
            foreach (var caller in callers)
            {
                caller.ReplaceAllUsesWith(LLVMValueRef.CreateConstInt(module.GetCtx().Int8Type, 0));
                caller.InstructionEraseFromParent();
            }
        }

    }
}
