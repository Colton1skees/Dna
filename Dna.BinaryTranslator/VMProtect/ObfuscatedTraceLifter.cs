using Dna;
using Dna.BinaryTranslator;
using Dna.BinaryTranslator.Runtime;
using Dna.BinaryTranslator.Unsafe;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public class ObfuscatedTraceLifter
    {
        private readonly LLVMContextRef ctx;

        private readonly IDna dna;

        private readonly IReadOnlyList<Instruction> instructions;

        private IReadOnlyList<TraceInst> traceInsts;

        public ObfuscatedTraceLifter(LLVMContextRef ctx, IDna dna, IReadOnlyList<Instruction> instructions)
        {
            this.ctx = ctx;
            this.dna = dna;
            this.instructions = instructions;
            traceInsts = GetTraceInsts(instructions);
        }

        public ObfuscatedTraceLifter(LLVMContextRef ctx, IDna dna, IReadOnlyList<TraceInst> traceInsts)
        {
            this.ctx = ctx;
            this.dna = dna;
            this.traceInsts = traceInsts;
        }

        private IReadOnlyList<TraceInst> GetTraceInsts(IReadOnlyList<Instruction> instructions) 
        {
            var traceInsts = new List<TraceInst>(); 
            for (int i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                var traceInst = new TraceInst(i, inst, dna.Binary.ReadBytes(inst.IP, inst.Length));
                traceInsts.Add(traceInst);
            }

            return traceInsts;
        }

        public (RemillArch arch, VmpParameterizedStateStructure function) Lift()
        {
            var arch = new RemillArch(ctx, RemillOsId.kOSWindows, RemillArchId.kArchAMD64_AVX512);

            // Load the remill semantics into a new module.
            var module = RemillUtils.LoadArchSemantics(arch, Path.Combine(Directory.GetCurrentDirectory(), "Semantics"));

            // Lift the trace into an LLVM IR function.
            var function = TraceLifter.Lift(module, arch, traceInsts);

            function.GlobalParent.PrintToFile("translatedFunction.ll");

            // Apply concrete implementations to all intrinsics.
            // E.g. __remill_memory_read() and __remill_memory_write().
            var runtime = UnsafeRuntimeImplementer.Implement(function.GlobalParent);

            // Create a new function which doesn't take a state structure pointer.
            // Instead it takes all registers as `noalias ptr` arguments.
            var parameterizedStateStruct = VmpParameterizedStateStructure.CreateFromFunction(arch, function);
            function = parameterizedStateStruct.OutputFunction;

            function.GlobalParent.PrintToFile("translatedFunction.ll");
            // Replace the remill return and error intrinsics with
            // functions that are more amenable to optimization.
            ErrorAndReturnImplementer.Implement(function);

            // Create a single @memory pointer.
            var memoryPtr = runtime.MemoryPointer;
            var builder = LLVMBuilderRef.Create(ctx);
            builder.Position(function.EntryBasicBlock, function.EntryBasicBlock.FirstInstruction);
            var dominatingLoad = builder.BuildLoad2(ctx.GetPtrType(), memoryPtr.Value, "mem");

            var targets = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == memoryPtr && x != dominatingLoad).ToList();
            if (targets.Any())
            {
                foreach (var other in targets)
                {
                    other.ReplaceAllUsesWith(dominatingLoad);
                    other.InstructionEraseFromParent();
                }
            }

            return (arch, parameterizedStateStruct);
        }
    }
}
