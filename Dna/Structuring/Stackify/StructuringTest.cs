using Dna.ControlFlow;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Structuring.Stackify
{
    public class StructuringTest
    {
        public static ControlFlowGraph<AbstractInst> GetSimpleComparison()
        {
            // Create an empty IR cfg.
            var cfg = new ControlFlowGraph<AbstractInst>(0);

            // Create a block which jumps depending on whether rcx == 0;
            var entry = cfg.CreateBlock(0);
            var t0 = new TemporaryOperand(0, 1);
            var rcx = new RegisterOperand(X86Registers.RegisterMapping[register_e.ID_REG_X86_RCX]);
            var cmp = new InstCond(CondType.Eq, t0, rcx, new ImmediateOperand(0, 64));
            var jcc = new InstJcc(cmp.Dest, new ImmediateOperand(1, 64), new ImmediateOperand(2, 64));
            entry.Instructions.Add(cmp);
            entry.Instructions.Add(jcc);

            // Create the then block, which writes a constant to RAX.
            var then = cfg.CreateBlock(1);
            var rax = new RegisterOperand(X86Registers.RegisterMapping[register_e.ID_REG_X86_RAX]);
            var store = new InstCopy(rax, new ImmediateOperand(0x111111, 64));
            var ret = new InstRet();
            then.Instructions.Add(store);
            then.Instructions.Add(ret);

            // Create the else block, which writes a different constant to RAX.
            var elif = cfg.CreateBlock(2);
            store = new InstCopy(rax, new ImmediateOperand(0x22222, 64));
            ret = new InstRet();
            elif.Instructions.Add(store);
            elif.Instructions.Add(ret);

            // Create outgoing edges to connect each basic block.
            entry.AddOutgoingEdge(new BlockEdge<AbstractInst>(entry, then));
            entry.AddOutgoingEdge(new BlockEdge<AbstractInst>(entry, elif));

            // Return the cfg.
            return cfg;
        }

        public void Run()
        {
            // Test sample one.
            var cfg = GetSimpleComparison();

            var stackifier = new CfgStackifier();
            var wasm = stackifier.Stackify(cfg);

            var astEmitter = new ASTEmitter();
            astEmitter.Emit(wasm);

            Console.WriteLine(astEmitter.builder.ToString());
        }
    }
}
