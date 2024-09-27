using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.SEH;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BlockMapping = System.Collections.Generic.IReadOnlyDictionary<Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>, LLVMSharp.Interop.LLVMBasicBlockRef>;

namespace Dna.BinaryTranslator.Lifting
{
    public class SehTranslator
    {
        private readonly ulong imagebase;

        private readonly RemillArch arch;

        private readonly ScopeTableTree scopeTableTree;

        private readonly LLVMContextRef ctx;

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private LLVMValueRef function;

        private readonly BlockMapping blockMapping;

        /// <summary>
        /// Given a lifted function and it's scope table, insert SEH try/begin macros accordingly. For filter functions we create a bodyless
        /// placeholder function. This returns a mapping of (seh filter function address, placeholder LLVM IR filter function).
        /// </summary>
        /// <param name="imagebase">The base address of the binary.</param>
        /// <param name="scopeTable">The SEH scope table entry.</param>
        /// <param name="translatedFunction">The lifted LLVM IR function.</param>
        /// <param name="blockMapping">A mapping of x86 control flow graph blocks to their LLVM IR counterparts.</param>
        /// <returns></returns>
        public static IReadOnlyList<LiftedSehEntry> LiftSehEntries(ulong imagebase, RemillArch arch, ScopeTableTree scopeTableTree, LLVMValueRef translatedFunction, BlockMapping blockMapping)
            => new SehTranslator(imagebase, arch, scopeTableTree, translatedFunction, blockMapping).LiftSehEntries();

        private SehTranslator(ulong imagebase, RemillArch arch, ScopeTableTree scopeTableTree, LLVMValueRef translatedFunction, BlockMapping blockMapping)
        {
            this.imagebase = imagebase;
            this.arch = arch;
            this.scopeTableTree = scopeTableTree;
            function = translatedFunction;
            this.blockMapping = blockMapping;
            ctx = translatedFunction.GetFunctionCtx();
            module = translatedFunction.GlobalParent;
            builder = LLVMBuilderRef.Create(ctx);
        }

        private IReadOnlyList<LiftedSehEntry> LiftSehEntries()
        {
            // Mapping of <filter function address, placeholder LLVM IR function>().
            var liftedEntries = new List<LiftedSehEntry>();

            // Lift all SEH entries.
            foreach (var node in scopeTableTree.AllNodes)
            {
                var liftedEntry = LiftSehEntry(node);
                liftedEntries.Add(liftedEntry);
            }

            // Sometimes in binary code you have a try statement that ends with a jump to another try statement.
            // This can create a scenario where during SEH lifting, we emit a @try.end() to %label macro which 
            // jumps directly into a try(while skipping the @try.begin macro invocation).
            // As a post processing step we identify these cases and redirect the edges to go through the macro.
            RedirectTryEndTargetsToNewlyInsertedPreheaders(liftedEntries);

            //File.WriteAllText("translatedFunction.ll", function.ToString());
            return liftedEntries.AsReadOnly();
        }

        // https://godbolt.org/z/fv6fe1YKz
        private LiftedSehEntry LiftSehEntry(ScopeTableNode node)
        {
            // Get the start basic block that's guarded by the SEH region.
            var entry = node.Entry;
            var (tryBlock, startLlvmBlock) = blockMapping.Single(x => x.Key.Address == entry.BeginAddr);
            var (exceptBlock, exceptLlvmBlock) = blockMapping.Single(x => x.Key.Address == entry.HandlerAddr);

            // Set the personality of our lifted function to use MSVC exceptions.
            var sehBuilder = new SehIntrinsicBuilder(module, builder);
            var personalityFunc = sehBuilder.CreateMsvcPersonalityFunction();
            function.PersonalityFn = personalityFunc;

            // Create a "dispatcher preheader". This block is the target of the "@try.begin()" macro,
            // and it immediately jumps to the start llvm block.
            var dispatcherPreheader = function.AppendBasicBlock("dispatcherPreheader");
            builder.PositionAtEnd(dispatcherPreheader);
            builder.BuildBr(startLlvmBlock);

            // Emit a @llvm.seh.try.begin()
            //          to label %tryBlock, unwind label %catchBlock
            var tryPreheader = function.AppendBasicBlock("tryPreheader");
            var catchBlock = function.AppendBasicBlock("catchBlock");
            builder.PositionAtEnd(tryPreheader);
            var invokeTryBeginInst = sehBuilder.EmitSehTryScopeBegin(dispatcherPreheader, catchBlock);

            // Emit %2 = catchswitch within none [label %catchPadBlock] unwind to caller
            var exceptHeader = function.AppendBasicBlock("__except");
            var catchPadBlock = function.AppendBasicBlock("catchPadBlock");
            builder.PositionAtEnd(catchBlock);
            var catchSwitch = sehBuilder.EmitCatchSwitch(catchPadBlock);

            // Emit:
            //  %3 = catchpad within %2 [ptr filterFunc]
            //  catchret from %3 to label %__except
            var liftedFilterFunc = CreateSehFilterFunction(entry.FilterAddr);
            builder.PositionAtEnd(catchPadBlock);
            var catchPad = sehBuilder.EmitCatchPad(catchSwitch, liftedFilterFunc.LlvmFunction);
            sehBuilder.EmitCatchRet(catchPad, exceptHeader);

            // Emit: 
            //  %2 = call i32 @llvm.eh.exceptioncode(token %catchpad)
            builder.PositionAtEnd(exceptHeader);
            sehBuilder.EmitEhGetExceptionCode(catchPad);
            builder.BuildBr(exceptLlvmBlock);

            // Make all jumps out of the 'try' statement go through our @llvm.seh.try.end() invocation block instead.
            InsertTryStatementPostheaders(sehBuilder, tryBlock, catchBlock, entry);

            // Make all jumps to the 'try' statement go through our @llvm.seh.try.begin() block instead.
            // Note that we execute this after `InsertTryStatementPostheaders` because creating @llvm.seh.try.end
            // macros may insert new edges to the try statement.
            ConnectTryStatementPreheader(tryBlock, tryPreheader, entry);

            // Immediately before the provided @llvm.seh.try.begin() invocation instruction, insert volatile stores of concrete values to the
            // rsp and imagebase global variables. 
            StoreInputsToFilterGlobalVariables(entry.BeginAddr, invokeTryBeginInst, liftedFilterFunc);
            return new LiftedSehEntry(node, tryPreheader, dispatcherPreheader, catchPadBlock, liftedFilterFunc);
        }

        /// <summary>
        /// For each jump into a try statement, replace it with a jump to our artificial preheader.
        /// The preheader invokes @llvm.seh.try.begin() and jumps to the try basic block.
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="preheader"></param>
        /// <param name="entry"></param>
        private void ConnectTryStatementPreheader(BasicBlock<Instruction> tryBlock, LLVMBasicBlockRef preheader, ScopeTableEntry entry)
        {
            // Create a lambda that returns true if the input basic is within the try statement.
            var isInsideThisTry = (BasicBlock<Instruction> targetBlock) => { return targetBlock.Address >= entry.BeginAddr && targetBlock.Address < entry.EndAddr; };

            // Get all graph edges that jump into a try region.
            var edgesIntoGuardedRegion = tryBlock.GetIncomingEdges().Where(x => !isInsideThisTry(x.SourceBlock)).ToHashSet();

            foreach (var edge in edgesIntoGuardedRegion)
                RedirectEdgeTo(edge, preheader);
        }

        private void InsertTryStatementPostheaders(SehIntrinsicBuilder sehBuilder, BasicBlock<Instruction> tryBlock, LLVMBasicBlockRef catchSwitchBlock, ScopeTableEntry entry)
        {
            // Since SEH regions are SEME(single entry multi exit), we get all outgoing edges out of the try statement.
            // So if you had:
            //  try { call foo() } catch() {} call heyImOutsideTheTry(),
            // this would return the edge from inside the try to the first basic block outside of the try.
            var exitingEdges = SehRegionAnalysis.GetExitingEdgesFromRegion(tryBlock, entry.BeginAddr, entry.EndAddr);

            // Then for each edge to a location outside of the try statement, we insert an artifical postheader block which invokes @llvm.seh.try.end().
            foreach (var outgoingEdge in exitingEdges)
            {
                // Fetch the llvm source and target blocks.
                var sourceBlock = blockMapping[outgoingEdge.SourceBlock];
                var targetBlock = blockMapping[outgoingEdge.TargetBlock];

                // Create an artifical preheader block containing:
                //  invoke void @llvm.seh.try.end()
                //      to label % __try.cont unwind label %catch.dispatch
                var headerBlock = function.AppendBasicBlock($"seh_end_preheader_from_{outgoingEdge.SourceBlock.Address.ToString("X")}");
                builder.PositionAtEnd(headerBlock);
                sehBuilder.EmitSehTryScopeEnd(targetBlock, catchSwitchBlock);

                // Replace all jumps to the target block with jumps to our artificial header block.
                RedirectEdgeTo(outgoingEdge, headerBlock);
            }
        }

        private void RedirectEdgeTo(BlockEdge<Instruction> outgoingEdge, LLVMBasicBlockRef newTarget)
        {
            // Fetch the llvm source and target blocks.
            var sourceBlock = blockMapping[outgoingEdge.SourceBlock];
            var targetBlock = blockMapping[outgoingEdge.TargetBlock];

            // Replace all jumps to the original target block with jumps to our new target block.
            var exitInstruction = sourceBlock.LastInstruction;
            ReplaceInstancesOfOperand(exitInstruction, targetBlock.AsValue(), newTarget.AsValue());
        }

        private void ReplaceInstancesOfOperand(LLVMValueRef inst, LLVMValueRef from, LLVMValueRef to)
        {
            for (uint i = 0; i < (uint)inst.OperandCount; i++)
            {
                var operand = inst.GetOperand(i);
                if (operand == from)
                    inst.SetOperand(i, to);
            }
        }

        // TODO: Add noundef, nocapture attributes to filter arguments.
        private LiftedFilterFunction CreateSehFilterFunction(ulong address)
        {
            // Create a global variable that RSP is loaded from.
            var rspGlobal = module.AddGlobal(ctx.Int64Type, $"seh_filter_{address.ToString("X")}_rsp_recover");
            LLVMCloning.MakeDsoLocal(rspGlobal, true);

            // Create a global that the imagebase is loaded from.
            var imagebaseGlobal = module.AddGlobal(ctx.Int64Type, $"seh_filter_{address.ToString("X")}_imagebase_recover");
            LLVMCloning.MakeDsoLocal(imagebaseGlobal, true);

            // Create the prototype.
            var prototype = LLVMTypeRef.CreateFunction(ctx.Int32Type, new LLVMTypeRef[] { ctx.GetPtrType(), ctx.GetPtrType() });

            // Create the filter function.
            var func = module.AddFunction($"SehFilter{address.ToString("X")}", prototype);

            // Create the entry basic block.
            var block = func.AppendBasicBlock("entry");
            builder.PositionAtEnd(block);

            // Load the frame pointer of the native / emulated function. 
            // Note that we use a volatile load to prevent the optimizer from doing any kind of analysis or optimizations over the load.
            var rsp = builder.BuildLoad2(ctx.Int64Type, rspGlobal, "rsp");
            rsp.Volatile = true;

            // Load the imagebase. Note that this changes at runtime.
            var runtimeImageBase = builder.BuildLoad2(ctx.Int64Type, imagebaseGlobal, "imagebase");
            runtimeImageBase.Volatile = true;

            // Compute the address of the binary filter function.
            var offset = LLVMValueRef.CreateConstInt(ctx.Int64Type, address - imagebase);
            var funcAddr = builder.BuildAdd(runtimeImageBase, offset);
            var funcPtr = builder.BuildIntToPtr(funcAddr, ctx.GetPtrType());

            // Return a terminating tail call into the native filter function. TODO: Lift this function instead.
            var rspToPtr = builder.BuildIntToPtr(rsp, ctx.GetPtrType());
            var sehFilterCode = builder.BuildCall2(prototype, funcPtr, new LLVMValueRef[] { func.GetParam(0), rspToPtr });
            LLVMCloning.MakeMustTail(sehFilterCode);
            builder.BuildRet(sehFilterCode);

            return new LiftedFilterFunction(address, func, rspGlobal, imagebaseGlobal);
        }

        // Immediately before the provided @llvm.seh.try.begin() invocation instruction, insert volatile stores of concrete values to the
        // rsp and imagebase global variables. 
        private void StoreInputsToFilterGlobalVariables(ulong tryBeginRip, LLVMValueRef invokeSehTryBeginInst, LiftedFilterFunction filterFunction)
        {
            // Position the builder right before the invocation to @llvm.seh.try.begin().
            builder.PositionBefore(invokeSehTryBeginInst);

            // All SEH filter functions use the prototype `typedef int (__fastcall* f_native_filter)(struct _EXCEPTION_POINTERS* ep, unsigned long long rsp);`.
            // To properly virtualize the filter function we need to grab the stack frame pointer from the remill state structure and pass it in to the RSP pointer.
            // Traditionally you would use the @llvm.localescape() intrinsic to capture this dependency, but we cannot use it since the intrinsic blocks all inlining.
            // Instead we spill the stack pointer to a global variable, and substitute it with a @@llvm.localrecover() intrinsic way later on in the pipeline.
            var statePtr = function.GetParam(0);
            var rsp = arch.GetRegisterByName(arch.StackPointerRegisterName);
            var rspValue = builder.BuildLoad2(ctx.Int64Type, rsp.GetAddressOf(statePtr, builder));
            var store = builder.BuildStore(rspValue, filterFunction.RspGlobal);
            store.Volatile = true;

            // Load the rip value.
            var ripValue = builder.BuildLoad2(ctx.Int64Type, RemillUtils.LoadNextProgramCounterRef(invokeSehTryBeginInst.InstructionParent));
            // Compute offset of the function relative to the fixed image base.
            var funcOffset = tryBeginRip - imagebase;
            // Use this offset to compute the runtime imagebase, relative to the current instruction pointer.
            var runtimeImgBase = builder.BuildSub(ripValue, LLVMValueRef.CreateConstInt(ctx.GetInt64Ty(), funcOffset), "imgbase");
            store = builder.BuildStore(runtimeImgBase, filterFunction.ImagebaseGlobal);
            store.Volatile = true;
        }

        // During the SEH lifting process we need to redirect to make sure that all
        // entries and exits to an SEH region go through "try.begin" and "try.end" macro invocations.
        // As-is there exists an edge case where adding a "try.end" macro 
        // creates a new edge to another try region, and this edge does not go through the macro.
        // We special case this and make sure that all @try.end macros
        // go through the correct try.begin macro if that try.end macro was targeting another try region.
        private void RedirectTryEndTargetsToNewlyInsertedPreheaders(IEnumerable<LiftedSehEntry> liftedSehEntries)
        {
            foreach (var liftedSehEntry in liftedSehEntries)
            {
                var entry = liftedSehEntry.ScopeTableNode.Entry;
                var (tryBlock, startLlvmBlock) = blockMapping.Single(x => x.Key.Address == entry.BeginAddr);
                var val = startLlvmBlock.AsValue();
                var preds = CFGApi.GetBlockPredecessors(startLlvmBlock);
                foreach (var incomingBlock in preds)
                {
                    var exitInst = incomingBlock.LastInstruction;
                    if ((exitInst.InstructionOpcode == LLVMOpcode.LLVMCall || exitInst.InstructionOpcode == LLVMOpcode.LLVMInvoke) && exitInst.ToString().Contains("void @llvm.seh.try.end"))
                        ReplaceInstancesOfOperand(exitInst, startLlvmBlock.AsValue(), liftedSehEntry.PreheaderBlock.AsValue());
                }
            }
        }
    }
}
