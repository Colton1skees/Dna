using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.Lifting;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;

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

// Lift the cfg to llvm IR.
//var compiler = new LLVMLifter(architecture);
//compiler.Compile(liftedCfg);

Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");

Console.WriteLine("Dumping cfg");
Console.WriteLine(GraphFormatter.FormatGraph(liftedCfg));
Console.WriteLine("Finished.");
Console.ReadLine();
