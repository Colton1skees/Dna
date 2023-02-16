using ClangSharp;
using Dna.ControlFlow;
using Dna.Decompilation;
using Dna.Lifting;
using Grpc.Net.Client;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var lifter2 = new LLVMLifter(architecture);
            lifter2.Lift(function);

            // Decompile to pseudo C.
            return Decompile(lifter2.Module);
        }

        public string Decompile(LLVMModuleRef module)
        {
            // Decompile to pseudo C.
            var ast = decompiler.Decompile(module);
            return ast;
        }
    }
}
