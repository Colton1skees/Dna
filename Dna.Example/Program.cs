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

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse a (virtualized) control flow graph from the binary.
ulong funcAddr = 0x14000123C;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);


// The VM entry spans across multiple routines. To avoid disassembling multiple
// control flow graphs, we selectively insert instructions needed to have a
// correct CFG for the entirety of the vm entry.
/*
var target = cfg.GetBlocks().First();
target.Instructions.Insert(0, dna.BinaryDisassembler.GetInstructionAt(0x14000177E));
target.Instructions.Insert(1, dna.BinaryDisassembler.GetInstructionAt(0x140001783));
target.Instructions.Insert(2, dna.BinaryDisassembler.GetInstructionAt(0x140001789));
target.Instructions.Insert(3, dna.BinaryDisassembler.GetInstructionAt(0x14000178F));
target.Instructions.Insert(4, dna.BinaryDisassembler.GetInstructionAt(0x140001794)); 
target.Instructions.Insert(5, dna.BinaryDisassembler.GetInstructionAt(0x140002CA0));
target.Instructions.Insert(6, dna.BinaryDisassembler.GetInstructionAt(0x140002CA5));
target.Instructions.Insert(7, dna.BinaryDisassembler.GetInstructionAt(0x140002CAA));
target.Instructions.Insert(8, dna.BinaryDisassembler.GetInstructionAt(0x140002CAE));
target.Instructions.Insert(9, dna.BinaryDisassembler.GetInstructionAt(0x140002CB2));
target.Instructions.Insert(10, dna.BinaryDisassembler.GetInstructionAt(0x140002CB6));
target.Instructions.Insert(11, dna.BinaryDisassembler.GetInstructionAt(0x140019225));
target.Instructions.Insert(12, dna.BinaryDisassembler.GetInstructionAt(0x14001922A));
*/

// Print the disassembled control flow graph.
var prompt = () =>
{
    Console.WriteLine("Press enter to continue...");
    //Console.ReadLine();
};

Console.WriteLine("Disassembled cfg:\n{0}", GraphFormatter.FormatGraph(cfg));
prompt();

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

// Elminate deadcode from the control flow graph.
bool dce = false;
if (dce)
{
    var blockDcePass = new BlockDcePass(liftedCfg);
    blockDcePass.Run();
}

// Print the optimized control flow graph.
bool printLiftedCfg = true;
if (printLiftedCfg)
{
    Console.WriteLine("Lifted cfg:\n{0}", GraphFormatter.FormatGraph(liftedCfg));
    //prompt();
}

bool writeDotGraph = false;
if (writeDotGraph)
{
    // Create a .DOT file for visualizing the IR cfg.
    var dotGraph = GraphVisualizer.GetDotGraph(liftedCfg);
    File.WriteAllText("graph.dot", dotGraph.Compile(false, false));
}

bool emulate = true;
if (emulate)
{
    // Load the binary into unicorn engine.
    var unicornEmulator = new UnicornEmulator(architecture);
    var symbolicEmulator = new SymbolicEmulator(architecture);

    BinaryMapper.MapPEFile(unicornEmulator, binary);
    BinaryMapper.MapPEFile(symbolicEmulator, binary);

    // Setup the stack.
    ulong rsp = 0x100000000;
    unicornEmulator.MapMemory(rsp, 0x1000 * 1200);
    symbolicEmulator.MapMemory(rsp, 0x1000 * 1200);
    rsp += 0x20000;

    // Setup the segment registers.g
    unicornEmulator.MapMemory(0, 0x1000 * 1000);
    symbolicEmulator.MapMemory(0, 0x1000 * 1000);

    for (ulong i = 0; i < 32; i++)
    {
        ulong baseAddr = 0x14006C45D;
        unicornEmulator.WriteMemory(baseAddr + i, new byte[] { 0x90 });
        symbolicEmulator.WriteMemory(baseAddr + i, new byte[] { 0x90 });
    }

    unicornEmulator.SetRegister(register_e.ID_REG_X86_RSP, rsp);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_RBP, rsp);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_RIP, 0x140001299);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RSP, rsp);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RBP, rsp);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RIP, 0x140001299);

    unicornEmulator.SetRegister(register_e.ID_REG_X86_CF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_CF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_PF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_PF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_AF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_AF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ZF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ZF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_SF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_SF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_TF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_TF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_IF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_IF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_DF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_DF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_OF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_OF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_NT, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_NT, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_AC, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_AC, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_VIF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_VIF, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_VIP, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_VIP, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);

    /*
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    unicornEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    */

    unicornEmulator.SetRegister(register_e.ID_REG_X86_R14, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_R14, 0);

    Dictionary<ulong, byte> unicornMemoryWrites = new Dictionary<ulong, byte>();
    unicornEmulator.SetMemoryWriteCallback((ulong address, int size, ulong value) =>
    {
        var bytes = BitConverter.GetBytes(value);
        for(ulong i = 0; i < (ulong)size; i++)
        {
            unicornMemoryWrites[address + i] = bytes[i];
        }
    });

    unicornEmulator.SetInstExecutedCallback((ulong address, int size) =>
    {
        var symbolicRip = symbolicEmulator.GetRegister(register_e.ID_REG_X86_RIP);
        var unicornRip = unicornEmulator.GetRegister(register_e.ID_REG_X86_RIP);

        var symbolicRsp = symbolicEmulator.GetRegister(register_e.ID_REG_X86_RSP);
        var unicornRsp = unicornEmulator.GetRegister(register_e.ID_REG_X86_RSP);

        //var symbolicRflags = symbolicEmulator.GetRegister(register_e.ID_REG_X86_EFLAGS);
        var unicornRflags = unicornEmulator.GetRegister(register_e.ID_REG_X86_EFLAGS);

        Console.WriteLine($"Symbolic rip: 0x{symbolicRip.ToString("X")}");
        Console.WriteLine($"Unicorn rip: 0x{unicornRip.ToString("X")}");
        Console.WriteLine($"Unicorn rip: 0x{unicornRip.ToString("X")}");
        Console.WriteLine($"Symbolic rsp: 0x{symbolicRsp.ToString("X")}");
        Console.WriteLine($"Unicorn rsp: 0x{unicornRsp.ToString("X")}");
        // Console.WriteLine($"Symbolic rflags: 0x{symbolicRflags.ToString("X")}");
        Console.WriteLine($"Unicorn rflags: 0x{unicornRflags.ToString("X")}");

        var symbolicPf = symbolicEmulator.GetRegister(register_e.ID_REG_X86_PF);
        var unicornPf = unicornEmulator.GetRegister(register_e.ID_REG_X86_PF);
        if (symbolicPf != unicornPf)
        {
            Console.WriteLine("PFs don't match");
            Debugger.Break();
        }

        var symbolicAf = symbolicEmulator.GetRegister(register_e.ID_REG_X86_AF);
        var unicornAf = unicornEmulator.GetRegister(register_e.ID_REG_X86_AF);
        var unicornFlags = unicornEmulator.Emulator.Registers.EFLAGS;
        if (symbolicAf != unicornAf)
        {
            Console.WriteLine("AFs don't match");
            Debugger.Break();
        }

        foreach (var unicornMemAddr in unicornMemoryWrites)
        {
            var symbolicMemValue = symbolicEmulator.ReadMemory(unicornMemAddr.Key, 1)[0];
            if (symbolicMemValue != unicornMemAddr.Value)
            {
                throw new InvalidOperationException("Unicorn memory mapping does not match.");
            }
        }

        if (symbolicRip != unicornRip)
        {
            Debugger.Break();
        }

        unicornMemoryWrites.Clear();
        symbolicEmulator.ExecuteNext();
    });

    // Execute the function.
    unicornEmulator.Start(0x140001299);
    symbolicEmulator.Start(0x140001299);

    Console.WriteLine("Started");
    Thread.Sleep(100000);
}

// Lift the control flow graph to LLVM IR.
var llvmLifter = new LLVMLifter(architecture);
llvmLifter.Lift(liftedCfg);
llvmLifter.Module.PrintToFile(@"lifted.ll");

bool optimize = true;
if (optimize)
{
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
}


llvmLifter.Module.PrintToFile(@"optimized.ll");

// Optionally write the llvm IR to the console.
bool printLLVM = false;
if (printLLVM)
    llvmLifter.Module.Dump();
prompt();

// Optionally decompile the lifted function to go-to free pseudo C, via Rellic.
// On my machine, a fork of Rellic runs under WSL2 and communiucates via gRPC.
// If you are not hosting this server at localhost:50051, then the API
// call will fail. You can find the service here(https://github.com/Colton1skees/rellic-api),
// although it will take a bit of leg work for outside use.
bool decompile = false;
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
