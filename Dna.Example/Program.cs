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
using static Dna.LLVMInterop.NativeLLVMInterop;
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


var llLines = File.ReadAllLines(@"C:\Users\colton\Downloads\prototyping\metadata_input.ll").ToList();
var sanitizedLines = MetadataRemover.RemoveMetadata(llLines);
File.WriteAllLines(@"C:\Users\colton\Downloads\prototyping\sanitized_output.ll", sanitizedLines);
Console.WriteLine("Sanitized IL.");

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

var inst = architecture.Disassembly(dna.BinaryDisassembler.GetInstructionAt(0x140015410));

var translator = new X86Translator(architecture);
var astConverter = new AstToIntermediateConverter(architecture);

var translated = translator.TranslateInstruction(inst);
var flatInstructions = translated.SelectMany(x => astConverter.ConvertFromSymbolicExpression(x));

/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-earlycse-mssa-optimization-cap=1000000",
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

LlvmUtilities.LLVMParseCommandLineOptions(new string[] {
    "test",
    "-memdep-block-scan-limit=10000000",
    });
foreach (var flatInst in flatInstructions)
{
    Console.WriteLine(flatInst);
}

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
bool printLiftedCfg = false;
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


List<Iced.Intel.Instruction> icedInstructions = new();

bool emulate = false;
if (emulate)
{
    // Load the binary into unicorn engine.
    var unicornEmulator2 = new UnicornEmulator(architecture);
    var symbolicEmulator2 = new SymbolicEmulator(architecture);

    BinaryMapper.MapPEFile(unicornEmulator2, binary);
    BinaryMapper.MapPEFile(symbolicEmulator2, binary);

    // Setup the stack.
    ulong rsp = 0x100000000;
    unicornEmulator2.MapMemory(rsp, 0x1000 * 1200);
    symbolicEmulator2.MapMemory(rsp, 0x1000 * 1200);
    rsp += 0x20000;

    // Setup the segment registers.g
    unicornEmulator2.MapMemory(0, 0x1000 * 1000);
    symbolicEmulator2.MapMemory(0, 0x1000 * 1000);

    for (ulong i = 0; i < 32; i++)
    {
        ulong baseAddr = 0x14006C45D;
        unicornEmulator2.WriteMemory(baseAddr + i, new byte[] { 0x90 });
        symbolicEmulator2.WriteMemory(baseAddr + i, new byte[] { 0x90 });
    }

    for (ulong i = 0; i < 0x1000; i++)
    {
        ulong baseAddr = 0;
        unicornEmulator2.WriteMemory(baseAddr + i, new byte[] { 0x0 });
        symbolicEmulator2.WriteMemory(baseAddr + i, new byte[] { 0x0 });
    }

    unicornEmulator2.SetRegister(register_e.ID_REG_X86_RSP, rsp);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_RBP, rsp);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_RIP, 0x140001299);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_RSP, rsp);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_RBP, rsp);

    // Update low parts of rbp.
    var casted = (uint)rsp;
    var casted2 = (ushort)rsp;
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_RBP, rsp);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_EBP, casted);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_BP, casted2);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_BPL, (byte)casted);

    // Update low parts of RSP.
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_ESP, casted);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_SP, casted2);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_SPL, (byte)casted);


    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_RIP, 0x140001299);

    unicornEmulator2.SetRegister(register_e.ID_REG_X86_CF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_CF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_PF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_PF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_AF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_AF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_ZF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_ZF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_SF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_SF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_TF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_TF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_IF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_IF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_DF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_DF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_OF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_OF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_NT, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_NT, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_AC, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_AC, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_VIF, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_VIF, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_VIP, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_VIP, 0);
    unicornEmulator2.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_ID, 0);

    unicornEmulator2.SetRegister(register_e.ID_REG_X86_R14, 0);
    symbolicEmulator2.SetRegister(register_e.ID_REG_X86_R14, 0);

    symbolicEmulator2.WriteMemory<ulong>(0x10001FFE0, 0);

    foreach (var register in X86Registers.RegisterMapping.Values)
    {
        var parent = register.ParentId;
        if (parent == register_e.ID_REG_X86_RIP || parent == register_e.ID_REG_X86_RSP || parent == register_e.ID_REG_X86_RCX
            || parent == register_e.ID_REG_X86_RDX || parent == register_e.ID_REG_X86_RBP || parent == register_e.ID_REG_INVALID
            || parent == register_e.ID_REG_X86_MXCSR)
            continue;

        if (X86Registers.RegisterMapping[parent].BitSize != 64)
            continue;

        Console.WriteLine($"Setting register: {register.Id}");
        symbolicEmulator2.SetRegister(register.Id, 0);
        unicornEmulator2.SetRegister(register.Id, 0);
    }

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

    Dictionary<ulong, byte> unicornMemoryWrites = new Dictionary<ulong, byte>();
    Dictionary<ulong, byte> unicornMemoryReads = new Dictionary<ulong, byte>();
    unicornEmulator2.SetMemoryWriteCallback((ulong address, int size, ulong value) =>
    {
        var bytes = BitConverter.GetBytes(value);
        for (ulong i = 0; i < (ulong)size; i++)
        {
            unicornMemoryWrites[address + i] = bytes[i];
        }
    });

    unicornEmulator2.SetMemoryReadCallback((ulong address, int size, ulong value) =>
    {
        var bytes = BitConverter.GetBytes(value);
        for (ulong i = 0; i < (ulong)size; i++)
        {
            unicornMemoryReads[address + i] = bytes[i];
        }
    });

    int count = 0;
    unicornEmulator2.SetInstExecutedCallback((ulong address, int size) =>
    {
        Console.WriteLine($"count: {count}");
        count++;
        var symbolicRip = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_RIP);
        var unicornRip = unicornEmulator2.GetRegister(register_e.ID_REG_X86_RIP);
        if (symbolicRip == 0x140015A94)
            Debugger.Break();

        var symbolicBytes = symbolicEmulator2.ReadMemory(unicornRip, 16);
        var uniBytes = unicornEmulator2.ReadMemory(symbolicRip, 16);

        var symbolicDisassembly = dna.BinaryDisassembler.GetInstructionFromBytes(symbolicRip, symbolicBytes);
        var uniDisassembly = dna.BinaryDisassembler.GetInstructionFromBytes(unicornRip, uniBytes);
        Console.WriteLine($"Symbolic inst: {symbolicDisassembly}");
        Console.WriteLine($"Uni inst: {uniDisassembly}");

        if(address == 0x14000B38C)
        {
            Debugger.Break();
        }

        if(address == 0x140015C47)
        {
            Debugger.Break();
        }

        var disassembled = dna.BinaryDisassembler.GetInstructionAt(symbolicRip);
        icedInstructions.Add(disassembled);
        if (unicornRip == 0x1400012A8)
        {
            var uniCfg = new ControlFlowGraph<Iced.Intel.Instruction>(0x140001299);
            var entryBlock = uniCfg.CreateBlock(0x140001299);
            foreach (var insn in icedInstructions)
                entryBlock.Instructions.Add(insn);


            var myIrCfg = cfgLifter.LiftCfg(uniCfg);
            var targetBlk = myIrCfg.GetBlocks().First();
            var clone = targetBlk.Instructions.ToList();
            targetBlk.Instructions.Clear();
            foreach (var irInst in clone)
            {
                if (irInst is InstJmp || irInst is InstJcc || irInst is InstJmpInd || irInst is InstRet)
                    continue;

                targetBlk.Instructions.Add(irInst);
            }

            targetBlk.Instructions.Add(new InstRet());

            // Lift the control flow graph to LLVM IR.
            var uniLlvmLifter = new LLVMLifter(architecture);
            uniLlvmLifter.Lift(myIrCfg);

            uniLlvmLifter.Module.PrintToFile(@"unicorn.ll");
            var passManager = uniLlvmLifter.Module.CreateFunctionPassManager();
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
                passManager.RunFunctionPassManager(uniLlvmLifter.llvmFunction);
            }

            passManager.FinalizeFunctionPassManager();

            uniLlvmLifter.Module.PrintToFile(@"liftedUnicornOptimized.ll");
            Console.WriteLine("Done...");
            Debugger.Break();
        }

        var unicornRax = unicornEmulator2.GetRegister(register_e.ID_REG_X86_RAX);

        if (symbolicRip == 0x14004485a)
        {
            var mem = unicornEmulator2.ReadMemory<ulong>(0x10001FFE0);
            Console.WriteLine($"unicorn rax: 0x{unicornRax.ToString("X")}");
            Console.WriteLine($"unicorn mem: 0x{mem.ToString("X")}");
            Debugger.Break();
        }

        var symbolicCf = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_CF);
        var unicornCf = unicornEmulator2.GetRegister(register_e.ID_REG_X86_CF);

        if (symbolicRip == 0x140015410)
        {
            var symbolicEsi = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_ESI);
            var unicornEsi = unicornEmulator2.GetRegister(register_e.ID_REG_X86_ESI);

            Debugger.Break();
        }

        var symbolicRsp = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_RSP);
        var unicornRsp = unicornEmulator2.GetRegister(register_e.ID_REG_X86_RSP);

        var symbolicRax = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_RAX);

        //var symbolicRflags = symbolicEmulator.GetRegister(register_e.ID_REG_X86_EFLAGS);
        var unicornRflags = unicornEmulator2.GetRegister(register_e.ID_REG_X86_EFLAGS);

        Console.WriteLine($"Symbolic rip: 0x{symbolicRip.ToString("X")}");
        Console.WriteLine($"Unicorn rip: 0x{unicornRip.ToString("X")}");

        Console.WriteLine($"Symbolic rax: 0x{symbolicRax.ToString("X")}");
        Console.WriteLine($"Unicorn rax: 0x{unicornRax.ToString("X")}");

        Console.WriteLine($"Symbolic rsp: 0x{symbolicRsp.ToString("X")}");
        Console.WriteLine($"Unicorn rsp: 0x{unicornRsp.ToString("X")}");
        // Console.WriteLine($"Symbolic rflags: 0x{symbolicRflags.ToString("X")}");
        Console.WriteLine($"Unicorn rflags: 0x{unicornRflags.ToString("X")}");

        var symbolicPf = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_PF);
        var unicornPf = unicornEmulator2.GetRegister(register_e.ID_REG_X86_PF);
        if (symbolicPf != unicornPf)
        {
            Console.WriteLine("PFs don't match");
            Debugger.Break();
        }


        var symbolicZf = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_ZF);
        var unicornZf = unicornEmulator2.GetRegister(register_e.ID_REG_X86_ZF);
        if (symbolicZf != unicornZf)
        {
            Console.WriteLine("ZFs don't match.");
            Debugger.Break();
        }

        if (symbolicCf != unicornCf)
        {
            Console.WriteLine("CFs don't match.");
            Debugger.Break();
        }

        var symbolicR12 = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_R12B);
        var unicornR12 = unicornEmulator2.GetRegister(register_e.ID_REG_X86_R12B);

        var symbolicR11 = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_R11);
        var unicornR11 = unicornEmulator2.GetRegister(register_e.ID_REG_X86_R11);
        if (symbolicR12 != unicornR12)
        {
            Console.WriteLine("R12Bs don't match.");
            Debugger.Break();
        }

        var symbolicAf = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_AF);
        var unicornAf = unicornEmulator2.GetRegister(register_e.ID_REG_X86_AF);
        var unicornFlags = unicornEmulator2.Emulator.Registers.EFLAGS;

        if (symbolicRip != unicornRip)
        {
            Debugger.Break();
        }

        if (symbolicAf != unicornAf)
        {
            Console.WriteLine("AFs don't match");
            Debugger.Break();
        }

        if (unicornRip != symbolicRip)
        {
            Console.WriteLine("RIPs don't match.");
            Debugger.Break();
        }

        var symbolicR14 = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_R14);
        var unicornR14 = unicornEmulator2.GetRegister(register_e.ID_REG_X86_R14);
        foreach (var register in X86Registers.RegisterMapping.Values)
        {
            var parent = register.ParentId;
            if (parent == register_e.ID_REG_X86_RIP || parent == register_e.ID_REG_X86_RCX
                || parent == register_e.ID_REG_X86_RDX || parent == register_e.ID_REG_INVALID
                || parent == register_e.ID_REG_X86_MXCSR)
                continue;

            // Ignore eflags since we don't treat eflags in the same manner as unicorn engine.
            if (register.Id == register_e.ID_REG_X86_EFLAGS)
                continue;

            if (X86Registers.RegisterMapping[parent].BitSize != 64)
                continue;

            //Console.WriteLine($"Setting register: {register.Id}");
            var symReg = symbolicEmulator2.GetRegister(register.Id);
            var uReg = unicornEmulator2.GetRegister(register.Id);

            if (symReg != uReg)
            {
                Console.WriteLine($"Unicorn value: 0x{uReg.ToString("X")}\n Sym value: {symReg.ToString("X")}");
                Console.WriteLine($"Unicorn and symbolic values don't match for reg {register.Id}");
                Debugger.Break();
            }
        }

        foreach (var unicornMemAddr in unicornMemoryWrites)
        {
            var symbolicMemValue = symbolicEmulator2.ReadMemory(unicornMemAddr.Key, 1)[0];
            if (symbolicMemValue != unicornMemAddr.Value)
            {
                //throw new InvalidOperationException("Unicorn memory mapping does not match.");
                Console.WriteLine("Written symbolic memory does not match.");
                Debugger.Break();
            }
        }

        foreach (var unicornMemAddr in unicornMemoryReads)
        {
            var symbolicMemValue = symbolicEmulator2.ReadMemory(unicornMemAddr.Key, 1)[0];
            if (symbolicMemValue != unicornMemAddr.Value && !unicornMemoryWrites.ContainsKey(unicornMemAddr.Key))
            {
                //throw new InvalidOperationException("Unicorn memory mapping does not match.");
                Console.WriteLine("Read symbolic memory does not match.");
                Debugger.Break();
            }
        }

        unicornMemoryWrites.Clear();
        unicornMemoryReads.Clear();
        symbolicEmulator2.ExecuteNext();



        symbolicAf = symbolicEmulator2.GetRegister(register_e.ID_REG_X86_AF);
        unicornAf = unicornEmulator2.GetRegister(register_e.ID_REG_X86_AF);
        unicornFlags = unicornEmulator2.Emulator.Registers.EFLAGS;
        if (symbolicAf != unicornAf)
        {
            Console.WriteLine("AFs don't match");
            // Debugger.Break();
        }

        Console.WriteLine("");
    });

    // Execute the function.
    unicornEmulator2.Start(0x140001299);
    symbolicEmulator2.Start(0x140001299);

    Console.WriteLine("Started");
    Thread.Sleep(100000);
}

// Lift the control flow graph to LLVM IR.
var llvmLifter = new LLVMLifter(architecture);
llvmLifter.Lift(liftedCfg);
//llvmLifter.Module.PrintToFile(@"lifted_cfg.ll");


bool optimize = true;
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
    for (int i = 0; i < 10; i++)
    {
        passManager2.RunFunctionPassManager(llvmLifter.llvmFunction);
    }

    passManager2.FinalizeFunctionPassManager();
}


llvmLifter.Module.PrintToFile(@"lifted_cfg_optimized.ll");

LlvmUtilities.LLVMParseCommandLineOptions(new string[] {
    "test",
    "-memdep-block-scan-limit=1000000000",
    });
var ctx = LLVMContextRef.Create();


LlvmUtilities.LLVMParseCommandLineOptions(new string[] {
    "test",
    "-memdep-block-scan-limit=1000000000",
    });
/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-earlycse-mssa-optimization-cap=1000000",
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
var memBuffer = LlvmUtilities.CreateMemoryBuffer(@"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Debug\net7.0\unicorn_alias_analysis3.ll");
ctx.TryParseIR(memBuffer, out LLVMModuleRef unicornTraceModule, out string unicornLoadMsg);


unicornTraceModule = llvmLifter.Module;
/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-earlycse-mssa-optimization-cap=1000000",
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

LlvmUtilities.LLVMParseCommandLineOptions(new string[] {
    "test",
    "-memdep-block-scan-limit=100000000",
    });
// Optionally write the llvm IR to the console.
bool printLLVM = false;
if (printLLVM)
    unicornTraceModule.Dump();

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

var ptr = Marshal.GetFunctionPointerForDelegate(new dgReadBinaryContents(readBytes));

/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-earlycse-mssa-optimization-cap=1000000",
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
for (int i = 0; i < 1; i++)
{

    LLVMInteropApi.Test(unicornTraceModule, ptr);
    LlvmUtilities.LLVMParseCommandLineOptions(new string[] {
    "test",
    "-memdep-block-scan-limit=10000000",
    });
}

Debugger.Break();

Console.WriteLine("Done.");
//unicornTraceModule.Dump();

unicornTraceModule.PrintToFile(@"optimized_vm_entry.ll");
Debugger.Break();


var llvmFunc = unicornTraceModule.FirstFunction;
var blk = llvmFunc.FirstBasicBlock;

var llvmToIr = new LLVMInstToIR(unicornTraceModule, architecture);
var nextInst = blk.FirstInstruction;
while (true)
{
    if (nextInst == null)
        break;
    llvmToIr.LowerInstruction(nextInst);
    nextInst = nextInst.NextInstruction;
}


Console.WriteLine("Finished translation to llvm IR.");
var themidaCfg = new ControlFlowGraph<AbstractInst>(0x14000123C);
var themidaBlock = themidaCfg.CreateBlock(0x14000123C);
themidaBlock.Instructions.AddRange(llvmToIr.Output);


//BlockSsaConstructor.ConstructSsa(themidaBlock);

Console.WriteLine($"Lifted cfg: {GraphFormatter.FormatGraph(themidaCfg)}");
Console.WriteLine("Foobar.");

bool emulateLlvm = true;
if(emulateLlvm)
{
    // Load the binary into unicorn engine.
    var symbolicEmulator = new SymbolicEmulator(architecture);

    BinaryMapper.MapPEFile(symbolicEmulator, binary);

    // Setup the stack.
    ulong rsp = 0x100000000;
    symbolicEmulator.MapMemory(rsp, 0x1000 * 1200);
    rsp += 0x20000;

    // Setup the segment registers.g
    symbolicEmulator.MapMemory(0, 0x1000 * 1000);

    for (ulong i = 0; i < 32; i++)
    {
        ulong baseAddr = 0x14006C45D;
        symbolicEmulator.WriteMemory(baseAddr + i, new byte[] { 0x90 });
    }

    for (ulong i = 0; i < 0x1000; i++)
    {
        ulong baseAddr = 0;
        symbolicEmulator.WriteMemory(baseAddr + i, new byte[] { 0x0 });
    }

    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RSP, rsp);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RBP, rsp);

    // Update low parts of rbp.
    var casted = (uint)rsp;
    var casted2 = (ushort)rsp;
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RBP, rsp);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_EBP, casted);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_BP, casted2);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_BPL, (byte)casted);

    // Update low parts of RSP.
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ESP, casted);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_SP, casted2);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_SPL, (byte)casted);


    symbolicEmulator.SetRegister(register_e.ID_REG_X86_RIP, 0x140001299);

    symbolicEmulator.SetRegister(register_e.ID_REG_X86_CF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_PF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_AF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ZF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_SF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_TF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_IF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_DF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_OF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_NT, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_AC, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_VIF, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_VIP, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_ID, 0);
    symbolicEmulator.SetRegister(register_e.ID_REG_X86_R14, 0);

    symbolicEmulator.WriteMemory<ulong>(0x10001FFE0, 0);

    foreach (var register in X86Registers.RegisterMapping.Values)
    {
        var parent = register.ParentId;
        if (parent == register_e.ID_REG_X86_RIP || parent == register_e.ID_REG_X86_RSP || parent == register_e.ID_REG_X86_RCX
            || parent == register_e.ID_REG_X86_RDX || parent == register_e.ID_REG_X86_RBP || parent == register_e.ID_REG_INVALID
            || parent == register_e.ID_REG_X86_MXCSR)
            continue;

        if (X86Registers.RegisterMapping[parent].BitSize != 64)
            continue;

        Console.WriteLine($"Setting register: {register.Id}");
        symbolicEmulator.SetRegister(register.Id, 0);
    }

    foreach(var symbolicInst in themidaBlock.Instructions)
    {
        if(symbolicInst is InstRet)
        {
            var raxValue = symbolicEmulator.GetRegister(register_e.ID_REG_X86_RAX);
            Console.WriteLine($"rax value: 0x{raxValue.ToString("X")}");
            Debugger.Break();
        }
        symbolicEmulator.engine.ExecuteInstruction(symbolicInst);
        Console.WriteLine("symexing inst");
    }

    Console.WriteLine("Done.");
}

/*
var llvmLifterModule = llvmLifter.Module;


var llvmFunc = llvmLifter.llvmFunction;
var blk = llvmFunc.FirstBasicBlock;

var llvmToIr = new LLVMInstToIR(llvmLifter.Module, architecture);
var nextInst = blk.FirstInstruction;
while (true)
{
    llvmToIr.LowerInstruction(nextInst);
    nextInst = nextInst.NextInstruction;
}
*/

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
