using Dna.ControlFlow;
using Dna.Optimization.Passes;
using Rivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using TritonTranslator.Conversion;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;
using BlockMapping 
    = System.Collections.Generic.Dictionary<Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>, Dna.ControlFlow.BasicBlock<TritonTranslator.Intermediate.AbstractInst>>;

namespace Dna.Lifting
{
    public class CfgLifter
    {
        private readonly ICpuArchitecture architecture;

        private readonly X86Translator translator;

        private readonly AstToIntermediateConverter astConverter;

        private BlockMapping blockMapping;

        public CfgLifter(ICpuArchitecture architecture)
        {
            this.architecture = architecture;
            translator = new X86Translator(architecture);
            astConverter = new AstToIntermediateConverter();
        }

        public ControlFlowGraph<AbstractInst> LiftCfg(ControlFlowGraph<Iced.Intel.Instruction> inGraph)
        {
            // Create an output graph.
            var liftedCfg = new ControlFlowGraph<AbstractInst>(inGraph.StartAddress);

            // Create an IR graph block for each native basic block.
            var blocks = inGraph.GetBlocks();
            blockMapping = new BlockMapping();
            foreach (var block in blocks)
                blockMapping[block] = liftedCfg.CreateBlock(block.Address);

            // Update the output graph edges.
            foreach(var block in blocks)
                LiftEdge(block);

            // Lift the input block.
            foreach(var block in blocks)
                LiftBlock(block);

            return liftedCfg;
        }

        private void LiftEdge(BasicBlock<Iced.Intel.Instruction> inputBlock)
        {
            // Sync the incoming edges.
            var liftedBlock = blockMapping[inputBlock];
            foreach (var incomingEdge in inputBlock.GetIncomingEdges())
            {
                var liftedSource = blockMapping[incomingEdge.SourceBlock];
                var liftedTarget = blockMapping[incomingEdge.TargetBlock];
                var liftedEdge = new BlockEdge<AbstractInst>(liftedSource, liftedTarget);
                liftedBlock.AddIncomingEdge(liftedEdge);
            }

            // Sync the outgoing edges.
            foreach (var outgoingEdge in inputBlock.GetOutgoingEdges())
            {
                var liftedSource = blockMapping[outgoingEdge.SourceBlock];
                var liftedTarget = blockMapping[outgoingEdge.TargetBlock];
                var liftedEdge = new BlockEdge<AbstractInst>(liftedSource, liftedTarget);
                liftedBlock.AddOutgoingEdge(liftedEdge);
            }
        }

        private void LiftBlock(BasicBlock<Iced.Intel.Instruction> inputBlock)
        {
            List<Iced.Intel.Instruction> output = new List<Iced.Intel.Instruction>(inputBlock.Instructions.Count * 20);

            inputBlock.Instructions.AddRange(output);

            Console.WriteLine("inst count: {0}", inputBlock.Instructions.Count);

            // Lift all instructions from native -> AST -> 3 address code representation.
            var liftedInstructions = inputBlock.Instructions
                .SelectMany(x => translator.TranslateInstruction(architecture.Disassembly(x)))
                .SelectMany(x => astConverter.ConvertFromSymbolicExpression(x))
                .ToList();

            // Discard all redundant temporaries.
            var sw = Stopwatch.StartNew();
            IOptimizationPass pass = new BlockTemporaryPropagationPass(liftedInstructions);
            pass.Run();

            /*
            pass = new BlockDeadcodeElimination(architecture, liftedInstructions);
            pass.Run();

            pass = new BlockTemporaryPropagationPass(liftedInstructions);
            pass.Run();
            pass = new BlockDeadcodeElimination(architecture, liftedInstructions);
            pass.Run();
            */

            Console.WriteLine("Took {0} ms to run optimization pass.", sw.ElapsedMilliseconds);

            // Move the lifted instructions into the basic block.
            var liftedBlock = blockMapping[inputBlock];
            liftedBlock.Instructions.AddRange(liftedInstructions);
            UpdateBlockExitInstruction(liftedBlock);
        }

        /// <summary>
        /// Uses pattern matching to insert jmp/jcc/ret instructions from the lifted IR.
        /// </summary>
        /// <param name="block"></param>
        /// <exception cref="Exception"></exception>
        private void UpdateBlockExitInstruction(BasicBlock<AbstractInst> block)
        {
            var outgoingEdges = block.GetOutgoingEdges();
            var count = outgoingEdges.Count();

            // Insert a RET if the block has no outgoing edges.
            if(count == 0)
            {
                block.Instructions.Add(new InstRet());
            }

            else if (count == 1)
            {
                // Since the AST form IR we lift from has no concept of branching,
                // we attempt to pattern match the "rip = copy immX" assignment and confirm 
                // that it matches up with our cfg.
                var exitInstruction = block.ExitInstruction;
                var err = new InvalidOperationException(String.Format("Failed to find jump to basic block {0}.", outgoingEdges.First().TargetBlock.Name));
                if (exitInstruction.Dest == null || exitInstruction.Dest is not RegisterOperand)
                    throw err;

                var destRegister = exitInstruction.Dest as RegisterOperand;
                if (destRegister.Register.Id != register_e.ID_REG_X86_RIP)
                    throw err;

                // If the exit instruction is not a copy of an immediate, then throw.
                // More aggressive pattern matching is needed to handle other cases.
                if (exitInstruction is not InstCopy || exitInstruction.Op1 is not ImmediateOperand)
                    throw err;

                // Confirm that the CFG outgoing block destination matches up with the lifted jump destination.
                var immDest = exitInstruction.Op1 as ImmediateOperand;
                var outgoingAddr = outgoingEdges.Single().TargetBlock.Address;
                if (immDest.Value != outgoingAddr)
                    throw new InvalidOperationException(String.Format("Block has jump to {0} when {1} was expected", immDest.Value.ToString("X"), outgoingAddr.ToString("X")));

                // Add a branch to the destination.
                block.Instructions.Add(new InstJmp(immDest));
            }

            else if (count == 2)
            {
                // Since the AST form IR we lift from has no concept of branching,
                // we attempt to pattern match the "rip = select cond, i64, i64" assignment and confirm 
                // that it matches up with our cfg.
                var exitInstruction = block.ExitInstruction;
                var err = new InvalidOperationException(String.Format("Failed to find jump to basic block {0}.", outgoingEdges.First().TargetBlock.Name));
                if (exitInstruction.Dest == null || exitInstruction.Dest is not RegisterOperand)
                    throw err;

                var destRegister = exitInstruction.Dest as RegisterOperand;
                if (destRegister.Register.Id != register_e.ID_REG_X86_RIP)
                    throw err;

                // If the exit instruction is not a copy of an immediate, then throw.
                // More aggressive pattern matching is needed to handle other cases.
                if (exitInstruction is not InstSelect || exitInstruction.Op2 is not ImmediateOperand || exitInstruction.Op3 is not ImmediateOperand)
                    throw err;

                var selectInst = exitInstruction as InstSelect;
                var cond = selectInst.Op1;
                var immDest0 = selectInst.Op2 as ImmediateOperand;
                var immDest1 = selectInst.Op3 as ImmediateOperand;

                block.Instructions.Add(new InstJcc(cond, immDest0, immDest1));
            }

            else
            {
                throw new InvalidOperationException("Basic blocks cannot have more than two outgoing edges.");
            }
        }
    }
}
