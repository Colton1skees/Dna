using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Emulation;
using Dna.Lifting;
using Dna.Optimization;
using Dna.Optimization.Passes;
using Dna.Optimization.Ssa;
using Dna.Relocation;
using Rivers;
using Rivers.Analysis;
using System.Diagnostics;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;

var graph = new Graph();
var n0 = new Node("0");
var n1 = new Node("1");
var n2 = new Node("2");
var n3 = new Node("3");
var n4 = new Node("4");

graph.Nodes.Add(n0);
graph.Nodes.Add(n1);
graph.Nodes.Add(n2);
graph.Nodes.Add(n3);
graph.Nodes.Add(n4);

n0.OutgoingEdges.Add(n1);
n1.OutgoingEdges.Add(n2);
n1.OutgoingEdges.Add(n3);
n2.OutgoingEdges.Add(n4);
n3.OutgoingEdges.Add(n4);

var analysis = new DominatorAnalysis(graph);
var tree = analysis.GetDominatorTree();


// Load the windows binary.
var path = @"C:\Users\colton\source\repos\ObfuscationTester\x64\Release\ObfuscationTester.exe";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse the cfg of the target function.
ulong funcAddr = 0x140001030;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);
Console.WriteLine(GraphFormatter.FormatGraph(cfg));

// Instantiate an x86 cfg lifter.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);


Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");

var simplifier = new BlockDcePass(liftedCfg);
simplifier.Run();

Console.WriteLine("Dumping cfg");
Console.WriteLine(GraphFormatter.FormatGraph(liftedCfg));

// Instantiate an emulator & map the binary into it's memory space.
var emu = new UnicornEmulator(architecture);

GC.KeepAlive(emu);
PEMapper.MapBinary(emu, binary);

// Setup register state.
emu.SetRegister(register_e.ID_REG_X86_RCX, 4);
emu.SetRegister(register_e.ID_REG_X86_RDX, 8888);
emu.SetRegister(register_e.ID_REG_X86_R8, 999);
emu.SetRegister(register_e.ID_REG_X86_R9, 23323);

/*
emu.Start(0x140001030);

while(true)
{
    Thread.Sleep(50);
}
*/


// Lift the cfg to llvm IR.
var compiler = new LLVMLifter(architecture);
compiler.Compile(liftedCfg);

//var constructor = new SsaDiGraph(liftedCfg);
//constructor.Transform();

//var constructor = new SsaDiGraph(liftedCfg);
//var sw = Stopwatch.StartNew();
//constructor.Transform();
//Console.WriteLine("Took {0} ms to transform SSA.", sw.ElapsedMilliseconds);
Console.WriteLine("Finished.");
Console.WriteLine(emu.codeHook);
Console.ReadLine();
