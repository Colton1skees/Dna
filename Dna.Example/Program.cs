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
using Google.Protobuf;
using Grpc.Net.Client;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;

// Load the 64 bit PE file.
// Note: This file is automatically copied to the build directory.
var path = @"SampleExecutable";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse a control flow graph from the binary.
ulong funcAddr = 0x140001030;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);
Console.WriteLine("Disassembled cfg:\n{0}", GraphFormatter.FormatGraph(cfg));
Console.WriteLine(GraphFormatter.FormatGraph(cfg));

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

// Elminate deadcode from the control flow graph.
var blockDcePass = new BlockDcePass(liftedCfg);
blockDcePass.Run();

// Print the optimized control flow graph.
Console.WriteLine("Optimized cfg:\n{0}", GraphFormatter.FormatGraph(liftedCfg));

// Create a .DOT file for visualizing the IR cfg.
var dotGraph = GraphVisualizer.GetDotGraph(liftedCfg);
File.WriteAllText("graph.dot", dotGraph.Compile(false, false));

// Lift the control flow graph to LLVM IR.
var llvmLifter = new LLVMLifter(architecture);
llvmLifter.Lift(liftedCfg);

// Optionally write the llvm IR to the console.
bool printLLVM = true;
if (printLLVM)
    llvmLifter.DumpModule();

Console.WriteLine("Press enter to decompile");

var channel = GrpcChannel.ForAddress(new Uri("http://localhost:50051"));
var client = new RellicDecompilation.RellicDecompilationClient(channel);
var bytes = llvmLifter.Serialize();
var byteString = ByteString.CopyFrom(bytes);
var reply = client.Decompile(new DecompileCommand()
{
    LlvmModuleText = byteString
});

var sw2 = Stopwatch.StartNew();
for (int i = 0; i < 50; i++)
{
    bytes = llvmLifter.Serialize();
    byteString = ByteString.CopyFrom(bytes);
    reply = client.Decompile(new DecompileCommand()
    {
        LlvmModuleText = byteString
    });
}

sw2.Stop();
Console.WriteLine("Took {0} ms to decompile 1000 methods", sw2.ElapsedMilliseconds);
// Console.WriteLine(reply.DecompiledText);



llvmLifter.WriteToBitcodeFile("Lifted.ll");
//Console.WriteLine(File.ReadAllText("Lifted.ll"));

Console.WriteLine("Finished.");
Console.ReadLine();
