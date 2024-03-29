﻿using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Lifting;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using Dna.LLVMInterop.Passes;
using Dna.Relocation;
using Dna.Utilities;
using DotNetGraph.Extensions;
using ELFSharp.UImage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using static Dna.LLVMInterop.NativeOptimizationApi;

namespace Dna.Devirtualization
{
    public class CfgExplorer
    {
        private readonly IDna dna;

        private readonly ICpuArchitecture architecture;

        private dgReadBinaryContents readBinaryContents;

        private nint ptrReadBinaryContents;

        Dictionary<ulong, HashSet<ulong>> learnedEdges = new();

        private List<ulong> handlers = new();

        public CfgExplorer(IDna dna, ICpuArchitecture architecture)
        {
            this.dna = dna;
            this.architecture = architecture;
            var readBytes = (ulong address, uint size) =>
            {
                var bytes = dna.Binary.ReadBytes(address, (int)size);
                var value = size switch
                {
                    1 => bytes[0],
                    2 => BitConverter.ToUInt16(bytes),
                    4 => BitConverter.ToUInt32(bytes),
                    8 => BitConverter.ToUInt64(bytes),
                    _ => throw new InvalidOperationException()
                };
                return (ulong)value;
            };

            readBinaryContents = new dgReadBinaryContents(readBytes);
            ptrReadBinaryContents = Marshal.GetFunctionPointerForDelegate(readBinaryContents);
        }

        public void DevirtualizeRoutine(ulong address)
        {
            int handlerCount = 0;
            handlers.Add(address);
            while(true)
            {
                var newCfg = new ControlFlowGraph<Iced.Intel.Instruction>(address);

                var handlerCfgs = handlers.Select(x => dna.RecursiveDescent.ReconstructCfg(address)).ToList();

                ulong blockCount = 0;
                for(int i = 0; i < handlerCfgs.Count; i++)
                {
                    var handlerCfg = handlerCfgs[i];
                    var exitBlocks = newCfg.GetBlocks().Where(x => x.OutgoingEdges.Count == 0).ToHashSet();

                    foreach(var block in handlerCfg.GetBlocks())
                    {
                        // Create a new block in the new cfg.
                        var newBlock = newCfg.CreateBlock(blockCount);

                        // Copy the block instructions.
                        newBlock.Instructions.AddRange(block.Instructions);

                        //
                    }

                }

                /*
                //
                // Apply recurside descent while feeding the CFG explorer with newly
                // discovered edges.
                var cfg = dna.RecursiveDescent.ReconstructCfg(address, GetDiscoveredEdges);



                var dot = GraphVisualizer.GetDotGraph(cfg);
                var compiled = dot.Compile();
                File.WriteAllText($"partial_cfg_{handlerCount}.dot", compiled);

                // Lift the control flow graph to our IR.
                var cfgLifter = new CfgLifter(architecture);
                var liftedCfg = cfgLifter.LiftCfg(cfg);

                // Lift the cfg to LLVM IR.
                var llvmLifter = new LLVMLifter(architecture);
                llvmLifter.Lift(liftedCfg);

                llvmLifter.Module.PrintToFile($"handler_{handlerCount}_preopt.ll");

                OptimizeVirtualizedFunction(llvmLifter, cfg);

                llvmLifter.Module.PrintToFile($"handler_{handlerCount}_postopt.ll");

                // Compile to a .exe using clang.
                Console.WriteLine("Compiling to an exe.");
                string llPath = "devirtualized.ll";
                llvmLifter.Module.PrintToFile(llPath);
                var compiledPath = ClangCompiler.Compile(llPath);

                Console.WriteLine("Loading into IDA.");
                var exePath = IDALoader.Load(compiledPath);
                Console.WriteLine("Loaded executable into IDA.");

                handlerCount++;
                */
            }
        }

        private void OptimizeVirtualizedFunction(LLVMLifter llvmLifter, ControlFlowGraph<Iced.Intel.Instruction> asmCfg)
        {
            for (int i = 0; i < 10; i++)
            {
                // Run a fast, O3-like pipeline.
                RunO3(llvmLifter);

                // Run our pipeline with custom passes.
                RunCustomPipeline(llvmLifter);
            }

            // Get the lifted LLVM function.
            var func = llvmLifter.llvmFunction;

            // Get all stores to RIP.
            var ripStores = func
                .GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMSharp.Interop.LLVMOpcode.LLVMStore && x.GetOperand(1).Name == "rip")
                .ToList();

            Console.WriteLine($"Found {0} RIP stores.", ripStores.Count);
            if(ripStores.Count > 1)
            {
                Debugger.Break();
            }

            ulong? newRip = 0;
            foreach(var ripStore in ripStores)
            {
                if(ripStore.GetOperand(0).Kind != LLVMSharp.Interop.LLVMValueKind.LLVMConstantIntValueKind)
                {
                    llvmLifter.Module.PrintToFile("cant_resolve.ll");

                    var compiledPath = ClangCompiler.Compile("cant_resolve.ll");

                    Console.WriteLine("Loading into IDA.");
                    var exePath = IDALoader.Load(compiledPath);
                    Console.WriteLine("Loaded executable into IDA.");
                    Console.WriteLine("Conditional RIP detected.");
                    Debugger.Break();
                }

                var constInt = ripStore.GetOperand(0).ConstIntZExt;
                newRip = constInt;
            }

            if (newRip == 0)
                Debugger.Break();

            var terminatorBlocks = asmCfg.GetBlocks().Where(x => x.OutgoingEdges.Count == 0).ToList();

            var terminatorInsts = terminatorBlocks.Select(x => x.ExitInstruction).ToList();
            if(terminatorInsts.Any())
            foreach (var terminatorBlock in terminatorBlocks)
            {
                // Create an edge list if it does not exist already.
                learnedEdges.TryAdd(terminatorBlock.Address, new HashSet<ulong>());

                // Save the learned edges.
                var edgeList = learnedEdges[terminatorBlock.Address];
                edgeList.Add(newRip.Value);
            }
        }

        private void RunO3(LLVMLifter llvmLifter)
        {
            var passManager2 = llvmLifter.Module.CreateFunctionPassManager();
            passManager2.AddBasicAliasAnalysisPass();
            passManager2.AddTypeBasedAliasAnalysisPass();
            passManager2.AddScopedNoAliasAAPass();
          //  passManager2.AddLowerExpectIntrinsicPass();
            passManager2.AddCFGSimplificationPass();
            passManager2.AddPromoteMemoryToRegisterPass();
            passManager2.AddEarlyCSEPass();
            passManager2.AddDCEPass();
            passManager2.AddAggressiveDCEPass();
            passManager2.AddDeadStoreEliminationPass();
            passManager2.AddInstructionCombiningPass();
            passManager2.AddCFGSimplificationPass();
            passManager2.AddDeadStoreEliminationPass();
            passManager2.AddAggressiveDCEPass();
            passManager2.InitializeFunctionPassManager();
            for (int i = 0; i < 5; i++)
            {
                passManager2.RunFunctionPassManager(llvmLifter.llvmFunction);
            }

            passManager2.FinalizeFunctionPassManager();
        }

        private void RunCustomPipeline(LLVMLifter llvmLifter)
        {
            llvmLifter.Module.PrintToFile("beforecustompipeline.ll");
            // Run the custom pipeline with very light settings.
            for (int i = 0; i < 3; i++)
            {
                OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, false, 0, false, 0, false);
            }

            var ptrAlias = ClassifyingAliasAnalysisPass.PtrGetAliasResult;

            // Run the O3 pipeline with custom alias analysis.
            Console.WriteLine("o1");
            OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

            Console.WriteLine("o2");

            var cfPass = new ControlFlowStructuringPass();
            var pStructureFunction = Marshal.GetFunctionPointerForDelegate(cfPass.PtrStructureFunction);



            // Run the O3 pipeline with ptr alias analysis AND aggressive loop unrolling enabled.
            OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, true, true, ptrAlias, false, 0, false, false, pStructureFunction);

            Console.WriteLine(  "o3");
            // Run the O3 pipeline one last time with custom alias analysis.
            OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false, false);
            Console.WriteLine(  "done");
            // Run a pass to concretize known constants within binary sections.
            var myPass = new ConstantConcretizationPass(llvmLifter.llvmFunction, llvmLifter.builder, dna.Binary);
            myPass.Execute();

            Console.WriteLine( "up to 5");
            // Finally run our pipeline again with custom alias analysis.
            for (int i = 0; i < 5; i++)
            {
                OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);
            }
        }

        private IEnumerable<ulong> GetDiscoveredEdges(BasicBlock<Iced.Intel.Instruction> block)
        {
            // Return nothing if we don't know the edges.
            if (!learnedEdges.ContainsKey(block.Address))
                return new List<ulong>();

            var exitInstruction = block.ExitInstruction;
            if(exitInstruction.Mnemonic != Iced.Intel.Mnemonic.Jmp)
            {
                Debugger.Break();
            }

            // Otherwise, we have proved edges, which need to be fed back to the recovery algorithm.
            var edges = learnedEdges[block.Address];

            // We don't support branches *yet*.
            if (edges.Count != 1)
                throw new InvalidOperationException();


            var assembler = new Iced.Intel.Assembler(64);
            assembler.jmp(edges.Single());

            //var encoded = InstructionRelocator.EncodeInstructions(assembler.Instructions.ToList(), block.ExitInstruction.IP, out ulong endRIP);
            var encoded = InstructionRelocator.RelocateInstructions(assembler.Instructions.ToList(), block.ExitInstruction.IP);
            block.ExitInstruction = encoded.Single();
            return edges;
        }
    }
}
