using Dna.Extensions;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Wrapper
{
    public static class RegionPrinter
    {
        private static int indent = 0;

        public static string PrintRegion(Region region)
        {
            StringBuilder sb = new StringBuilder();
            FormatRegion(region, ref sb);

            Console.WriteLine("");
            Console.WriteLine(sb.ToString());
            Debugger.Break();

            return sb.ToString();
        }

        private static void FormatRegion(Region region, ref StringBuilder builder)
        {
            switch (region)
            {
                case SequenceRegion seqReg:
                    foreach (var child in region.Children)
                    {
                        FormatRegion(child, ref builder);
                    }
                    break;

                case BlockRegion blockReg:
                    var blk = blockReg.BasicBlock;
                    PrintBlock(blk, ref builder);
                    break;

                case NaturalLoopRegion loopReg:
                    //Console.WriteLine("");
                    //Console.WriteLine(builder.ToString());

                    builder.AppendLine(GetIndent() + "do");
                    builder.AppendLine(GetIndent() + "{");
                    indent += 1;

                    FormatRegion(loopReg.Head, ref builder);
                    indent -= 1;
                    builder.AppendLine(GetIndent() + "while(true);");

                    break;


                case IfThenRegion ifThenRegion:
                    var head = ifThenRegion.Head;
                    FormatRegion(head, ref builder);

                    var branchInst = ifThenRegion.TerminatorInstruction;
                    var name = branchInst.GetOperand(0).Name;

                    builder.AppendLine(GetIndent() + $"if({name})");
                    builder.AppendLine(GetIndent() + "{");
                    indent += 1;
                    FormatRegion(ifThenRegion.Then, ref builder);
                    indent -= 1;
                    builder.AppendLine(GetIndent() + "}");
                    break;

                case BreakRegion breakRegion:
                    builder.AppendLine(GetIndent() + "break;");
                    break;

                case ReturnRegion returnRegion:
                    FormatRegion(returnRegion.Head, ref builder);
                    builder.AppendLine(GetIndent() + "return;");
                    break;
                default:
                    Console.WriteLine("");
                    Console.WriteLine(builder.ToString());
                    Debugger.Break();
                    break;
            }
        }

        private static void PrintBlock(LLVMBasicBlockRef block, ref StringBuilder builder)
        {
            foreach (var inst in block.GetInstructions())
            {
                if (inst == block.LastInstruction)
                    break;
                builder.AppendLine(GetIndent() + new string(inst.ToString().SkipWhile(x => x == ' ').ToArray()));
            }
        }

        private static string GetIndent()
        {
            return new string(' ', 4 * indent);
        }
    }
}
