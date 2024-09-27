using Dna.Extensions;
using LLVMSharp.Interop;
using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public class LLVMToBinjaGraph
    {
        private readonly LLVMValueRef function;

        private readonly List<LLVMBasicBlockRef> blocks;

        private readonly StringBuilder sb = new();

        private Dictionary<LLVMBasicBlockRef, string> blockToPythonVar = new();

        public LLVMToBinjaGraph(LLVMValueRef function)
        {
            this.function = function;
            blocks = function.GetBlocks().ToList();
        }

        /// <summary>
        /// https://github.com/numba/llvmlite/issues/741.
        ///  Branches are strange.  The operands are ordered: [Cond, FalseDest,] TrueDest.
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public void Process()
        {
            sb.AppendLine("graph = FlowGraph()");
            int i = 0;

            // Convert the blocks to string.
            foreach(var block in blocks)
            {
                var blockName = $"node_{i}";
                blockToPythonVar.Add(block, blockName);

                sb.AppendLine($"{blockName} = FlowGraphNode(graph)");
                //sb.AppendLine($"{blockName}.lines = []");
                var split = block.ToString().Split(new[] { '\r', '\n' });
                foreach (var line in split.Skip(1))
                {
                    sb.AppendLine($"{blockName}.lines = {blockName}.lines + ['{line}']");
                    SeparateLineIntoTokens(line);
                }

                sb.AppendLine($"graph.append({blockName})");
                i++;
            }

            // Add the edges.
            foreach(var block in blocks)
            {
                var blkName = blockToPythonVar[block];
                var exitInst = block.LastInstruction;
                if(exitInst.InstructionOpcode == LLVMOpcode.LLVMBr && exitInst.OperandCount == 1)
                {
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.UnconditionalBranch, {blockToPythonVar[exitInst.GetOperands().Single().AsBasicBlock()]})");
                }

                else if(exitInst.InstructionOpcode == LLVMOpcode.LLVMBr && exitInst.OperandCount == 3)
                {
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.TrueBranch, {blockToPythonVar[exitInst.GetOperand(2).AsBasicBlock()]})");
                    sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.FalseBranch, {blockToPythonVar[exitInst.GetOperand(1).AsBasicBlock()]})");
                    //sb.AppendLine($"{blkName}.add_outgoing_edge(BranchType.UserDefinedBranch, {blockToPythonVar[exitInst.GetOperand(2).AsBasicBlock()]}, {blockToPythonVar[exitInst.GetOperand(1).AsBasicBlock()]})");
                }

                else if(exitInst.InstructionOpcode == LLVMOpcode.LLVMRet)
                {

                }

                else
                {
                    throw new InvalidOperationException($"Unsupported terminator instruction: {exitInst}");
                }
            }

            sb.AppendLine($"show_graph_report(\"Custom Graph\", graph)");
            Console.WriteLine(sb.ToString());
        }

        public void SeparateLineIntoTokens(string line)
        {
            List<string> tokens = new List<string>();
            // Skip all the whitespace.
            line = ReadWhitespace(line).withoutWhitespace;
            if(line.StartsWith("%"))
            {
                var assignment = ReadVarAssignment(line);
                tokens.Add($"InstructionTextToken(InstructionTextTokenType.RegisterToken, \"{assignment.varName}\"),");
                tokens.Add($"InstructionTextToken(InstructionTextTokenType.OperandSeparatorToken, \" = \"),");
                line = assignment.without;
            }

            var opcodeInfo = Readopcode(line);
            line = opcodeInfo.without;
            tokens.Add($"InstructionTextToken(InstructionTextTokenType.InstructionToken, \"{opcodeInfo.opcode}\"),");
            tokens.Add($"InstructionTextToken(InstructionTextTokenType.OperandSeparatorToken, \" \")");
            ReadOperands(line);

        }

        private (string whitespace, string withoutWhitespace) ReadWhitespace(string line)
        {
            var whitespace = line.TakeWhile(x => x == ' ').ToArray();
            var noWhitespace = new string(line.Skip(whitespace.Length).ToArray());
            return (new string(whitespace), noWhitespace);
        }

        private (string varName, string assignmentSpace, string without) ReadVarAssignment(string line)
        {
            var varName = new string(line.TakeWhile(x => x != ' ').ToArray());
            var assignmentSpace = " = ";
            var without = new string(line.Skip(varName.Length + assignmentSpace.Length).ToArray());
            return (varName, assignmentSpace, without);
        }

        private (string opcode, string whitespace, string without) Readopcode(string line)
        {
            if (line.StartsWith("getelementptr inbounds "))
                return ("getelementptr inbounds", " ", line.Replace("getelementptr inbounds ", ""));

            var opcode = new string(line.TakeWhile(x => x != ' ').ToArray());
            var whitespace = " ";
            var without = line.Replace(opcode + whitespace, "");
            return (opcode, whitespace, without);
        }

        private List<string> ReadOperands(string input)
        {
            while(true)
            {
                var untilComma = new string(input.TakeWhile(x => x != ',').ToArray());
                var split = untilComma.Split(" ", StringSplitOptions.None);
                Console.WriteLine("");
                break;
            }

            return null;
        }

        private string ProcessSplitIntoToken(string input)
        {
            if (input.StartsWith("i") || input.StartsWith("f") || input == "ptr")
            {
                return $"InstructionTextToken(InstructionTextTokenType.TypeNameToken, \"{input}\"),";
            }

            if (input.StartsWith("%"))
            {
                return $"InstructionTextToken(InstructionTextTokenType.RegisterToken, \"{input}\"),";
            }

            if(long.TryParse(input, out long result) || ulong.TryParse(input, out ulong uresult))
            {
                return $"InstructionTextToken(InstructionTextTokenType.IntegerToken, \"{input}\"),";
            }

            else if (input.StartsWith("!") || input.StartsWith("align"))
                return null;

            throw new InvalidOperationException("TODO!");
        }
    }
}
