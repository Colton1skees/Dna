using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.JmpTables.Precise
{
    public static class InputVariableCollector
    {
        public static HashSet<SouperInst> Collect(SouperInst inst)
        {
            var output = new HashSet<SouperInst>();
            CollectInputVariables(inst, ref output);
            return output;
        }

        private static void CollectInputVariables(SouperInst inst, ref HashSet<SouperInst> inputVariables)
        {
            switch(inst.Kind)
            {
                case SouperInstKind.Phi:
                    Debugger.Break();
                    break;

                case SouperInstKind.Var:
                    var origin = inst.Origins.Single();
                    inputVariables.Add(inst);
                    break;
                case SouperInstKind.Add:
                case SouperInstKind.AddNSW:
                case SouperInstKind.AddNUW:
                case SouperInstKind.AddNW:
                case SouperInstKind.Sub:
                case SouperInstKind.SubNSW:
                case SouperInstKind.SubNUW:
                case SouperInstKind.SubNW:
                case SouperInstKind.Mul:
                case SouperInstKind.MulNSW:
                case SouperInstKind.MulNUW:
                case SouperInstKind.MulNW:
                case SouperInstKind.UDiv:
                case SouperInstKind.SDiv:
                case SouperInstKind.UDivExact:
                case SouperInstKind.SDivExact:
                case SouperInstKind.URem:
                case SouperInstKind.SRem:
                case SouperInstKind.And:
                case SouperInstKind.Or:
                case SouperInstKind.Xor:
                case SouperInstKind.Shl:
                case SouperInstKind.ShlNSW:
                case SouperInstKind.ShlNUW:
                case SouperInstKind.ShlNW:
                case SouperInstKind.LShr:
                case SouperInstKind.LShrExact:
                case SouperInstKind.AShr:
                case SouperInstKind.AShrExact:
                case SouperInstKind.Select:
                case SouperInstKind.ZExt:
                case SouperInstKind.SExt:
                case SouperInstKind.Trunc:
                case SouperInstKind.Eq:
                case SouperInstKind.Ne:
                case SouperInstKind.Ult:
                case SouperInstKind.Slt:
                case SouperInstKind.Ule:
                case SouperInstKind.Sle:
                case SouperInstKind.CtPop:
                case SouperInstKind.BSwap:
                case SouperInstKind.Cttz:
                case SouperInstKind.Ctlz:
                case SouperInstKind.BitReverse:
                case SouperInstKind.FShl:
                case SouperInstKind.FShr:
                case SouperInstKind.ExtractValue:
                case SouperInstKind.SAddWithOverflow:
                case SouperInstKind.SAddO:
                case SouperInstKind.UAddWithOverflow:
                case SouperInstKind.UAddO:
                case SouperInstKind.SSubWithOverflow:
                case SouperInstKind.SSubO:
                case SouperInstKind.USubWithOverflow:
                case SouperInstKind.USubO:
                case SouperInstKind.SMulWithOverflow:
                case SouperInstKind.SMulO:
                case SouperInstKind.UMulWithOverflow:
                case SouperInstKind.UMulO:
                case SouperInstKind.SAddSat:
                case SouperInstKind.UAddSat:
                case SouperInstKind.SSubSat:
                case SouperInstKind.USubSat:
                case SouperInstKind.Freeze:
                case SouperInstKind.Const:
                    foreach (var op in inst.Operands)
                        CollectInputVariables(op, ref inputVariables);
                    break;
                default:
                    throw new InvalidOperationException($"Unrecognized souper inst kind: {inst.Kind} from {inst}");
            }
        }
    }
}
