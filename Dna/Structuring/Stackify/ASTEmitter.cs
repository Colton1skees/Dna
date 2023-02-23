using Dna.ControlFlow;
using Dna.Structuring.Stackify.Structured;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Intermediate;

namespace Dna.Structuring.Stackify
{
    

    public class ASTEmitter
    {
        public CodeBuilder builder = new CodeBuilder();

        List<WasmBlock> og;

        private HashSet<BasicBlock<AbstractInst>> loops = new();

        public void Emit(List<WasmBlock> ast)
        {
            if(og == null)
            {
                og = ast;
            }
            foreach(var wasmBlock in ast)
            {
                ProcessWasmBlock(wasmBlock);
            }
        }

        private void ProcessWasmBlock(WasmBlock wasmBlock)
        {
            switch(wasmBlock)
            {
                // Do nothing with block params since they don't exist.
                case BlockParams wasm:
                    break;
                case Block wasm:
                    Emit(wasm.Body);
                    break;
                case Leaf wasm:
                    var text = FormatBlock(wasm.Block);
                    builder.Append(text);
                    break;
                case If wasm:
                    builder.StartIfStatement(wasm.Cond.ToString());
                    Emit(wasm.IfTrue);
                    builder.EndClause();
                    builder.StartElseStatement("");
                    Emit(wasm.IfFalse);
                    builder.EndClause();
                    break;
                case Return wasm:
                    builder.AppendReturnStatement("void");
                    break;
                case Loop wasm:
                    // This is incorrect.
                    loops.Add(wasm.Header);
                    builder.StartForLoop("loop", "loop", "loop");
                    Emit(wasm.Body);
                    builder.EndClause();
                    break;
                case Br wasm:
                    // This is incorrec.t
                    if (loops.Contains(wasm.Target?.Index))
                    {
                        builder.AppendLine("continue");
                    }
                    else
                    {
                        builder.AppendLine("break;");
                    }
                    break;
                default:
                    Console.WriteLine("");
                    Console.WriteLine(builder.ToString());
                    throw new InvalidOperationException();
            }
        }

        private string FormatBlock(BasicBlock<AbstractInst> block)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var instruction in block.Instructions)
                stringBuilder.AppendLine(instruction.ToString());
            return stringBuilder.ToString();
        }

    }
}
