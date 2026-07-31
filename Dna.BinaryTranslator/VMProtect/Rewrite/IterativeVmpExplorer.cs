using Bitwuzla;
using Dna.Binary;
using Dna.BinaryTranslator.JmpTables;
using Dna.BinaryTranslator.Lifting;
using Dna.BinaryTranslator.Unsafe;
using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop;
using Dna.LLVMInterop.API.LLVMBindings;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Passes;
using Dna.Passes.Mba;
using Dna.Relocation;
using Dna.SEH;
using Dna.Utilities;
using FASTER.core;
using Iced.Intel;
using LLVMSharp.Interop;
using Mba.Common.Ast;
using Mba.Simplifier;
using Mba.Simplifier.Bindings;
using Mba.Simplifier.Utility;
using Mba.Utility;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Z3;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using WebAssembly.Instructions;
using static Dna.BinaryTranslator.VMProtect.VmpJmpTableSolver;
using static Iced.Intel.AssemblerRegisters;


using VmCfg = Dna.ControlFlow.InstGraph<Dna.BinaryTranslator.VMProtect.VmHandler, Dna.BinaryTranslator.VMProtect.Rewrite.HandlerMetadata>;


namespace Dna.BinaryTranslator.VMProtect.Rewrite
{
    public struct HandlerMetadata
    {
        public bool IsComplete;
    }

    public record Ok<T>(T Value);
    public record Err<E>(E Error);
    public union Result<T, E>(Ok<T>, Err<E>);
    public static class ResultExtensions
    {
        public static bool IsOk<T, E>(this Result<T, E> result) => result is Ok<T>;

        public static bool IsErr<T, E>(this Result<T, E> result) => result is Err<E>;

        public static T Ok<T, E>(this Result<T, E> result) => ((Ok<T>)result.Value).Value;

        public static E Err<T, E>(this Result<T, E> result) => ((Err<E>)result.Value).Error;
    }


    public record HandlerData(RemillRegister Vip, RemillRegister Vkey, RemillRegister ImgBaseReg, bool hasVkeyStackSlot = false);

    public class IterativeVmpExplorer
    {
        private readonly IDna dna;

        private readonly RemillArch arch;

        private readonly LLVMContextRef ctx;

        private readonly ulong funcRip;

        //private readonly VmHandlerCache handlerCache;

        private readonly Dictionary<ulong, ulong> bytecodeAddrToRip = new();

        private OrderedSet<VmHandler> handlers = new OrderedSet<VmHandler>();

        public IterativeVmpExplorer(IDna dna, RemillArch arch, LLVMContextRef ctx, ulong funcRip)
        {
            this.dna = dna;
            this.arch = arch;
            this.ctx = ctx;
            this.funcRip = funcRip;
            //this.handlerCache = new VmHandlerCache(ctx);
        }

        public static Func<BasicBlock<Instruction>, IEnumerable<ulong>> DescentCallback(IDna dna)
        {
            return (BasicBlock<Instruction> x) => { return DescentCallback(dna, x); };
        }

        public static IEnumerable<ulong> DescentCallback(IDna dna, BasicBlock<Instruction> block)
        {
            var inst = block.ExitInstruction;
            if (inst.Mnemonic != Mnemonic.Call)
                return null;
            if (!inst.HasImmediateBranchTarget())
                return null;
            var target = inst.GetImmediateBranchTarget();
            return new ulong[] { target };
        }

        public static Func<ulong, bool> ShouldContinueCallback(IDna dna)
        {
            return (ulong x) => { return ShouldContinueCallback(dna, x); };
        }

        public static bool ShouldContinueCallback(IDna dna, ulong ip)
        {
            var inst = dna.BinaryDisassembler.GetInstructionAt(ip);
            if (inst.Mnemonic == Mnemonic.Call)
                return false;

            return true;
        }


        public LLVMValueRef Run()
        {
            arch.GetOrLoadSemantics();
            var outModule = IterativeVmpTranslator.CreateOutputModule(ctx, arch);

            // Append the first handler to the handler set.
            // Note that we use the random garbage for the bytecode pointer.
            // This is fine because the bytecode pointer of the first handler does not really matter.
            handlers.Add(new VmHandler(funcRip, funcRip));
            bytecodeAddrToRip[funcRip] = funcRip;

            HashSet<ulong> vmexitHandlerRips = new();

            // Mark the first handler for lifting.
            var handlersRipsToLift = new OrderedSet<(ulong nativeRip, bool isVmEnter)>();
            handlersRipsToLift.Add((funcRip, true));

            // Append the handler to the VM cfg
            var vCfg = new VmCfg();
            vCfg.GetOrAdd(handlers.First());
            vCfg.Instructions[handlers.First()].Metadata.IsComplete = false;

            var blockCache = new VmBlockCache(ctx);
            LLVMValueRef liftedFunction = default;
            int ii = 0;
            var handlerLifter = new HandlerLifter(dna, ctx, arch);

            var stateStruct = new VmpParameterizedStateStructure(arch, ctx, false);
            var stateStruct2 = new ParameterizedStateStructure(arch, ctx, false, true, false);
            RemillRegister bytecodeRegister = handlerLifter.GetVmenterBytecodeRegister(stateStruct2, handlerLifter.LiftHandler(funcRip, true));
            Dictionary<VmHandler, RemillRegister> handlerVips = new();
            Dictionary<ulong, HandlerData> handlerRipToRegisters = new();
            Dictionary<VmHandler, ulong> handlerToVkey = new();
            Dictionary<VmHandler, ulong> handlerToImagebase = new();


            Console.WriteLine($"TODO: Stop passing bytecoderegister as vkey for first handler");
            handlerRipToRegisters[handlers.First().NativeRip] = new HandlerData(bytecodeRegister, bytecodeRegister, bytecodeRegister);
            handlerVips[handlers.First()] = bytecodeRegister;

            bool optHeavy = false;

            var serialize = () =>
            {
                var sb = new StringBuilder();
                var handlers = vCfg.Instructions.Keys.OrderBy(x => x.BytecodeRip);
                foreach (var handler in handlers)
                {
                    sb.AppendLine($"0x{handler.BytecodeRip.ToString("X")}:");
                    sb.AppendLine($"    NativeRIP: {handler.NativeRip.ToString("X")}");

                    //var regs = handlerRipToRegisters[handler.BytecodeRip];
                    if (handlerRipToRegisters.TryGetValue(handler.NativeRip, out var regs))
                    {
                        var vipName = regs.Vip == null ? "null" : regs.Vip.Name;
                        var vkeyName = regs.Vkey == null ? "null" : regs.Vkey.Name;
                        sb.AppendLine($"    VIP: {vipName}");
                        sb.AppendLine($"    VKEY: {vkeyName}");
                    }

                    ulong? vkeyImm = handlerToVkey.TryGetValue(handler, out var existing) ? existing : null;
                    sb.AppendLine($"    VKEY_IMM: {vkeyImm}");

                    var succs = vCfg.Instructions[handler].Successors.Select(x => x.BytecodeRip.ToString("X"));
                    sb.AppendLine($"    SUCCS: {String.Join(", ", succs)}");
                }

                File.WriteAllText($"iter/{ii}_groundTruth.txt", sb.ToString());
            };

            // For each handler RIP, store the vip/vkey

            if (false)
            {
                handlerLifter.cacheModule.PrintToFile("translatedFunction.ll");
                foreach (var rip in handlerLifter.handlerRipToLlvmFunction)
                {
                    var key = rip.Key;
                    if (rip.Key == handlers.First().BytecodeRip)
                        continue;


                    var vip = GetVipByUsage(rip.Key, rip.Value, stateStruct);
                    if (vip == null)
                    {
                        Debugger.Break();
                        continue;
                    }

                    //var otherReg = handlerLifter.GetBytecodeRegister(cfg)

                    Console.WriteLine($"Found vip");

                    var vkey = GetVkeyByUsage(rip.Key, rip.Value, stateStruct, vip);


                }
            }
            var sw = Stopwatch.StartNew();
            //handlerLifter.LiftHandler(0x140048BBD, false);
            while (true)
            {

                if (vCfg.Instructions.ContainsKey(new VmHandler(0x14000FE70, 0)))
                {
                    // liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                    // Debugger.Break();
                }


                // It's failing once we hit the loop?
                if (ii == 960)
                {
                    var name = Guid.NewGuid().ToString() + ".ll";
                    liftedFunction.GlobalParent.PrintToFile((name));

                    var compiledPath3 = ClangCompiler.Compile(name);

                    Console.WriteLine("Loading into IDA.   ");
                    var exePath3 = IDALoader.Load(compiledPath3, true);
                    Debugger.Break();
                }




                //AdhocInstCombinePass.bar = numFast;
                Console.WriteLine($"Lifting iteration {ii++} at {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"{numFast} / {numFast + numHeavy} solvers finished");

                if (liftedFunction.Handle != 0)
                    liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                serialize();

                var text = new BinjaVmCfgViewerV2(vCfg).Run(handlerRipToRegisters, handlerToVkey);
                File.WriteAllText("binja.py", text);

                /*
                // see VIPs.txt
                if (false && ii == 551)
                {
                    Console.WriteLine($"VIPs: ");
                    foreach(var inst in vCfg.Instructions)
                    {
                        var incomingVip = inst.Key.BytecodeRip == funcRip ? bytecodeRegister : inst.Value.Predecessors.Select(x => handlerVips[x]).DistinctBy(x => x.Name).Single();
                        var outgoingVip = handlerVips[inst.Key];

                        Console.WriteLine($"{inst.Key.BytecodeRip.ToString("X")} {incomingVip} {outgoingVip}");
                    }

                    Debugger.Break();
                }
                */

                //var join = String.Join(", ", handlerLifter.handlerRipToLlvmFunction.Keys.Select(x => "0x" + x.ToString("X")));
                //Console.WriteLine(join);


                //// Identify all VmExit handlers.
                //foreach (var (rip, isVmEnter) in handlersRipsToLift)
                //{
                //    //handlerLifter.LiftHandler(rip, handlers.Count == 1);
                //    var cfg = HandlerLifter.DisHandler(dna, rip);
                //    if (HandlerLifter.IsVmexit(cfg))
                //        vmexitHandlerRips.Add(rip);
                //}


                // Lift all native handlers to LLVM IR and cache them
                //IterativeVmpTranslator.LiftHandlersIntoCache(arch, handlerCache, dna, handlersRipsToLift);
                handlersRipsToLift.Clear();

                // Lift the partial CFG
                //var stateStruct = handlerCache.GetLiftedHandler(handlers.First().NativeRip).ParameterizedStateStructure;

                // So the problem is with rebuilding???
                // ii >= 1040 triggers it
                //if (ii >= 1 && liftedFunction.Handle != 0)
                //if (ii >= 620 && liftedFunction.Handle != 0)
                //if (false)
                //if (ii >= 730 && liftedFunction.Handle != 0)
                if (false)
                {
                    liftedFunction.DeleteFunction();
                    liftedFunction.Handle = 0;
                }
                AdhocInstCombinePass.Validate(liftedFunction);
                liftedFunction = new IterativeCfgBuilder(dna, outModule, arch, stateStruct, vCfg, handlerLifter, vmexitHandlerRips, handlerVips, handlerToVkey, handlerToImagebase, handlerRipToRegisters).Run(liftedFunction, handlers.First());
                AdhocInstCombinePass.Validate(liftedFunction);
                FixMemPtr(liftedFunction.GlobalParent);

                AdhocInstCombinePass.Validate(liftedFunction);

                //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                liftedFunction.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

                IterativeVmpTranslator.CanonicalizeMemoryPtr(liftedFunction);

                //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                // Run our optimization pipeline
                //PassPipeline.Run(dna.Binary, liftedFunction, false);
                //VmpPassPipeline.Run(dna.Binary, liftedFunction);

                //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                // Solve for any unknown indirect jumps in the control flow graph.
                //var solver = new VmpJmpTableSolver(liftedFunction);
                //var (newTables, bytecodePtrToRips) = solver.Solve();

                /*
                var solver = new VmpSolver(arch, liftedFunction);

                var dests = solver.SolveRIPs(stateStruct);

                var sum = String.Join(", ", stateStruct.RegisterArgumentIndices.OrderBy(x => x.Value).Select(x => $"{x.Key.Name} {x.Value}"));
                Console.WriteLine($"[{sum}]");

                // Compute the VIP/vkey reg for each handler
                foreach(var (bytecodePtr, outgoingHandlers) in dests)
                {
                    // Compute the VIP and vkey
                    var regs = GetHandlerRegisters(stateStruct, stateStruct2, handlers.First(), handlerLifter, bytecodePtr, outgoingHandlers, handlerRipToRegisters);

                    foreach (var rip in outgoingHandlers)
                        handlerRipToRegisters[rip] = regs;
                }


                var (newTables, bytecodePtrToRips) = solver.Solve(handlerRipToRegisters, stateStruct);
                */


                //if (ii == 107)
                //{
                //    handlerLifter.cacheModule.PrintToFile("dbgHandlers.ll");
                //    liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                //    Debugger.Break();
                //}

                if (ii == 531)
                {
                    //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                    // Debugger.Break();
                }

                if (optHeavy)
                {
                    OptimizeHeavy(liftedFunction);
                    optHeavy = false;
                }

                var solution = OptimizeAndSolve(stateStruct, stateStruct2, liftedFunction, handlerLifter, handlerRipToRegisters);
                if (solution is Err<InvalidOperationException> err)
                {
                    liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                    var hname = "DBG_HandlerCache.ll";
                    handlerLifter.cacheModule.PrintToFile(hname);
                    ClangCompiler.Compile(hname);

                    throw err.Error;
                }

                var (newTables, bytecodePtrToRips) = solution.Ok();

                Console.WriteLine($"Target handler RIPs: {String.Join(" ", bytecodePtrToRips.Select(x => x.Value.rip.ToString("X")))}");

                /*
                if (liftedFunction.GetInstructions().Any(x => x.InstructionOpcode == LLVMOpcode.LLVMXor && x.GetOperand(1).IsConstant() && x.GetOperand(1).ConstIntZExt == 1685814885))
                {
                    liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                    Debugger.Break();
                }
                */
                if (!newTables.Any())
                {
                    OptimizeHeavy(liftedFunction);

                    Console.WriteLine($"Finished devirt");
                    outModule.PrintToFile("translatedFunction.ll");
                    IDALoader.Load(ClangCompiler.Compile("translatedFunction.ll"));
                    Console.WriteLine("Finished devirt");

                    Debugger.Break();
                    Console.ReadLine();
                }

                foreach (var entry in bytecodePtrToRips)
                {
                    if (entry.Value.vkey != null)
                        handlerToVkey[new VmHandler(entry.Key, 0)] = entry.Value.vkey.Value;
                    if (entry.Value.vbase != null)
                        handlerToImagebase[new VmHandler(entry.Key, 0)] = entry.Value.vbase.Value;

                    var cfg = HandlerLifter.DisHandler(dna, entry.Value.rip);
                    if (HandlerLifter.IsVmexit(cfg))
                        vmexitHandlerRips.Add(entry.Value.rip);

                    if (bytecodeAddrToRip.TryAdd(entry.Key, entry.Value.rip) && !handlerLifter.ContainsHandler(entry.Value.rip))
                    {
                        //handlerLifter.LiftHandler(rip, handlers.Count == 1);


                    }


                    if (bytecodeAddrToRip[entry.Key] != entry.Value.rip)
                        throw new InvalidOperationException($"Multiple native addresses assigned to the same bytecode ptr!");
                }

                var cloneAddr = (VmHandler x) => x;
                var cloneMetadata = (HandlerMetadata x) => x;
                var newCfg = vCfg.Clone(cloneAddr, cloneMetadata);

                var getHandler = (ulong x) => new VmHandler(x, bytecodeAddrToRip[x]);

                foreach (var table in newTables)
                {
                    // Update the CFG with new edge info
                    var handler = getHandler(table.JmpFromAddr);
                    foreach (var pred in table.KnownPredecessorAddresses)
                        newCfg.AddEdge(getHandler(pred), handler);
                    foreach (var succ in table.KnownOutgoingAddresses)
                        newCfg.AddEdge(handler, getHandler(succ));
                }

                //if (ii == 529)
                //   Debugger.Break();

                // Get all newly added nodes & nodes with new predecessors
                bool isRebuildRequired = false;
                WorkList<VmHandler> worklist = new WorkList<VmHandler>();
                foreach (var (handler, info) in newCfg.Instructions)
                {
                    // All new nodes get marked as changed
                    if (!vCfg.Contains(handler))
                    {
                        worklist.AddToBack(handler);

                        var incomingVip = info.Predecessors.Select(x => handlerVips[x]).DistinctBy(x => x.Name).Single();
                        handlerVips[handler] = handlerLifter.GetBytecodeRegister(HandlerLifter.DisHandler(dna, handler.NativeRip), stateStruct2, handlerLifter.LiftHandler(handler.NativeRip, false), false, incomingVip);

                        continue;
                    }

                    // If a node has new predecessors, it should be marked as changed.
                    var oldInfo = vCfg.Instructions[handler];
                    if (!oldInfo.Predecessors.SetEquals(info.Predecessors))
                    {
                        worklist.AddToBack(handler);
                        isRebuildRequired = true;
                        continue;
                    }
                }

                // Get all reachable nodes starting from the worklist
                // (this is includes the changed worklist members themselves)



                var reachableNodes = GetReachableNodes(newCfg, worklist);
                foreach (var (handler, info) in newCfg.Instructions)
                {
                    info.Metadata.IsComplete = !reachableNodes.Contains(handler);

                    // If a formerly complete node becomes incomplete, we need to rebuild the full CFG!
                    if (reachableNodes.Contains(handler) && vCfg.Contains(handler))
                        isRebuildRequired = true;

                }

                var (oldCfg, oldLabels) = GetCfg(vCfg, handlers.First());

                var (tCfg, tLabels) = GetCfg(newCfg, handlers.First());

                if (!oldLabels.Keys.ToHashSet().SetEquals(tLabels.Keys))
                {
                    Console.WriteLine($"Iteration {ii} requires full opt");
                    //OptimizeHeavy(liftedFunction);
                    //Debugger.Break();

                    optHeavy = true;
                }


                // Replace the CFG
                vCfg = newCfg;


                var printInst = (VmHandler x) =>
                {
                    return $"0x{x.BytecodeRip.ToString("X")} {vCfg.Instructions[x].Metadata.IsComplete}";
                };

                //Console.WriteLine("\n\n" + GraphFormatter.FormatGraph(tCfg, printInst));

                //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");

                if (false && ii == 500)
                {
                    FixMemPtr(handlerLifter.cacheModule);
                    handlerLifter.cacheModule.PrintToFile("cacheModule.ll");

                    /*
                    foreach(var h in handlerLifter.handlerRipToLlvmFunction.Keys)
                    {
                        handlerLifter.Lift(liftedFunction.GlobalParent, h, h == funcRip);
                    }

                    liftedFunction.GlobalParent.PrintToFile("allHandlers.ll");

                    Console.WriteLine("Done");
                    */
                    Debugger.Break();
                }

                //isRebuildRequired = true;
                if (isRebuildRequired)
                {
                    Console.WriteLine("Forcing rebuild!");
                    liftedFunction.DeleteFunction();
                    liftedFunction = null;
                }

                //Debugger.Break();
            }

            // TODO tomorrow: Hook up incremental algorithm
            Debugger.Break();
            return default;
        }

        static int numFast = 0;
        static int numHeavy = 0;

        static int si = 0;

        private Result<JmpTablesWithHandlerRips2, InvalidOperationException> OptimizeAndSolve(VmpParameterizedStateStructure stateStruct, ParameterizedStateStructure stateStruct2, LLVMValueRef function, HandlerLifter handlerLifter, Dictionary<ulong, HandlerData> handlerRipToRegisters)
        {
            si++;
            //foreach (var inst in function.GetInstructions())
            //   ConstantFoldingAPI.DropPoisonGeneratingFlags(inst);

            var solve = () => TrySolve(stateStruct, stateStruct2, function, handlerLifter, handlerRipToRegisters);



            if (si % 10 == 0)
            {
                OptimizeHeavy(function);
                si++;

                var solution = solve();
                if (solution is Ok<JmpTablesWithHandlerRips2> ok0)
                {
                    return ok0;
                }
            }

            int iter = 0;
            while (iter < 5)
            {
                var optimizeFast = () =>
                {
                    var storeToLoad = new CombinedFixedpointOptPass(dna.Binary, new FixedpointPassConfig());
                    var pStoreToLoad = Marshal.GetFunctionPointerForDelegate(storeToLoad.PtrToStoreLoadPropagation);
                    OptimizationApi.OptimizeModuleVmp(function.GlobalParent, function, false, false, 0, false, 0, false, false, 0, pStoreToLoad, 0, 0, fastPipeline: true);
                };



                // In most cases we solve for the VIPs using a simple and cheap pipeline.
                for (int i = 0; i < 2; i++)
                {
                    numFast += 1;
                    optimizeFast();
                    //OptimizeHeavy(function);
                }

                var solution = solve();
                if (solution is Ok<JmpTablesWithHandlerRips2> ok0)
                {
                    return ok0;
                }

                OptimizeHeavy(function);
                numHeavy += 1;
                if (solution is Ok<JmpTablesWithHandlerRips2> ok1)
                    return ok1;

                iter++;
            }

            Console.WriteLine($"All iterations failed");

            //function.GlobalParent.PrintToFile("translatedFunction.ll");

            return solve();
        }

        private void OptimizeHeavy(LLVMValueRef function)
        {
            var storeToLoad = new CombinedFixedpointOptPass(dna.Binary, new FixedpointPassConfig());
            var pStoreToLoad = Marshal.GetFunctionPointerForDelegate(storeToLoad.PtrToStoreLoadPropagation);
            storeToLoad.config.InstSimplify = true;


            var instCombine = new AdhocInstCombinePass();
            var pInstCombine = Marshal.GetFunctionPointerForDelegate(instCombine.PtrToStoreLoadPropagation);

            var multiUseCloning = new MultiUseCloningPass();
            var pMultiUseCloning = Marshal.GetFunctionPointerForDelegate(multiUseCloning.PtrToStoreLoadPropagation);

            //if (i != 0 && i % 2 != 0)
            //    MbaDeobfuscationPass.Run(function);
            bool useCloning = true;
            OptimizationApi.OptimizeModuleVmp(function.GlobalParent, function, false, false, 0, false, 0, false, false, 0, pStoreToLoad, pInstCombine, useCloning ? pMultiUseCloning : 0);
        }

        private Result<JmpTablesWithHandlerRips2, InvalidOperationException> TrySolve(VmpParameterizedStateStructure stateStruct, ParameterizedStateStructure stateStruct2, LLVMValueRef liftedFunction, HandlerLifter handlerLifter, Dictionary<ulong, HandlerData> handlerRipToRegisters)
        {
            // Attempt to solve the handler RIPs    
            var solver = new VmpSolver(arch, liftedFunction);
            var ripResult = solver.SolveRIPs(stateStruct);
            //var solver2 = new VmpSolver2(dna, liftedFunction);
            //var ripResult = solver2.SolveRIPs(arch, stateStruct);
            if (ripResult is not Ok<Dictionary<ulong, HashSet<ulong>>> dests)
                return new Err<InvalidOperationException>(ripResult.Err());

            // Compute the VIP/vkey reg for each handler
            foreach (var (bytecodePtr, outgoingHandlers) in dests.Value)
            {
                //liftedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
                // Compute the VIP and vkey
                var regs = GetHandlerRegisters(stateStruct, stateStruct2, handlers.First(), handlerLifter, bytecodePtr, outgoingHandlers, handlerRipToRegisters);

                foreach (var rip in outgoingHandlers)
                {
                    // if (rip == 0x14012158B)
                    //     regs = new(regs.Vip, arch.GetRegisterByName("RBP"));
                    // if (rip == 0x14001367C)
                    //     regs = new(regs.Vip, arch.GetRegisterByName("R8"));
                    handlerRipToRegisters[rip] = regs;
                }
            }

            var branchResults = solver.Solve(handlerRipToRegisters, stateStruct);
            return branchResults;
        }

        private HandlerData GetHandlerRegisters(VmpParameterizedStateStructure stateStruct, ParameterizedStateStructure stateStruct2, VmHandler entryHandler, HandlerLifter handlerLifter, ulong bytecodeAddr, HashSet<ulong> outgoingHandlers, Dictionary<ulong, HandlerData> handlerRipToRegister)
        {
            var debugModule = ctx.CreateModuleWithName("debugHandlers");
            var prevRip = bytecodeAddrToRip[bytecodeAddr];
            var jmpFrom = handlerLifter.Lift(debugModule, prevRip, entryHandler.BytecodeRip == bytecodeAddr);
            var targets = outgoingHandlers.Select(x => (x, handlerLifter.Lift(debugModule, x, x == entryHandler.BytecodeRip))).ToList();

            HashSet<(RemillRegister, RemillRegister, RemillRegister)> registers = new();
            bool hasStackKey = false;
            foreach (var (rip, t) in targets)
            {
                var existingRegister = handlerRipToRegister[prevRip].Vip;
                if (HandlerLifter.IsVmexit(HandlerLifter.DisHandler(dna, rip)))
                {
                    registers.Add(new(existingRegister, existingRegister, existingRegister));
                    continue;
                }


                var vip = GetVipByUsage(rip, t, stateStruct);




                var otherVip = handlerLifter.GetBytecodeRegister(HandlerLifter.DisHandler(dna, prevRip), stateStruct2, jmpFrom, prevRip == entryHandler.BytecodeRip, existingRegister);
                /*
                if (vip.Name != otherVip.Name)
                {
                  
                    Debugger.Break();
                }
                */

                //if (rip == 0x14005DFB7)
                //    Debugger.Break();

                RemillRegister vkey = null;
                if (existingRegister.Name != vip.Name)
                {
                    //Debugger.Break();
                    vip = otherVip;

                    Console.WriteLine("Nullll");



                    hasStackKey = GetStackKeyByUsage(rip, t, stateStruct, vip);



                    //handlerLifter.cacheModule.PrintToFile("translatedFunction.ll");

                    //vkey = GetVkeyByUsage(rip, t, stateStruct, vip);
                    //Debugger.Break();

                }

                else
                {
                    // TODO: Solve for concrete vkey and store it
                    vkey = GetVkeyByUsage(rip, t, stateStruct, vip);
                    Debug.Assert(vkey != null);
                }


                var imagebaseReg = GetImagebaseRegister(rip, t, stateStruct);

                if (vkey == null)
                {
                    //Debugger.Break();
                }





                //handlerLifter.cacheModule.PrintToFile("translatedFunction.ll");

                //
                registers.Add((vip, vkey, imagebaseReg));
                //Console.WriteLine();
            }

            Debug.Assert(registers.Count == 1);
            var single = registers.Single();
            return new HandlerData(single.Item1, single.Item2, single.Item3, hasStackKey);
            //debugModule.PrintToFile("translatedFunction.ll");

            //Debugger.Break();
        }

        private static readonly LLVMOpcode[] opcodesToVisit =
        {
                LLVMOpcode.LLVMAdd,
                LLVMOpcode.LLVMSub,
                LLVMOpcode.LLVMMul,
                LLVMOpcode.LLVMAnd,
                LLVMOpcode.LLVMOr,
                LLVMOpcode.LLVMXor,
                LLVMOpcode.LLVMAShr,
                LLVMOpcode.LLVMLShr,
                LLVMOpcode.LLVMShl,
                LLVMOpcode.LLVMICmp,
                LLVMOpcode.LLVMSelect,
                LLVMOpcode.LLVMSExt,
                LLVMOpcode.LLVMZExt,
                LLVMOpcode.LLVMTrunc,
        };

        private bool GetStackKeyByUsage(ulong rip, LLVMValueRef function, VmpParameterizedStateStructure stateStructure, RemillRegister vipReg)
        {
            var vipArg = function.GetParam((uint)stateStructure.RegisterArgumentIndices[vipReg]);
            var loads = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMLoad) && IsGepRegister(x.GetOperand(0))).ToList();

            var shouldContinue = (LLVMValueRef x) =>
            {
                if (x.Is(opcodesToVisit))
                    return true;

                if (x.Is(LLVMOpcode.LLVMCall) && IsIntegerIntrinsic(x.GetCallInstTarget()))
                    return true;

                return false;
            };

            function.GlobalParent.PrintToFile("translatedFunction.ll");

            LLVMValueRef? best = null;
            int bestScore = 0;
            foreach (var load in loads)
            {
                var users = CollectUsers(load, 50, shouldContinue).ToHashSet();
                var score = users.Sum(x => IsPossibleVipDecryptionInst(x));
                if (score < 5)
                    continue;


                var gep = load.GetOperand(0);
                if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
                    continue;
                if (gep.OperandCount != 2 || gep.GetOperand(1).Kind != LLVMValueKind.LLVMArgumentValueKind)
                    continue;


                Console.WriteLine($"Cand has score: {score}");

                if (score < bestScore)
                    continue;



                best = load;
            }


            if (best == null)
                return false;


            return true;
        }

        private RemillRegister GetImagebaseRegister(ulong rip, LLVMValueRef function, VmpParameterizedStateStructure stateStructure)
        {
            var ripArg = function.GetParam((uint)stateStructure.RegisterOutputArgumentIndices[arch.GetRegisterByName("RIP")]);
            var store = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMStore) && x.GetOperand(1) == ripArg).SingleOrDefault();
            if (store.Handle == 0)
                return null;

            var add = store.GetOperand(0);
            if (!add.Is(LLVMOpcode.LLVMAdd))
                return null;

            var regVal = add.GetOperand(1);
            if (regVal.Kind != LLVMValueKind.LLVMArgumentValueKind)
                return null;
            if (add.GetOperand(0).Kind == LLVMValueKind.LLVMArgumentValueKind)
                throw new InvalidOperationException("This shouldn't happen");

            var r = stateStructure.OrderedRegisterArguments[function.GetParams().IndexOf(regVal)];
            return r;
        }

        private RemillRegister GetVkeyByUsage(ulong rip, LLVMValueRef function, VmpParameterizedStateStructure stateStructure, RemillRegister vipReg)
        {
            //if (rip == 0x1400c02e0)

            var vipArg = function.GetParam((uint)stateStructure.RegisterArgumentIndices[vipReg]);
            //var loads = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMLoad) && IsGepRegister(x.GetOperand(0)) && DecomposeSum(x.GetOperand(0).GetOperand(1)).Contains(vipArg)).ToList();

            var clone = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMLoad) && IsGepRegister(x.GetOperand(0))).ToList();
            List<LLVMValueRef> loads = new();
            foreach (var load in clone.ToList())
            {
                var elements = new List<LLVMValueRef>();
                UnfoldGep(load.GetOperand(0), elements);
                if (elements.Contains(vipArg))
                    loads.Add(load);
            }

            var shouldContinue = (LLVMValueRef x) =>
            {
                if (x.Is(opcodesToVisit))
                    return true;

                if (x.Is(LLVMOpcode.LLVMCall) && IsIntegerIntrinsic(x.GetCallInstTarget()))
                    return true;

                return false;
            };

            var loadUsers = loads.SelectMany(x => CollectUsers(x, 50, shouldContinue)).ToHashSet();

            List<RemillRegister> cands = new();
            foreach (var (reg, idx) in stateStructure.RegisterArgumentIndices)
            {
                if (reg == vipReg)
                    continue;

                // Skip if this register is ever used as a memory destination
                var arg = function.GetParam((uint)idx);
                if (IsStoredToBase(function, arg))
                    continue;


                // Get the users of the register
                var argUsers = CollectUsers(arg, 50, shouldContinue);

                var intersection = loadUsers.Intersect(argUsers).ToHashSet();
                var count = intersection.Sum(x => IsPossibleVipDecryptionInst(x));

                bool isVkey = count >= 5;

                /*
                if (!loads.Any(load => argUsers.Any(user => user.GetOperands().Contains(load))))
                {
                    Debug.Assert(!isVkey);
                    continue;
                }
                */

                if (!isVkey)
                    continue;

                //var users = loads.SelectMany(x => CollectUsers(x, 50, shouldContinue)).TOha

                Debug.Assert(isVkey);
                cands.Add(reg);
            }

            //if (cands.Count != 1)
            //   Debugger.Break();



            return cands.SingleOrDefault();
        }


        private RemillRegister GetVipByUsage(ulong rip, LLVMValueRef function, VmpParameterizedStateStructure stateStruct)
        {
            /*
            if (rip == 0x00000001400e6773)
                Debugger.Break();
            //if (rip == 0x140096818)
           //     Debugger.Break();
            if (rip == 0x1400e3c7d)
                Debugger.Break();
            */



            var loads = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMLoad) && IsGepRegister(x.GetOperand(0))).ToList();

            //HashSet<(LLVMValueRef, int) > options = new();
            Dictionary<LLVMValueRef, int> options = new();
            foreach (var x in loads)
            {
                var users = CollectUsers(x, 15);
                var count = users.Sum(x => IsPossibleVipDecryptionInst(x));
                if (count < 5)
                    continue;

                /*
                // add i64 %RSI, 1
                // =>
                // %RSI
                var regArg = x.GetOperand(0).GetOperand(1);
                if (regArg.Is(LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub))
                    regArg = regArg.GetOperands().Single(x => x.Is(LLVMValueKind.LLVMArgumentValueKind));
                */

                var values = new List<LLVMValueRef>();
                UnfoldGep(x.GetOperand(0), values);

                LLVMValueRef regArg = values[1];

                if (IsStoredToBase(function, regArg))
                    continue;


                options.TryAdd(regArg, 0);
                options[regArg] += count;
            }

            HashSet<LLVMValueRef> cands = options.Select(x => x.Key).ToHashSet();
            if (options.Count == 2)
            {
                var arr = options.OrderBy(x => x.Value).ToArray();
                Console.WriteLine($"Warning: Handler at RIP 0x{rip.ToString("X")} has two VIP registers candidates: [{arr[0]}], [{arr[1]}]. Picked the second one.");
                cands.Remove(arr[0].Key);
                Debugger.Break();
            }

            // There should only be one candidate.
            // If there are multiple, narrow down by it's access patterns.
            if (cands.Count > 1)
                return null;

            var match = cands.SingleOrDefault();
            if (match.Handle == 0)
                return null;

            var reg = GetReg(function, match, stateStruct);
            return reg;
        }

        private HashSet<LLVMValueRef> CollectUsers(LLVMValueRef inst, int limit, Func<LLVMValueRef, bool> shouldContinue = null)
        {
            var set = new HashSet<LLVMValueRef>();
            CollectUsers(inst, set, limit);
            return set;
        }

        private void CollectUsers(LLVMValueRef inst, HashSet<LLVMValueRef> seen, int limit, Func<LLVMValueRef, bool> shouldContinue = null)
        {
            // Skip functions
            if (inst.Is(LLVMValueKind.LLVMFunctionValueKind, LLVMValueKind.LLVMBasicBlockValueKind))
                return;

            if (seen.Count >= limit)
                return;

            if (!seen.Add(inst))
                return;

            if (shouldContinue != null && !shouldContinue(inst))
                return;

            foreach (var user in inst.GetUsers())
                CollectUsers(user, seen, limit, shouldContinue);
        }

        private int IsPossibleVipDecryptionInst(LLVMValueRef inst)
        {
            if (inst.Is(LLVMOpcode.LLVMAnd, LLVMOpcode.LLVMOr, LLVMOpcode.LLVMXor))
                return 1;

            if (inst.Is(LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub))
                return 1;

            if (inst.Is(LLVMOpcode.LLVMZExt, LLVMOpcode.LLVMSExt))
                return 1;

            if (inst.Is(LLVMOpcode.LLVMCall) && IsIntegerIntrinsic(inst.GetCallInstTarget()))
                return 2;

            return 0;
        }

        private static bool IsIntegerIntrinsic(LLVMValueRef function)
        {
            return (function.Name.StartsWith("llvm.fshl") || function.Name.StartsWith("llvm.bswap"));
        }

        private bool IsGepRegister(LLVMValueRef gep)
        {
            if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
                return false;

            var unfolded = new List<LLVMValueRef>();
            UnfoldGep(gep, unfolded);

            if (unfolded.Count != 3 && unfolded.Count != 2)
                return false;

            var reg = unfolded[1];
            if (reg.Kind != LLVMValueKind.LLVMArgumentValueKind)
                return false;
            if (unfolded.Count > 2 && !unfolded[2].IsConstant())
                return false;

            //if (IsAddRegister(reg))
            //    return true;
            //if (gep.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind)
            //    return true;

            return true;
        }

        private static void UnfoldGep(LLVMValueRef gep, List<LLVMValueRef> values)
        {
            if (gep.Is(LLVMOpcode.LLVMAdd))
            {
                goto process;
            }

            if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
            {
                values.Add(gep);
                return;
            }

        process:
            for (int i = 0; i < gep.OperandCount; i++)
                UnfoldGep(gep.GetOperand((uint)i), values);
        }


        private bool IsAddRegister(LLVMValueRef add)
        {
            if (!add.Is(LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub))
                return false;
            if (!add.GetOperand(1).IsConstant())
                return false;
            if (add.GetOperand(0).Kind != LLVMValueKind.LLVMArgumentValueKind)
                return false;
            return true;
        }

        private RemillRegister GetReg(LLVMValueRef function, LLVMValueRef param, VmpParameterizedStateStructure stateStruct)
        {
            var index = Array.IndexOf(function.GetParams(), param);
            return stateStruct.OrderedRegisterArguments[index];
        }

        // Returns true if `basePtr` is used as a store destination
        private static bool IsStoredToBase(LLVMValueRef function, LLVMValueRef basePtr)
        {
            var stores = function.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMStore));
            foreach (var store in stores)
            {
                var gep = store.GetOperand(1);
                if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
                    continue;

                var seen = new List<LLVMValueRef>();
                //foreach (var operand in gep.GetOperands())
                //    DecomposeSum(operand, seen);
                UnfoldGep(gep, seen);

                if (seen.Contains(basePtr))
                    return true;
            }

            return false;
        }

        private static HashSet<LLVMValueRef> DecomposeSum(LLVMValueRef value)
        {
            var seen = new HashSet<LLVMValueRef>();
            DecomposeSum(value, seen);
            return seen;
        }

        private static void DecomposeSum(LLVMValueRef value, HashSet<LLVMValueRef> seen)
        {
            if (!seen.Add(value))
                return;

            if (value.Is(LLVMOpcode.LLVMAdd, LLVMOpcode.LLVMSub))
            {
                foreach (var operand in value.GetOperands())
                    DecomposeSum(operand, seen);
            }
        }

        private HashSet<VmHandler> GetReachableNodes(VmCfg vCfg, WorkList<VmHandler> worklist)
        {
            var seen = new HashSet<VmHandler>();
            while (worklist.Count != 0)
            {
                var pop = worklist.PopBack();
                if (!seen.Add(pop))
                    continue;

                worklist.AddRangeToBack(vCfg.Instructions[pop].Successors);
            }

            return seen;
        }

        public static void FixMemPtr(LLVMModuleRef module)
        {
            var memPtr = module.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(module.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }
        }

        public static (ControlFlowGraph<VmHandler>, Dictionary<VmHandler, BasicBlock<VmHandler>>) GetCfg(VmCfg vCfg, VmHandler entry)
        {
            var cfg = new ControlFlowGraph<VmHandler>(entry.BytecodeRip);

            Dictionary<VmHandler, BasicBlock<VmHandler>> labels = new();
            foreach (var (handler, info) in vCfg.Instructions)
            {
                var isEntrypoint = handler == entry;
                var hasMultiplePredecessors = info.Predecessors.Count > 1;
                var isCyclic = info.Predecessors.Contains(handler);
                var isCondTarget = info.Predecessors.Any(x => vCfg.Instructions[x].Successors.Count > 1);

                var isLabel = isEntrypoint || hasMultiplePredecessors || isCyclic || isCondTarget;
                if (!isLabel)
                    continue;

                labels.Add(handler, cfg.CreateBlock(handler.BytecodeRip));
            }

            foreach (var label in labels.Keys)
                VisitLabel(vCfg, labels, label);

            return (cfg, labels);
        }

        public static void VisitLabel(VmCfg vCfg, Dictionary<VmHandler, BasicBlock<VmHandler>> labels, VmHandler handler)
        {
            var block = labels[handler];
            while (true)
            {
                block.Instructions.Add(handler);

                var succs = vCfg.Instructions[handler].Successors;
                if (!succs.Any())
                    break;

                // Add an outgoing edges if any targets are labels
                if (succs.Any(x => labels.ContainsKey(x)))
                {
                    block.AddOutgoingEdges(succs.Select(x => new BlockEdge<VmHandler>(block, labels[x])));
                    return;
                }

                handler = succs.Single();
            }
        }


    }

    public class IterativeCfgBuilder
    {
        IDna dna;

        private readonly LLVMModuleRef module;

        private readonly RemillArch arch;

        private readonly VmpParameterizedStateStructure stateStruct;

        private readonly VmCfg vCfg;

        private readonly HandlerLifter handlerCache;

        private readonly IReadOnlySet<ulong> vmexitHandlerRips;
        private readonly Dictionary<VmHandler, ulong> handlerToVkey;
        private readonly Dictionary<VmHandler, ulong> handlerToVbase;

        //private readonly Dictionary<VmHandler, RemillRegister> handlerVips;
        private readonly Dictionary<ulong, HandlerData> handlerRipToRegisters;
        private LLVMBuilderRef builder;

        public IterativeCfgBuilder(IDna dna, LLVMModuleRef module, RemillArch arch, VmpParameterizedStateStructure stateStruct, VmCfg vCfg, HandlerLifter handlerCache, IReadOnlySet<ulong> vmexitHandlerRips, Dictionary<VmHandler, RemillRegister> handlerVips, Dictionary<VmHandler, ulong> handlerToVkey, Dictionary<VmHandler, ulong> handlerToVbase, Dictionary<ulong, HandlerData> handlerRipToRegisters)
        {
            this.dna = dna;
            this.module = module;
            this.arch = arch;
            this.stateStruct = stateStruct;
            this.vCfg = vCfg;
            this.handlerCache = handlerCache;
            this.vmexitHandlerRips = vmexitHandlerRips;
            this.handlerToVkey = handlerToVkey;
            this.handlerToVbase = handlerToVbase;
            //this.handlerVips = handlerVips;
            this.handlerRipToRegisters = handlerRipToRegisters;
            builder = LLVMBuilderRef.Create(module.Context);
        }

        public LLVMValueRef Run(LLVMValueRef translatedFunction, VmHandler entryHandler)
        {

            var bar = arch.Registers;

            var jmpHandler = module.GetNamedFunction("vmp_branch");
            bool incremental = translatedFunction.Handle != 0;
            //if (incremental)
            if (false)
            {
                var calls = jmpHandler.Handle == 0 ? new List<LLVMValueRef>() : RemillUtils.CallersOf(jmpHandler);

                // Get all complete instructions
                var completeInstructions = vCfg.Instructions.Keys.Where(x => vCfg.Instructions[x].Metadata.IsComplete).ToHashSet();

                foreach (var caller in calls)
                {
                    var srcIp = caller.GetOperand((uint)caller.OperandCount - 2).ConstIntZExt;
                    var handler = vCfg.Instructions.Single(x => x.Key.BytecodeRip == srcIp).Key;
                    var vNode = vCfg.Instructions[handler];
                    var outgoingAddresses = vNode.Successors.OrderBy(x => x).ToList();

                    // If we have a call to `vmp_branch` targeting a complete node, we need to rebuild the CFG.
                    // This happens when the control flow graph has cycles and the edges converge.
                    // Nvm this was a bug in my code
                    if (outgoingAddresses.Any(x => completeInstructions.Contains(x)))
                    {
                        translatedFunction.Handle = 0;
                        incremental = false;
                        break;
                    }
                }
            }

            if (!incremental)
            {
                translatedFunction = module.AddFunction($"PartialCfg", stateStruct.ParameterizedFunctionPrototype);
                translatedFunction.AppendBasicBlock("entry");
            }

            foreach (var outReg in stateStruct.RegisterOutputArgumentIndices.Values)
                LLVMUtil.MakeParamNoAlias(translatedFunction.GetParam((uint)outReg));

            var callers = jmpHandler.Handle == 0 ? new List<LLVMValueRef>() : RemillUtils.CallersOf(jmpHandler);

            // Stack allocate a local state structure and copy all registers into it
            // Problem: Copying the values from the registers won't work anymore.. where do we put everything??
            builder.PositionAt(translatedFunction.EntryBasicBlock, translatedFunction.EntryBasicBlock.FirstInstruction);
            var registerAllocaMapping = VmPartialBlockLifter.CreateLocalStateStruct(builder, stateStruct, translatedFunction);

            // Lift all handlers into their own basic block
            var exitBlock = translatedFunction.AppendBasicBlock("exit");
            builder.PositionAt(exitBlock, exitBlock.FirstInstruction);
            builder.BuildRetVoid();

            var toDelete = new HashSet<LLVMValueRef>();

            var blockMapping = LiftInsts(incremental, translatedFunction, exitBlock, entryHandler, registerAllocaMapping, toDelete);

            // If rebuilding cfg from scratch, insert jump from entry block to first VM instruction
            if (!incremental)
            {
                builder.PositionAtEnd(translatedFunction.EntryBasicBlock);
                builder.BuildBr(blockMapping[entryHandler]);
                //module.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

                /*
                module.PrintToFile(("translatedFunction.ll"));
                foreach (var f in toDelete)
                {
                    var any = translatedFunction.GetInstructions().First(x => x.InstructionOpcode == LLVMOpcode.LLVMCall && x.GetCallInstTarget() == f);

                        

                    var callers2 = RemillUtils.CallersOf(f);
                    LLVMCloning.InlineFunction(f);
                    module.PrintToFile(("translatedFunction.ll"));
                    f.DeleteFunction();
                }
                */


                module.PrintToFile(("translatedFunction.ll"));
                return translatedFunction;
            }

            // Wire new handlers into CFG
            // We need to identify the vmp_maybe_unsolved_jmp calls and hook them up
            foreach (var caller in callers)
            {
                // Fetch register values from the local state structure
                builder.PositionBefore(caller);
                //VmPartialBlockLifter.LoadOutputRegisters(builder, translatedFunction, registerAllocaMapping, stateStruct);
                foreach (var (reg, alloca) in registerAllocaMapping) // todo: use deterministic order
                {
                    var idx = (uint)stateStruct.RegisterArgumentIndices[reg];
                    builder.BuildStore(caller.GetOperand(idx), alloca);
                }

                var srcIp = caller.GetOperand((uint)caller.OperandCount - 2).ConstIntZExt;
                var handler = vCfg.Instructions.Single(x => x.Key.BytecodeRip == srcIp).Key;

                // You can't concretize the VIP here because it's unknown..
                var destReg = vCfg.Instructions[handler].Successors.Select(x => handlerRipToRegisters[x.NativeRip]).Distinct().Single().Vip;
                var destIdx = (uint)stateStruct.OrderedRegisterArguments.IndexOf(destReg);

                // need to fix up bytecode ptr here
                var destIp = caller.GetOperand(destIdx);
                //var destBlock = blockMapping.Single(x => x.Key.BytecodeRip ==  destIp);


                //var srcIp = caller.GetOperand(2).ConstIntZExt;


                var vNode = vCfg.Instructions[handler];





                var outgoingAddresses = vNode.Successors.OrderBy(x => x).ToList();
                // If this lookup fails, an incremental rebuild was not possible. TODO: Set `incremental` to false and clear CFG in this case
                // In this case the instruction is marked as complete
                //
                // In some cases we have a complete block jumping to a complete block, case where edges did not change.
                //var defaultBlock = blockMapping[outgoingAddresses.First()];
                var defaultBlock = CreateCaseBlock(translatedFunction, destIp, outgoingAddresses.First(), blockMapping);
                var liftedCases = new HashSet<VmHandler>() { outgoingAddresses.First() };

              
                //builder.PositionAtEnd(caller.InstructionParent);

                var swtch = builder.BuildSwitch(destIp, defaultBlock, (uint)outgoingAddresses.Count);

                foreach (var target in outgoingAddresses)
                {
                    if (liftedCases.Contains(target))
                        continue;

                    liftedCases.Add(target);

                    /*
                    var targetBlock = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Value;

                    swtch.AddCase(LLVMValueRef.CreateConstInt(module.Context.Int64Type, target.BytecodeRip), targetBlock);
                    */

                    var targetHandler = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Key;
                    var targetBlock = CreateCaseBlock(translatedFunction, destIp, targetHandler, blockMapping);

                    var key = LLVMValueRef.CreateConstInt(module.Context.Int64Type, target.BytecodeRip);
                    swtch.AddCase(key, targetBlock);
                }

                LLVMUtil.SplitBlockAt(caller.InstructionParent, caller, "split", true);
                swtch.InstructionParent.LastInstruction.InstructionEraseFromParent();


                // TODO: Split basic block before lifting edges..
                // copy from args to local state structure.. make them no alias
                // module.PrintToFile(("translatedFunction.ll"));
                //Debugger.Break();
            }

            // If we cant incremental build, need to do everything from scatch. Put each inst in basic block, hook them up. Maybe split into SESE regions just to not be insanely unreadable. Insert hooks on exit
            // If incremental, we are just wiring into an existing
            // TODO: If incremental build, wire up state structure and outgoing edges.
            // vmp_unsolved_jump(target ptr, rip, jmp from bytecode ptr)
            //module.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
            //Debugger.Break();
            return translatedFunction;
        }

        private Dictionary<VmHandler, LLVMBasicBlockRef> LiftInsts(bool incremental, LLVMValueRef function, LLVMBasicBlockRef exitBlock, VmHandler entryHandler, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, HashSet<LLVMValueRef> toDelete)
        {
            // Create blocks for each lifted instructions
            var blockMapping = new Dictionary<VmHandler, LLVMBasicBlockRef>();
            foreach (var (handler, info) in vCfg.Instructions)
            {
                // Skip if we are doing incremental building and already have this inst in the IR
                if (incremental && info.Metadata.IsComplete)
                    continue;

                blockMapping[handler] = function.AppendBasicBlock($"bb_{handler.BytecodeRip.ToString("X")}");
            }

            foreach (var (handler, block) in blockMapping)
            {
                builder.PositionAtEnd(block);

                // Concretize the VIP register!
                if (handler != entryHandler)
                {
                    var regInfo = handlerRipToRegisters[handler.NativeRip];
                    var incomingVipRegister = regInfo.Vip;
                    builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, handler.BytecodeRip), registerAllocaMapping[incomingVipRegister]);

                    // Concretize vkey if its known
                    if (handlerToVkey.TryGetValue(handler, out var existing))
                    {
                        // TODO: If this is a vmexit, do not concretize bytecode rip and stuff
                        var incomingVkeyRegister = regInfo.Vkey;

                        if (incomingVkeyRegister == null)
                        {
                            Console.WriteLine($"Failed to concretize vkey for handler: {handler.NativeRip}");
                        }

                        else
                        {
                            builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, existing), registerAllocaMapping[incomingVkeyRegister]);
                        }


                        if (regInfo.hasVkeyStackSlot)
                        {

                        }

                    }

                    // Concretize vkey if its known
                    if (handlerToVbase.TryGetValue(handler, out var existingBase))
                    {
                        // TODO: If this is a vmexit, do not concretize bytecode rip and stuff
                        var incomingImgbaseReg = regInfo.ImgBaseReg;

                        if (incomingImgbaseReg == null)
                        {
                            Console.WriteLine($"Failed to concretize vkey for handler: {handler.NativeRip}");
                        }

                        else
                        {
                            builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, existingBase), registerAllocaMapping[incomingImgbaseReg]);
                        }


                        if (regInfo.hasVkeyStackSlot)
                        {

                        }

                    }
                }

                var liftedHandler = handlerCache.Lift(module, handler.NativeRip, handler == entryHandler);

                if (handler != entryHandler)
                {
                    var regInfo = handlerRipToRegisters[handler.NativeRip];
                    liftedHandler.Name = liftedHandler.Name + Guid.NewGuid().ToString();

                    var vipConst = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, handler.BytecodeRip);
                    var vipIndex = stateStruct.RegisterArgumentIndices[regInfo.Vip];
                    liftedHandler.GetParam((uint)vipIndex).ReplaceAllUsesWith(vipConst);

                    if (handlerToVkey.TryGetValue(handler, out var existing2))
                    {
                        var incomingVkeyRegister = regInfo.Vkey;

                        if (incomingVkeyRegister == null)
                        {
                            //Console.WriteLine($"Failed to concretize vkey for handler: {handler.NativeRip}");
                        }

                        else
                        {
                            var vkeyConst = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, existing2);
                            var index = stateStruct.RegisterArgumentIndices[incomingVkeyRegister];
                            liftedHandler.GetParam((uint)index).ReplaceAllUsesWith(vkeyConst);
                        }

                    }

                    if (handlerToVbase.TryGetValue(handler, out var existing3))
                    {
                        var incomingBaseReg = regInfo.ImgBaseReg;

                        if (incomingBaseReg == null)
                        {
                            //Console.WriteLine($"Failed to concretize vkey for handler: {handler.NativeRip}");
                        }

                        else
                        {
                            var vkeyConst = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, existing3);
                            var index = stateStruct.RegisterArgumentIndices[incomingBaseReg];
                            liftedHandler.GetParam((uint)index).ReplaceAllUsesWith(vkeyConst);
                        }   

                    }


                    /*
                    var func = module.GetNamedFunction("SaveUnknown");
                    if (func.Handle == 0)
                    {
                        var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new[] { LLVMTypeRef.Int64 });
                        func = module.AddFunction("SaveUnknown", prototype);
                        LLVMUtilApi.AddNoSideEffectAttributes(func);
                    }
                   
                    Console.WriteLine($"Visiting at {liftedHandler.Name}\n");
                    Console.WriteLine($"\n\n");
                    //VmpPassPipeline.Run(dna.Binary, liftedHandler);
                    for (int i = 0; i < 5; i++)
                    {
                        liftedHandler.GlobalParent.PrintToFile("translatedFunction.ll");

                        var storeToLoad = new CombinedFixedpointOptPass(dna.Binary, new FixedpointPassConfig());
                        var pStoreToLoad = Marshal.GetFunctionPointerForDelegate(storeToLoad.PtrToStoreLoadPropagation);
                        OptimizationApi.OptimizeModuleVmp(liftedHandler.GlobalParent, liftedHandler, false, false, 0, false, 0, false, false, 0, pStoreToLoad, 0, 0, fastPipeline: true);
                    }

                    if (liftedHandler.GetInstructions().Any(x => x.Is(LLVMOpcode.LLVMCall) && (x.ToString().Contains("bswap") || x.ToString().Contains("fshl"))))
                    {

                        var tgtLoad = liftedHandler.GetInstructions().First(x => x.Is(LLVMOpcode.LLVMLoad) && x.GetOperand(0).Is(LLVMOpcode.LLVMGetElementPtr));

                        liftedHandler.GlobalParent.PrintToFile("translatedFunction.ll");
                        var temp = LLVMBuilderRef.Create(module.GetCtx());
                        temp.PositionBefore(tgtLoad.NextInstruction);
                        var call2 = temp.BuildCall2(func.GetFunctionPrototype(), func, new LLVMValueRef[] { tgtLoad });

                        Debugger.Break();
                    }
                    */

                }

                var getSaveVip = () =>
                {
                    var func = module.GetNamedFunction("SaveVIP");
                    if (func.Handle == 0)
                    {
                        var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new[] { LLVMTypeRef.Int64, LLVMTypeRef.Int64 });
                        func = module.AddFunction("SaveVIP", prototype);
                        LLVMUtilApi.AddNoSideEffectAttributes(func);
                    }
                    ;
                    return func;
                };



                var call = VmPartialBlockLifter.CallVmHandler(builder, function, liftedHandler, registerAllocaMapping, stateStruct);

                bool dbgIntrins = false;


                builder.PositionBefore(call);
                var saveVip = getSaveVip();

                if (dbgIntrins)
                    builder.BuildCall2(saveVip.GetFunctionPrototype(), saveVip, new[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, handler.BytecodeRip), LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, handler.NativeRip) });



                builder.PositionAtEnd(call.InstructionParent);
                if (true && handler != entryHandler && handlerRipToRegisters.TryGetValue(handler.NativeRip, out var slotInfo) && slotInfo.hasVkeyStackSlot)
                {

                    /*
                    var name = $"solve_{handler.BytecodeRip.ToString("X")}";
                    var load = liftedHandler.GetInstructions().Skip(1).First(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad);

                    var glob = liftedHandler.GlobalParent.GetNamedGlobal(name);
                    if (glob.Handle == 0)
                    {
                        glob = liftedHandler.GlobalParent.AddGlobal(load.TypeOf, name);
                        glob.Initializer = LLVMValueRef.CreateConstInt(load.TypeOf, 0);
                    }

                 
                    //var value = LLVMValueRef.CreateConstInt(load.TypeOf, handler.BytecodeRip);
                    //load.ReplaceAllUsesWith(value);

                    builder.PositionBefore(load.NextInstruction);

                    builder.BuildStore(load, glob);

                    liftedHandler.Name = liftedHandler.Name + Guid.NewGuid().ToString();

                    liftedHandler.GlobalParent.PrintToFile("translatedFunction.ll");
                    //Debugger.Break();
                    */


                    var func = module.GetNamedFunction("SaveVkey");
                    if (func.Handle == 0)
                    {
                        var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new[] { LLVMTypeRef.Int64 });
                        func = module.AddFunction("SaveVkey", prototype);
                        LLVMUtilApi.AddNoSideEffectAttributes(func);
                    }


                    var load = liftedHandler.GetInstructions().Skip(1).First(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad);
                    Debug.Assert(load.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind);
                    builder.PositionBefore(load.NextInstruction);

                    if (dbgIntrins)
                        builder.BuildCall2(func.GetFunctionPrototype(), func, new[] { load });

                }


                builder.PositionAtEnd(call.InstructionParent);
                LiftInstEdges(handler, function, exitBlock, blockMapping, registerAllocaMapping);
                //module.PrintToFile("translatedFunction.ll");

                IterativeVmpExplorer.FixMemPtr(module);


                LLVMCloning.InlineFunction(liftedHandler);
                //module.PrintToFile("translatedFunction.ll");
                liftedHandler.DeleteFunction();
                //toDelete.Add(liftedHandler);
            }

            return blockMapping;
        }

        private static LLVMBasicBlockRef CreateCaseBlock(LLVMValueRef func, LLVMValueRef bytecodePtr, VmHandler handler, Dictionary<VmHandler, LLVMBasicBlockRef> blockMapping)
        {
            var module = func.GlobalParent;
            var builder = LLVMBuilderRef.Create(func.GetFunctionCtx());
            var assumeBlock = func.AppendBasicBlock($"{handler.BytecodeRip.ToString("X")}_assumption");
            builder.PositionAtEnd(assumeBlock);

            var key = LLVMValueRef.CreateConstInt(module.Context.Int64Type, handler.BytecodeRip);
            var assumedCond = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, bytecodePtr, key);

            var assumeFn = GetAssumeIntrinsic(assumeBlock.Parent.GlobalParent);
            var call = builder.BuildCall2(assumeFn.GetFunctionPrototype(), assumeFn, new LLVMValueRef[] { assumedCond });

            builder.BuildBr(blockMapping[handler]);
            return assumeBlock;
        }

        private void LiftInstEdges(VmHandler handler, LLVMValueRef function, LLVMBasicBlockRef exitBlock, Dictionary<VmHandler, LLVMBasicBlockRef> blockMapping, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping)
        {
            var llvmBlock = blockMapping[handler];

            bool isVmExit = vmexitHandlerRips.Contains(handler.NativeRip);
            var info = vCfg.Instructions[handler];
            bool isComplete = info.Metadata.IsComplete;

            // If this is an exit node, store all register values and exit.
            if ((isComplete && info.Successors.Count == 0) || isVmExit)
            {
                VmPartialBlockLifter.UpdateOutputRegisters(builder, function, registerAllocaMapping, stateStruct);
                builder.BuildRetVoid();
                return;
            }


            // Load the indirect jump value.
            var int64Ty = module.Context.GetInt64Ty();

            var liftedCases = new HashSet<VmHandler>();
            LLVMBasicBlockRef defaultBlock = null;
            var outgoingAddresses = info.Successors.OrderBy(x => x).ToList();
            builder.PositionAtEnd(llvmBlock);

            var none = vCfg.Instructions[handler].Successors.Count == 0;
            var bytecodeRegister = none ? null : vCfg.Instructions[handler].Successors.Select(x => handlerRipToRegisters[x.NativeRip].Vip).Distinct().Single();
            var indirectPc = none ? null : VmCfgLifter.LoadBytecodePointer(builder, bytecodeRegister, registerAllocaMapping);


            // If the jump table is considered incomplete(we know some edges but potentially not all), then we lift the jump table as a switch statement
            // where the known values get their own 'case', and the default case points to an remill_jump intrinsic.
            if (!isComplete)
            {
                defaultBlock = function.AppendBasicBlock($"reprove_new_edge_for_jmp_table_{handler.BytecodeRip.ToString("X")}");
                builder.PositionAtEnd(defaultBlock);
                // We shouldn't need to update output registers anymore because we're forwarding all registers to the call
                // VmPartialBlockLifter.UpdateOutputRegisters(builder, function, registerAllocaMapping, stateStruct);
                //VmCfgLifter.AddCallToIndirectBranchIntrinsic(module, builder, llvmBlock, bytecodeRegister, registerAllocaMapping, exitBlock, handler.BytecodeRip);
                AddCallToIndirectBranchIntrinsic(module, builder, registerAllocaMapping, handler.BytecodeRip, exitBlock);
                builder.PositionAtEnd(llvmBlock);
            }

            // If the jump table is considered complete(i.e. we are 100% confident that we know all possible outgoing values),
            // then we make the default case for the jump table point to a randomly selected(first) jump table outgoing block.
            else
            {
                defaultBlock = CreateCaseBlock(function, indirectPc, outgoingAddresses.First(), blockMapping);
                liftedCases.Add(outgoingAddresses.First());
            }


            // If no edges are known, jump to the default block
            if (none)
            {
                builder.BuildBr(defaultBlock);
                return;
            }

            //var bytecodeRegister = handlerVips[handler];
            builder.PositionAtEnd(llvmBlock);
            var swtch = builder.BuildSwitch(indirectPc, defaultBlock, (uint)outgoingAddresses.Count);

            foreach (var target in outgoingAddresses)
            {
                if (liftedCases.Contains(target))
                    continue;

                liftedCases.Add(target);
                //var targetBlock = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Value;
                var targetHandler = blockMapping.Single(x => x.Key.BytecodeRip == target.BytecodeRip).Key;
                var targetBlock = CreateCaseBlock(function, indirectPc, targetHandler, blockMapping);

                var key = LLVMValueRef.CreateConstInt(module.Context.Int64Type, target.BytecodeRip);
                swtch.AddCase(key, targetBlock);


            }
        }

        private static LLVMValueRef GetFirstNonPhiInstruction(LLVMBasicBlockRef block)
        {
            var inst = block.FirstInstruction;
            while (inst.Handle != 0 && inst.Is(LLVMOpcode.LLVMPHI))
                inst = inst.NextInstruction;

            return inst;
        }


        private static LLVMValueRef GetAssumeIntrinsic(LLVMModuleRef module)
        {
            var assume = module.GetNamedFunction("llvm.assume");
            if (assume.Handle != 0)
                return assume;

            var ctx = module.GetCtx();
            var prototype = LLVMTypeRef.CreateFunction(ctx.VoidType, new LLVMTypeRef[] { ctx.Int1Type });
            return module.AddFunction("llvm.assume", prototype);
        }


        public LLVMValueRef AddCallToIndirectBranchIntrinsic(LLVMModuleRef module, LLVMBuilderRef builder, IReadOnlyDictionary<RemillRegister, LLVMValueRef> registerAllocaMapping, ulong exitFromRip, LLVMBasicBlockRef exitBlock)
        {
            var values = stateStruct.OrderedRegisterArguments.Select(x => builder.BuildLoad2(LLVMTypeRef.Int64, registerAllocaMapping[x]))
                .Append(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, exitFromRip))
                .ToArray();
            var func = GetOrCreateJmpIntrinsic(module, stateStruct.OrderedRegisterArguments);
            var call = builder.BuildCall2(func.GetFunctionPrototype(), func, values);
            builder.BuildBr(exitBlock);
            return call;
        }

        public static LLVMValueRef GetOrCreateJmpIntrinsic(LLVMModuleRef module, IReadOnlyList<RemillRegister> registers)
        {
            // The intrinsic accepts a list of all registers, and an additional i64 argument containing the bytecode pointer we jumped from.
            // call(rax, rcx, ..., BYTECODE_PTR)
            var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, registers.Select(x => LLVMTypeRef.Int64).Append(LLVMTypeRef.Int64).ToArray());
            var func = module.GetFunctions().SingleOrDefault(x => x.Name == "vmp_branch");
            if (func.Handle != 0)
                return func;

            func = module.AddFunction("vmp_branch", prototype);
            return func;
        }

    }

    public class HandlerLifter
    {
        private readonly IDna dna;

        private readonly LLVMContextRef ctx;

        private readonly RemillArch arch;

        public readonly LLVMModuleRef cacheModule;

        public readonly Dictionary<ulong, LLVMValueRef> handlerRipToLlvmFunction = new();

        public HandlerLifter(IDna dna, LLVMContextRef ctx, RemillArch arch)
        {
            this.dna = dna;
            this.ctx = ctx;
            this.arch = arch;

            if (File.Exists("cacheModule.ll"))
            {
                cacheModule = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, "cacheModule.ll").Value;
                foreach (var f in cacheModule.GetFunctions().Where(x => x.Name.StartsWith("Parameterized_TranslatedFrom") && !x.Name.Contains("from_cache")))
                {
                    var split = f.Name.Split(new string[] { "Parameterized_TranslatedFrom", "_" }, StringSplitOptions.RemoveEmptyEntries);
                    var parsed = ulong.Parse(split[0], System.Globalization.NumberStyles.HexNumber);
                    handlerRipToLlvmFunction[parsed] = f;
                }

                return;
            }

            cacheModule = ctx.CreateModuleWithName("HandlerCache");
        }

        public static ControlFlowGraph<Instruction> DisHandler(IDna dna, ulong ip)
        {
            return dna.RecursiveDescent.ReconstructCfg(ip, IterativeVmpExplorer.DescentCallback(dna), null, IterativeVmpExplorer.ShouldContinueCallback(dna));
        }

        public static bool IsVmexit(ControlFlowGraph<Instruction> cfg)
        {
            return cfg.GetInstructions().Count(x => x.Mnemonic == Iced.Intel.Mnemonic.Pop) > 10;
        }

        public bool ContainsHandler(ulong rip)
            => handlerRipToLlvmFunction.ContainsKey(rip);

        public LLVMValueRef Lift(LLVMModuleRef module, ulong handlerRip, bool isVmEnter)
        {
            var handler = LiftHandler(handlerRip, isVmEnter);
            var wrapperName = handler.Name + "_from_cache";

            var existingInDest = module.GetNamedFunction(wrapperName);
            if (existingInDest.Handle != 0)
                return existingInDest;

            // Add a new function with the exact same prototype as the handler function.
            var newHandler = cacheModule.AddFunction(wrapperName, handler.GetFunctionPrototype());

            // Build a call to the original handler function, followed by a RET.
            var builder = LLVMBuilderRef.Create(ctx);
            var entryBb = newHandler.AppendBasicBlock("entry");
            builder.PositionAtEnd(entryBb);
            var call = builder.BuildCall2(handler.GetFunctionPrototype(), handler, newHandler.GetParams());


            builder.BuildRetVoid();

            // Inlines ALL calls to the handler function.
            // TODO: Inline only the call we just created. We just need to add a pinvoke import for this.
            LLVMCloning.InlineFunction(handler);

            //cacheModule.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);

            // Move the newly created function into the target module.
            newHandler = FunctionIsolator.IsolateFunctionInto(module, newHandler);
            return newHandler;
        }

        public LLVMValueRef LiftHandler(ulong handlerRip, bool isVmEnter)
        {
            if (handlerRipToLlvmFunction.TryGetValue(handlerRip, out var existing))
                return existing;

            Console.WriteLine($"New handler: 0x{handlerRip.ToString("X")}");

            var cfg = HandlerLifter.DisHandler(dna, handlerRip);

            //if (cfg.GetInstructions().Any(x => x.ToString().Contains("pushfq")))
            //    Debugger.Break();


            var scopeTable = IterativeFunctionTranslator.GetScopeTable(dna.Binary, handlerRip);
            var scopeTableTree = new ScopeTableTree(scopeTable);
            (cfg, var fallthroughFromIps) = IterativeFunctionTranslator.PreprocessCfg(cfg, scopeTable);

            // TODO: The sub rsp rewriting might emit an instruction with a different length. I might need to concretize RIPs in the CFG translator.
            if (isVmEnter)
                ReplaceStackExpansion(cfg);

            // Lift the function using remill.
            var encodedCfg = X86CfgEncoder.EncodeCfg(dna.Binary, cfg);
            var (liftedFunction, blockMapping, filterFunctions) = CfgTranslator.Translate(dna.Binary.BaseAddress, arch, new BinaryFunction(encodedCfg, scopeTableTree, new List<JmpTable>()), fallthroughFromIps, CallHandlingKind.VMProtect);
            liftedFunction = FunctionIsolator.IsolateFunctionIntoNewModule(arch, liftedFunction);


            foreach (var inst in liftedFunction.GetInstructions())
                ConstantFoldingAPI.DropPoisonGeneratingFlags(inst);

            //liftedFunction.Handle = 0;


            var (stripped, stateStruct) = IterativeFunctionTranslator.StripRuntimeVmp(dna, ctx, arch, liftedFunction);
            liftedFunction.Handle = 0;



            foreach (var inst in stripped.GetInstructions())
                ConstantFoldingAPI.DropPoisonGeneratingFlags(inst);
            //stripped.GlobalParent.PrintToFile("translatedFunction.ll");
            // liftedFunction.Handle = 0;


            // This is where the VIP gets swapped from RSI to r9
            // Notably RSI is set to some BS + a binary offset


            // Eliminate any stack expansion in the IR
            EliminateStackExpansionLoop(stripped, handlerRip);

            EliminateStackAlignment(stripped, stateStruct);


            foreach (var inst in stripped.GetInstructions())
                ConstantFoldingAPI.DropPoisonGeneratingFlags(inst);

            // Optimize one last time
            OptimizationApi.OptimizeModule(stripped.GlobalParent, stripped, false, false, 0, false, 0, false);



            foreach (var inst in stripped.GetInstructions())
                ConstantFoldingAPI.DropPoisonGeneratingFlags(inst);
            //stripped.GlobalParent.PrintToFile("translatedFunction.ll");

            // Move the newly created function into the target module.
            var newHandler = FunctionIsolator.IsolateFunctionInto(cacheModule, stripped);
            handlerRipToLlvmFunction[handlerRip] = newHandler;
            return newHandler;

            //File.WriteAllText("binja.py", new LLVMToBinjaGraph(stripped).Process());

        }


        public RemillRegister GetVmenterBytecodeRegister(ParameterizedStateStructure stateStruct, LLVMValueRef lifted)
        {
            // Get all constant addresses being used as pointers
            var gepConstants = lifted.GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr && x.OperandCount == 2 && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(1).ConstIntZExt))
                .Select(x => x.GetOperand(1));

            var constStores = lifted.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0).Kind == LLVMValueKind.LLVMConstantIntValueKind && dna.Binary.IsConstantData(x.GetOperand(0).ConstIntZExt)).ToList();

            // %24 = getelementptr inbounds i8, ptr %mem, i64 5368736796
            // store i64 5368736796, ptr %out_RSI, align 8
            LLVMValueRef targetStore = constStores
                .SingleOrDefault(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && gepConstants.Contains(x.GetOperand(0)));

            // Otherwise look for a unique bytecode address that is not stored anywhere else
            if (targetStore.Handle == 0)
                targetStore = constStores.Single(x => x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind && constStores.Count(y => x.GetOperand(0) == y.GetOperand(0)) == 1);


            var destRegArg = targetStore.GetOperand(1);

            var destIdx = Array.IndexOf(lifted.GetParams(), destRegArg);

            var reg = stateStruct.RegisterOutputArgumentIndices.Single(x => x.Value == destIdx).Key;
            return reg;
        }

        public RemillRegister GetBytecodeRegister(ControlFlowGraph<Instruction> cfg, ParameterizedStateStructure stateStruct, LLVMValueRef stripped, bool isVmEnter, RemillRegister existingRegister)
        {
            if (isVmEnter)
                return GetVmenterBytecodeRegister(stateStruct, stripped);

            var inReg = stateStruct.GetRegInputParam(existingRegister, stripped);
            var outReg = stateStruct.GetRegOutputParam(existingRegister, stripped);

            var store = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(1) == outReg);

            var add = store.GetOperand(0);
            var isVmExit = IsVmexit(cfg);
            var matches = Matches(inReg, add);
            if (!matches && !isVmEnter && !isVmExit)
            {
                var instructions = stripped.EntryBasicBlock.GetInstructions();
                var firstLoad = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0).Kind == LLVMValueKind.LLVMInstructionValueKind && x.GetOperand(0).InstructionOpcode == LLVMOpcode.LLVMGetElementPtr);
                var firstAdd = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMAdd && x.GetOperand(0) == firstLoad && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind);
                var destRegParam = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0) == firstAdd && x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind).GetOperand(1);
                var destReg = stateStruct.RegisterOutputArgumentIndices.Single(x => stripped.GetParam((uint)x.Value) == destRegParam);
                return destReg.Key;

                /*
                var instructions = stripped.EntryBasicBlock.GetInstructions();
                var firstAdd = instructions.First(x => x.InstructionOpcode == LLVMOpcode.LLVMAdd && x.GetOperand(0).Kind == LLVMValueKind.LLVMArgumentValueKind && x.GetOperand(1).Kind == LLVMValueKind.LLVMConstantIntValueKind);
                var destRegParam = stripped.GetInstructions().Single(x => x.InstructionOpcode == LLVMOpcode.LLVMStore && x.GetOperand(0) == firstAdd && x.GetOperand(1).Kind == LLVMValueKind.LLVMArgumentValueKind).GetOperand(1);
                var destReg = stateStruct.RegisterOutputArgumentIndices.Single(x => stripped.GetParam((uint)x.Value) == destRegParam);
                return destReg.Key;
                */
            }

            return existingRegister;

        }


        private bool Matches(LLVMValueRef inReg, LLVMValueRef add)
        {
            if (add.Kind != LLVMValueKind.LLVMInstructionValueKind || add.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;
            if (IsIncrement(inReg, add))
                return true;
            if (IsIncrementLoadReg(add))
                return true;

            return false;
        }

        private static bool IsIncrement(LLVMValueRef inReg, LLVMValueRef add)
        {
            if (add.GetOperand(0) != inReg)
                return false;
            if (add.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        //  %0 = getelementptr inbounds i8, ptr %mem, i64 %R11
        //  %1 = load i64, ptr %0, align 8
        //  %add.i.i108.i = add i64 %1, 4
        //  store i64 %add.i.i108.i, ptr %out_RSI, align 8
        private static bool IsIncrementLoadReg(LLVMValueRef add)
        {
            if (add.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            var load = add.GetOperand(0);
            if (load.Kind != LLVMValueKind.LLVMInstructionValueKind || load.InstructionOpcode != LLVMOpcode.LLVMLoad)
                return false;

            var gep = load.GetOperand(0);
            if (gep.Kind != LLVMValueKind.LLVMInstructionValueKind || gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr || gep.OperandCount != 2)
                return false;

            if (gep.GetOperand(1).Kind != LLVMValueKind.LLVMArgumentValueKind)
                return false;

            return true;
        }

        private void ReplaceStackExpansion(ControlFlowGraph<Instruction> cfg)
        {
            foreach (var block in cfg.GetBlocks())
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    if (!IsSubRspImm(block.Instructions[i]))
                        continue;

                    var assembler = new Assembler(64);
                    assembler.sub(rsp, 12582912);
                    var encodedInst = InstructionEncoder.RelocateInstructions(assembler.Instructions.ToList(), block.Instructions[i].IP).Single();
                    block.Instructions[i] = encodedInst;
                }
            }
        }

        private bool IsAndRspImm(Instruction x)
            => x.Mnemonic == Mnemonic.And && x.Op0Kind == OpKind.Register && x.Op0Register == Register.RSP && x.Op1Kind.IsImmediate() && x.GetImmediate(1) == (ulong)0xFFFFFFFFFFFFFFF0;

        private bool IsSubRspImm(Instruction x)
            => x.Mnemonic == Mnemonic.Sub && x.Op0Kind == OpKind.Register && x.Op0Register == Register.RSP && x.Op1Kind.IsImmediate();

        private void EliminateStackAlignment(LLVMValueRef function, ParameterizedStateStructure stateStruct)
        {
            foreach (var inst in function.GetInstructions())
            {
                if (inst.InstructionOpcode != LLVMOpcode.LLVMAnd)
                    continue;

                if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    continue;

                var imm = inst.GetOperand(1).ConstIntZExt;
                if (imm is not (unchecked((ulong)-16) or unchecked((ulong)-256) or 0xF or 0xFF))
                    continue;

                var rsp = stateStruct.GetRegInputParam(arch.GetRegisterByName(arch.StackPointerRegisterName), function);
                var lhs = inst.GetOperand(0);

                var matches = lhs == rsp || (lhs.InstructionOpcode == LLVMOpcode.LLVMAdd && lhs.GetOperand(0) == rsp);
                if (!matches)
                    continue;
                inst.ReplaceAllUsesWith(lhs);

                //function.GlobalParent.PrintToFile("translatedFunction.ll");

                //Debugger.Break();
            }
        }

        // VMProtect handlers will relocate the stack to a new location if they run out of space.
        // To make optimization a bit easier we modify the VMEnter to allocate a huge amount of stack space and delete all of the stack expansion loops.
        private void EliminateStackExpansionLoop(LLVMValueRef function, ulong handlerRip)
        {
            var hasCycles = () =>
            {
                var entryBlock = function.EntryBasicBlock;
                if (entryBlock.LastInstruction.InstructionOpcode != LLVMOpcode.LLVMBr || entryBlock.LastInstruction.OperandCount <= 1)
                    return false;
                var b0 = entryBlock.LastInstruction.GetOperand(1).AsBasicBlock();
                var b0Cyclic = ReachesCycle(b0, new(), new());
                var b1 = entryBlock.LastInstruction.GetOperand(2).AsBasicBlock();
                var b1Cyclic = ReachesCycle(b1, new(), new());
                return b0Cyclic || b1Cyclic;
            };


            // Get a bitmask indicating which branches lead to a cycle
            var getCycles = (LLVMBasicBlockRef b0, LLVMBasicBlockRef b1) =>
            {

                var b0Cyclic = ReachesCycle(b0, new(), new());
                var b1Cyclic = ReachesCycle(b1, new(), new());

                uint r = 0;
                r |= b0Cyclic ? 1u : 0;
                r |= b1Cyclic ? 2u : 0;
                return r;
            };

            /*
            // If neither has a cycle, there is no stack expansion.
            if (getCycles() == 0)
                return;

            // Run the full pass pipeline hoping that any spurious cycles get eliminated
            PassPipeline.Run(dna.Binary, function);
            var cycles = getCycles();
            if (cycles == 0)
                return;

            // This should never happen. There should be only one path leading to the virtual stack expansion loop
            if (cycles == 3)
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                throw new InvalidOperationException($"Cyclic handler at 0x{handlerRip.ToString("X")}");
            }

            // Update the branch instruction accordingly.
            var terminator = function.EntryBasicBlock.LastInstruction;
            var cond = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, cycles == 1 ? 1u : 0);
            terminator.SetOperand(0, cond);

            if (handlerRip == 0x140003EC2)
            {
                PassPipeline.Run(dna.Binary, function);
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();
            }
            */



            var exitInsts = function.GetBlocks()
                .Select(x => x.Terminator)
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMBr && x.OperandCount == 3 && IsStackExpansionPredicate(x) && getCycles(x.GetOperand(1).AsBasicBlock(), x.GetOperand(2).AsBasicBlock()) == 1)
                .ToList();

            if (exitInsts.Count == 0)
                return;


            //function.GlobalParent.PrintToFile("ProblematicHandler.ll");
            exitInsts.Single().SetOperand(0, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1));

        }

        //   %6 = add i64 %RDI, 224
        // %.not = icmp ugt i64 % 3, %6
        private static bool IsStackExpansionPredicate(LLVMValueRef x)
        {
            var cond = x.GetOperand(0);
            if (cond.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            if (cond.ICmpPredicate != LLVMIntPredicate.LLVMIntUGT)
                return false;
            if (!IsAddImm(cond.GetOperand(0)) && !IsAddImm(cond.GetOperand(1)))
                return false;

            return true;
        }

        private static bool IsAddImm(LLVMValueRef x)
        {
            if (x.Kind != LLVMValueKind.LLVMInstructionValueKind)
                return false;
            if (x.InstructionOpcode != LLVMOpcode.LLVMAdd)
                return false;
            if (x.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;

            return true;
        }

        private bool ReachesCycle(LLVMBasicBlockRef curr, HashSet<LLVMBasicBlockRef> visited, HashSet<LLVMBasicBlockRef> stack)
        {
            if (stack.Contains(curr))
                return true;
            if (visited.Contains(curr))
                return false;

            stack.Add(curr);
            visited.Add(curr);
            var terminator = curr.Terminator;
            if (terminator != null)
            {
                for (var i = 0; i < terminator.SuccessorsCount; i++)
                {
                    if (ReachesCycle(terminator.GetSuccessor((uint)i), visited, stack))
                        return true;
                }
            }

            stack.Remove(curr);
            return false;
        }
    }

    public record JmpTablesWithHandlerRips2(IReadOnlyList<VmpJmpTable> Tables, IReadOnlyDictionary<ulong, (ulong rip, ulong? vkey, ulong? vbase)> bytecodePtrToRip);

    // VMP solver but using bitwuzla
    public class VmpSolver2
    {
        private readonly IDna dna;
        private readonly LLVMValueRef func;

        private readonly MbaDeobfuscationPass converter;

        private readonly BitwuzlaTranslator translator;

        public TermManager Tm => translator.z3Ctx;

        private readonly AstCtx ctx;


        public VmpSolver2(IDna dna, LLVMValueRef func)
        {
            this.dna = dna;
            this.func = func;
            converter = new(func);
            translator = new(converter.ctx);
            AstIdx.ctx = converter.ctx;
            ctx = converter.ctx;
        }

        public Result<Dictionary<ulong, HashSet<ulong>>, InvalidOperationException> SolveRIPs(RemillArch arch, VmpParameterizedStateStructure stateStruct)
        {
            var target = VmpSolver.GetBranchIntrinsic(func.GlobalParent);
            if (target.Handle == 0)
                return new Err<InvalidOperationException>(new InvalidOperationException("No jump tables to solve"));


            var calls = RemillUtils.CallersOf(target);
            var ripIndex = (uint)stateStruct.RegisterArgumentIndices[arch.GetRegisterByName(arch.ProgramCounterRegisterName)];

            var output = new Dictionary<ulong, HashSet<ulong>>();
            foreach (var call in calls)
            {
                var jmpFromVip = call.GetOperand((uint)call.OperandCount - 2);
                if (jmpFromVip.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    throw new InvalidOperationException($"Jumping from unknown VIP {jmpFromVip}");

                var rips = new HashSet<ulong>();
                var rip = call.GetOperand(ripIndex);
                if (rip.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                {
                    rips.Add(rip.ConstIntZExt);
                }

                else if (rip.Kind == LLVMValueKind.LLVMInstructionValueKind && rip.InstructionOpcode == LLVMOpcode.LLVMSelect)
                {
                    var constants = new LLVMValueRef[] { rip.GetOperand(1), rip.GetOperand(2) };
                    if (!constants.All(x => x.Kind == LLVMValueKind.LLVMConstantIntValueKind))
                        return new Err<InvalidOperationException>(new InvalidOperationException($"Cannot solve RIP for {rip}"));
                    rips.AddRange(constants.Select(x => x.ConstIntZExt));
                }

                else
                {
                    var solutions = SolveVip(func, rip);
                    rips.AddRange(solutions);
                }


                output.TryAdd(jmpFromVip.ConstIntZExt, new());
                output[jmpFromVip.ConstIntZExt].AddRange(rips);

            }

            return new Ok<Dictionary<ulong, HashSet<ulong>>>(output);
        }

        public IReadOnlySet<LLVMValueRef> GetVmpTargets(LLVMValueRef func)
        {
            HashSet<LLVMValueRef> targets = new();
            foreach(var inst in func.GetInstructions())
            {
                if (inst.Is(LLVMOpcode.LLVMGetElementPtr))
                {
                    targets.AddRange(inst.GetOperands().Where(x => MbaDeobfuscationPass.IsValidIntegerInst(x)));
                    continue;
                }

                if (inst.Is(LLVMOpcode.LLVMCall) && inst.GetCallInstTarget().Name == "vmp_branch")
                {
                    targets.AddRange(inst.GetOperands().Where(x => MbaDeobfuscationPass.IsValidIntegerInst(x)));
                    continue;
                }
            }

            return targets;
        }
        public void Simplify(LLVMValueRef func)
        {
            File.WriteAllText("llvmconstraints.py", new LLVMToBinjaGraph(func).Process());
            var li = new LoopInfo(func);
            var dt = new DominatorTree(func);

            List<AstIdx> toVisit = new();
            //var targets = MbaDeobfuscationPass.GetTargets(func);
            var targets = GetVmpTargets(func);
            foreach (var target in func.GetInstructions())
                toVisit.Add(converter.GetAst(target));

            int simplCount = 0;

            foreach (var block in converter.defMap.Where(x => x.Key.Is(LLVMValueKind.LLVMInstructionValueKind)).Select(x => x.Key.InstructionParent))
                Visit(li, block);

            List<(AstIdx, LLVMValueRef)> simplifications = new();

            var sw = Stopwatch.StartNew();
            //foreach(var (value, idx) in converter.defMap)
            var solver = MkSolver(100);
            solver.Push();
            LLVMBasicBlockRef currBlock = null;
            int rejected = 0;
            while (true)
            {
                foreach (var value in LLVMUtil.GetRpoInstructions(func))
                {
                    //if (!targets.Contains(value))
                    //    continue;
                    // Skip if this is not 
                    if (!converter.defMap.TryGetValue(value, out var idx))
                        continue;
                    if (ctx.IsSymbol(idx) || ctx.IsConstant(idx))
                        continue;

                    // If we enter a new block, throw away the previous constraints.
                    if (currBlock.Handle != 0 && currBlock != value.InstructionParent && !dt.ProperlyDominates(currBlock, value.InstructionParent))
                    {
                        solver.Pop();
                        solver.Push();
                    }

                    currBlock = value.InstructionParent;

                    //Console.WriteLine($"Visiting {value}");

                    //if (value.ToString().Contains("%local_state_struct.sroa.229.2328.extract.trunc.i.i ="))
                    //    Debugger.Break();
                    //if (ctx.GetCost(idx) < 5)
                    if (false)
                    {
                        Console.WriteLine($"Skipping {idx}");
                        continue;
                    }

                    var cost = ctx.GetCost(idx);

                    if (ctx.GetWidth(idx) <= 0)
                        continue;
                    //Console.WriteLine($"Visiting {idx}");

                    var translated = translator.Translate(idx);

                    if (!value.Is(LLVMValueKind.LLVMInstructionValueKind))
                        continue;

                    var blockInfo = Visit(li, value.InstructionParent);
                    //continue;

                    var cond = translator.Translate(blockInfo.assumptions);
                    cond = Tm.MkIte(cond == 1, Tm.MkTrue(), Tm.MkFalse());
                    cond = solver.Simplify(cond);
                    solver.Assert(cond);


                    var solutions = EnumerateSolutions(solver, translated, 2);
                    if (solutions == null)
                    {
                        //Console.WriteLine($"Rejected: {idx}");
                        rejected++;
                        continue;
                    }
                    if (solutions.Count != 1)
                    {
                        //Console.WriteLine($"Rejected: {idx}");
                        rejected++;
                        continue;
                    }


                    var constInt = LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(ctx.GetWidth(idx)), solutions.First());
                    value.ReplaceAllUsesWith(constInt);
                    //Debugger.Break();
                    simplCount++;
                    simplifications.Add((idx, constInt));
                }

                sw.Stop();

                Console.WriteLine($"Found {simplCount} simplifications in {sw.ElapsedMilliseconds}ms");
                break;
                //Debugger.Break();
            }

            //Debugger.Break();
        }

        public record BlockInfo
        {
            // BlockPC varirable to specify which predecessor is true
            // This is an index variable specifying which predecessor was taken
            public AstIdx predSelector;

            // Assumptions that hold if this block is reached
            public AstIdx assumptions;
        }

        Dictionary<LLVMBasicBlockRef, BlockInfo> BlockMap = new();

        public BlockInfo Visit(LoopInfo loopInfo, LLVMBasicBlockRef block)
        {
            // Return the existing info if we've already computed it
            if (BlockMap.TryGetValue(block, out var existing))
                return existing;

            var preds = block.GetPredecessors().ToList();
            var info = new BlockInfo();
            info.predSelector = ctx.Symbol($"pc{BlockMap.Count}", 8);
            info.assumptions = ctx.ICmp(Predicate.Ule, info.predSelector, ctx.Constant(preds.Count == 0 ? 0 : (ulong)preds.Count - 1, 8));
            BlockMap[block] = info;

            // Stop collecting constraints if this is a loop header.
            if (loopInfo.IsLoopHeader(block))
                return info;

            List<AstIdx> assumptions = new();
            assumptions.Add(ctx.True());
            for(var i = 0; i < preds.Count; i++)
            {
                var pred = preds[i];
                var predInfo = Visit(loopInfo, pred);

                var getImplication = (AstIdx cond) =>
                {
                    // Imply the path constraints of the predecessor
                    cond = ctx.And(cond, predInfo.assumptions);

                    // If the ith predecessor was taken, apply the condition.
                    assumptions.Add(Implies(ctx.ICmp(Predicate.Eq, info.predSelector, ctx.Constant((ulong)i, 8)), cond));
                };

                // If it's an unconditional branch, forward the constraints of the predecessor.
                var term = pred.LastInstruction;
                if (term.Is(LLVMOpcode.LLVMBr) && term.OperandCount == 1)
                {
                    getImplication(ctx.Constant(1, 1));
                }

                else if (term.Is(LLVMOpcode.LLVMBr) && term.OperandCount == 3)
                {
                    // If this block is the false destination, we need to invert the condition.
                    var cond = converter.GetAst(term.GetOperand(0));
                    var trueBlock = term.GetOperand(2).AsBasicBlock();
                    var falseBlock = term.GetOperand(1).AsBasicBlock();
                    if (block != trueBlock)
                        cond = ctx.Neg(cond);
                    Debug.Assert(trueBlock != falseBlock);

                    // Add the implication
                    getImplication(cond);
                }

                else if (term.Is(LLVMOpcode.LLVMSwitch))
                {
                    throw new InvalidOperationException();
                }

                else
                {
                    Debugger.Break();
                }
            }

            // Assert that a valid predecessor must be chosen.
            assumptions.Add(ctx.ICmp(Predicate.Ule, info.predSelector, ctx.Constant((ulong)preds.Count - 1, 8)));

            foreach(var phi in block.GetInstructions().Where(x => x.Is(LLVMOpcode.LLVMPHI)))
            {
                // TODO: Handle PHIs
                //Debugger.Break();
            }

            // Create the assumptions that are true when this block is taken.
            // It's implemented as and AND of implications.
            info.assumptions = ctx.And(assumptions);
            return info;
        }

        private AstIdx Implies(AstIdx a, AstIdx b)
        {
            return ctx.Or(ctx.Neg(a), b);
        }

        public HashSet<ulong> SolveVip(LLVMValueRef func, LLVMValueRef value)
        {
            func.GlobalParent.PrintToFile("translatedFunction.ll");
            var ast = converter.GetAst(value);
            var inputVars = ctx.CollectVariables(ast);
            var loadIndices = inputVars.Where(x => converter.inverseSubstMap[x].Is(LLVMOpcode.LLVMLoad)).ToList();
            if (!loadIndices.Any())
                throw new InvalidOperationException();

            var loadIdx = loadIndices.Single();
            var loadInst = converter.inverseSubstMap[loadIdx];
            var loadNode = translator.Translate(loadIdx);
            var gep = loadInst.GetOperand(0);
            if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
                throw new InvalidOperationException();

            var gepInst = gep.GetOperand(1);
            var gepIdx = converter.GetAst(gepInst);
            var translated = translator.Translate(gepIdx);


            // Need to associate addresses with their values. VMProtect does math directly over the pointers
            var solver = MkSolver();
            var gepSolutions = EnumerateSolutions(MkSolver(), translated).ToList();
            HashSet<ulong> solutions = new();
            for (var i = 0; i < gepSolutions.Count; i++)
            {
                var deref = BinaryContentsReader.Dereference(dna.Binary, gepSolutions[i], loadInst.TypeOf.IntWidth);
                solver.Assert(Tm.MkImplies(translated == gepSolutions[i], loadNode == deref));
                solutions.Add(deref);
            }
            //var solutions = DereferenceAddresses(gepSolutions, loadInst.TypeOf.IntWidth);
          
            var constraint = ConstrainValues(translator.Translate(loadIdx), solutions);
            solver.Assert(constraint);

            var results = EnumerateSolutions(solver, translator.Translate(ast));

            return results;
        }


        /// <summary>
        /// Given a set of jump table solutions(aka &jumpTable[index]), return the set of dereferenced values.
        /// </summary>
        private IReadOnlyList<ulong> DereferenceAddresses(IReadOnlyList<ulong> addresses, uint bitWidth)
            => addresses.Select(x => BinaryContentsReader.Dereference(dna.Binary, x, bitWidth)).ToList().AsReadOnly();

        private BvSolver MkSolver(int timeout = 200)
        {
            var options = new Options();
            options.Set(BitwuzlaOption.BITWUZLA_OPT_PRODUCE_MODELS, true);
            options.Set(BitwuzlaOption.BITWUZLA_OPT_TIME_LIMIT_PER, (ulong)timeout);
            var solver = new BvSolver(Tm, options);
            return solver;
        }

        private HashSet<ulong> EnumerateSolutions(
        BvSolver solver,
        Term translated,
        uint? max = null)
        {
            if (!translated.Sort.IsBv)
                throw new ArgumentException("The enumerated term must be a bit-vector.", nameof(translated));

            if (translated.Sort.BvSize > 64)
                throw new NotSupportedException(
                    "Enumerating bit-vectors wider than 64 bits requires a wider result type.");

            var values = new HashSet<ulong>();

            solver.Push();
            while (!max.HasValue || (ulong)values.Count < max.Value)
            {
                switch (solver.CheckSat())
                {
                    case Result.Unsat:
                        solver.Pop();
                        return values;

                    case Result.Unknown:
                        solver.Pop();
                        return null;
                        throw new InvalidOperationException(
                            "Bitwuzla returned Unknown before solution enumeration completed.");

                    case Result.Sat:
                        var modelValue = Tm.GetIntegerValue(solver.GetValue(translated));

                        if (!values.Add(modelValue))
                        {
                            throw new InvalidOperationException(
                                "Bitwuzla returned a value that was already blocked.");
                        }

                        // Exclude this value while allowing any assignment to other
                        // variables that produces a different value for translated.
                        solver.Assert(translated != modelValue);
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Bitwuzla returned an unrecognized result.");
                }
            }

            solver.Pop();
            return values;
        }

        private Term ConstrainValues(Term variable, IEnumerable<ulong> values)
        {
            var initial = Tm.MkFalse();
            foreach (var v in values)
                initial |= (variable == v);
            return initial;
        }
    }

    public class VmpSolver
    {
        private readonly RemillArch arch;

        private readonly LLVMValueRef function;

        public VmpSolver(RemillArch arch, LLVMValueRef function)
        {
            this.arch = arch;
            this.function = function;
        }

        // For each bytecode RIP, collect a set of unique handler RIPs it can branch to
        public Result<Dictionary<ulong, HashSet<ulong>>, InvalidOperationException> SolveRIPs(VmpParameterizedStateStructure stateStruct)
        {
            var target = GetBranchIntrinsic(function.GlobalParent);
            if (target.Handle == 0)
                return new Err<InvalidOperationException>(new InvalidOperationException("No jump tables to solve"));


            var calls = RemillUtils.CallersOf(target);
            var ripIndex = (uint)stateStruct.RegisterArgumentIndices[arch.GetRegisterByName(arch.ProgramCounterRegisterName)];

            var output = new Dictionary<ulong, HashSet<ulong>>();
            foreach (var call in calls)
            {
                var jmpFromVip = call.GetOperand((uint)call.OperandCount - 2);
                if (jmpFromVip.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    throw new InvalidOperationException($"Jumping from unknown VIP {jmpFromVip}");

                var rips = new HashSet<ulong>();
                var rip = call.GetOperand(ripIndex);
                if (rip.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                {
                    rips.Add(rip.ConstIntZExt);
                }

                else if (rip.Kind == LLVMValueKind.LLVMInstructionValueKind && rip.InstructionOpcode == LLVMOpcode.LLVMSelect)
                {
                    var constants = new LLVMValueRef[] { rip.GetOperand(1), rip.GetOperand(2) };
                    if (!constants.All(x => x.Kind == LLVMValueKind.LLVMConstantIntValueKind))
                        return new Err<InvalidOperationException>(new InvalidOperationException($"Cannot solve RIP for {rip}"));
                    rips.AddRange(constants.Select(x => x.ConstIntZExt));
                }

                else
                {
                    return new Err<InvalidOperationException>(new InvalidOperationException($"Cannot solve RIP for {rip}"));
                }

                output.TryAdd(jmpFromVip.ConstIntZExt, new());
                output[jmpFromVip.ConstIntZExt].AddRange(rips);

            }

            return new Ok<Dictionary<ulong, HashSet<ulong>>>(output);
        }

        public Result<JmpTablesWithHandlerRips2, InvalidOperationException> Solve(Dictionary<ulong, HandlerData> handlerRipToRegisters, VmpParameterizedStateStructure stateStructure)
        {
            var targetFunc = GetBranchIntrinsic(function.GlobalParent);
            if (targetFunc.Handle == 0)
            {
                function.GlobalParent.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
                return new Err<InvalidOperationException>(new InvalidOperationException($"No jump tables to solve!"));
            }

            var output = new List<VmpJmpTable>();
            var jmpCalls = RemillUtils.CallersOf(targetFunc).Where(x => x.InstructionParent.Parent == function);

            var bytecodePtrToRip = new Dictionary<ulong, (ulong rip, ulong? vkey, ulong? vbase)>();
            foreach (var jmpCall in jmpCalls)
            {
                var jmpFrom = jmpCall.GetOperand((uint)jmpCall.OperandCount - 2);

                if (jmpFrom.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                {
                    //function.GlobalParent.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
                    return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify jump from address for call {jmpCall}"));
                }

                // If we are jumping out of a vmexit, skip it.
                var constJmpFromAddress = jmpFrom.ConstIntZExt;




                var result = ClassifyHandlerEdge(jmpCall, handlerRipToRegisters, stateStructure);
                if (result is not Ok<HandlerEdge> edge)
                {
                    var err = (Err<InvalidOperationException>)result.Value;
                    return new Err<InvalidOperationException>(err.Error);
                }



                if (edge.Value is ConstantJmpTableEdge constantEdge)
                {
                    LLVMValueRef vkey = null;
                    LLVMValueRef vbaseValue = null;
                    var vk = handlerRipToRegisters[constantEdge.handlerRip].Vkey;
                    var vbase = handlerRipToRegisters[constantEdge.handlerRip].ImgBaseReg;
                    if (vk != null)
                    {
                        var idx = (uint)stateStructure.GetRegisterArgumentIndex(handlerRipToRegisters[constantEdge.handlerRip].Vkey);
                        vkey = jmpCall.GetOperand(idx);
                        if (!vkey.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vkey for {vkey}"));
                    }

                    if (vbase != null)
                    {
                        var idx = (uint)stateStructure.GetRegisterArgumentIndex(vbase);
                        vbaseValue = jmpCall.GetOperand(idx);
                        if (!vbaseValue.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vbase for {vbaseValue}"));
                    }

                    bytecodePtrToRip.Add(constantEdge.bytecodePtr, (constantEdge.handlerRip, vkey.Handle == 0 ? null : vkey.ConstIntZExt, vbaseValue.Handle == 0 ? null : vbaseValue.ConstIntZExt));
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { constantEdge.bytecodePtr }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else if (edge.Value is TwoBytecodeOneHandlerEdge twoBytecodeOneHandlerEdge)
                {
                    LLVMValueRef vkey = null;
                    LLVMValueRef vbaseValue = null;
                    var vk = handlerRipToRegisters[twoBytecodeOneHandlerEdge.handlerRip].Vkey;
                    var vbase = handlerRipToRegisters[twoBytecodeOneHandlerEdge.handlerRip].ImgBaseReg;
                    if (vk != null)
                    {
                        vkey = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vk));
                        if (!vkey.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vkey for {vkey}"));
                    }

                    if (vbase != null)
                    {
                        vbaseValue = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vbase));
                        if (!vbaseValue.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vbase for {vbaseValue}"));
                    }

                    bytecodePtrToRip.Add(twoBytecodeOneHandlerEdge.bytecodePtr1, (twoBytecodeOneHandlerEdge.handlerRip, vkey.Handle == 0 ? null : vkey.ConstIntZExt, vbaseValue.Handle == 0 ? null : vbaseValue.ConstIntZExt));
                    bytecodePtrToRip.Add(twoBytecodeOneHandlerEdge.bytecodePtr2, (twoBytecodeOneHandlerEdge.handlerRip, vkey.Handle == 0 ? null : vkey.ConstIntZExt, vbaseValue.Handle == 0 ? null : vbaseValue.ConstIntZExt));
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { twoBytecodeOneHandlerEdge.bytecodePtr1, twoBytecodeOneHandlerEdge.bytecodePtr2 }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else if (edge.Value is TwoBytecodeTwoHandlerEdge twoBytecodeTwoHandlerEdge)
                {
                    var vk1 = handlerRipToRegisters[twoBytecodeTwoHandlerEdge.handlerRip1].Vkey;
                    var vk2 = handlerRipToRegisters[twoBytecodeTwoHandlerEdge.handlerRip2].Vkey;
                    var vb1 = handlerRipToRegisters[twoBytecodeTwoHandlerEdge.handlerRip1].ImgBaseReg;
                    var vb2 = handlerRipToRegisters[twoBytecodeTwoHandlerEdge.handlerRip2].ImgBaseReg;

                    LLVMValueRef vkey1 = null;
                    LLVMValueRef vkey2 = null;
                    LLVMValueRef vbase1 = null;
                    LLVMValueRef vbase2 = null;
                    if (vk1 != null && vk2 != null)
                    {
                        vkey1 = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vk1));
                        vkey2 = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vk2));
                        if (vkey1 == vkey2 && vkey1.Is(LLVMOpcode.LLVMSelect))
                        {
                            var k1 = vkey1.GetOperand(1);
                            var k2 = vkey1.GetOperand(2);
                            (vkey1, vkey2) = (k1, k2);
                        }


                        if (!vkey1.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vkey for {vkey1}"));
                    }

                    if (vb1 != null && vb2 != null)
                    {
                        vbase1 = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vb1));
                        vbase2 = jmpCall.GetOperand((uint)stateStructure.GetRegisterArgumentIndex(vb2));
                        if (vbase1 == vbase2 && vbase1.Is(LLVMOpcode.LLVMSelect))
                        {
                            var base1 = vbase1.GetOperand(1);
                            var base2 = vbase1.GetOperand(2);
                            (vbase1, vbase2) = (base1, base2);
                        }

                        if (!vbase1.IsConstant())
                            return new Err<InvalidOperationException>(new InvalidOperationException($"Could not identify constant vbase for {vbase1}"));
                    }

                    bytecodePtrToRip.Add(twoBytecodeTwoHandlerEdge.bytecodePtr1, (twoBytecodeTwoHandlerEdge.handlerRip1, vkey1.Handle == 0 ? null : vkey1.ConstIntZExt, vbase1.Handle == 0 ? null : vbase1.ConstIntZExt));
                    bytecodePtrToRip.Add(twoBytecodeTwoHandlerEdge.bytecodePtr2, (twoBytecodeTwoHandlerEdge.handlerRip2, vkey2.Handle == 0 ? null : vkey2.ConstIntZExt, vbase2.Handle == 0 ? null : vbase2.ConstIntZExt));
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { twoBytecodeTwoHandlerEdge.bytecodePtr1, twoBytecodeTwoHandlerEdge.bytecodePtr2 }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else
                {
                    // Otherwise this is probably an unsolved jump table. Error out.
                    //jmpCall.InstructionParent.Parent.GlobalParent.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
                    return new Err<InvalidOperationException>(new InvalidOperationException($"Failed to solve indirect jump! {jmpCall}"));
                }

                foreach (var entry in bytecodePtrToRip)
                {
                    //jmpCall.InstructionParent.Parent.GlobalParent.PrintToFile(ArtifactPaths.Resolve("translatedFunction.ll"));
                    if (entry.Key == entry.Value.rip)
                        Debugger.Break();
                }

            }

            return new Ok<JmpTablesWithHandlerRips2>(new(output, bytecodePtrToRip));
        }

        private static Result<HandlerEdge, InvalidOperationException> ClassifyHandlerEdge(LLVMValueRef jmpCall, Dictionary<ulong, HandlerData> handlerRipToRegisters, VmpParameterizedStateStructure stateStructure)
        {
            var nativeInstPtr = jmpCall.GetOperand((uint)stateStructure.RegisterArgumentIndices.Single(x => x.Key.Name == "RIP").Value);

            HandlerData handlerData = null;
            if (nativeInstPtr.IsConstant())
                handlerData = handlerRipToRegisters[nativeInstPtr.ConstIntZExt];
            if (nativeInstPtr.Is(LLVMOpcode.LLVMSelect))
                handlerData = new LLVMValueRef[2] { nativeInstPtr.GetOperand(1), nativeInstPtr.GetOperand(2) }.Select(x => handlerRipToRegisters[x.ConstIntZExt]).Distinct().Single();

            // Try to simple a constant to constant edge.
            var bytecodePtr = jmpCall.GetOperand((uint)stateStructure.RegisterArgumentIndices.Single(x => x.Key.Name == handlerData.Vip.Name).Value);
            HandlerEdge edge = TryMatchConstantEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return new Ok<HandlerEdge>(edge);
            // Try to match a select of two possible bytecode pointers, where both handlers share the same RIP.
            edge = TryMatchTwoBytecodeOneHandlerEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return new Ok<HandlerEdge>(edge);
            edge = TryMatchTwoBytecodeTwoHandlerEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return new Ok<HandlerEdge>(edge);

            // Otherwise this is probably an unsolved jump table. Error out.
            var memPtr = jmpCall.InstructionParent.Parent.GlobalParent.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(jmpCall.InstructionParent.Parent.GlobalParent.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }

            return new Err<InvalidOperationException>(new InvalidOperationException($"Failed to solve indirect jump! {jmpCall}"));
        }

        private static ConstantJmpTableEdge TryMatchConstantEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we're not dealing with a constant outgoing bytecode ptr and a constant handler rip, return null.
            if (bytecodePtr.Kind != LLVMValueKind.LLVMConstantIntValueKind || nativeInstPtr.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;
            return new ConstantJmpTableEdge(bytecodePtr.ConstIntZExt, nativeInstPtr.ConstIntZExt);
        }

        private static TwoBytecodeOneHandlerEdge TryMatchTwoBytecodeOneHandlerEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we are not selecting from two constant bytecode ptrs, or if native instruction point is not a constant, return null
            if (!IsSelectOfTwoConstants(bytecodePtr) || nativeInstPtr.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;
            return new TwoBytecodeOneHandlerEdge(bytecodePtr.GetOperand(1).ConstIntZExt, bytecodePtr.GetOperand(2).ConstIntZExt, nativeInstPtr.ConstIntZExt);
        }

        private static TwoBytecodeTwoHandlerEdge TryMatchTwoBytecodeTwoHandlerEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we are not selecting from two constant bytecode ptrs and two constant handler RIPs, return null.
            if (!IsSelectOfTwoConstants(bytecodePtr) || !IsSelectOfTwoConstants(nativeInstPtr))
                return null;
            // If the select statements are picking two different conditions, we cannot clearly map which bytecode pointer belongs to which handler.
            if (bytecodePtr.GetOperand(0) != nativeInstPtr.GetOperand(0))
                return null;
            return new TwoBytecodeTwoHandlerEdge(bytecodePtr.GetOperand(1).ConstIntZExt, nativeInstPtr.GetOperand(1).ConstIntZExt, bytecodePtr.GetOperand(2).ConstIntZExt, nativeInstPtr.GetOperand(2).ConstIntZExt);
        }

        private static bool IsSelectOfTwoConstants(LLVMValueRef inst)
        {
            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;
            // If either operand is not a constant, return false.
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind || inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;
            return true;
        }

        public static LLVMValueRef GetBranchIntrinsic(LLVMModuleRef module)
            => module.GetFunctions().SingleOrDefault(x => x.Name.Contains("vmp_branch"));
    }

    // TODO: Implement check. Is vip/vkey solvable, bail if early


    public static class VmpPassPipeline
    {
        public static void Run(IBinary bin, LLVMValueRef function)
        {
            var mod = function.GlobalParent;
            var func = mod.GetFunctions().FirstOrDefault(func => func.Name == "vmp_branch");
            var callers = func == null ? new List<LLVMValueRef>() : RemillUtils.CallersOf(func).Where(x => x.InstructionParent.Parent == function);

            int iter = 0;
            while (iter < 5)
            {

                var optimizeFast = () =>
                {
                    var storeToLoad = new CombinedFixedpointOptPass(bin, new FixedpointPassConfig());
                    var pStoreToLoad = Marshal.GetFunctionPointerForDelegate(storeToLoad.PtrToStoreLoadPropagation);
                    OptimizationApi.OptimizeModuleVmp(function.GlobalParent, function, false, false, 0, false, 0, false, false, 0, pStoreToLoad, 0, 0, fastPipeline: true);
                };

                for (int i = 0; i < 1; i++)
                {
                    optimizeFast();
                }

                iter++;
            }
        }
    }
}
