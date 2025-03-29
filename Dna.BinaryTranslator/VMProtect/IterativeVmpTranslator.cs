using Dna.BinaryTranslator;
using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.Runtime;
using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Utilities;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public record FunctionWithStateStructure(LLVMValueRef Function, VmpParameterizedStateStructure ParameterizedStateStructure);

    public class IterativeVmpTranslator
    {
        private readonly IDna dna;

        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly LLVMModuleRef outModule;

        private readonly ulong funcRip;

        private readonly VmHandlerCache handlerCache;

        private readonly Dictionary<ulong, ulong> bytecodeAddrToRip = new();

        private OrderedSet<VmHandler> handlers = new OrderedSet<VmHandler>();

        private List<VmpJmpTable> solvedTables = new();

        public IterativeVmpTranslator(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.outModule = CreateOutputModule(ctx, arch);
            this.handlerCache = new VmHandlerCache(ctx);
            this.funcRip = funcRip;
        }

        private static LLVMModuleRef CreateOutputModule(LLVMContextRef ctx, RemillArch arch)
        {
            // Create a new remill module.
            var guid = Guid.NewGuid().ToString();
            var outModule = ctx.CreateModuleWithName("outmodule" + guid);
            outModule.Target = "x86_64-pc-windows-msvc";
            arch.PrepareModuleDataLayout(outModule);
            return outModule;
        }

        public LLVMValueRef Run()
        {
            // Append the first handler to the handler set.
            // Note that we use the random garbage for the bytecode pointer.
            // This is fine because the bytecode pointer of the first handler does not really matter.
            handlers.Add(new VmHandler(funcRip, funcRip));

            HashSet<ulong> vmexitHandlerRips = new();

            // Mark the first handler for lifting.
            var handlersRipsToLift = new OrderedSet<(ulong nativeRip, bool isVmEnter)>();
            handlersRipsToLift.Add((funcRip, true));

            // Signal that the current jmp table is unsolved.
            solvedTables.Add(new(handlers.Single().BytecodeRip, new List<ulong>(), new List<ulong>(), isComplete: false));

            var blockCache = new VmBlockCache(ctx);
            LLVMValueRef output;
            while (true)
            {
                // Identify all VmExit handlers.
                foreach(var (rip, isVmEnter) in handlersRipsToLift)
                {
                    var cfg = dna.RecursiveDescent.ReconstructCfg(rip);
                    var insts = cfg.GetInstructions();
                    var count = insts.Count(x => x.Mnemonic == Iced.Intel.Mnemonic.Pop);
                    if (count > 10)
                        vmexitHandlerRips.Add(rip);
                }

                // If any new handlers have been discovered, lift them to LLVM IR and cache them.
                LiftHandlersIntoCache(handlerCache, dna, handlersRipsToLift);
                // Clear the to-lift worklists.
                handlersRipsToLift.Clear();

                outModule.PrintToFile("translatedFunction.ll");

                // Create a control flow graph out of the current partial CFG.
                // Create a "true" CFG by inlining direct jumps to targets with only one predecessor.
                var vmCfg = GetVmCfg(funcRip, handlers, solvedTables);
                vmCfg = InlineSESEBlocks(vmCfg, solvedTables);
    
                foreach(var t in solvedTables)
                    Console.WriteLine(t);
                
                var partialBlockLifter = new VmPartialBlockLifter(ctx, arch, vmCfg, handlerCache, blockCache, GetJmpTableMapping());
                var liftedBlockMapping = partialBlockLifter.Lift();

                var memPtr = partialBlockLifter.module.GetNamedGlobal("memory");
                if(memPtr.Handle != 0)
                {
                    memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                    var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(partialBlockLifter.module.GetPtrType());
                    memPtr.Initializer = memoryPtrNull;
                }

                partialBlockLifter.module.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
                // Optimize each basic block individually.

                // Canonicalize all mem ptrs first.
                foreach (var func in partialBlockLifter.module.GetFunctions().Where(x => x.Name.Contains("block")))
                {
                    CanonicalizeMemoryPtr(func);
                }

                // Optimize each block
                foreach (var func in partialBlockLifter.module.GetFunctions().Where(x => x.Name.Contains("block")))
                {
                    PassPipeline.Run(dna.Binary, func, false);
                }

                // Connect the basic blocks together, then inline them.
                // TODO: Stop using partial block lifter module
                var stateStruct = handlerCache.GetLiftedHandler(handlers.First().NativeRip).ParameterizedStateStructure;
                var lifter2 = new VmCfgLifter(partialBlockLifter.module, arch, stateStruct, vmCfg, liftedBlockMapping, GetJmpTableMapping(), vmexitHandlerRips.AsReadOnly());
                var liftedFunction = lifter2.Lift();

                // Isolate the lifted function into it's own module.
                liftedFunction = FunctionIsolator.IsolateFunctionInto(outModule, liftedFunction);
                outModule.PrintToFile("translatedFunction.ll");

                var callTargets = outModule.GetFunctions().Where(x => x.Name.Contains("TranslatedFrom"));
                foreach(var caller in callTargets)
                    LLVMCloning.InlineFunction(caller);

                // Create a single @memory pointer.
                CanonicalizeMemoryPtr(liftedFunction);


                // Run our optimization pipeline.
                liftedFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
                PassPipeline.Run(dna.Binary, liftedFunction, false);

                liftedFunction.GlobalParent.WriteToLlFile("translatedFunction.ll");
                Console.WriteLine("Compiling to an exe.");

                // Solve for any unknown indirect jumps in the control flow graph.
                var solver = new VmpJmpTableSolver(liftedFunction);
                var (newTables, bytecodePtrToRips) = solver.Solve();

                foreach(var entry in bytecodePtrToRips)
                {
                    bytecodeAddrToRip.TryAdd(entry.Key, entry.Value);
                    if (bytecodeAddrToRip[entry.Key] != entry.Value)
                    {
                        throw new InvalidOperationException($"Multiple native addresses assigned to the same bytecode ptr!");
                    }
                }

                var newOutgoingEdges = new HashSet<ulong>();
                foreach (var newTable in newTables)
                    newOutgoingEdges.AddRange(newTable.KnownOutgoingAddresses);
                foreach(var  newOutgoingEdge in newOutgoingEdges)
                {
                    // If this is a new edge jump from destination, create an entry for it.
                    if(!solvedTables.Any(x => x.JmpFromAddr == newOutgoingEdge))
                    {
                        solvedTables.Add(new(newOutgoingEdge, new List<ulong>(), new List<ulong>(), isComplete: false));
                    }

                    // If we haven't seen this handler RIP before, add it to the to-be-lifted set.
                    if (handlers.Any(x => x.BytecodeRip == bytecodePtrToRips[newOutgoingEdge]))
                        continue;

                    var newHandler = new VmHandler(newOutgoingEdge, bytecodePtrToRips[newOutgoingEdge]);
                    handlers.Add(newHandler);
                    if(!handlerCache.ContainsHandler(newHandler.NativeRip))
                        handlersRipsToLift.Add((newHandler.NativeRip, false));
                    else
                        Console.WriteLine("seen");
                }
              
                if (!newTables.Any())
                {
                    memPtr = outModule.GetNamedGlobal("memory");
                    if (memPtr.Handle != 0)
                    {
                        memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                        var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(outModule.GetPtrType());
                        memPtr.Initializer = memoryPtrNull;
                    }

                    output = liftedFunction;

                    outModule.WriteBitcodeToFile("translatedFunction.bc");
                    outModule.PrintToFile("translatedFunction.ll");
                    outModule.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    Debugger.Break();
                    break;
                }

                // Update the set of jump tables while merging newly collected information.
                solvedTables = MergeJumpTables(funcRip, dna, solvedTables, newTables);

                // Throw away the old function. This reduces wasted time on optimizing irrelevant functions.
                liftedFunction.DeleteFunction();

                // Create a control flow graph out of the current partial CFG.
                var newCfg = GetVmCfg(funcRip, handlers, solvedTables);

                // Create a "true" CFG by inlining direct jumps to targets with only one predecessor.
                newCfg = InlineSESEBlocks(newCfg, solvedTables);

                // Convert the cfg to binja IR
                var viewer = new BinjaVmCfgViewer(newCfg);
                var compiled = viewer.Run();
                File.WriteAllText("vmcfg.py", compiled);

                // Now we need to figure out which basic blocks we can cache.
                // IF a partial block remains unchanged, we ca
                var srcModule = partialBlockLifter.module;
                blockCache = ConstructBlockCache(ctx, srcModule, newCfg, GetJmpTableMapping(), liftedBlockMapping);
            }

            return output;
        }

        private static void LiftHandlersIntoCache(VmHandlerCache handlerCache, IDna dna, OrderedSet<(ulong nativeRip, bool isVmEnter)> handlerRipsToLift)
        {
            var output = new List<(ulong nativeRip, FunctionWithStateStructure function)>();
            foreach (var handler in handlerRipsToLift)
            {
                // Create a new handler extractor.
                var extractor = new VmpHandlerExtractor(dna);

                // Get a sequential list of instructions for the handler.
                // This is only legal because VMP handlers do not have legitimate branches.
                var handlerInstructions = extractor.Process(handler.nativeRip, handler.isVmEnter);

                // Lift the handler to LLVM IR.
                var traceLifter = new ObfuscatedTraceLifter(handlerCache.CacheModule.Context, dna, handlerInstructions);
                var (arch, function) = traceLifter.Lift();

                // Clone the function into our persistent module.
                var outputFunction = FunctionIsolator.IsolateFunctionInto(handlerCache.CacheModule, function.OutputFunction);

                // Optimize the handler.
                PassPipeline.Run(dna.Binary, outputFunction);

                // Add the handler to the cache
                handlerCache.AddFunction(handler.nativeRip, new FunctionWithStateStructure(outputFunction, function));
            }
        }

        private static void CanonicalizeMemoryPtr(LLVMValueRef function)
        {
            // Create a single @memory pointer.
            var ctx = function.GlobalParent.Context;
            var memoryPtr = function.GlobalParent.GetGlobals().Single(x => x.Name.Contains("memory"));
            var builder = LLVMBuilderRef.Create(ctx);
            builder.Position(function.EntryBasicBlock, function.EntryBasicBlock.FirstInstruction);
            var dominatingLoad = builder.BuildLoad2(ctx.GetPtrType(), memoryPtr, "mem");

            var targets = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == memoryPtr && x != dominatingLoad).ToList();
            if (targets.Any())
            {
                foreach (var other in targets)
                {
                    other.ReplaceAllUsesWith(dominatingLoad);
                    other.InstructionEraseFromParent();
                }
            }
        }

        private IReadOnlyDictionary<ulong, VmpJmpTable> GetJmpTableMapping()
        {
            var output = new Dictionary<ulong, VmpJmpTable>();
            foreach (var table in solvedTables)
            {
                output.Add(table.JmpFromAddr, table);
            }

            return output;
        }

        private static ControlFlowGraph<VmHandler> GetVmCfg(ulong funcRip, OrderedSet<VmHandler> handlers, IReadOnlyList<VmpJmpTable> solvedTables)
        {
            // TODO: If a handler is considered "solved"(all known outgoing edges are added), inline it 
            // into it's parent.
            var cfg = new ControlFlowGraph<VmHandler>(funcRip);
            var entryBb = cfg.CreateBlock(funcRip);
            entryBb.Instructions.Add(handlers.First());

            var handlerToBlock = new Dictionary<ulong, BasicBlock<VmHandler>>();
            handlerToBlock.Add(handlers.First().BytecodeRip, entryBb);
            foreach (var vmHandler in handlers.Where(x => x != handlers.First()))
            {
                var newBlock = cfg.CreateBlock(vmHandler.BytecodeRip);
                newBlock.Instructions.Add(vmHandler);
                handlerToBlock.Add(vmHandler.BytecodeRip, newBlock);
            }

            var handlerToJmpTable = new Dictionary<VmHandler, VmpJmpTable>();
            foreach (var handler in handlers)
            {
                var jmpTableEntry = solvedTables.SingleOrDefault(x => x.JmpFromAddr == handler.BytecodeRip);
                if (jmpTableEntry == null)
                    continue;

                handlerToJmpTable.Add(handler, jmpTableEntry);
            }

            foreach (var handler in handlers)
            {
                // Skip if there's no jump table entry for this handler.
                if (!handlerToJmpTable.TryGetValue(handler, out var jmpTable))
                    continue;

                // If there is a known outgoing edge from this handler, add it.
                var srcBlock = handlerToBlock[handler.BytecodeRip];
                foreach (var outgoingAddr in jmpTable.KnownOutgoingAddresses)
                {
                    var dstBlock = handlerToBlock[outgoingAddr];
                    srcBlock.AddOutgoingEdge(new(srcBlock, dstBlock));
                }
            }

            return cfg;
        }

        private List<VmpJmpTable> MergeJumpTables(ulong cfgAddress, IDna dna, IReadOnlyList<VmpJmpTable> oldTableInformation, IReadOnlyList<VmpJmpTable> newTableInformation)
        {
            // For each previously proved set of jmp table information (note that there should only be one JmpTable object per jmp table 'from' address),
            // collect a mapping of <jmp from address, set of outgoing edges>.
            var oldJmpTablePredecessorMapping = new Dictionary<ulong, HashSet<ulong>>();
            foreach (var table in oldTableInformation)
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

            // Reconstruct a new control flow graph.
            var newCfg = GetVmCfg(cfgAddress, handlers, mergedTables.Select(x => x.Value).ToList());

            // Finally build a new list of jump tables.
            List<VmpJmpTable> finalTables = new();
            var seen = new HashSet<ulong>();
            foreach (var block in newCfg.GetBlocks())
            {
                // If this is not a jump table block then skip it.
                var jmpFromAddr = block.Address;
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
                var newTable = new VmpJmpTable(jmpFromAddr, mergedTable.KnownOutgoingAddresses.ToList(), newPredecessors.ToList(), isComplete);
                finalTables.Add(newTable);
            }

            return finalTables;
        }

        // Given a list of information about jump tables, concatenate it into
        // one jump table.
        private static VmpJmpTable MergeJumpTable(IReadOnlyList<VmpJmpTable> allTables)
        {
            // Get the jump table address.
            var address = allTables.GroupBy(x => x.JmpFromAddr).Single().Key;

            // Collect all outgoing edges
            var outgoingAddresses = allTables.SelectMany(x => x.KnownOutgoingAddresses).ToHashSet();

            // Collect all known incoming edges. Note that we actually don't need to compute this, because
            // we discard this information later on(because the entire cfg changes anyways when new jump table information is discovered - so we need to recompute the incoming edges.
            var incomingEdges = allTables.SelectMany(x => x.KnownPredecessorAddresses).ToHashSet();

            return new VmpJmpTable(address, outgoingAddresses.ToList(), incomingEdges.ToList());
        }

        private static IReadOnlySet<ulong> GetBlockPredecessors(ControlFlowGraph<VmHandler> cfg, BasicBlock<VmHandler> block)
        {
            var queue = new Queue<BasicBlock<VmHandler>>();
            var seen = new HashSet<ulong>();
            queue.Enqueue(block);
            while (queue.Any())
            {
                // Pop the latest item.
                var popped = queue.Dequeue();
                seen.Add(popped.Address);

                foreach (var incomingEdge in popped.GetIncomingEdges())
                {
                    // Skip if we've already added this predecessor to the list.
                    if (seen.Contains(incomingEdge.SourceBlock.Address))
                        continue;

                    queue.Enqueue(incomingEdge.SourceBlock);
                }
            }

            return seen;
        }

        // Get a cfg where single entry single exit regions are turned into basic blocks.
        private static ControlFlowGraph<VmHandler> InlineSESEBlocks(ControlFlowGraph<VmHandler> cfg, IReadOnlyList<VmpJmpTable> solvedTables)
        {
            // Collect each potential block starting point.
            HashSet<VmHandler> blockStarts = GetLabels(cfg, solvedTables).Select(x => x.Instructions.Single()).ToHashSet();

            // Walk through the old cfg, building up a mapping of outgoing edges from handler to handler.
            var oldOutgoingEdgeMapping = new Dictionary<VmHandler, HashSet<VmHandler>>();
            foreach(var block in cfg.GetBlocks())
            {
                var edgeSet = block.GetOutgoingEdges().Select(x => x.TargetBlock.Instructions.Single()).ToHashSet();
                oldOutgoingEdgeMapping.Add(block.Instructions.Single(), edgeSet);
            }

            // Wrapper method for fetching the outgoing edges of a block.
            var getEdges = (VmHandler handler) =>
            {
                return oldOutgoingEdgeMapping[handler];
            };
            

            // Create a new cfg.
            var newCfg = new ControlFlowGraph<VmHandler>(cfg.StartAddress);

            // Map each known block starting point to a block in the new cfg.
            var handlerToBlock = new Dictionary<VmHandler, BasicBlock<VmHandler>>();
            foreach (var handler in blockStarts)
            {
                var newBlock = newCfg.CreateBlock(handler.BytecodeRip);
                newBlock.Instructions.Add(handler);
                handlerToBlock.Add(handler, newBlock);
            }

            // Push all block starts to be visited.
            var queue = new Stack<VmHandler>();
            foreach(var block in blockStarts)
                queue.Push(block);

            // Walk through each block, turning sequences of known direct handler->handler jumps into straightline basic blocks.
            while (queue.Any())
            {
                // Fetch a block to process.
                var owningHandler = queue.Pop();
                var owningBlock = handlerToBlock[owningHandler];

                var current = owningHandler;
                while(true)
                {
                    // Fetch the outgoing edges.
                    IReadOnlySet<VmHandler> outgoingEdges = getEdges(current);

                    // If there is only one outgoing edge and the target does not belong to another block, inline it.
                    if(outgoingEdges.Count == 1 && !blockStarts.Contains(outgoingEdges.Single()))
                    {
                        var next = outgoingEdges.Single();
                        owningBlock.Instructions.Add(next);
                        current = next;
                        continue;
                    }

                    // Otherwise this is either a multiple destination jump(jcc or switch), OR we don't own the target.
                    // Insert the edges and then move onto the next owning block by breaking out of this loop.
                    else
                    {
                        foreach(var target in outgoingEdges)
                        {
                            var targetBlock = handlerToBlock[target];
                            owningBlock.OutgoingEdges.Add(new BlockEdge<VmHandler>(owningBlock, targetBlock));
                        }

                        break;
                    }
                }
            }

            return newCfg;
        }

        // Get all basic block starting points
        private static IReadOnlySet<BasicBlock<VmHandler>> GetLabels(ControlFlowGraph<VmHandler> cfg, IReadOnlyList<VmpJmpTable> solvedTables)
        {
            // Get all blocks.
            var blocks = cfg.GetBlocks();

            // Append the entry basic block.
            var set = new HashSet<BasicBlock<VmHandler>>();
            set.Add(blocks.First());

            // Build a mapping of <bytecode ptr, jmp table entry>
            var bytecodeToJmpTable = new Dictionary<ulong, VmpJmpTable>();
            foreach(var table in solvedTables)
                bytecodeToJmpTable.Add(table.JmpFromAddr, table);

            // Append all basic blocks that are not complete.
            var incompleteBlocks = blocks.Where(x => !bytecodeToJmpTable[x.Address].IsComplete).ToHashSet();
            set.AddRange(incompleteBlocks);

            // Append all blocks with more than one predecessor.
            var multiPredecessorBlocks = blocks.Where(x => x.IncomingEdges.Count > 1).ToHashSet();
            set.AddRange(multiPredecessorBlocks);

            // Collect and append all targets of conditional jumps(or switch statements).
            var multiExitBlocks = blocks.Where(x => x.OutgoingEdges.Count > 1).ToHashSet();
            var exitTargets = multiExitBlocks.SelectMany(x => x.GetOutgoingEdges()).Select(x => x.TargetBlock).ToHashSet();
            set.AddRange(exitTargets);
            return set;
        }

        private static VmBlockCache ConstructBlockCache(LLVMContextRef ctx, LLVMModuleRef srcModule, ControlFlowGraph<VmHandler> updatedCfg, IReadOnlyDictionary<ulong, VmpJmpTable> updatedJmpTables, IReadOnlyDictionary<VmHandler, (LLVMValueRef func, BasicBlock<VmHandler> block)> liftedBlockMapping)
        {
            // The updated cfg is a new control flow graph created with the newly solved control flow edges taken into account.
            // Now we need to build a mapping between vmhandler and the corresponding basic block within the new control flow graph.
            Dictionary<VmHandler, BasicBlock<VmHandler>> handlerToNewBlock = new();
            foreach(var block in updatedCfg.GetBlocks())
            {
                var handler = block.EntryInstruction;
                handlerToNewBlock.Add(handler, block);
            }

            var blockCache = new VmBlockCache(ctx, srcModule);
            foreach (var (handler, (func, block)) in liftedBlockMapping)
            {
                // Skip if a cached block with this instruction pointer is not present.
                if (!handlerToNewBlock.ContainsKey(handler))
                    continue;

                // Fetch the old and new basic block.
                var oldBlock = block;
                var newBlock = handlerToNewBlock[handler];

                // Skip if it's not safe to cache.
                if (!IsBlockCacheable(oldBlock, newBlock, updatedJmpTables))
                    continue;

                blockCache.AddBlock(oldBlock, func);
            }

            return blockCache;
        }

        private static bool IsBlockCacheable(BasicBlock<VmHandler> oldBlock, BasicBlock<VmHandler> newBlock, IReadOnlyDictionary<ulong, VmpJmpTable> jmpTableMapping)
        {
            // Make sure that the exploration algorithm is only adding new information to basic blocks.
            // In theory this assumption will break if a jump into the middle of a block is discovered later on.
            if (oldBlock.Instructions.Count > newBlock.Instructions.Count)
                return false;
                //throw new InvalidOperationException($"Block size should not be decreasing during iterative exploration!");

            // If there is a mismatch of instructions between the blocks, we can't cache it. Although this should really never happen.e
            var inBoth = newBlock.Instructions.Take(oldBlock.Instructions.Count).ToList();
            if (!oldBlock.Instructions.SequenceEqual(inBoth))
                return false;
            // If any of shared instructions are now considered incomplete, this should never happen.
            if (inBoth.Any(x => !jmpTableMapping[x.BytecodeRip].IsComplete))
                return false;

            // Otherwise we've proved that the old block is an exact subset of the new block. 
            // Therefore we can cache it safely.
            return true;
        }
    }

}
