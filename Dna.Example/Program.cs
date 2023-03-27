using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Emulation;
using Dna.Lifting;
using Dna.Optimization;
using Dna.Optimization.Passes;
using Dna.Relocation;
using Dna.Synthesis.Jit;
using Dna.Synthesis.Miasm;
using Dna.Synthesis.Parsing;
using Dna.Synthesis.Simplification;
using Dna.Synthesis.Utils;
using DotNetGraph.Extensions;
using Rivers;
using Rivers.Analysis;
using System.Diagnostics;
using Grpc.Net.Client;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using ClangSharp.Interop;
using ClangSharp;
using Dna.Decompiler;
using Dna.Emulation.Unicorn;
using Dna.Decompilation;
using Dna.Structuring.Stackify;
using Dna.Emulation.Symbolic;
using TritonTranslator.Intermediate;
using System;
using TritonTranslator.Conversion;
using LLVMSharp.Interop;
using Dna.Decompiler.Rellic;
using Dna.LLVMInterop;
using System.Runtime.InteropServices;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using static Dna.LLVMInterop.NativeOptimizationApi;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.Passes;
using Dna.Utilities;
using Dna.Devirtualization;

// Load the 64 bit PE file.
// Note: This file is automatically copied to the build directory.
var path = @"C:\Users\colton\source\repos\ObfuscationTester\x64\Release\ObfuscationTester.themida.exe";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Replace themida spinlock with nop.
binary.WriteBytes(0x000000014001552B, new byte[]
{
    0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
    0x90, 0x90,
    0x90, 0x90, 0x90, 0x90, 0x90
});

/*
var assembler = new Iced.Intel.Assembler(64);
assembler.jmp(0x14004B2EE);

var encoded = InstructionRelocator.EncodeInstructions(assembler.Instructions.ToList(), 0x140015C47, out ulong endRIP);
binary.WriteBytes(0x140015C47, encoded);
*/

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse a (virtualized) control flow graph from the binary.
ulong funcAddr = 0x14000123C;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

bool explore = false;
if (explore)
{
    var cfgExplorer = new CfgExplorer(dna, architecture);
    cfgExplorer.DevirtualizeRoutine(funcAddr);
}

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

var llvmLifter = new LLVMLifter(architecture);


var ctx = LLVMContextRef.Create();
var memBuffer = LlvmUtilities.CreateMemoryBuffer(@"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Debug\net7.0\cant_resolve_crash.ll");
ctx.TryParseIR(memBuffer, out LLVMModuleRef unicornTraceModule, out string unicornLoadMsg);

llvmLifter.module = unicornTraceModule;
llvmLifter.llvmFunction = llvmLifter.Module.FirstFunction;

/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-memdep-block-scan-limit=10000000",
    "-dse-memoryssa-defs-per-block-limit=1000000",
    "-dse-memoryssa-partial-store-limit=1000000",
    "-dse-memoryssa-path-check-limit=1000000",
    "-dse-memoryssa-scanlimit=1000000",
    "-dse-memoryssa-walklimit=1000000",
    "-dse-memoryssa-otherbb-cost=2",
    "-memssa-check-limit=1000000",
    "memdep-block-number-limit=10000",
    "-memdep-block-scan-limit=1000000",
    "-gvn-max-block-speculations=1000000",
    "-gvn-max-num-deps=1000000",
});
*/

// Lift the cfg to LLVM IR.
//llvmLifter.Lift(liftedCfg);

// Optimize the routine.
bool optimize = false;
if (optimize)
{
    var passManager2 = llvmLifter.Module.CreateFunctionPassManager();
    passManager2.AddBasicAliasAnalysisPass();
    passManager2.AddTypeBasedAliasAnalysisPass();
    passManager2.AddScopedNoAliasAAPass();
    passManager2.AddLowerExpectIntrinsicPass();
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

var readBytes = (ulong address, uint size) =>
{
    var bytes = binary.ReadBytes(address, (int)size);
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


var dgReadBytes = new dgReadBinaryContents(readBytes);
var ptrReadBinaryContents = Marshal.GetFunctionPointerForDelegate(dgReadBytes);

var ptrAlias = ClassifyingAliasAnalysisPass.PtrGetAliasResult;

var llPath = @"optimized_vm_entry2.ll";



// Run the standard O3 pipeline.
for (int i = 0; i < 5; i++)
{
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, false, 0, false, 0, false);
}

// Run the O3 pipeline with custom alias analysis.
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

// Run the O3 pipeline with ptr alias analysis AND aggressive loop unrolling enabled.
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, true, true, ptrAlias, false, 0, false);



// Run the O3 pipeline one last time with custom alias analysis.
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

//llvmLifter.Module.PrintToFile(llPath);
//var myPass = new ConstantConcretizationPass(llvmLifter.llvmFunction, llvmLifter.builder, binary);
//myPass.Execute();

// Run the O3 pipeline one last time with custom alias analysis.
PointerClassifier.Seen.Clear();
PointerClassifier.print = true;
for (int i = 0; i < 7; i++)
{
    Console.WriteLine(i);
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

    var pass2 = new ConstantConcretizationPass(llvmLifter.llvmFunction, llvmLifter.builder, binary);
    pass2.Execute();

    llvmLifter.Module.PrintToFile("foo.ll");
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false, true);
    Console.WriteLine("foo foo foo");
}


llvmLifter.Module.PrintToFile(llPath);

bool compile = true;
if (compile)
{
    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.");
    var compiledPath = ClangCompiler.Compile(llPath);

    Console.WriteLine("Loading into IDA.");
    var exePath = IDALoader.Load(compiledPath);
    Console.WriteLine("Loaded executable into IDA.");
}

Debugger.Break();