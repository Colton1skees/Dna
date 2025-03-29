using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes
{
    // Somewhat naive pass for eliminating control flow flattening.
    public class CFGUnflattenPass
    {

        private readonly LLVMValueRef function;

        public static bool Run(LLVMValueRef function) => new CFGUnflattenPass(function).Run();

        private CFGUnflattenPass(LLVMValueRef function)
        {
            this.function = function;
        }

        private bool Run()
        {
            // Run reg2mem / jump threading
            LLVMCloning.PrepareForCloning(function, true);

            if (function.GetInstructions().Any(x => x.InstructionOpcode == LLVMOpcode.LLVMPHI))
                LLVMCloning.PrepareForCloning(function, false);

            function.GlobalParent.PrintToFile("beforecloning.ll");

            var directJmps = function.GetBlocks().Where(x => x.LastInstruction.InstructionOpcode == LLVMOpcode.LLVMBr && x.LastInstruction.OperandCount == 1).Select(x => x.LastInstruction).ToList();
            if (!directJmps.Any())
                return false;

            // Clone each basic block into it's predecessor if the predecessor unconditionally branches.
            foreach(var brInst in directJmps)
            {
                foreach(var inst in brInst.GetOperand(0).AsBasicBlock().GetInstructions())
                    Console.WriteLine(inst);

                Console.WriteLine($"Inst {brInst} Kind and body: {brInst.GetOperand(0).Kind}");
                var clone = LLVMCloning.CloneBasicBlock(brInst.GetOperand(0).AsBasicBlock());
                brInst.SetOperand(0, clone.AsValue());

                // Really stupid remap

                foreach (var inst in clone.GetInstructions())
                {
                    //if (inst.Name.Length > 0)
                    //    inst.Name = Guid.NewGuid().ToString();
                    //if(inst.GetUsers().Any())
                }
                //if (!LLVMCloning.MergeBlockIntoPredecessor(clone))
                //    throw new InvalidOperationException("Failed to merge basic block!");
            }

            function.GlobalParent.PrintToFile("cloned.ll");

            return true;
            // Clone the basic block.
        }
    }
}
