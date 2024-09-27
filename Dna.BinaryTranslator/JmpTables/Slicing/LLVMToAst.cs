using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;

namespace Dna.BinaryTranslator.JmpTables.Slicing
{
    public static class LLVMToAst
    {
        public static AbstractNode GetDefinition(
                LLVMValueRef value,
                Dictionary<LLVMValueRef, AbstractNode> definitionMapping,
                Func<LLVMValueRef, bool> isDefinedInDifferentLoop,
                Func<uint, string, TemporaryNode> createTemp)
        {
            // If we already have a definition for the value, return it.
            if (definitionMapping.ContainsKey(value))
                return definitionMapping[value];

            // If it's an argument, then create a new symbolic definition mapping.
            if (value.Kind == LLVMValueKind.LLVMArgumentValueKind)
            {
                var intWidth = value.TypeOf.IntWidth;
                var temp = createTemp(intWidth, value.Name);
                definitionMapping.Add(value, temp);
                return temp;
            }

            // If the value is a constant, store the definition as a constant.
            if (value.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                var constInt = new IntegerNode(value.ConstIntZExt, value.TypeOf.IntWidth);
                definitionMapping.Add(value, constInt);
                return constInt;
            }

            // Throw if it's not an instruction. We shouldn't be attempting to compute definitions for anything else.
            if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
                throw new InvalidOperationException($"Cannot compute definition for {value}. The kind {value.Kind} is not supported.");

            // Create a symbolic definition for the variable if it's defined inside of a different loop.
            // This basically asserts that values defined inside of loops can be anything.
            if (isDefinedInDifferentLoop(value))
            {
                var unreachableDefinition = createTemp(value.TypeOf.IntWidth, value.Name);
                definitionMapping.Add(value, unreachableDefinition);
                return unreachableDefinition;
            }

            var emit = (AbstractNode node) =>
            {
                definitionMapping.TryAdd(value, node);
            };

            var op1 = () =>
            {
                var val = GetDefinition(value.GetOperand(0), definitionMapping, isDefinedInDifferentLoop, createTemp);
                return val;
            };

            var op2 = () =>
            {
                var val = GetDefinition(value.GetOperand(1), definitionMapping, isDefinedInDifferentLoop, createTemp);
                return val;
            };

            var op3 = () =>
            {
                var val = GetDefinition(value.GetOperand(2), definitionMapping, isDefinedInDifferentLoop, createTemp);
                return val;
            };

            var inst = value;
            switch (inst.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                    emit(new BvaddNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAnd:
                    emit(new BvandNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAShr:
                    emit(new BvashrNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMLShr:
                    emit(new BvlshrNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMMul:
                    emit(new BvmulNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMOr:
                    emit(new BvorNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSDiv:
                    emit(new BvsdivNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMShl:
                    emit(new BvshlNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMICmp:
                    // Note: Since SMTLIB2 does not have an `NEQ` type node,
                    // we need to utilize an EQ node & swap the order of the ite input nodes.
                    if (inst.ICmpPredicate == LLVMIntPredicate.LLVMIntNE)
                    {
                        emit(new EqualNode(op2(), op1()));
                        break;
                    }

                    var predicate = GetCondType(inst.ICmpPredicate);
                    AbstractNode cmp = predicate switch
                    {
                        CondType.Eq => new EqualNode(op1(), op2()),
                        CondType.Sge => new BvsgeNode(op1(), op2()),
                        CondType.Sgt => new BvsgtNode(op1(), op2()),
                        CondType.Sle => new BvsleNode(op1(), op2()),
                        CondType.Slt => new BvsltNode(op1(), op2()),
                        CondType.Uge => new BvugeNode(op1(), op2()),
                        CondType.Ugt => new BvugtNode(op1(), op2()),
                        CondType.Ule => new BvuleNode(op1(), op2()),
                        CondType.Ult => new BvultNode(op1(), op2()),
                        _ => throw new InvalidOperationException(string.Format("CondType {0} is not valid.", predicate))
                    };

                    emit(cmp);
                    break;
                case LLVMOpcode.LLVMSRem:
                    emit(new BvsremNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMSub:
                    emit(new BvsubNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMUDiv:
                    emit(new BvudivNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMURem:
                    emit(new BvuremNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMXor:
                    emit(new BvxorNode(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMTrunc:
                    var destTy = inst.TypeOf;
                    emit(new ExtractNode(destTy.IntWidth - 1u, 0, op1()));
                    break;
                case LLVMOpcode.LLVMSelect:
                    emit(new IteNode(op1(), op2(), op3()));
                    break;
                case LLVMOpcode.LLVMSExt:
                    var sxTy = inst.TypeOf;
                    var srcTy = inst.GetOperand(0).TypeOf;
                    emit(new SxNode(new IntegerNode(sxTy.IntWidth - srcTy.IntWidth, sxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMZExt:
                    var zxTy = inst.TypeOf;
                    var srcZxTy = inst.GetOperand(0).TypeOf;
                    emit(new ZxNode(new IntegerNode(zxTy.IntWidth - srcZxTy.IntWidth, zxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMLoad:
                    var intWidth = value.TypeOf.IntWidth;
                    var temp = createTemp(intWidth, value.ToString().Split(" = ")[0].Replace(" ", ""));
                    emit(temp);
                    break;
                case LLVMOpcode.LLVMPHI:
                    // Here is where things get REALLY complicated. When we encounter a phi node, 
                    // we must represent the value as set of nested ITEs, which select the value
                    // depending on the incoming branch.
                    // To determine which branch we came from, we use the set of path constraints collected
                    // from the predecessor block.
                    var phiWidth = value.TypeOf.IntWidth;
                    var phiTemp = createTemp(phiWidth, value.ToString().Split(" = ")[0].Replace(" ", ""));
                    emit(phiTemp);
                    break;
                default:
                    throw new InvalidOperationException($"Failed to translate LLVM inst {inst} to AST. The OPCode is not supported.");
            }

            return definitionMapping[value];
        }

        private static CondType GetCondType(LLVMIntPredicate type)
        {
            return type switch
            {
                LLVMIntPredicate.LLVMIntEQ => CondType.Eq,
                LLVMIntPredicate.LLVMIntSGE => CondType.Sge,
                LLVMIntPredicate.LLVMIntSGT => CondType.Sgt,
                LLVMIntPredicate.LLVMIntSLE => CondType.Sle,
                LLVMIntPredicate.LLVMIntSLT => CondType.Slt,
                LLVMIntPredicate.LLVMIntUGE => CondType.Uge,
                LLVMIntPredicate.LLVMIntUGT => CondType.Ugt,
                LLVMIntPredicate.LLVMIntULE => CondType.Ule,
                LLVMIntPredicate.LLVMIntULT => CondType.Ult,
                _ => throw new InvalidOperationException($"Cond type {type} cannot be converted to smtlib cond.")
            };
        }
    }
}
