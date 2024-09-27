using Dna.Extensions;
using Dna.Binary;
using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Reconstruction;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BlockMapping = System.Collections.Generic.IReadOnlyDictionary<Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>, LLVMSharp.Interop.LLVMBasicBlockRef>;
using System.Diagnostics;
using Dna.BinaryTranslator.X86;
using Dna.SEH;
using System.Runtime.Intrinsics.X86;
using Unicorn.X86;
using AsmResolver.Patching;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;

namespace Dna.BinaryTranslator.Lifting
{
    public enum CallHandlingKind
    {
        /// <summary>
        /// Using this call handling kind, CALL intrinsics will be lifted as if they are a part of the function.
        /// After the call, the subsequent instructions are lifted 
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Using this call handling kind, CALL intrinsics will be lifted as a terminating tail call. 
        /// The responsibility is then on the caller to insert some kind of virtual dispatcher to re-enter the
        /// VM at the correct point.
        /// </summary>
        Vmexit = 1,
    }

    public class CfgTranslator
    {
        // Binary base address.
        private readonly ulong imagebase;

        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly BinaryFunction binaryFunction;

        private readonly IReadOnlySet<ulong> fallthroughFromIps;

        private readonly CallHandlingKind callHandlingKind;

        private ControlFlowGraph<Instruction> Cfg => binaryFunction.Cfg;

        public static (LLVMValueRef function, BlockMapping blockMapping, IReadOnlyList<LiftedSehEntry> filterFunctions) Translate(
            ulong imagebase, 
            RemillArch arch,
            BinaryFunction binaryFunction, 
            IReadOnlySet<ulong> fallthroughFromIps, 
            CallHandlingKind callHandlingKind = CallHandlingKind.Normal
            )
        {
            var ctx = LLVMContextRef.Global;
            return Translate(imagebase, arch, "C:\\Users\\colton\\Downloads\\remill-17-semantics", ctx, binaryFunction, fallthroughFromIps, callHandlingKind);
        }

        public static (LLVMValueRef function, BlockMapping blockMapping, IReadOnlyList<LiftedSehEntry> filterFunctions) Translate(
            ulong imagebase,
            RemillArch arch,
            string semanticsPath,
            LLVMContextRef ctx,
            BinaryFunction binaryFunction,
            IReadOnlySet<ulong> fallthroughFromIps,
            CallHandlingKind callHandlingKind = CallHandlingKind.Normal
        )
        {
            return new CfgTranslator(imagebase, arch, semanticsPath, ctx, binaryFunction, fallthroughFromIps, callHandlingKind).Translate();
        }

        private CfgTranslator(ulong imagebase, RemillArch arch, string semanticsPath, LLVMContextRef ctx, BinaryFunction binaryFunction, IReadOnlySet<ulong> fallthroughFromIps, CallHandlingKind callHandlingKind)
        {
            this.imagebase = imagebase;
            this.arch = arch;
            this.ctx = ctx;
            module = RemillUtils.LoadArchSemantics(arch, semanticsPath);
            builder = ctx.CreateBuilder();
            this.binaryFunction = binaryFunction;
            this.fallthroughFromIps = fallthroughFromIps;
            this.callHandlingKind = callHandlingKind;
        }

        private (LLVMValueRef function, BlockMapping blockMapping, IReadOnlyList<LiftedSehEntry> filterFunctions) Translate()
        {
            // Use remill to declare and initialize an empty function. This function has the default remill function prototype.
            var translatedFunction = arch.DeclareLiftedFunction($"TranslatedFrom{Cfg.StartAddress.ToString("X")}", module);
            arch.InitializeEmptyLiftedFunction(translatedFunction);

            // In the artificial entry block(the basic block which remill inserts via `InitializeEmptyLiftedFunction),
            // store a concrete [initialRip] to Remill's 'NEXT_PC' variable.
            // Note: We should NOT be conditionally concretizing depending on the call type used here.
            // It should be made explicit elsewhere.
            // If you run into case where you see memory accesses to [0x1400xxxxxxxxx] in code that's intended to be runnable,
            // this is probably the logic at fault.
            var entryBlock = Cfg.GetBlocks().First();
            builder.PositionAtEnd(translatedFunction.EntryBasicBlock);
            if (callHandlingKind != CallHandlingKind.Vmexit)
                ConcretizeInstructionPointer(translatedFunction, entryBlock.EntryInstruction.IP);

            // For each basic block, create an empty `stub` block in the LLVM control flow graph.
            var blockMapping = CreateLlvmBlocks(translatedFunction);

            // When creating an initial function through remill's `InitializeEmptyLiftedFunction` api,
            // an empty entry basic block is created. To make the cfg valid, we must add an unconditional branch
            // from this entry basic block to the entry basic block of the native cfg.
            builder.BuildBr(blockMapping[entryBlock]);

            // Lift all native basic blocks to LLVM IR using remill.
            LiftBlocks(blockMapping);

            // Lift all SEH entries.
            var filterFunctions = LiftSehEntries(translatedFunction, blockMapping);

            // Build a mapping of <llvm cfg block name, x86 block>..
            var irBlockNameToX86 = blockMapping.ToDictionary(x => x.Value.AsValue().Name, x => x.Key);

            return (translatedFunction, blockMapping, filterFunctions);
        }

        private BlockMapping CreateLlvmBlocks(LLVMValueRef function)
        {
            // Create LLVM blocks for each native basic block, then store a mapping between these two items.
            var blocks = Cfg.GetBlocks();
            var blockMapping = new Dictionary<BasicBlock<Instruction>, LLVMBasicBlockRef>();
            foreach (var block in blocks)
            {
                var llvmBlock = function.AppendBasicBlock($"bb_{block.Address.ToString("X")}");
                blockMapping.Add(block, llvmBlock);
            }

            return blockMapping.AsReadOnly();
        }

        public void ConcretizeInstructionPointer(LLVMValueRef function, ulong initialRip)
        {
            // Store a concrete instruction pointer to Remill's `NEXT_PC` variable.
            var constPc = LLVMValueRef.CreateConstInt(ctx.GetInt64Ty(), initialRip);
            builder.BuildStore(constPc, RemillUtils.LoadNextProgramCounterRef(function.EntryBasicBlock));
        }

        private void LiftBlocks(BlockMapping blockMapping)
        {
            foreach (var block in blockMapping)
            {
                // Lift the basic block into the LLVM IR function.
                LiftBlock(block.Key, blockMapping);
            }
        }

        private void LiftBlock(BasicBlock<Instruction> block, BlockMapping blockMapping)
        {
            var llvmBlock = blockMapping[block];
            foreach (var inst in block.Instructions)
            {
                // Decode and lift the instruction into it's llvm basic block.
                var remillInst = DecodeInstruction(inst);
                remillInst.Lifter.LiftIntoBlock(remillInst, llvmBlock);

                // Post process the instruction.
                // This inserts stubs for vmexit, jmp to branches, etc.
                builder.PositionAtEnd(llvmBlock);
                var flow = inst.FlowControl;
                if (flow.IsCall())
                {
                    if (callHandlingKind == CallHandlingKind.Normal)
                        LiftFallthroughCall(llvmBlock);
                    else if (callHandlingKind == CallHandlingKind.Vmexit)
                        LiftVmexitCall(llvmBlock, inst);
                }

                else if (flow.IsRet())
                {
                    LiftRet(llvmBlock);
                }

                else if (flow.IsBranch())
                {
                    LiftBranch(blockMapping, block, inst);
                }
            }

            var exitInst = block.ExitInstruction;
            builder.PositionAtEnd(llvmBlock);
            var exitFlow = exitInst.FlowControl;

            // A fallthrough basic block is defined as a basic block that falls into another
            // basic block without the use of a control flow instruction. This can happen often because compilers are very good at
            // saving space, so they order the basic blocks in a manner that removes the need for unconditional jmp instructions.
            // So in the cases of fallthrough edges, we update the LLVM basic block's terminator instruction to unconditionally
            // jump to fallthrough targets if one exists.
            bool isVmexitCall = exitFlow.IsCall() && callHandlingKind == CallHandlingKind.Vmexit;
            bool isFallthrough = !isVmexitCall && !(exitFlow.IsRet() || exitFlow.IsBranch()) && block.OutgoingEdges.Any();
            if (isFallthrough)
            {
                Console.WriteLine(exitInst);
                builder.PositionAtEnd(llvmBlock);
                builder.BuildBr(blockMapping[block.GetOutgoingEdges().Single().TargetBlock]);
            }
        }

        private RemillInstruction DecodeInstruction(Instruction inst)
        {
            // We prefer to use a mapping of <instruction, byte[]> for a few reasons:
            //   - removes the dependency on `IBinary` or `Dna`.
            //   - allows us to overwrite or change an instruction *without* modifying the binary contents.
            //   - prevents us from having to re-encode the instruction, which can change the length(causing disastrous results, since the `NextIP` field change.
            var instBytes = binaryFunction.GetInstructionEncodingAt(inst.IP);

            // Decode the instruction again using ICED, but this time use the bytes supplied in the encoding mapping.
            // If the provided instruction is not identical to the one decoded from the decoding mapping, something is majorly wrong.
            var icedInst = BinaryDisassembler.GetInstructionFromBytes(inst.IP, instBytes);
            if (icedInst != inst)
                throw new InvalidOperationException($"Decoding mismatch between control flow graph inst {inst} and decoded assembly mapping instruction {icedInst}");

            // Decode the instruction using remill & validate it.
            var remillInst = arch.DecodeInstruction(inst.IP, instBytes);
            if (remillInst == null || remillInst.Pc != inst.IP || remillInst.NextPc != inst.NextIP || remillInst.NumBytes != (ulong)inst.Length)
                throw new InvalidOperationException($"Mismatch in data between remill instruction {remillInst?.Text} & iced instruction 0x{inst}");
            return remillInst;
        }

        private void LiftFallthroughCall(LLVMBasicBlockRef llvmBlock)
        {
            // Add the call intrinsic.
            RemillUtils.AddCall(llvmBlock, arch.IntrinsicTable.FunctionCall, arch.IntrinsicTable);

            // On function call, remill stores the return address value to %RETURN_PC.
            // However, it does *not* update %NEXT_PC to point to the post-call value.
            // So to fix this, first we start off by loading the const return address from %RETURN_PC.
            var returnPc = builder.BuildLoad2(ctx.GetInt64Ty(), RemillUtils.LoadReturnProgramCounterRef(llvmBlock));

            // Then we store the return address to NEXT_PC. This allows the lifted code to have correct behavior.
            builder.BuildStore(returnPc, RemillUtils.LoadNextProgramCounterRef(llvmBlock));
        }

        private void LiftVmexitCall(LLVMBasicBlockRef llvmBlock, Instruction nativeCallInst)
        {
            // Tail calling is not allowed inside of TRY regions. So if the call instruction is inside of a TRY statement,
            // we lift it as a 'remill_function_call' instruction followed by a 'ret'.
            LLVMValueRef callInst;
            if (binaryFunction.ScopeTableTree.ScopeTable.IsAddressInsideTryStatement(nativeCallInst.IP))
            {
                callInst = RemillUtils.AddCall(llvmBlock, arch.IntrinsicTable.FunctionCall, arch.IntrinsicTable);
                builder.BuildRet(callInst);
            }

            // Otherwise if the call is not inside of a TRY statement then we are free to just terminating tail call it.
            else
            {
                callInst = RemillUtils.AddTerminatingTailCall(llvmBlock, arch.IntrinsicTable.FunctionCall, arch.IntrinsicTable);
            }

            // Set the 3rd parameter of remill_function_call to the NextIP. This is then
            // used later on as a vm re-entry key.
            var key = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, nativeCallInst.IP);
            callInst.SetOperand(2, LLVMValueRef.CreateConstIntToPtr(key, ctx.GetPtrType()));
        }

        private void LiftRet(LLVMBasicBlockRef llvmBlock) => RemillUtils.AddTerminatingTailCall(llvmBlock, arch.IntrinsicTable.FunctionReturn, arch.IntrinsicTable);

        private void LiftBranch(BlockMapping blockMapping, BasicBlock<Instruction> block, Instruction branchInst)
        {
            // On x64, a branch instruction with an immediate destination
            // is always either an unconditional branch, or a JCC.
            // Note: JCCs can *never* have an indirect destination.
            if (branchInst.HasImmediateBranchTarget())
            {
                // If this is an unconditional branch, insert a `br` to the dest block.
                var immDest = branchInst.GetImmediateBranchTarget();
                var immDestBlock = blockMapping.Single(x => x.Key.Address == immDest).Value;
                if (!branchInst.FlowControl.IsConditional())
                {
                    builder.BuildBr(immDestBlock);
                    return;
                }

                // Otherwise, process this as a JCC.
                // First we must handle the "optimistic" case, where we concretize the RIP
                // and assume that the source binary has been loaded at it's ideal base address.
                var i64Ty = ctx.GetInt64Ty();
                if (callHandlingKind == CallHandlingKind.Normal)
                {
                    // Insert a branch to: ite(load(remill_next_pc) == immDest ? immDest : inst.NextIP)
                    var nextPc = builder.BuildLoad2(i64Ty, RemillUtils.LoadNextProgramCounterRef(blockMapping[block]));
                    var immConstInt = LLVMValueRef.CreateConstInt(nextPc.TypeOf, immDest);
                    var cmp = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, nextPc, immConstInt, "jcc");
                    var falseBlock = blockMapping.Single(x => x.Key.Address == branchInst.NextIP).Value;
                    builder.BuildCondBr(cmp, immDestBlock, falseBlock);

                    // https://godbolt.org/z/Gzb44njx7
                    // This addresses a really trivial case where LLVM fails at conditional constant propagation.
                    // If we set the RIP to a select i1, block1Rip, block2Rip, LLVM constant propagation
                    // fails to propagate the constant RIP inside of block1. 
                    // To fix this, we add an instruction to the start of both blocks,
                    // which updatres the RIP to the correct value. This removes the need for constant propagation.
                    builder.Position(immDestBlock, immDestBlock.FirstInstruction);
                    builder.BuildStore(immConstInt, RemillUtils.LoadNextProgramCounterRef(immDestBlock));

                    builder.Position(falseBlock, falseBlock.FirstInstruction);
                    immConstInt = LLVMValueRef.CreateConstInt(nextPc.TypeOf, branchInst.NextIP);
                    builder.BuildStore(immConstInt, RemillUtils.LoadNextProgramCounterRef(falseBlock));
                    return;
                }

                // Otherwise we must process this pessimistically(aka when the code actually needs to execute at runtime).
                else if (callHandlingKind == CallHandlingKind.Vmexit)
                {
                    // var immDestOffset = inst.IP - immDest;
                    // var nextPcOffset = inst.IP - inst.NextIp
                    // var offset = inst.Ip - load(remill_next_pc)
                    // var cmp = offset == immDestOffset ? immDestOffset : nextPcOffset
                    var immDestOffset = branchInst.IP - immDest;
                    var immDestOffsetConstInt = LLVMValueRef.CreateConstInt(i64Ty, immDestOffset);
                    var nextPcOffset = branchInst.IP - branchInst.NextIP;
                    var nextPcOffsetConstInt = LLVMValueRef.CreateConstInt(i64Ty, nextPcOffset);
                    var llvmInstIp = builder.BuildLoad2(i64Ty, RemillUtils.LoadProgramCounterRef(blockMapping[block]));
                    var llvmNextIp = builder.BuildLoad2(i64Ty, RemillUtils.LoadNextProgramCounterRef(blockMapping[block]));
                    var offset = builder.BuildSub(llvmInstIp, llvmNextIp);
                    var cmp = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, offset, immDestOffsetConstInt, "jccOffset");
                    var falseBlock = blockMapping.Single(x => x.Key.Address == branchInst.NextIP).Value;
                    builder.BuildCondBr(cmp, immDestBlock, falseBlock);

                    // https://godbolt.org/z/Gzb44njx7
                    // This addresses a really trivial case where LLVM fails at conditional constant propagation.
                    // If we set the RIP to a select i1, block1Rip, block2Rip, LLVM constant propagation
                    // fails to propagate the constant RIP inside of block1. 
                    // To fix this, we add an instruction to the start of both blocks,
                    // which updatres the RIP to the correct value. This removes the need for constant propagation.
                    builder.Position(immDestBlock, immDestBlock.FirstInstruction);
                    //builder.BuildStore(builder.BuildAdd(llvmInstIp, immDestOffsetConstInt), RemillUtils.LoadNextProgramCounterRef(immDestBlock));

                    builder.Position(falseBlock, falseBlock.FirstInstruction);
                    //builder.BuildStore(builder.BuildAdd(llvmInstIp, nextPcOffsetConstInt), RemillUtils.LoadNextProgramCounterRef(falseBlock));

                    //Debugger.Break();
                    return;
                }
            }

            // If we know any bounds of the jump table, then lift it as a switch.
            if (binaryFunction.JmpTables.ContainsKey(branchInst.IP))
            {
                LiftResolvedJumpTable(blockMapping, blockMapping[block], branchInst);
            }

            else
            {
                // Otherwise, this is an unresolved indirect jump. 
                // Thus we must insert a returning tail call to an indirect JMP intrinsic.
                // Also note the hack we use below to preserve which RIP the indirect jmp is coming from.
                AddCallToIndirectBranchIntrinsic(blockMapping[block], branchInst.IP);
            }
        }

        private void LiftResolvedJumpTable(BlockMapping blockMapping, LLVMBasicBlockRef llvmBlock, Instruction branchInst)
        {
            // Load the indirect jump value.
            var int64Ty = ctx.GetInt64Ty();
            var indirectPc = builder.BuildLoad2(int64Ty, RemillUtils.LoadNextProgramCounterRef(llvmBlock));

            var jmpTable = binaryFunction.JmpTables[branchInst.IP];
            var liftedCases = new HashSet<ulong>
            {
            };

            LLVMBasicBlockRef defaultBlock = null;

            // If the jump table is considered incomplete(we know some edges but potentially not all), then we lift the jump table as a switch statement
            // where the known values get their own 'case', and the default case points to an remill_jump intrinsic.
            if (!jmpTable.IsComplete)
            {
                defaultBlock = llvmBlock.Parent.AppendBasicBlock($"reprove_new_edge_for_jmp_table_{jmpTable.JmpFromAddr.ToString("X")}");
                builder.PositionAtEnd(defaultBlock);
                AddCallToIndirectBranchIntrinsic(defaultBlock, jmpTable.JmpFromAddr);
                builder.PositionAtEnd(llvmBlock);
            }

            // If the jump table is considered complete(i.e. we are 100% confident that we know all possible outgoing values),
            // then we make the default case for the jump table point to a randomly selected(first) jump table outgoing block.
            else
            {
                // Select a random(the first) switch case block to be used as the default case.
                defaultBlock = blockMapping.Single(x => x.Key.Address == jmpTable.KnownOutgoingAddresses.First()).Value;
                // Keep track of the fact that we've already lifted this block, so that we don't lift it twice.
                liftedCases.Add(jmpTable.KnownOutgoingAddresses.First());
            }

            var swtch = builder.BuildSwitch(indirectPc, defaultBlock, (uint)jmpTable.KnownOutgoingAddresses.Count);
            foreach (var target in jmpTable.KnownOutgoingAddresses)
            {
                if (liftedCases.Contains(target))
                    continue;

                liftedCases.Add(target);
                var targetBlock = blockMapping.Single(x => x.Key.Address == target).Value;
                swtch.AddCase(LLVMValueRef.CreateConstInt(ctx.Int64Type, target), targetBlock);
            }
        }

        private void AddCallToIndirectBranchIntrinsic(LLVMBasicBlockRef llvmBlock, ulong exitFromRip)
        {
            // Create the indirect jump.
            var int64Ty = ctx.GetInt64Ty();
            var indirectPc = builder.BuildLoad2(int64Ty, RemillUtils.LoadNextProgramCounterRef(llvmBlock));
            var taillCall = RemillUtils.AddTerminatingTailCall(llvmBlock, arch.IntrinsicTable.Jump, arch.IntrinsicTable);

            // Note: This is a massive hack. At any given point, we need to know what the original RIP of the indirect jump instruction was.
            // So, to do this in the easiest way possible, we replace the memptr argument of the jump intrinsic with a constant inttoptr.
            // This could have disastrous effects, so fix it later.
            builder.PositionBefore(taillCall);
            var ptrType = ctx.GetPtrType();
            var constIntPtr = LLVMValueRef.CreateConstIntToPtr(LLVMValueRef.CreateConstInt(int64Ty, exitFromRip), ptrType);
            taillCall.SetOperand(2, constIntPtr);
        }

        private IReadOnlyList<LiftedSehEntry> LiftSehEntries(LLVMValueRef translatedFunction, BlockMapping blockMapping)
        {
            // If the scope table is null then there are no SEH entries.
            // TODO: Handle exception types other than SEH.
            if (binaryFunction.ScopeTableTree == null)
                return new List<LiftedSehEntry>().AsReadOnly();

            return SehTranslator.LiftSehEntries(imagebase, arch, binaryFunction.ScopeTableTree, translatedFunction, blockMapping);
        }
    }
}
