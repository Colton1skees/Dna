using Dna.ControlFlow;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO;
using Dna.LLVMInterop.API.LLVMBindings.Transforms;
using Dna.LLVMInterop;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.Reconstruction;
using Dna.Utilities;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Dna.LLVMInterop.Passes;
using JumpTableMapping = System.Collections.Generic.Dictionary<ulong, Dna.BinaryTranslator.JmpTables.JmpTable>;
using X86Block = Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>;
using Dna.BinaryTranslator.X86;
using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.JmpTables.Precise;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Binary;
using Dna.SEH;
using Dna.BinaryTranslator.Safe;
using Dna.ControlFlow.Extensions;
using Dna.BinaryTranslator.Lifting;
using Unicorn.X86;

namespace Dna.BinaryTranslator.Unsafe
{
    /// <summary>
    /// Class for statically translating a control flow graph when only the function start and end are known.
    /// This class iteratively lifts, transforms, optimizes, identifies jump table bounds, and re-lifts
    /// until the entirety of the control flow graph has been explored. 
    /// 
    /// Because this class makes aggressive and unsafe assumptions(e.g. alias analysis, assuming all functions are fastcalls)
    /// it may not be used for recompilation. It should be used only for identifying the complete control flow graph(in the presence of jump tables).
    /// 
    /// The algorithm used for exploring control flow graphs here is fundamentally similar to SATURN(https://arxiv.org/pdf/1909.01752.pdf).
    /// Lift control flow graph, optimize, statically backwards slice each indirect jump, use an smt solver to identify indirect jump bounds,
    /// and recurse until the entire control flow graph has been recovered.
    /// </summary>
    public class IterativeFunctionTranslator
    {
        private readonly IDna dna;

        private RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly ulong funcAddress;

        private List<JmpTable> solvedTables = new();

        public static BinaryFunction Translate(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
            => new IterativeFunctionTranslator(dna, arch, ctx, funcRip).Translate();

        private IterativeFunctionTranslator(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.funcAddress = funcRip;
        }

        private BinaryFunction Translate()
        {
            LLVMValueRef? finalLiftedFunction;
            ControlFlowGraph<Instruction> cfg = null;

            // Parse the scope table of the function.
            var scopeTable = BinaryScopeTable.TryGetFromFunctionAddress(dna.Binary, funcAddress);
            scopeTable = new(0, new List<ScopeTableEntry>());
            var scopeTableTree = new ScopeTableTree(scopeTable);
            var handlerAddresses = scopeTable.Entries.Select(x => x.HandlerAddr);

            while (true)
            {
                arch = new RemillArch(ctx, RemillOsId.kOSWindows, RemillArchId.kArchAMD64_AVX512);

                // Apply recursive descent to disassemble the control flow graph.
                // When an unresolvable branch is encountered(call rax, jmp rax, etc.), the callback
                // is invoked to check if any known edges exist.
                // If the callback returns any elements, then the recursive descent disassembler
                // adds them as edges to the indirect branch, and continues applying recursive descent to them.
                // Note: The algorithm will *not* fix up or modify the indirect branch to be some type of direct branch.
                // It is the responsibility of the caller to then update the control flow graph to replace
                // the indirect branch with a sequence of correct direct branches(assuming we want to do that).
                cfg = dna.RecursiveDescent.ReconstructCfg(funcAddress, GetKnownIndirectEdgesCallback, handlerAddresses);

                (cfg, var fallthroughFromIps) = PreprocessCfg(cfg, scopeTable);

                // Lift the function using remill.
                var encodedCfg = X86CfgEncoder.EncodeCfg(dna.Binary, cfg);
                (var liftedFunction, var blockMapping, var filterFunctions) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, "C:\\Users\\colton\\Downloads\\remill-17-semantics", ctx, new BinaryFunction(encodedCfg, scopeTableTree, solvedTables.AsReadOnly()), fallthroughFromIps, CallHandlingKind.Normal);
                liftedFunction = FunctionIsolator.IsolateFunctionIntoNewModuleWithSehSupport(arch, liftedFunction, filterFunctions.Select(x => x.LiftedFilterFunction).ToList().AsReadOnly()).function;

                liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                // Strip away as much of the remill runtime as possible.
                // This includes replacing intrinsics with concrete implementations,
                // as well as things like assuming that all calls are fastcall.
                liftedFunction = StripRuntime(liftedFunction);

                liftedFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
                Console.WriteLine("Compiling to an exe.");
                var compiledPath = ClangCompiler.Compile("translatedFunction.ll");
                var loaded = IDALoader.Load(compiledPath);

                // Solve for any indirect jumps in the control flow graph.
                var newTables = JmpTableSolver.SolveJumpTables(dna.Binary, liftedFunction);

                Console.WriteLine("Loading into IDA.");
                var exePath = IDALoader.Load(compiledPath);

                // Exit the exploration loop if we've solved all jump tables.
                if (!newTables.Any())
                {
                    finalLiftedFunction = liftedFunction;
                    break;
                }

                // Update the set of jump tables while merging newly collected information.
                solvedTables = MergeJumpTables(funcAddress, dna, solvedTables, newTables);

                Console.WriteLine("foobar.");
            }

            finalLiftedFunction.Value.GlobalParent.WriteToLlFile("translatedFunction.ll");
            Console.WriteLine("Compiling to an exe.");
            var compiledPath2 = ClangCompiler.Compile("translatedFunction.ll");

            Console.WriteLine("Loading into IDA.");
            var exePath2 = IDALoader.Load(compiledPath2);

            Console.WriteLine("");
            foreach (var jmpTable in solvedTables)
            {
                Console.WriteLine(jmpTable);
            }

            // Reapply recursive descent to the control flow graph. Here we do this to undo the 
            // code where we de-duplicate fallthrough edges.
            cfg = dna.RecursiveDescent.ReconstructCfg(funcAddress, GetKnownIndirectEdgesCallback, handlerAddresses);
            var encoded = X86CfgEncoder.EncodeCfg(dna.Binary, cfg);
            return new BinaryFunction(encoded, scopeTableTree, solvedTables.AsReadOnly());
        }

        private IEnumerable<ulong> GetKnownIndirectEdgesCallback(BasicBlock<Instruction> block) 
            => solvedTables.SingleOrDefault(x => x.JmpFromAddr == block.ExitInstruction.IP)?.KnownOutgoingAddresses?.ToList();

        private (ControlFlowGraph<Instruction> cfg, HashSet<ulong> fallthroughFromIps) PreprocessCfg(ControlFlowGraph<Instruction> cfg, ScopeTable scopeTable)
        {
            // Deduplicate all fallthrough edges to remove all duplicated indirect jumps.
            // Since our control flow graph structure has no concept of fallthrough edges, this returns a list of ulongs
            // indicating instructions IPs that fallthrough to their next basic block without a control flow branch instruction.
            (cfg, HashSet<ulong> fallthroughFromIps) = FallthroughDeduplicator.DeduplicateFallthroughEdges(cfg, Enumerable.Empty<ulong>());

            // Why do we split
            var sehPoints = scopeTable.Entries.SelectMany(x => new List<ulong>() { x.BeginAddr, x.EndAddr, x.HandlerAddr }).ToList();
            var (splitTargets, sehFallthroughAddresses) = X86CfgSplitter.SplitBlocksAtSeh(cfg, sehPoints.ToHashSet());
            fallthroughFromIps.AddRange(sehFallthroughAddresses);

            (splitTargets, sehFallthroughAddresses) = X86CfgSplitter.SplitBlocksAtCalls(cfg);
            fallthroughFromIps.AddRange(sehFallthroughAddresses);

            return (cfg, fallthroughFromIps);
        }

        private LLVMValueRef StripRuntime(LLVMValueRef function)
        {
            // Apply concrete implementations to all simple.
            // E.g. __remill_memory_read() and __remill_memory_write().
            var runtime = UnsafeRuntimeImplementer.Implement(function.GlobalParent);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Create a new function which doesn't take a state structure pointer.
            // Instead it takes all root registers as `noalias ptr` arguments.
            var parameterizedStateStruct = ParameterizedStateStructure.CreateFromFunction(arch, function);

            // Set the function variable to the output function. This is required,
            // since parameterization requires creating a completely new function
            // while inlining the original function.
            // Note: The old function is also destroyed(deleted) at this point.
            function = parameterizedStateStruct.OutputFunction;
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Modify the @__remill_function_call intrinsic to use the fastcall ABI.
            FastcallAbiInserter.Insert(arch, runtime, function, parameterizedStateStruct);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Replace the remill return and error intrinsics with
            // functions that allow more strong optimization.
            ErrorAndReturnImplementer.Implement(function);
            function.GlobalParent.WriteToLlFile("translatedFunction.ll");

            // Remove state ptr from @remill_jump intrinsic
            var jumpIntrinsic = function.GlobalParent.GetFunctions().FirstOrDefault(x => x.Name == "__remill_jump");
            List<LLVMValueRef> targets = jumpIntrinsic == default(LLVMValueRef) ? new() :  RemillUtils.CallersOf(jumpIntrinsic).Where(x => x.InstructionParent.Parent == function).ToList();
            foreach(var target in targets)
            {
                Console.WriteLine(target.OperandCount);
                var operands = target.GetOperands().ToList();
                target.SetOperand(0, target.GetOperand(2));
                Console.WriteLine("");
            }

            // Create a single @memory pointer.
            var memoryPtr = runtime.MemoryPointer;
            var builder = LLVMBuilderRef.Create(ctx);
            builder.Position(function.EntryBasicBlock, function.EntryBasicBlock.FirstInstruction);
            var dominatingLoad = builder.BuildLoad2(ctx.GetPtrType(), memoryPtr.Value, "mem");

            targets = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == memoryPtr && x != dominatingLoad).ToList();
            if (targets.Any())
            {
                foreach (var other in targets)
                {
                    other.ReplaceAllUsesWith(dominatingLoad);
                    other.InstructionEraseFromParent();
                }
            }

            for (int x = 0; x < 2; x++)
            {
                function.GlobalParent.WriteToLlFile("translatedFunction.ll");

                // Optimize the routine.
                for (int i = 0; i < 2; i++)
                {
                    OptimizationApi.OptimizeModule(function.GlobalParent, function, false, false, 0, false, 0, false);

                    // For some reason the set of passes in OptimizeModule optimizes away LLVM's
                    // `__C_specific_handler` personality function - even if it's being actively used as a personality function.
                    // As a short term fix we reinsert the personality function after each invocation.
                    var sehBuilder = new SehIntrinsicBuilder(function.GlobalParent, LLVMBuilderRef.Create(ctx));
                    sehBuilder.CreateMsvcPersonalityFunction();


                    function.GlobalParent.WriteToLlFile("translatedFunction.ll");
                    var fpm = new FunctionPassManager();
                    var pmb = new PassManagerBuilder();
                    var moduleManager = new PassManager();

                    // Remove all switches. This simplifies analysis since we don't need to handle
                    // cases where more than two case predecessors exist.
                    fpm.Add(PassApi.CreateUnSwitchPass());
                    // Remove irreducible control flow. Thus we only work with sane loops.
                    fpm.Add(UtilsPasses.CreateFixIrreduciblePass());
                    // Canonicalize the loop. Make sure all loops have dedicated exits(that is, no exit block for the loop has a predecessor
                    // that is outside the loop. This implies that all exit blocks are dominated by the loop header.)
                    fpm.Add(UtilsPasses.CreateLoopSimplifyPass());

                    fpm.Add(UtilsPasses.CreateLCSSAPass());

                    pmb.PopulateFunctionPassManager(fpm);
                    pmb.PopulateModulePassManager(moduleManager);

                    fpm.DoInitialization();
                    fpm.Run(function);
                    fpm.DoFinalization();
                }


                function.GlobalParent.WriteToLlFile("translatedFunction.ll");

                var newMod = ClangCompiler.Optimize(function.GlobalParent, "translatedFunction.ll", true);

                function = newMod.GetNamedFunction(function.Name);

                function.GlobalParent.WriteToLlFile("translatedFunction.ll");

                var idk = ClangCompiler.Compile("translatedFunction.ll");
            }

            return function;
        }

        private static List<JmpTable> MergeJumpTables(ulong cfgAddress, IDna dna, IReadOnlyList<JmpTable> oldTableInformation, IReadOnlyList<JmpTable> newTableInformation)
        {
            // For each previously proved set of jmp table information (note that there should only be one JmpTable object per jmp table 'from' address),
            // collect a mapping of <jmp from address, set of outgoing edges>.
            var oldJmpTablePredecessorMapping = new Dictionary<ulong, HashSet<ulong>>();
            foreach(var table in oldTableInformation)
            {
                if (oldJmpTablePredecessorMapping.ContainsKey(table.JmpFromAddr))
                    throw new InvalidOperationException($"Jump tables should never be duplicated like this!");
                oldJmpTablePredecessorMapping[table.JmpFromAddr] = table.KnownPredecessorAddresses;
            }

            // Merge the set of old and new jump table solutions into a single JmpTable. 
            // Basically for any given jmp table at address X, this concatenates all known information about the jump table
            // into a single jump table.
            var mergedTables = oldTableInformation
                .Concat(newTableInformation)
                .GroupBy(x => x.JmpFromAddr)
                .Select(x => MergeJumpTable(x.ToList()))
                .ToList()
                .ToDictionary(x => x.JmpFromAddr, x => x);

            // Construct a callback that takes a basic block and returns the set of known indirect outgoing targets, if any exist. This is then used by the recursive descent reconstructor
            // to lift switch statements into the x86 cfg.
            var getKnownIndirectEdgesCallback = (X86Block block) =>
            {
                if (mergedTables.ContainsKey(block.ExitInstruction.IP))
                    return mergedTables[block.ExitInstruction.IP].KnownOutgoingAddresses.ToList();

                return null;
            };

            // Apply recursive descent to the cfg using the newly discovered jump table information.
            var newCfg = dna.RecursiveDescent.ReconstructCfg(cfgAddress, getKnownIndirectEdgesCallback, BinaryScopeTable.TryGetFromFunctionAddress(dna.Binary, cfgAddress).Entries.Select(x => x.HandlerAddr));

            // Finally build a new list of jump tables.
            List<JmpTable> finalTables = new();
            var seen = new HashSet<ulong>();
            foreach(var block in newCfg.GetBlocks())
            {
                // If this is not a jump table block then skip it.
                var jmpFromAddr = block.ExitInstruction.IP;
                if (!mergedTables.ContainsKey(jmpFromAddr))
                    continue;

                // The recursive descent algorithm used in Dna will clone instructions in the case of fallthrough basic blocks.
                // So sometimes an indirect jump may be cloned into multiple basic blocks - but the instruction IP remains the same.
                // To avoid duplicating the jump table object, we keep track of basic blocks that we've already seen.
                if (seen.Contains(jmpFromAddr))
                    continue;
                seen.Add(jmpFromAddr);

                var mergedTable = mergedTables[jmpFromAddr];

                // Get the set of formerly known jump table predecessors(or get an empty set if it previously had no known predecessors).
                var oldPredecessors = oldJmpTablePredecessorMapping.ContainsKey(jmpFromAddr) ? oldJmpTablePredecessorMapping[jmpFromAddr] : new HashSet<ulong>();

                // Get the set of jump table predecessors using the current control flow graph.
                var newPredecessors = GetBlockPredecessors(newCfg, block);

                // If new predecessors to the jump table are discovered, then we consider this to be incomplete. Now the set of possible outgoing edges needs to be solved again.
                bool isComplete = oldPredecessors.SetEquals(newPredecessors);

                // Add the jump to the list.
                var newTable = new JmpTable(jmpFromAddr, mergedTable.KnownOutgoingAddresses.ToList(), newPredecessors.ToList(), isComplete);
                finalTables.Add(newTable);

            }

            return finalTables;
        }

        // Given a list of information about jump tables, concatenate it into
        // one jump table.
        private static JmpTable MergeJumpTable(IReadOnlyList<JmpTable> allTables)
        {
            // Get the jump table address.
            var address = allTables.GroupBy(x => x.JmpFromAddr).Single().Key;

            // Collect all outgoing edges
            var outgoingAddresses = allTables.SelectMany(x => x.KnownOutgoingAddresses).ToHashSet();

            // Collect all known incoming edges. Note that we actually don't need to compute this, because
            // we discard this information later on(because the entire cfg changes anyways when new jump table information is discovered - so we need to recompute the incoming edges.
            var incomingEdges = allTables.SelectMany(x => x.KnownPredecessorAddresses).ToHashSet();

            return new JmpTable(address, outgoingAddresses.ToList(), incomingEdges.ToList());
        }

        private static IReadOnlySet<ulong> GetBlockPredecessors(ControlFlowGraph<Instruction> cfg, X86Block block)
        {
            var queue = new Queue<X86Block>();
            var seen = new HashSet<ulong>();
            queue.Enqueue(block);
            while(queue.Any())
            {
                // Pop the latest item.
                var popped = queue.Dequeue();
                seen.Add(popped.Address);

                foreach(var incomingEdge in popped.GetIncomingEdges())
                {
                    // Skip if we've already added this predecessor to the list.
                    if (seen.Contains(incomingEdge.SourceBlock.Address))
                        continue;

                    queue.Enqueue(incomingEdge.SourceBlock);
                }
            }

            return seen;
        }
    }
}
