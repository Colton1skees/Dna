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
using Dna.LLVMInterop.Passes;
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

// Regrettably, install some runtime hooks to fix some FFI issues w/ LLVMSharp
LazyLLVMFixes.InstallModuleToStringBugFix(RemillUtils.LLVMModuleToString);
LazyLLVMFixes.InstallModuleToFileBugFix();
LazyLLVMFixes.InstallValueToStringBugFix(RemillUtils.LLVMValueToString);

bool idk = true;
if (idk)
{
    /*
var peImage = PEImage.FromFile(vmtPath);
var exceptions = peImage.Exceptions.GetEntries().ToList();
var target = exceptions.Single(x => (ulong)x.Begin.Rva + bin.BaseAddress == 0x140001C70) as X64RuntimeFunction;
*/

    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.");
    var compiledPath33 = ClangCompiler.Compile("canvm.ll");

    Console.WriteLine("Loading into IDA.");
    var exePath33 = IDALoader.Load(compiledPath33);

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
            Console.WriteLine($"{tab} Begin: 0x{(bin.BaseAddress + entry.Begin.Rva).ToString("X")}");
            Console.WriteLine($"{tab} End: 0x{(bin.BaseAddress + entry.End.Rva).ToString("X")}");
            Console.WriteLine($"{tab} Handler 0x{(bin.BaseAddress + entry.Filter.Rva).ToString("X")}");
            Console.WriteLine($"{tab} Target 0x{(bin.BaseAddress + entry.ExceptionHandler.Rva).ToString("X")}");
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
    Console.WriteLine($"Stack height: 0x{height.ToString("X")}");
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
    Console.WriteLine("Compiling to an exe.");
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

Console.WriteLine("foobar");

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

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

var llvmLifter = new LLVMLifter(architecture);

llvmLifter.Lift(liftedCfg);

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

// Create a function for reading bytes at an arbitrary rva.
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

// Create an unmanaged function pointer for the read memory function.
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

// Run the O3 pipeline one last time with custom alias analysis.
PointerClassifier.Seen.Clear();
PointerClassifier.print = true;
for (int i = 0; i < 10; i++)
{
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false, false);
}


llvmLifter.Module.WriteToLlFile(llPath);

// Marshal LLVM's function CFG to Dna's control flow graph structure.
ControlFlowGraph<LLVMValueRef> llvmGraph = new ControlFlowGraph<LLVMValueRef>(0);
foreach (var llvmBlock in llvmLifter.llvmFunction.BasicBlocks)
{
    // Allocate a new block.
    var blk = llvmGraph.CreateBlock((ulong)llvmBlock.Handle);

    // Copy the instructions.
    blk.Instructions.AddRange(llvmBlock.GetInstructions());
}

// Update block edges.
foreach (var block in llvmGraph.GetBlocks())
{
    var exitInstruction = block.ExitInstruction;

    var operands = exitInstruction.GetOperands().ToList();
    foreach (var operand in operands)
    {
        if (operand.Kind != LLVMValueKind.LLVMBasicBlockValueKind)
            continue;

        var outgoingBlk = llvmGraph.GetBlocks().Single(x => x.Address == (ulong)operand.Handle);
        block.AddOutgoingEdge(new BlockEdge<LLVMValueRef>(block, outgoingBlk));
    }
}

// Print the llvm CFG to a .dot file.
var dotGraph = GraphVisualizer.GetDotGraph(llvmGraph);
var dot = dotGraph.Compile();
File.WriteAllText("llvmGraph.dot", dot);

var fpm2 = new FunctionPassManager();
var pmb2 = new PassManagerBuilder();
var moduleManager2 = new PassManager();

// Create a reducible control flow graph.
fpm2.Add(ScalarPasses.CreateCFGSimplificationPass());
fpm2.Add(PassApi.CreateControlledNodeSplittingPass());
fpm2.Add(ScalarPasses.CreateCFGSimplificationPass());
fpm2.Add(PassApi.CreateUnSwitchPass());
fpm2.Add(ScalarPasses.CreateLoopSimplifyCFGPass());
fpm2.Add(PassApi.CreateLoopExitEnumerationPass());
fpm2.Add(PassApi.CreateUnSwitchPass());

// Structure the CFG.
var cfPass = new ControlFlowStructuringPass();
var nativeCfPass = PassApi.CreateControlFlowStructuringPass(cfPass.PtrStructureFunction);
fpm2.Add(nativeCfPass);
pmb2.PopulateFunctionPassManager(fpm2);
pmb2.PopulateModulePassManager(moduleManager2);

fpm2.DoInitialization();
fpm2.Run(llvmLifter.llvmFunction);
fpm2.DoFinalization();

// Optionally compile the LLVM IR to an executable.
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