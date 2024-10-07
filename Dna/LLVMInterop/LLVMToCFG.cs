using Dna.ControlFlow;
using Dna.Extensions;
using Dna.Lifting;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public static class LLVMToCFG
    {
        /// <summary>
        /// https://github.com/numba/llvmlite/issues/741.
        ///  Branches are strange.  The operands are ordered: [Cond, FalseDest,] TrueDest.
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public static ControlFlowGraph<LLVMValueRef> GetCFG(LLVMValueRef function)
        {
            ControlFlowGraph<LLVMValueRef> llvmGraph = new ControlFlowGraph<LLVMValueRef>(0);
            foreach (var llvmBlock in function.GetBasicBlocks())
            {
                // Allocate a new block.
                var blk = llvmGraph.CreateBlock((ulong)llvmBlock.Handle);

                // Copy the instructions.
                blk.Instructions.AddRange(llvmBlock.GetInstructions());
            }

            foreach (var block in llvmGraph.GetBlocks())
            {
                var exitInstruction = block.ExitInstruction;
                // if (exitInstruction.ToString().Contains("cond120131"))
                //  Debugger.Break();
                var operands = exitInstruction.GetOperands().ToList();
                foreach (var operand in operands)
                {
                    Console.WriteLine(operand.ToString());
                    if (operand.Kind != LLVMValueKind.LLVMBasicBlockValueKind)
                        continue;

                    var outgoingBlk = llvmGraph.GetBlocks().Single(x => x.Address == (ulong)operand.Handle);
                    block.AddOutgoingEdge(new BlockEdge<LLVMValueRef>(block, outgoingBlk));
                }


                if (exitInstruction.ToString().Contains("cond120131"))
                {
                    var op0 = block.ExitInstruction.GetOperand(1);
                    var op1 = block.ExitInstruction.GetOperand(2);
                    Console.WriteLine(exitInstruction);
                    Console.WriteLine(op0);
                    Console.WriteLine(op1);
                    var edge1 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(1).Handle.ToString("X"));
                    var edge2 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(2).Handle.ToString("X"));
                    Console.WriteLine("why.");
                    //  Debugger.Break();
                }
                Console.WriteLine(exitInstruction);
            }

            return llvmGraph;
        }
    }
}
