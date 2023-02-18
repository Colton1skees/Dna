using Dna.Extensions;
using Dna.Symbolic;
using Iced.Intel;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Ast;
using TritonTranslator.Conversion;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Emulation.Symbolic
{
    public class SymbolicEmulator : ICpuEmulator
    {
        private readonly ICpuArchitecture architecture;

        private readonly X86Translator translator;

        private readonly AstToIntermediateConverter astConverter;

        private readonly SymbolicExecutionEngine engine;

        private dgOnMemoryRead memReadCallback;

        private dgOnMemoryWrite memWriteCallback;

        private readonly Z3AstBuilder z3Translator = new Z3AstBuilder(new Microsoft.Z3.Context());

        public SymbolicEmulator(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
            translator = new X86Translator(architecture);
            astConverter = new AstToIntermediateConverter();
            engine = new SymbolicExecutionEngine(EvaluateSymbolicAst);
        }

        public AbstractNode EvaluateSymbolicAst(IOperand operand)
        {
            return null;
        }

        public ulong GetRegister(register_e regId)
        {
            // Get an AST for the register.
            var registerAst = engine.GetOperandDefinition(new RegisterOperand(architecture.GetRegister(regId)));

            // Translate the AST to a z3 AST.
            var expr = GetZ3Ast(registerAst);

            // Evaluate the AST.
            var evaluation = expr.Simplify();
            if (evaluation is not BitVecNum bvNum)
                throw new Exception($"Could not evaluate register: {regId} to constant. One of the variables must be symbolized.");

            return bvNum.UInt64;
        }

        public void SetRegister(register_e regId, ulong value)
        {
            var regOperand = new RegisterOperand(architecture.GetRegister(regId));
            engine.StoreOperandDefinition(regOperand, new IntegerNode(value, regOperand.Bitsize));
        }

        public void MapMemory(ulong address, int size)
        {
            // The symbolic execution engine has no concept of 'mapping' memory. 
            // Mapping is just a concept to support integrating with unicorn engine.
            // Under the hood, this emulator considers any memory location without
            // an assigned AST to be 'unmapped'.
        }

        public byte[] ReadMemory(ulong addr, int size)
        {
            // This is O(n) which is definitely not ideal given that we can do it in O(1).
            // TODO: Refactor later when hashing of distinct memory nodes works.
            var buffer = new byte[size];
            for(int i = 0; i < size; i++)
            {
                var value = engine.MemoryDefinitions
                    .Single(x => FastEvaluate(x.Key) == addr + (ulong)i)
                    .Value;

                buffer[i] = (byte)FastEvaluate(value);
            }
            return buffer;
        }

        public void WriteMemory(ulong addr, byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var target = engine.MemoryDefinitions
                    .Single(x => FastEvaluate(x.Key) == addr + (ulong)i);

                engine.StoreMemoryDefinition(target.Key, new IntegerNode(buffer[i], 1));
            }
        }

        public void Start(ulong addr, ulong untilAddr = long.MaxValue)
        {
            // As a user you would probably expect this to do a typical
            // fetch -> decode -> execute loop, but due to time constraints
            // and my uses cases, we are going to avoid this.
            // TODO: Refactor.
        }

        public void ExecuteNext()
        {
            // Fetch and decode the instruction at the current symbolic RIP.
            var rip = GetRegister(register_e.ID_REG_X86_RIP);
            var bytes = ReadMemory(rip, 16);
            var instruction = GetInstructionFromBytes(rip, bytes);

            // Lift the instruction to our linear IR. It *would* be much faster to 
            // directly symbolically execute the AST form, but I prefer to symbolically
            // execute the linear form since I *never* use the raw lifted ASTs.
            // Context: This class is moreso for validating the semantics
            // of lifted code than it is actual emulation.
            var liftedInstructions = translator
                .TranslateInstruction(architecture.Disassembly(instruction))
                .SelectMany(x => astConverter.ConvertFromSymbolicExpression(x));

            // Symbolically execute each lifted linear instruction.
            foreach(var lifted in liftedInstructions)
            {
                engine.ExecuteInstruction(lifted);
            }
        }

        // TODO: Refactor this out.
        public Iced.Intel.Instruction GetInstructionFromBytes(ulong address, byte[] bytes)
        {
            var codeReader = new ByteArrayCodeReader(bytes);
            var decoder = Iced.Intel.Decoder.Create(64, codeReader);
            decoder.IP = address;
            return decoder.Decode();
        }

        private void OnMemoryRead(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memReadCallback?.Invoke(address, size);
        }

        private void OnMemoryWrite(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memWriteCallback?.Invoke(address, size, value);
        }

        public void SetMemoryReadCallback(dgOnMemoryRead callback)
        {
            memReadCallback = callback;
        }

        public void SetMemoryWriteCallback(dgOnMemoryWrite callback)
        {
            memWriteCallback = callback;
        }


        private ulong? FastEvaluate(AbstractNode node)
        {
            throw new NotImplementedException();
        }

        private Microsoft.Z3.Expr GetZ3Ast(AbstractNode ast)
        {
            return null;
        }
    }
}
