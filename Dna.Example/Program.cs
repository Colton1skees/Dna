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
using Dna.BinaryTranslator.VMProtect;
using Dna.Passes.Mba;
using Mba.Simplifier.DSL;
using Dna.BinaryTranslator.VMProtect.Rewrite;


bool genDsl = false;
if (genDsl)
{
    var dsl = DslParser.ParseDsl(File.ReadAllText("C:\\Users\\colton\\source\\repos\\dna-build-refactor\\Dna\\Simplifier\\Mba.Simplifier\\DSL\\simplification.rules"));
    var backend = new IsleBackend(dsl);
    backend.Generate();
    Debugger.Break();
}

// Regrettably, install some runtime hooks to fix some FFI issues w/ LLVMSharp
//LazyLLVMFixes.InstallModuleToStringBugFix(RemillUtils.LLVMModuleToString);
//LazyLLVMFixes.InstallModuleToFileBugFix();
//LazyLLVMFixes.InstallValueToStringBugFix(RemillUtils.LLVMValueToString);


bool dbgCode = false;
if (dbgCode)
{
    var irPath = "C:\\Users\\user\\Downloads\\debugme7.ll";
    var t = File.ReadAllText(irPath);
    Console.WriteLine(irPath);



    var tempNewMod = RemillUtils.LoadModuleFromFile(LLVMContextRef.Global, irPath).Value;


    /*
    var name3 = Guid.NewGuid().ToString() + ".ll";
    tempNewMod.PrintToFile((name3));


    var compiledPath33 = ClangCompiler.Compile(name3);

    Console.WriteLine("Loading into IDA.   ");
    var exePath33 = IDALoader.Load(compiledPath33, true);
    */

    var existingFunc = tempNewMod.GetFunctions().Single(x => x.Name.Contains("Part"));


    while (true)
    {
        var sw = Stopwatch.StartNew();
        unsafe
        {
            //MbaDeobfuscationPass.Run(existingFunc);
            //MultiUseCloningPass.Run(existingFunc);
            //existingFunc.GlobalParent.PrintToFile("instcombine.ll");
            if (false)
            {
                new AdhocInstCombinePass().InstCombine((LLVMOpaqueValue*)existingFunc.Handle, 0, 0, 0);
                MultiUseCloningPass.Run(existingFunc);
                MbaDeobfuscationPass.Run(existingFunc);
                var bar = new CombinedFixedpointOptPass(null, new FixedpointPassConfig());
            }

        } 

        // Note: Need to update this path on different samples
        var vmpPath = @"C:\Users\user\Downloads\PRIV_BINARIES\vmp3_private_tlb.vmp.exe";
        var vmpBin = WindowsBinary.From(vmpPath);
        var vmpDna = new Dna.Dna(vmpBin);


        //MbaDeobfuscationPass.Run(existingFunc);
        tempNewMod.PrintToFile(("translatedFunction.ll"));
        while (true)
        {
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                PassPipeline.Run(vmpBin, existingFunc, false, true, false);
                //PassPipeline.Run(vmpBin, existingFunc, false, false, true);
            }
            sw2.Stop();
            tempNewMod.PrintToFile(("translatedFunction.ll"));
            Console.WriteLine($"Fast optimization took {sw2.ElapsedMilliseconds}ms");
            break;
        }
        tempNewMod.PrintToFile(("translatedFunction.ll"));
        unsafe
        {
            //new AdhocInstCombinePass().InstCombine((LLVMOpaqueValue*)existingFunc.Handle, 0, 0, 0);
            //new AdhocInstCombinePass().InstCombine((LLVMOpaqueValue*)existingFunc.Handle, 0, 0, 0);
        }
        //MbaDeobfuscationPass.Run(existingFunc);
        tempNewMod.PrintToFile(("translatedFunction.ll"));
        PassPipeline.Run(vmpBin, existingFunc, false, false, fastPipeline: false);
        //  PassPipeline.Run(vmpBin, existingFunc);

        tempNewMod.PrintToFile("instcombine.ll");

        tempNewMod.PrintToFile(("translatedFunction.ll"));


        var name = Guid.NewGuid().ToString() + ".ll";
        tempNewMod.PrintToFile((name));


        var compiledPath3 = ClangCompiler.Compile(name);

        Console.WriteLine("Loading into IDA.    ");
        var exePath3 = IDALoader.Load(compiledPath3, true);



        //IDALoader.Load(ClangCompiler.Compile("instcombine.ll"));

        //File.WriteAllText("binja.py", new LLVMToBinjaGraph(existingFunc).Process());

        sw.Stop();
            Console.WriteLine($"Pass took {sw.ElapsedMilliseconds}ms ");
        Debugger.Break();
    }
    Debugger.Break();

    //PassPipeline.Run(bin, existingFunc);

}



bool useVmp = true;
if (useVmp)
{
    var (vmpPath, vmpAddr) = ("", 0ul);

    vmpPath = @"C:\Users\colton\Downloads\DNA Assets\vmptest.vmp.bin";
    vmpAddr = 0x140001030;


    vmpPath = @"C:\Users\user\Downloads\PRIV_BINARIES\vmp3_private_tlb.vmp.exe";
    vmpAddr = 0x1400032FA; // x+y
    vmpAddr = 0x14000335E; // 3 if/else statements with additions


    // simple_loop_64
    vmpAddr = 0x140003710;

    // complex_loop_32
    vmpAddr = 0x1400037AF;

    var vmpBin = WindowsBinary.From(vmpPath);
    var vmpDna = new Dna.Dna(vmpBin);

    var allHandlerAddrs = new List<ulong>() { 0x14000335E, 0x1400918C9, 0x1400EB2CC, 0x14008BD17, 0x1400CBB6C, 0x1400D6D9B, 0x140109594, 0x14007D4F7, 0x1400B92CF, 0x1400D7934, 0x1400F288F, 0x14007A819, 0x1400D8AF3, 0x1400319CA, 0x14008F5C1, 0x140079FE0, 0x140126495, 0x140110996, 0x14009D63A, 0x1400F0AC6, 0x1400C2674, 0x14005F617, 0x1400DED2C, 0x1400AAAE8, 0x1400DB568, 0x14009062F, 0x14003B347, 0x140067E40, 0x14012639A, 0x1400C0E70, 0x14010A566, 0x140066F56, 0x140125C13, 0x140050B5C, 0x14006D6B9, 0x14006B409, 0x14010AB93, 0x1400C137A, 0x140035E58, 0x14011B054, 0x140078A9A, 0x1400BD105, 0x1400B00A4, 0x1400BBD0B, 0x14006E0D6, 0x1400DA2DD, 0x140098200, 0x14003620C, 0x14003BA80, 0x14007E116, 0x1401220FE, 0x140108E41, 0x1400AC4BD, 0x140085505, 0x14011633F, 0x1400CF053, 0x14007A474, 0x1400F532B, 0x1400D4812, 0x14004DB4A, 0x1400AA6DB, 0x1400638D1, 0x140112442, 0x1400A0F94, 0x1400E3C7D, 0x1400932C6, 0x140115A63, 0x14012A10F, 0x14010018C, 0x14007F2BD, 0x1400B9DFA, 0x1400E7ECB, 0x140114BD3, 0x14005242F, 0x14008AE68, 0x14003E0D8, 0x14011551C, 0x1400A0983, 0x14009F36C, 0x1400ABC43, 0x140103030, 0x140080359, 0x14006EA6A, 0x14012A176, 0x14004F01B, 0x1400C6510, 0x140070125, 0x1400FFE84, 0x14011B178, 0x1400CC8CE, 0x140081BBA, 0x14008B144, 0x14002EDDC, 0x140092877, 0x140093527, 0x1400B39BC, 0x1400E86CD, 0x1400BD9A3, 0x1400946D1, 0x14011F02C, 0x14002E9E9, 0x140028545, 0x14005AE26, 0x14006B5F9, 0x1400EAD02, 0x1401249BE, 0x1400BA24C, 0x1400C58E7, 0x140110A85, 0x1400FA1D6, 0x1400CA402, 0x140067636, 0x1400E65CE, 0x1400DB137, 0x14002E347, 0x140083A6E, 0x140096E7B, 0x1400CE274, 0x140122264, 0x140037B8E, 0x140121F62, 0x1400D7A9D, 0x14003F4E8, 0x14006EF03, 0x140095E07, 0x1400AE425, 0x1400E243C, 0x1400C2E51, 0x14006EB29, 0x1400E88D4, 0x140091491, 0x140031840, 0x1400A4944, 0x1400824A8, 0x140118B14, 0x140112C7B, 0x14010ECBC, 0x1400E19BB, 0x1401083F7, 0x1400743D2, 0x14009CFFF, 0x1400751A8, 0x140031763, 0x140050BB3, 0x14003E48F, 0x14006AA7C, 0x140094621, 0x14007AB8F, 0x140049127, 0x1400CCBDF, 0x140060552, 0x1400862F8, 0x14008E2A9, 0x140037383, 0x140068099, 0x1400706F6, 0x14005091A, 0x1401210B1, 0x1400D9071, 0x140048EC0, 0x14012C40B, 0x1400B3A8D, 0x140076F7D, 0x14005195C, 0x1400C37E5, 0x14010220A, 0x1400855C1, 0x1400BBA61, 0x140082648, 0x140086945, 0x14006B4BE, 0x1400879B3, 0x14006D3FA, 0x140118CA8, 0x140129275, 0x14002FA8F, 0x14002FB68, 0x140121838, 0x140092CBF, 0x14003B2A0, 0x14003A02A, 0x1400CAF6A, 0x140039B4E, 0x14012B48E, 0x1400AFCE2, 0x140048BBD, 0x14006A73D, 0x14007A05B, 0x1400C48EA, 0x140115F45, 0x140116C4D, 0x14009C8C8, 0x1400FBEA6, 0x1400E3AF2, 0x1401113BE, 0x14009695E, 0x140083FC9, 0x140090F0D };

    var allInsts = allHandlerAddrs.SelectMany(x => HandlerLifter.DisHandler(vmpDna, x).GetInstructions()).DistinctBy(x => x.ToString());

    allInsts = allInsts.Where(x => x.Mnemonic.ToString() == "Pop");

   

    foreach(var al in allInsts)
    {
        Console.WriteLine($"0x{al.IP.ToString("X")} ");
    }


    var vmpCtx = LLVMContextRef.Global;
    var vmpArch = new RemillArch(vmpCtx, RemillOsId.kOSLinux, RemillArchId.kArchAMD64_AVX512);
    //var translator = new IterativeVmpTranslator(vmpDna, vmpArch, vmpCtx, vmpAddr);
    var translator = new IterativeVmpExplorer(vmpDna, vmpArch, vmpCtx, vmpAddr);
    var sw = Stopwatch.StartNew();
    var devirtedFunc = translator.Run();
    sw.Stop();

    Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms ");
    Debugger.Break();
    VmpContextRemovalPass.Run(devirtedFunc);

    


}

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
            Console.WriteLine($"{tab} Target 0x{(bin.BaseAddress + entry.ExceptionHandler.Rva).ToString("X")}  ");
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
    Console.WriteLine($"Stack height: 0x{height.ToString("X")}  ");
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
    Console.WriteLine("Compiling to an exe........................ .");
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
    fromFile.PrintToFile(ArtifactPaths.Resolve("nolshr.ll"));


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


var bcPath = RemillArch.GetDefaultSemanticsSearchPath();
ctx.TryGetBitcodeModule(LlvmUtilities.CreateMemoryBuffer(Path.Combine(bcPath, "amd64_sleigh.bc")), out LLVMModuleRef theModule, out string msg);


theModule.WriteToLlFile("remillModule.ll");

Console.WriteLine("foobar ");

var arch = new RemillArch(ctx, RemillOsId.kOSWindows, RemillArchId.kArchAMD64);


Console.WriteLine("Loading arch semantics");
var archModule = arch.GetOrLoadSemantics(bcPath);
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
