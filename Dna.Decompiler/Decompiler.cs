using Dna.ControlFlow;
using Dna.Decompilation;
using Dna.Lifting;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Intermediate;

namespace Dna.Decompiler
{
    public class Decompiler
    {
        private readonly ICpuArchitecture architecture;

        private readonly RellicLLVMDecompiler decompiler;

        public Decompiler(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
            decompiler = new RellicLLVMDecompiler();
        }

        public string Decompile(ControlFlowGraph<AbstractInst> function)
        {
            // Lift the function to LLVM IR.
            var lifter = new LLVMLifter(architecture);
            lifter.Lift(function);

            // Decompile the function to pseudo C.
            var ast = decompiler.Decompile(lifter.Module);
            return ast;
        }
    }
}
