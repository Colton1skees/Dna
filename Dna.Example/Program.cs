using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Emulation;
using Dna.Lifting;
using Dna.Optimization;
using Dna.Optimization.Passes;
using Dna.Optimization.Ssa;
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
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;

// Load the 64 bit PE file.
// Note: This file is automatically copied to the build directory.
var path = @"SampleExecutable";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Instantiate dna.
var dna = new Dna.Dna(binary);

var libraryPath = @"C:\Users\colton\source\repos\msynth\database\reduced_database.txt";
var lines = File.ReadAllLines(libraryPath);

var oracle = new SimplificationOracle(7, 30, libraryPath);

var simplifier = new ExpressionSimplifier(oracle, false, 1);

// Construct expression: { ~((~A)+B)
var a = new ExprId("a", 16);
var b = new ExprId("b", 16);
var c = new ExprId("c", 16);

var ored = new ExprOp(16, "|", a, b);
var anded = new ExprOp(16, "&", ored, a);

for(int i = 0; i < 250; i++)
{
    if(i % 3 == 0)
    {
        anded = new ExprOp(16, "*", c, anded);
    }

    else
    {
        anded = new ExprOp(16, "+", anded, b);
    }
}

// Simplify the expression
var sw = Stopwatch.StartNew();
var simplified = simplifier.Simplify(anded);

// Print the simplified expression
sw.Stop();
var str = ExpressionFormatter.FormatExpression(simplified);
Console.WriteLine(str);

Console.WriteLine("Took {0} ms to simplify expression", sw.ElapsedMilliseconds);
var jitter = new LLVMJitter();
List<MiasmExpr> exprs = new List<MiasmExpr>(lines.Length);
foreach(var line in lines)
{
    /*
    var expr = ExpressionDatabaseParser.ParseExpression(line);
    //exprs.Add(expr);
    //jitter.LiftAst(expr, ExprUtilities.GetUniqueVariables(expr));

    var exprStr = ExpressionFormatter.FormatExpression(expr);
    if(line != exprStr)
    {
        Console.WriteLine("Expected {0}\n", line);
        Console.WriteLine("But got {0}", exprStr);
        Console.WriteLine("");
    }
    */
}

//ExpressionDatabaseParser.ParseExpression(@"ExprOp("" + "", ExprOp("" ^ "", ExprId(""p2"", 8), ExprOp("" + "", ExprOp("" - "", ExprId(""p2"", 8)), ExprInt(2, 8))), ExprInt(2, 8))");

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


Console.WriteLine("Finished.");
Console.ReadLine();
