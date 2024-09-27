using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Unsafe
{
    public class ErrorAndReturnImplementer
    {
        private readonly LLVMValueRef function;

        private readonly LLVMBuilderRef builder;

        public static void Implement(LLVMValueRef function) => new ErrorAndReturnImplementer(function).Implement();

        private ErrorAndReturnImplementer(LLVMValueRef function)
        {
            this.function = function;
            builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
        }

        private void Implement()
        {
            ImplementError();
            ImplementReturn();
        }

        private void ImplementError()
        {
            var errorIntrinsic = function.GlobalParent.GetFunctions().SingleOrDefault(x => x.Name.Contains("__remill_error"));
            if (errorIntrinsic == null)
                return;

            var prototype = GetPrototype();
            var newError = function.GlobalParent.AddFunction("dna_error", prototype);
            foreach(var caller in RemillUtils.CallersOf(errorIntrinsic))
            {
                // Replace the call with a call to our intrinsic.
                builder.PositionBefore(caller);
                var callResult = builder.BuildCall2(prototype, newError, new LLVMValueRef[] { }, "dna_err_ptr");
                caller.ReplaceAllUsesWith(callResult);
                caller.InstructionEraseFromParent();
            }
        }

        private void ImplementReturn()
        {
            var errorIntrinsic = function.GlobalParent.GetFunctions().SingleOrDefault(x => x.Name.Contains("remill_function_return"));
            if (errorIntrinsic == null)
                return;

            var prototype = GetPrototype();
            var newError = function.GlobalParent.AddFunction("dna_return", prototype);
            foreach (var caller in RemillUtils.CallersOf(errorIntrinsic))
            {
                // Replace the call with a call to our intrinsic.
                builder.PositionBefore(caller);
                var callResult = builder.BuildCall2(prototype, newError, new LLVMValueRef[] { }, "dna_return_ptr");
                caller.ReplaceAllUsesWith(callResult);
                caller.InstructionEraseFromParent();
            }
        }

        private LLVMTypeRef GetPrototype()
        {
            var ctx = function.GetFunctionCtx();
            var types = new List<LLVMTypeRef>();
            //types.Add(ctx.Int64Type); // address
            //types.Add(ctx.GetPtrType()); // memory ptr

            return LLVMTypeRef.CreateFunction(ctx.GetPtrType(), types.ToArray());
        }
    }
}
