using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Lifting
{
    public class LLVMInstToIR
    {
        private readonly LLVMModuleRef module;

        private readonly ICpuArchitecture architecture;

        private readonly Dictionary<LLVMValueRef, IOperand> llvmOperandMapping = new Dictionary<LLVMValueRef, IOperand>();

        public LLVMInstToIR(LLVMModuleRef module, ICpuArchitecture architecture)
        {
            this.module = module;
            this.architecture = architecture;
        }

        public IEnumerable<AbstractInst> LowerInstruction(LLVMValueRef inst)
        {
            var output = new List<AbstractInst>();
            var emit = (AbstractInst instruction) =>
            {
                output.Add(instruction);
            };

            var op1 = () =>
            {
                return GetOperand(inst.GetOperand(0));
            };

            var op2 = () =>
            {
                return GetOperand(inst.GetOperand(1));
            };

            var dest = () =>
            {
                return CreateOperand(inst);
            };

            switch (inst.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                    emit(new InstAdd(dest(), op1(), op2()));
                    break;

            }

            return output;
        }

        private IOperand CreateOperand(LLVMValueRef destination)
        {
            return null;
        }

        private IOperand GetOperand(LLVMValueRef operand)
        {
            return llvmOperandMapping[operand];
        }
    }
}
