using Dna.Extensions;
using Dna.Symbolic;
using Iced.Intel;
using Microsoft.Z3;
using Rivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public readonly SymbolicExecutionEngine engine;

        private dgOnMemoryRead memReadCallback;

        private dgOnMemoryWrite memWriteCallback;

        private readonly Z3AstBuilder z3Translator = new Z3AstBuilder(new Microsoft.Z3.Context());

        public SymbolicEmulator(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
            translator = new X86Translator(architecture);
            astConverter = new AstToIntermediateConverter(architecture);
            engine = new SymbolicExecutionEngine(EvaluateSymbolicAst);
            engine.SetSymbolicVariableWriteCallback(HandleSymbolicVariableWrite);
            engine.SetSymbolicMemoryWriteCallback(HandleSymbolicMemoryWrite);
        }

        private AbstractNode EvaluateSymbolicAst(IOperand operand)
        {
            if (engine.VariableDefinitions.TryGetValue(operand, out AbstractNode ast))
                return ast;
            return CreateOperandNode(operand);
        }

        private AbstractNode CreateOperandNode(IOperand operand)
        {
            if (operand is ImmediateOperand immOp)
                return new IntegerNode(immOp.Value, immOp.Bitsize);
            else if (operand is RegisterOperand regOp)
                return new RegisterNode(regOp.Register);
            else if (operand is TemporaryOperand tempOp)
                return new TemporaryNode(tempOp.Uid, tempOp.Bitsize);
            else if (operand is SsaOperand ssaOp)
                return new SsaVariableNode((VariableNode)CreateOperandNode(ssaOp.BaseOperand), ssaOp.Version);
            else
                throw new InvalidOperationException(string.Format("Cannot create operand node for type {0}", operand.GetType().FullName));
        }

        private void HandleSymbolicVariableWrite(IOperand operand, AbstractNode value)
        {
            ulong z3Value = 0;
            if(value is MemoryNode memNode)
            {
                var addrNode = memNode.Expr1;
                var addr = EvaluateToUlong(addrNode);
                var bytes = ReadMemory(addr, (int)(value.BitSize / 8));
                z3Value = value.BitSize switch
                {
                    8 => bytes[0],
                    16 => BitConverter.ToUInt16(bytes),
                    32 => BitConverter.ToUInt32(bytes),
                    64 => BitConverter.ToUInt64(bytes),
                    _ => throw new InvalidOperationException()
                };
            }

            else
            {
                z3Value = EvaluateToUlong(value);
            }

            engine.StoreOperandDefinition(operand, new IntegerNode(z3Value, value.BitSize));
        }

        private void HandleSymbolicMemoryWrite(MemoryNode memoryNode, AbstractNode value)
        {
            // Evaluate the memory store address.
            var addr = ((BitVecNum)GetZ3Ast(memoryNode.Expr1).Simplify()).UInt64;

            // Evaluate the memory store value.
            var memValue = ((BitVecNum)GetZ3Ast(value).Simplify()).UInt64;

            // Convert the byte array to bytes.
            var bytes = value.BitSize switch
            {
                8 => new byte[] { (byte)memValue },
                16 => BitConverter.GetBytes((ushort)memValue),
                32 => BitConverter.GetBytes((uint)memValue),
                64 => BitConverter.GetBytes(memValue),
                _ => throw new InvalidOperationException()
            };

            // Update symbolic memory.
            WriteMemory(addr, bytes);
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

        public T ReadMemory<T>(ulong addr)
        {
            var size = MarshalType<T>.Size;
            var buffer = ReadMemory(addr, size);
            return MarshalType<T>.ByteArrayToObject(buffer);
        }

        public byte[] ReadMemory(ulong addr, int size)
        {
            // This is O(n) which is definitely not ideal given that we can do it in O(1).
            // TODO: Refactor later when hashing of distinct memory nodes works.
            var buffer = new byte[size];
            for(int i = 0; i < size; i++)
            {
                if(addr == 0x0140001299)
                {

                }

                /*
                var value = engine.MemoryDefinitions
                    .Single(x => FastEvaluate(x.Key) == addr + (ulong)i)
                    .Value;
                */
                
                if(addr == 0x0010001ff40)
                {

                }


                var memNode = new MemoryNode(new IntegerNode(addr + (ulong)i, 64), 8);
                var value = engine.MemoryDefinitions[memNode];

                buffer[i] = (byte)FastEvaluate(value);
            }
            return buffer;
        }

        public void WriteMemory<T>(ulong addr, T value)
        {
            var buffer = MarshalType<T>.ObjectToByteArray(value);
            WriteMemory(addr, buffer);
        }

        public void WriteMemory(ulong addr, byte[] buffer)
        {
            if (addr == 0x010001ffb8)
            {
                //Debugger.Break();
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                var memNode = new MemoryNode(new IntegerNode(addr + (ulong)i, 64), 8);
                engine.StoreMemoryDefinition(memNode, new IntegerNode(buffer[i], 8));
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

            //if (rip == 0x1400012A3)
                //Debugger.Break();

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
                if(rip == 0x14004605F)
                    Console.WriteLine(lifted);
                    /*
                    if (lifted.ToString().Contains("Reg(rip):64 ="))
                        Debugger.Break();
                    */
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

        /*
        private void OnMemoryRead(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memReadCallback?.Invoke(address, size);
        }

        private void OnMemoryWrite(Emulator emulator, MemoryType type, ulong address, int size, ulong value, object userData)
        {
            memWriteCallback?.Invoke(address, size, value);
        }
        */

        public void SetMemoryReadCallback(dgOnMemoryRead callback)
        {
            memReadCallback = callback;
        }

        public void SetMemoryWriteCallback(dgOnMemoryWrite callback)
        {
            memWriteCallback = callback;
        }

        public void SetInstExecutedCallback(dgOnInstExecuted callback)
        {
            throw new NotImplementedException();
        }

        private ulong? FastEvaluate(AbstractNode node)
        {
            if(node is MemoryNode memNode)
                node = memNode.Children[0];
            if (node is BvNode bvNode)
                node = (IntegerNode)bvNode.Expr1;
            if (node is IntegerNode intNode)
                return intNode.Value;
            return null;
        }

        public Microsoft.Z3.Expr GetZ3Ast(AbstractNode ast)
        {
            var isDefined = (AbstractNode obj) =>
            {
                // var result = GetOperandFromNode(obj);
                return true;
            };

            var getVar = (AbstractNode node) =>
            {
                var operand = GetOperandFromNode(node);
                if (operand is ImmediateOperand immOp)
                    return new IntegerNode(immOp.Value, immOp.Bitsize);
                return engine.VariableDefinitions[operand];
            };

            return z3Translator.GetZ3Ast(ast, isDefined, getVar);
        }

        private IOperand? GetOperandFromNode(AbstractNode node)
        {
            if (node is RegisterNode regNode)
                return new RegisterOperand(regNode.Register);
            if (node is TemporaryNode tempNode)
                return new TemporaryOperand(tempNode.Uid, tempNode.BitSize);
            if (node is IntegerNode intNode)
                return new ImmediateOperand(intNode.Value, intNode.BitSize);
            else
                throw new InvalidOperationException();
        }

        private ulong EvaluateToUlong(AbstractNode ast)
        {
            var z3Ast = GetZ3Ast(ast);

            //var solver = z3Translator.Solver;

            var evaluated = z3Ast.Simplify();

           // var solver = z3Translator.Ctx.MkSolver("QF_BV");

          //  var checkd = solver.Check();


            //var evaluated = solver.Model.Eval(z3Translator.Ctx.MkBV2Int((BitVecExpr)z3Ast, false), false);
            return ((BitVecNum)evaluated).UInt64;
        }
    }
}
