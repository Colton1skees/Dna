using Dna.BinaryTranslator.Lifting;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    public class SehLocalEscapeImplementer
    {
        private readonly LLVMValueRef liftedFunction;

        private readonly IReadOnlyList<LiftedFilterFunction> liftedFilterFunctions;

        private readonly LLVMContextRef ctx;

        private readonly LLVMBuilderRef builder;

        private readonly SehIntrinsicBuilder intrinsicBuilder;

        public static void Implement(LLVMValueRef liftedFunction, IReadOnlyList<LiftedFilterFunction> liftedFilterFunctions)
            => new SehLocalEscapeImplementer(liftedFunction, liftedFilterFunctions).Implement();

        private SehLocalEscapeImplementer(LLVMValueRef liftedFunction, IReadOnlyList<LiftedFilterFunction> liftedFilterFunctions)
        {
            this.liftedFunction = liftedFunction;
            this.liftedFilterFunctions = liftedFilterFunctions;
            ctx = liftedFunction.GetFunctionCtx();
            builder = LLVMBuilderRef.Create(ctx);
            intrinsicBuilder = new SehIntrinsicBuilder(liftedFunction.GlobalParent, builder);
        }

        private void Implement()
        {
            liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
            // If the function has no SEH filters then do nothing.
            if (!liftedFilterFunctions.Any())
                return;

            // Store the value of `rsp` and `imagebase` to local variables(allocas) which are then escaped using the llvm.localescape macro.
            // Then we update stores to the global variables to instead store to our local variables.
            EscapeLocals();

            // For each filter function:
            //  - Insert a @recoverfp and @llvm.localrecover intrinsic to recover pointers to the newly escaped local variables.
            //  - Load the value of both the escaped rsp and imgbase local variables.
            //  - Replace and delete the old loads of the rsp and imgbase global variables.
            RecoverEscapedLocals();
        }

        private void EscapeLocals()
        {
            // Insert an alloca for both `rsp` and `imagebase`
            builder.PositionBefore(liftedFunction.EntryBasicBlock.FirstInstruction);
            var escapedRsp = builder.BuildAlloca(ctx.Int64Type, "escapedRsp");
            var escapedImagebase = builder.BuildAlloca(ctx.Int64Type, "escapedImagebase");

            // Emit a @llvm.localescape intrinsic invocation. https://llvm.org/docs/LangRef.html#llvm-localescape-and-llvm-localrecover-intrinsics
            // NOTE: You cannot reorder these operands. Because localescape is zero indexed, rsp must stay at index 0, and imagebase must always stay at index 1.
            intrinsicBuilder.EmitSehLocalEscape(new List<LLVMValueRef>() { escapedRsp, escapedImagebase});
            
            foreach(var filter in liftedFilterFunctions)
            {
                // Get all stores to the `rsp` global variable within the current function.
                var rspGlobalStores = filter
                    .RspGlobal
                    .GetUsers()
                    .Where(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionParent.Parent == liftedFunction && x.InstructionOpcode == LLVMOpcode.LLVMStore)
                    .ToList();

                // Get all stores to the `imgbase` global variable within the current function.
                // Also note that each filter function gets it's own global variable.
                var imagebaseGlobalStores = filter
                    .ImagebaseGlobal
                    .GetUsers()
                    .Where(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionParent.Parent == liftedFunction && x.InstructionOpcode == LLVMOpcode.LLVMStore)
                    .ToList();

                // For each store to the global rsp/imgbase variable, replace it with a store to our local escaped variable.
                foreach (var store in rspGlobalStores)
                    store.SetOperand(1, escapedRsp);
                foreach (var store in imagebaseGlobalStores)
                    store.SetOperand(1, escapedImagebase);
            }
        }

        private void RecoverEscapedLocals()
        {
            foreach(var filter in liftedFilterFunctions)
            {
                filter.LlvmFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                // Emit @llvm.eh.recoverfp. Note that argument 1 of any filter function is the parent functions frame pointer.
                builder.PositionBefore(filter.LlvmFunction.EntryBasicBlock.FirstInstruction);
                var recoverFp = intrinsicBuilder.EmitSehRecoverFp(liftedFunction, filter.LlvmFunction.GetParam(1));

                // Recover a ptr to (i64 rsp, i64 imagebase) using the @llvm.localrecover intrinsic.
                var localRecover = intrinsicBuilder.EmitSehLocalRecover(liftedFunction, recoverFp, LLVMValueRef.CreateConstInt(ctx.Int32Type, 0));

                // Fetch rsp from the parent function's stack frame.
                var rsp = builder.BuildLoad2(ctx.Int64Type, localRecover, "rsp");

                // Fetch imagebase from the parent function's stack frame. Note that gep at index 8 marks the start of the imagebase pointer, because it's the second alloca in a list of two [alloca i64]s.
                var gep = builder.BuildInBoundsGEP2(ctx.Int8Type, localRecover, new LLVMValueRef[] { LLVMValueRef.CreateConstInt(ctx.Int64Type, 8) });
                var imgbase = builder.BuildLoad2(ctx.Int64Type, gep, "imgbase");

                //var users = filter.RspGlobal.GetUsersAsValues().ToList();
                var users = CFGApi.GetValueUsers(filter.RspGlobal);

                foreach(var user in users.Where(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind))
                {
                    Console.WriteLine($"\nOpcode: {user.InstructionOpcode}\n");
                }

                // For the current filter function, fetch the single load of the rsp spill global variable.
                var rspLoad = filter
                    .RspGlobal
                    .GetUsers()
                    .Single(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionParent.Parent == filter.LlvmFunction && x.InstructionOpcode == LLVMOpcode.LLVMLoad);

                // For the current filter function, fetch the single load of the imagebase spill global variable.
                var imgbaseLoad = filter
                    .ImagebaseGlobal
                    .GetUsers()
                    .Single(x => x.Kind == LLVMValueKind.LLVMInstructionValueKind && x.InstructionParent.Parent == filter.LlvmFunction && x.InstructionOpcode == LLVMOpcode.LLVMLoad);

                // Replace and delete the global rsp variable load.
                rspLoad.ReplaceAllUsesWith(rsp);
                rspLoad.InstructionEraseFromParent();

                // Replace and delete the global imagebase variable load.
                imgbaseLoad.ReplaceAllUsesWith(imgbase);
                imgbaseLoad.InstructionEraseFromParent();
            }
        }
    }
}
