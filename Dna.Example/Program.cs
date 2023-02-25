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

// Load the 64 bit PE file.
// Note: This file is automatically copied to the build directory.
var path = @"C:\Users\colton\source\repos\ObfuscationTester\x64\Release\ObfuscationTester.exe";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse a control flow graph from the binary.
ulong funcAddr = 0x14000136C;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);
//Console.WriteLine("Disassembled cfg:\n{0}", GraphFormatter.FormatGraph(cfg));
//Console.WriteLine(GraphFormatter.FormatGraph(cfg));

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);
// var liftedCfg = StructuringTest.GetSimpleComparison();

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

// Elminate deadcode from the control flow graph.
//var blockDcePass = new BlockDcePass(liftedCfg);
//blockDcePass.Run();

// Print the optimized control flow graph.
//Console.WriteLine("Optimized cfg:\n{0}", GraphFormatter.FormatGraph(liftedCfg));

// Create a .DOT file for visualizing the IR cfg.
//var dotGraph = GraphVisualizer.GetDotGraph(liftedCfg);
//File.WriteAllText("graph.dot", dotGraph.Compile(false, false));

// Load the binary into unicorn engine.
// var emulator = new UnicornEmulator(architecture);
//PEMapper.MapBinary(emulator, binary);

// Setup the stack.
//ulong rsp = 0x100000000;
//emulator.MapMemory(rsp, 0x1000 * 12);
//rsp += 0x100;
//emulator.SetRegister(register_e.ID_REG_X86_RSP, rsp);

// Execute the function.
//emulator.Start(0x140001747);

// Lift the control flow graph to LLVM IR.
var llvmLifter = new LLVMLifter(architecture);
llvmLifter.Lift(liftedCfg);

var exitNodes = liftedCfg.Nodes.Where(x => x.OutgoingEdges.Count == 0);


var passManager = llvmLifter.Module.CreateFunctionPassManager();

passManager.AddBasicAliasAnalysisPass();
passManager.AddTypeBasedAliasAnalysisPass();
passManager.AddScopedNoAliasAAPass();


passManager.AddLowerExpectIntrinsicPass();
passManager.AddCFGSimplificationPass();
passManager.AddPromoteMemoryToRegisterPass();
passManager.AddEarlyCSEPass();

passManager.AddDCEPass();
passManager.AddAggressiveDCEPass();
passManager.AddDeadStoreEliminationPass();

passManager.AddInstructionCombiningPass();
passManager.AddCFGSimplificationPass();
passManager.AddDeadStoreEliminationPass();
passManager.AddAggressiveDCEPass();
passManager.InitializeFunctionPassManager();
for (int i = 0; i < 10; i++)
{
    passManager.RunFunctionPassManager(llvmLifter.llvmFunction);
}
passManager.FinalizeFunctionPassManager();


// Optionally write the llvm IR to the console.
bool printLLVM = true;
if (printLLVM)
    llvmLifter.Module.Dump();

var llPath = @"C:\Users\colton\Downloads\dfgfgfgd\code.ll";
var compiledAsmPath = @"C:\Users\colton\Downloads\dfgfgfgd\code.asm";
var wasmTextPath = @"C:\Users\colton\Downloads\dfgfgfgd\code.wat";
var wasmBinaryPath = @"C:\Users\colton\Downloads\dfgfgfgd\code.wasm";
var compiledExePath = @"C:\Users\colton\Downloads\dfgfgfgd\code.exe";
llvmLifter.Module.PrintToFile(llPath);
llvmLifter.Module.WriteBitcodeToFile(@"C:\Users\colton\Downloads\dfgfgfgd\lifted.bc");

//var test = new StructuringTest();
//test.Run();

Console.WriteLine("Ran tests... press enter");
Console.WriteLine("\n\n\n");
//Console.ReadLine();

var stackifier = new StackifierTwo();
var wasm = stackifier.Stackify(liftedCfg);


var astEmitter = new ASTEmitter();
astEmitter.Emit(wasm);
Console.WriteLine(astEmitter.builder.ToString());
Console.WriteLine();


Console.WriteLine("Press enter.");
Console.ReadLine();
// Compile the bitcode to a native executable.
var nativeDecompiler = new LLVMDecompiler();
//nativeDecompiler.RunClang($"{llPath} -S -o {compiledAsmPath} -O3 -target x86_64");
//nativeDecompiler.RunClang($"{compiledAsmPath} -target x86_64 -O3 -c -o {compiledExePath}");

// Compile the bitcode to wasm text.
/*
nativeDecompiler.RunClang($"{llPath} -o {compiledAsmPath} -O3 -target wasm64");
var cmd = $"{llPath} -S -o {wasmTextPath} -O3 -target wasm64";
Console.WriteLine(cmd);
nativeDecompiler.RunClang(cmd);

// Compile the bitcode to wasm binary.
var cmd2 = $"wat2wasm {wasmTextPath} -o {wasmBinaryPath}";
Console.WriteLine(cmd2);
nativeDecompiler.RunClang(cmd2);
*/


var cmd = $"{llPath} -S -o {wasmTextPath} -O3 -target wasm64";
//nativeDecompiler.RunClang(cmd);
//nativeDecompiler.RunClang($"{llPath} -S -o {wasmBinaryPath} -O3 -target wasm64");
//nativeDecompiler.RunClang($"--target=wasm64 -O3 -nostdlib {llPath} -o {wasmBinaryPath} -Wl,--no-entry");
// Decompile the bitcode to pseudo C.
nativeDecompiler.Decompile(wasmBinaryPath);
Console.WriteLine("Press enter to run rellic.");
Console.ReadLine();
// Optionally decompile the lifted function to go-to free pseudo C, via Rellic.
// On my machine, a fork of Rellic runs under WSL2 and communiucates via gRPC.
// If you are not hosting this server at localhost:50051, then the API
// call will fail. You can find the service here(https://github.com/Colton1skees/rellic-api),
// although it will take a bit of leg work for outside use.
bool decompile = true;
if (decompile)
{
    // Create a decompiler instance.
    var decompiler = new Decompiler(architecture);

    // Decompile the lifted function to pseudo C.
    var ast = decompiler.Decompile(llvmLifter.Module);

    // Print the decompiled routine.
    Console.WriteLine("Decompiled routine:\n{0}", ast);
}

Console.WriteLine("Finished.");
Console.ReadLine();
