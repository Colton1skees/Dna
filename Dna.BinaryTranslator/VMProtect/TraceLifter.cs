using Dna.Binary;
using Dna.BinaryTranslator.X86;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Relocation;
using ELFSharp.ELF;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public record TraceInst(int InstIndex, Instruction Inst, byte[] Bytes);

    public class TraceLifter
    {
        private readonly LLVMContextRef ctx;

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly RemillArch arch;

        private readonly IReadOnlyList<RemillRegister> registers;

        private readonly IReadOnlyList<TraceInst> traceInsts;

        public static LLVMValueRef Lift(LLVMModuleRef remillModule, RemillArch arch, IReadOnlyList<TraceInst> traceInstructions) => new TraceLifter(remillModule, arch, traceInstructions).Lift();

        private TraceLifter(LLVMModuleRef module, RemillArch arch, IReadOnlyList<TraceInst> traceInstructions)
        {
            this.ctx = module.Context;
            this.module = module;
            this.builder = ctx.CreateBuilder();
            this.arch = arch;
            this.registers = arch.Registers;
            this.traceInsts = traceInstructions;
        }

        private LLVMValueRef Lift()
        {
            // Use remill to declare and initialize an empty function. This function has the default remill function prototype.
            var initialIp = traceInsts.First().Inst.IP;
            var translatedFunction = arch.DeclareLiftedFunction($"TranslatedFrom{initialIp.ToString("X")}", module);
            arch.InitializeEmptyLiftedFunction(translatedFunction);

            // In the artificial entry block(the basic block which remill inserts via `InitializeEmptyLiftedFunction),
            // store a concrete [initialRip] to Remill's 'NEXT_PC' variable.
            // If you run into case where you see memory accesses to [0x1400xxxxxxxxx] in code that's intended to be runnable,
            // this is probably the logic at fault.
            builder.PositionAtEnd(translatedFunction.EntryBasicBlock);
            ConcretizeInstructionPointer(translatedFunction, initialIp);

            // Lift the trace into the entry basic block.
            LiftTrace(translatedFunction.EntryBasicBlock);

            // Clone the function into a new module. This discards all unnecessary semantics, effectively trimming the module.
            // Note: (a) this applies basic optimizations, and (b) this returns a function, not a module.
            return FunctionIsolator.IsolateFunctionIntoNewModule(arch, translatedFunction);
        }

        private void ConcretizeInstructionPointer(LLVMValueRef function, ulong initialRip)
        {
            // Store a concrete instruction pointer to Remill's `NEXT_PC` variable.
            var constPc = LLVMValueRef.CreateConstInt(ctx.GetInt64Ty(), initialRip);
            builder.BuildStore(constPc, RemillUtils.LoadNextProgramCounterRef(function.EntryBasicBlock));
        }

        private void LiftTrace(LLVMBasicBlockRef llvmBlock)
        {
            // Lift each instruction.
            foreach(var traceInst in traceInsts)
            {
                // Decode the instruction.
                var remillInst = DecodeInstruction(traceInst);

                // Concretize the instruction pointer.
                var immConstInt2 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, traceInst.Inst.IP);
                var pcRef2 = RemillUtils.LoadNextProgramCounterRef(llvmBlock);
                //builder.BuildStore(immConstInt2, RemillUtils.LoadProgramCounterRef(llvmBlock));
                builder.BuildStore(immConstInt2, pcRef2);

                remillInst.Lifter.LiftIntoBlock(remillInst, llvmBlock);

                // If we see a CALL or JMP(of any kind, e.g. direct, indirect),
                // then forcefully update the instruction pointer.
                var flow = traceInst.Inst.FlowControl;
                if(flow.IsCall() || flow.IsBranch())
                {
                    // Fetch the next instruction from the trace.
                    var nextIp = traceInsts[traceInst.InstIndex].Inst.IP;
                    var immConstInt = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, nextIp);

                    // TODO: Uncomment this store code if you want to lift actual traces.
                    // Store that next IP into the %PROGRAM_COUNTER of the state structure.
                    // Note the correct behavior seems to be updating the '%NEXT_PC' variable, but updating both like we do here is likely fine.
                    // https://github.com/lifting-bits/remill/blob/17cff6b4df900c68ff583debec5fabe76a01d9a5/lib/BC/InstructionLifter.cpp#L147
                    var pcRef = RemillUtils.LoadProgramCounterRef(llvmBlock);
                    builder.BuildStore(immConstInt, RemillUtils.LoadProgramCounterRef(llvmBlock));
                    builder.BuildStore(immConstInt, pcRef);
                }

            }

            // Fetch the state pointer from argument 0.
            var statePtr = llvmBlock.Parent.GetParam(0);

            // Return RAX;
            var reg = registers.Single(x => x.Name == "RIP");
            var regPtr = reg.GetAddressOf(statePtr, builder);
            var regValue = builder.BuildLoad2(ctx.Int64Type, regPtr, "RIP");
            regValue = builder.BuildIntToPtr(regValue, ctx.GetPtrType());
            builder.BuildRet(regValue);
        }

        // Decodes an instruction into an RemillInst.
        // It also verifies that Iced and Remill have the same decoding output.
        private RemillInstruction DecodeInstruction(TraceInst traceInst)
        {
            // We prefer to use a mapping of <instruction, byte[]> for a few reasons:
            //   - removes the dependency on `IBinary` or `Dna`.
            //   - allows us to overwrite or change an instruction *without* modifying the binary contents.
            //   - prevents us from having to re-encode the instruction, which can change the length(causing disastrous results, since the `NextIP` field change.
            var instBytes = traceInst.Bytes;

            // Decode the instruction again using ICED, but this time use the bytes supplied in the encoding mapping.
            // If the provided instruction is not identical to the one decoded from the decoding mapping, something is majorly wrong.
            // Note that we make one exception for "sub rsp, 0xwhatever" for vmprotect.
            var inst = traceInst.Inst;
            var icedInst = BinaryDisassembler.GetInstructionFromBytes(inst.IP, instBytes);
            bool isMismatch = icedInst != inst;
            if (isMismatch && !icedInst.ToString().Contains("sub rsp,") && !inst.ToString().Contains("mov rax") && !inst.ToString().Contains("and rax") && !inst.ToString().Contains("push r13"))
                throw new InvalidOperationException($"Decoding mismatch between control flow graph inst {inst} and decoded assembly mapping instruction {icedInst}");

            // If there is an allowed mismatch(sub rsp, 0xwhatever), forcefully overwrite the bytes with the correct encoding.
            if(isMismatch)
                instBytes = InstructionEncoder.EncodeInstruction(traceInst.Inst, traceInst.Inst.IP);

            // Decode the instruction using remill & validate it.
            var remillInst = arch.DecodeInstruction(inst.IP, instBytes);
            Console.WriteLine(remillInst.ToString());
            if (remillInst == null || remillInst.Pc != inst.IP || remillInst.NextPc != inst.NextIP)
                throw new InvalidOperationException($"Mismatch in data between remill instruction {remillInst?.Text} & iced instruction 0x{inst}");
            return remillInst;
        }

        private void LiftCall(LLVMBasicBlockRef llvmBlock)
        {
            // On function call, remill stores the return address value to %RETURN_PC.
            // However, it does *not* update %NEXT_PC to point to the post-call value.
            // So to fix this, first we start off by loading the const return address from %RETURN_PC.
            var returnPc = builder.BuildLoad2(ctx.GetInt64Ty(), RemillUtils.LoadReturnProgramCounterRef(llvmBlock));

            // Then we store the return address to NEXT_PC. This allows the lifted code to have correct behavior.
            builder.BuildStore(returnPc, RemillUtils.LoadNextProgramCounterRef(llvmBlock));
        }

    }
}
