using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extensions
{
    public static class LLVMExtensions
    {
        public static void WriteToLlFile(this LLVMModuleRef module, string path)
        {
            File.WriteAllText(path, module.GetModuleText());
        }

        public static string GetModuleText(this LLVMModuleRef module)
        {
            return RemillUtils.LLVMModuleToString(module);
        }

        public static IEnumerable<LLVMValueRef> GetGlobals(this LLVMModuleRef module)
        {
            // Get the first global within the module.
            var next = module.FirstGlobal;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                // Setup the next global for iteration.
                next = next.NextGlobal;
            }
        }

        public static IEnumerable<LLVMValueRef> GetFunctions(this LLVMModuleRef module)
        {
            // Get the first function within the module.
            var next = module.FirstFunction;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next function.
                yield return next;

                // Setup the next function for iteration.
                next = next.NextFunction;
            }
        }

        public static IEnumerable<LLVMBasicBlockRef> GetBlocks(this LLVMValueRef value)
        {
            // Get the first global within the module.
            var next = value.FirstBasicBlock;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                if (next == value.LastBasicBlock)
                    yield break;

                // Setup the next global for iteration.
                next = next.Next;

            }
        }

        public static int GetPredCount(this LLVMBasicBlockRef block) => (int)CFGApi.GetBlockPredessorsCount(block);

        public static IReadOnlyList<LLVMBasicBlockRef> GetPredecessors(this LLVMBasicBlockRef block) => CFGApi.GetBlockPredecessors(block);

        public static int GetSuccCount(this LLVMBasicBlockRef block) => (int)CFGApi.GetBlockSuccessorsCount(block);

        public static IReadOnlyList<LLVMBasicBlockRef> GetSuccessors(this LLVMBasicBlockRef block) => CFGApi.GetBlockSuccessors(block);

        public static IEnumerable<LLVMValueRef> GetInstructions(this LLVMValueRef function)
        {
            return function.GetBlocks().SelectMany(x => x.GetInstructions());
        }

        public static IEnumerable<LLVMValueRef> GetInstructions(this LLVMBasicBlockRef block)
        {
            // Get the first global within the module.
            var next = block.FirstInstruction;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                if (next == block.LastInstruction)
                    yield break;

                // Setup the next global for iteration.
                next = next.NextInstruction;

            }
        }

        public static IEnumerable<LLVMValueRef> GetOperands(this LLVMValueRef value)
        {
            for (uint i = 0; i < value.OperandCount; i++)
            {
                yield return value.GetOperand(i);
            }
        }

        public static IReadOnlyList<LLVMValueRef> GetUsers(this LLVMValueRef value)
        {
            return CFGApi.GetValueUsers(value);
            //return value.GetUsers().Select(x => x.AsValue());
        }

        private static IEnumerable<LLVMUseRef> GetUsersOld(this LLVMValueRef value)
        {
            throw new InvalidOperationException($"Do not use this. The API seems to be broken.");

            // Get the first global within the module.
            var next = value.FirstUse;
            while (true)
            {
                // Exit if there are no more elements to yield.
                if (next == null)
                    yield break;

                // Yield the next global.
                yield return next;

                var nextUse = next.GetNextUse();
                if (next == nextUse)
                    yield break;

                // Setup the next global for iteration.
                next = nextUse;

            }
        }

        public static LLVMTypeRef GetFunctionPrototype(this LLVMValueRef func) => LLVMCloning.GetFunctionPrototype(func);

        private static unsafe LLVMUseRef GetNextUse(this LLVMUseRef use) => LLVM.GetNextUse(use);

        public static unsafe LLVMValueRef AsValue(this LLVMUseRef use) => new LLVMValueRef(use.Handle);
        
        public static unsafe LLVMValueRef GetFunction(this LLVMValueRef func) => func.InstructionParent.Parent;

        public static unsafe LLVMContextRef GetInstructionCtx(this LLVMValueRef inst) => inst.GlobalParent.Context;
        public static unsafe LLVMContextRef GetCtx(this LLVMBasicBlockRef block) => block.Parent.GlobalParent.Context;
        public static unsafe LLVMContextRef GetFunctionCtx(this LLVMValueRef func) => func.GlobalParent.Context;
        public static unsafe LLVMContextRef GetCtx(this LLVMModuleRef module) => module.Context;

        // Create integer types in context.
        public static unsafe LLVMTypeRef GetIntTy(this LLVMContextRef ctx, uint bitWidth) => LLVM.IntTypeInContext(ctx, bitWidth);
        public static unsafe LLVMTypeRef GetIntTy(this LLVMContextRef ctx, ulong bitWidth) => LLVM.IntTypeInContext(ctx, (uint)bitWidth);
        public static unsafe LLVMTypeRef GetInt8Ty(this LLVMContextRef ctx) => LLVM.IntTypeInContext(ctx, 8);
        public static unsafe LLVMTypeRef GetInt16Ty(this LLVMContextRef ctx) => LLVM.IntTypeInContext(ctx, 16);
        public static unsafe LLVMTypeRef GetInt32Ty(this LLVMContextRef ctx) => LLVM.IntTypeInContext(ctx, 32);
        public static unsafe LLVMTypeRef GetInt64Ty(this LLVMContextRef ctx) => LLVM.IntTypeInContext(ctx, 64);

        // Create pointer types in context.
        public static unsafe LLVMTypeRef GetPtrType(this LLVMValueRef ctx, uint addressSpace = 0) => ctx.GlobalParent.GetPtrType();
        public static unsafe LLVMTypeRef GetPtrType(this LLVMBasicBlockRef block, uint addressSpace = 0) => LLVM.PointerTypeInContext(block.GetCtx(), 0);
        public static unsafe LLVMTypeRef GetPtrType(this LLVMModuleRef module, uint addressSpace = 0) => module.Context.GetPtrType();
        public static unsafe LLVMTypeRef GetPtrType(this LLVMContextRef ctx, uint addressSpace = 0) => LLVM.PointerTypeInContext(ctx, 0);
    }
}
