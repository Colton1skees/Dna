using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Emulation;
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
using Dna.Decompiler;
using Dna.Emulation.Unicorn;
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
using Dna.Utilities;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO;
using static Dna.LLVMInterop.NativePassApi;
using System.Text;
using System.Numerics;
using Iced.Intel;
using Dna.LLVMInterop.API.Remill.Arch;
using System.Net.Http.Headers;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Example;
using System.IO;
using Dna.BinaryTranslator.Unsafe;
using Dna.BinaryTranslator.Safe;
using AsmResolver.PE;
using AsmResolver.PE.Exceptions.X64;
using Dna.SEH;
using RuntimePatches;
using Dna.BinaryTranslator;
using Dna.BinaryTranslator.JmpTables.Slicing;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.BinaryTranslator.JmpTables.Precise;
using Dna.Reconstruction;
using Dna.Passes;

// Regrettably, install some runtime hooks to fix some FFI issues w/ LLVMSharp
//LazyLLVMFixes.InstallModuleToStringBugFix(RemillUtils.LLVMModuleToString);
//LazyLLVMFixes.InstallModuleToFileBugFix();
//LazyLLVMFixes.InstallValueToStringBugFix(RemillUtils.LLVMValueToString);

// TODO: https://github.com/cnr-isti-vclab/meshlab/releases/download/MeshLab-2023.12/MeshLab2023.12-windows.exe
// Lift N functions from MeshLab
bool newPipeline = true;
if (newPipeline)
{
    // Load the meshlab binaries
    var meshLabPath = @"C:\Users\colton\source\repos\MeshLab Binaries\meshlab.exe";
    meshLabPath = @"C:\Users\colton\Downloads\VmTarget\VMTarget.exe";
    meshLabPath = @"C:\Users\colton\source\repos\obfuscateme\x64\Release\obfuscateme.exe";
    var meshLabBin = WindowsBinary.From(meshLabPath);
    var meshLabDna = new Dna.Dna(meshLabBin);

    // Parse all function bounds from the .pdata section
    var allFunctions = FunctionDetector.Run(meshLabBin);

    // Pick out and lift one of the larger functions.
    //ulong sAddr = 0x14002F480; // Well-behaved, very large functions
    //sAddr = 0x1400227C0; // Not well-behaved(floating point), very large function. 
    //ulong sAddr = 0x140001130; // Simple function from vmtarget.exe
    ulong sAddr = 0x140001000;

    // Use our iterative control flow graph exploration algorithm to recover the control flow graph
    //var targetFunc = allFunctions.Single(x => x.StartAddr == sAddr);
    var ourCtx = LLVMContextRef.Global;
    var remillArch = RemillArch.CreateWin64(ourCtx);
    var explored = IterativeFunctionTranslator.Translate(meshLabDna, remillArch, ourCtx, sAddr);

    // Translate the cfg to a human readable representation
    BrighteningTranslator.Run(meshLabDna, remillArch, ourCtx, explored);

    // Then finally recompile the control flow graph and reinsert it into the binary.
    var safeBinaryFunction = SafeFunctionTranslator.Translate(meshLabDna, remillArch, ourCtx, explored);

    FunctionGroupCompiler.Compile(meshLabDna, new List<SafelyTranslatedFunction>() { safeBinaryFunction });
    Debugger.Break();
}

bool idk = true;
if (idk)
{
    /*
var peImage = PEImage.FromFile(vmtPath);
var exceptions = peImage.Exceptions.GetEntries().ToList();
var target = exceptions.Single(x => (ulong)x.Begin.Rva + bin.BaseAddress == 0x140001C70) as X64RuntimeFunction;
*/
    // Load the binary into DNA.
    //var vmtPath = @"C:\Users\colton\Downloads\VMTarget.exe";
    var vmtPath = @"C:\Users\colton\source\repos\Devirtualizer\Devirtualizer\Assets\devirtualizeme64_vmp_3.0.9_v1.bin";
    var bin = WindowsBinary.From(vmtPath);
    var vmpDna = new Dna.Dna(bin);


    ulong addr = 0x14009b17d;
    var remillArch2 = new RemillArch(LLVMContextRef.Global, RemillOsId.kOSWindows, RemillArchId.kArchAMD64_AVX512);
    // 0x140001C60 = function with SEH
    var binaryFunction2 = IterativeFunctionTranslator.Translate(vmpDna, remillArch2, LLVMContextRef.Global, addr);

    //var bytes2 = bin.ReadBytes(0x140001000, 648);
    //File.WriteAllBytes(@"C:\Users\colton\source\repos\CppMbaTest\x64\Release\mba_bytes.txt", bytes2);

    /*
    bin.WriteBytes(0x1400A6981, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69E0, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69E6, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69EC, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69F2, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69F8, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A69FE, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6A04, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6A0A, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6A10, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    bin.WriteBytes(0x1400A6987, new byte[] { 0xC3 });
    */
    ulong fAddr = 0x140003610;
    var peImage = (SerializedPEImage)PEImage.FromFile(vmtPath);
    var exceptions = peImage.Exceptions.GetEntries().ToList();
    var target = exceptions.Single(x => (ulong)x.Begin.Rva + bin.BaseAddress == fAddr) as X64RuntimeFunction;

    var segRef = target.UnwindInfo.ExceptionHandlerData;

    var uwRef = target.UnwindInfo;
    var uwReader = peImage.PEFile.CreateReaderAtRva(uwRef.Rva + 0x4);
    // var unwindInfo = UnwindInfo.FromReader(peImage.ReaderContext, ref uwReader);


    List<byte> toParse = new();
    for (int i = 0; i < (uwRef.UnwindCodes.Length * 2) + 12; i++)
    {
        toParse.Add(bin.ReadBytes(bin.BaseAddress + uwRef.Rva + 0x4 + (ulong)i)[0]);
    }

    //  if (segRef != null && segRef.CanRead)
    if (false)
    {

        var reader = peImage.PEFile.CreateReaderAtRva(segRef.Rva);
        var scopeTable = BinaryScopeTable.FromReader(peImage.ReaderContext, ref reader);

        var tab = "    ";
        Console.WriteLine("");
        Console.WriteLine("Entry: 0x" + (bin.BaseAddress + scopeTable.Rva).ToString("X"));
        foreach (var entry in scopeTable.Entries)
        {
            Console.WriteLine($"{tab} Begin: 0x{(bin.BaseAddress + entry.Begin.Rva).ToString("X")} ");
            Console.WriteLine($"{tab} End: 0x{(bin.BaseAddress + entry.End.Rva).ToString("X")}");
            Console.WriteLine($"{tab} Handler 0x{(bin.BaseAddress + entry.Filter.Rva).ToString("X")}");
            Console.WriteLine($"{tab} Target 0x{(bin.BaseAddress + entry.ExceptionHandler.Rva).ToString("X")} ");
            Console.WriteLine("");
        }


    }


    //  var foobar = vmpDna.RecursiveDescent.ReconstructCfg(fAddr, null, new List<ulong>() { 0x140002DB5 });

    var sc = BinaryScopeTable.TryGetFromFunctionAddress(vmpDna.Binary, fAddr);

    var hierarchy = new ScopeTableTree(sc);

    // Console.WriteLine(foobar);
    //  Console.WriteLine(foobar.GetBlocks().Any(x => x.Address == 0x140002DB5));
    //  Console.WriteLine(BitConverter.IsLittleEndian);
    var bytes = uwRef.UnwindCodes.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
    //var (codes, height) = UnwindCodeParser.Parse(toParse.ToArray(), uwRef.UnwindCodes.Length * 2);
    //UnwindCodeParser.ParseUnwindCode(bin, bin.BaseAddress + uwReader.Rva, uwRef.UnwindCodes.Length * 2, uwRef.Version);
    var uwcAddr = bin.BaseAddress + uwReader.Rva;
    var codes = UnwindCodeParser.ParseUnwindCode(bin, uwcAddr, uwRef.UnwindCodes.Length * 2, uwRef.Version);

    var height = StackHeightCalculator.Get(codes);
    Console.WriteLine($"Stack height: 0x{height.ToString("X")} ");
    //Console.WriteLine(height);
    // Iteratively explore and lift the functiom until no new edges can be discovered.
    var remillArch = new RemillArch(LLVMContextRef.Global, RemillOsId.kOSWindows, RemillArchId.kArchAMD64_AVX512);
    // 0x140001C60 = function with SEH
    var binaryFunction = IterativeFunctionTranslator.Translate(vmpDna, remillArch, LLVMContextRef.Global, fAddr);

    // Then lift the control flow graph to compileable LLVM IR.
    remillArch = new RemillArch(LLVMContextRef.Global, RemillOsId.kOSWindows, RemillArchId.kArchAMD64_AVX512);
    var safelyTranslated = SafeFunctionTranslator.Translate(vmpDna, remillArch, LLVMContextRef.Global, binaryFunction);
    // Lastly compile the function back down to x86 and reinsert it into the binary.
    FunctionGroupCompiler.Compile(vmpDna, new List<SafelyTranslatedFunction>() { safelyTranslated });
    Debugger.Break();
}


bool prototypeBounds = false;
if(prototypeBounds)
{

    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.........................");
    var compiledPath3 = ClangCompiler.Compile("Vectorized.ll");

    Console.WriteLine("Loading into IDA.");
    var exePath3 = IDALoader.Load(compiledPath3);


    Console.WriteLine("");
    var fromFile = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, @"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Debug\net7.0-windows\Vectorized.ll").Value;

    var fromFunc = fromFile.GetFunctions().Single(x => x.Name.Contains("_Z11emulate_andtt"));

    var toSlice = fromFunc.GetInstructions().Single(x => x.ToString().Contains("%3 = shl"));
    var sliceBlk = toSlice.InstructionParent;

    var bld = LLVMBuilderRef.Create(fromFile.Context);
    bld.PositionBefore(toSlice);


    var l = LowerLshr.LowerLshrToLlvm(toSlice, bld);
    toSlice.ReplaceAllUsesWith(l);
    fromFile.PrintToFile("nolshr.ll");


    var loopInfo = new LoopInfo();
    var slicer = new SymbolicExpressionSlicer(sliceBlk.AsValue(), toSlice, loopInfo, null);

    var possiblyBoundedIndex = slicer.GetDefinition(toSlice);
    var constraints = slicer.ComputePathConstraints(sliceBlk);

    var bounds = Z3BoundSolver.GetSolutions(possiblyBoundedIndex, constraints);
    Console.WriteLine(bounds.Count);
    Console.WriteLine(possiblyBoundedIndex);
    Console.WriteLine(constraints.Single());
    Debugger.Break();
}

// new BoundTest().Test(File.ReadAllText("foobar.txt"));
/*
var cmp = ClangCompiler.Compile("perm.ll");
IDALoader.Load(cmp);
Debugger.Break();

bool toBinja = true;
if (toBinja)
{
    var fromFile = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, @"C:\Users\colton\Downloads\OriginalConsoleApplication1.bc");
    Console.WriteLine(fromFile);


    new LLVMToBinjaGraph(fromFile.Value.GetFunctions().First(x => x.Name.Contains("Parameterized_TranslatedFrom140098660"))).Process();
    Debugger.Break();
}
*/

/*
var tempMod = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, @"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Debug\net7.0-windows\cff.ll");

var fpm = new FunctionPassManager();
var pmb = new PassManagerBuilder();
var moduleManager = new PassManager();

// Create a reducible control flow graph.
fpm.Add(ScalarPasses.CreateCFGSimplificationPass());
fpm.Add(PassApi.CreateControlledNodeSplittingPass());
fpm.Add(ScalarPasses.CreateCFGSimplificationPass());

pmb.PopulateFunctionPassManager(fpm);
pmb.PopulateModulePassManager(moduleManager);

fpm.DoInitialization();
fpm.Run(tempMod.Value.GetFunctions().Single(x => x.Name.Contains("ub_5E45")));
fpm.DoFinalization();

tempMod.Value.WriteToLlFile("reducibled_cff.ll");

var cmp = ClangCompiler.Compile("cff.ll");
IDALoader.Load(cmp);
Debugger.Break();

*/

// Optionally compile the LLVM IR to an executable.
/*
bool compile2 = true;
if (compile2)
{
    var llPath2 = @"C:\Users\colton\Downloads\dfdfgfgfdsg";

    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.");
    var compiledPath2 = ClangCompiler.Compile(llPath2);

    Console.WriteLine("Loading into IDA.");
    var exePath2 = IDALoader.Load(compiledPath2);
    Console.WriteLine("Loaded executable into IDA.");
}
*/

bool compile3 = false;
if(compile3)
{
    var llPath2 = @"C:\Users\colton\Downloads\leo_ir.ll";

    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.");
    var compiledPath2 = ClangCompiler.Compile(llPath2);

    Console.WriteLine("Loading into IDA.");
    var exePath2 = IDALoader.Load(compiledPath2);
    Console.WriteLine("Loaded executable into IDA.");

    Debugger.Break();
}



bool peInj = false;
if (peInj)
{
    PEInjectorTest.Test();
    Debugger.Break();
}

// Load the 64 bit PE file.        
// Note: This file is automatically copied to the build directory.
var path = @"C:\Users\colton\source\repos\ClangJumpTables\x64\Release\ClangJumpTables.exe";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

// Instantiate dna.
var dna = new Dna.Dna(binary);

//var groundTruth = new DisassemblyGroundTruth();
//groundTruth.Run();

var sbtCtx = LLVMContextRef.Create();

Console.WriteLine("Translated.. press enter to continue.");
Debugger.Break();
Console.ReadLine();

throw new InvalidOperationException();
var cfg = dna.RecursiveDescent.ReconstructCfg(0x140001027);


var ctx = LLVMContextRef.Create();
Console.WriteLine((int)RemillArchId.kArchAMD64_AVX512);


var bcPath = "C:\\Users\\colton\\Downloads\\remill-17-semantics";
ctx.TryGetBitcodeModule(LlvmUtilities.CreateMemoryBuffer(@"C:\Users\colton\Downloads\remill-17-semantics" + "\\amd64_sleigh.bc"), out LLVMModuleRef theModule, out string msg);


theModule.WriteToLlFile("remillModule.ll");

Console.WriteLine("foobar ");

var arch = new RemillArch(ctx, RemillOsId.kOSWindows, RemillArchId.kArchAMD64);


Console.WriteLine("Loading arch semantics");
var archModule = RemillUtils.LoadArchSemantics(arch, bcPath);
Console.WriteLine("Getting reg name.");
Console.WriteLine(arch.StackPointerRegisterName);
Console.WriteLine("Got reg name");
//Console.ReadLine();

var firstCfgBlock = cfg.GetBlocks().First();
var addBytes = dna.Binary.ReadBytes(firstCfgBlock.EntryInstruction.IP, firstCfgBlock.EntryInstruction.Length);
ulong addAddr = 0;

var rCtx = arch.CreateInitialContext();



var liftedFunction = arch.DeclareLiftedFunction("remill_test", archModule);

arch.InitializeEmptyLiftedFunction(liftedFunction);

var inst = arch.DecodeInstruction(addAddr, addBytes);
//Console.WriteLine(inst);
//Console.WriteLine(inst.Text);

var remillBlock = liftedFunction.AppendBasicBlock("first_remill_block");
inst.Lifter.LiftIntoBlock(inst, remillBlock, false);

RemillUtils.AddTerminatingTailCall(remillBlock, arch.IntrinsicTable.Jump, arch.IntrinsicTable);

RemillOptimizer.OptimizeFunction(arch, liftedFunction);
Console.WriteLine(liftedFunction.PrintToString());

Console.WriteLine("");
var tbg = archModule.GetNamedFunction("_ZN12_GLOBAL__N_13ADDI3RnWImE2RnImLb1EE2InImEEEP6MemoryS8_R5StateT_T0_T1_");
Console.WriteLine(tbg.PrintToString());

var outModule = ctx.CreateModuleWithName("outmodule");
arch.PrepareModuleDataLayout(outModule);
RemillUtils.MoveFunctionIntoModule(liftedFunction, outModule);

outModule.WriteToLlFile("liftedRemill.ll");

Console.WriteLine("done");
Console.ReadLine();

// Parse a (virtualized) control flow graph from the binary.
ulong funcAddr = 0x1400012E4;
 cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);
