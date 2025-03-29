#define NOTGAMBA

using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;

namespace Dna.Passes
{
    /// <summary>
    /// Class representing a lifted AST representation of an LLVM IR value.
    /// </summary>
    /// <param name="LlvmValue">The input LLVM IR instruction.</param>
    /// <param name="Ast">A mutable AST representation of the chain of instructions that contribute to the IR instruction.</param>
    /// <param name="SubstitutionMapping">A mapping between LLVM IR values and substituted temporary variables. In the case of unsupported instructions or parameters, 
    /// the unsupported items are replaced with temporary variables.
    /// </param>
    public record LLVMAstWithSubstitutions(LLVMValueRef LlvmValue, AbstractNode Ast, IReadOnlyDictionary<LLVMValueRef, TemporaryNode> SubstitutionMapping);

    public class LLVMToTritonAst
    {
        private int tempCount = 0;

        private readonly AstContext astCtx;

        private readonly LLVMValueRef function;

        // If we are building up an AST and encounter an unsupported instruction(or a function argument),
        // the unsupported llvm value is substituted with a temporary variable.
        public readonly Dictionary<LLVMValueRef, TemporaryNode> SubstitutionMapping = new();

        public Dictionary<LLVMValueRef, AbstractNode> definitionMapping = new();

        /*
        public LLVMAstWithSubstitutions GetAst(AstContext astCtx, LLVMValueRef function, LLVMValueRef value)
        {
            var lifter = new LLVMToTritonAst(astCtx, function);
            var ast = lifter.GetAst(value);
            return new LLVMAstWithSubstitutions(value, ast, lifter.SubstitutionMapping);
        }
        */

        public LLVMToTritonAst(AstContext astCtx, LLVMValueRef function)
        {
            this.astCtx = astCtx;
            this.function = function;
        }

        public AbstractNode GetAst(LLVMValueRef value)
        {
            // If we already have a placeholder variable to represent this LLVM value, then return it.
            if (SubstitutionMapping.ContainsKey(value))
                return SubstitutionMapping[value];
            if(definitionMapping.ContainsKey(value))
                return definitionMapping[value];


            // If it's an argument, then create a new symbolic definition mapping.
            if (value.Kind == LLVMValueKind.LLVMArgumentValueKind)
            {
                var argumentAst = GetArgumentAst(value);
                SubstitutionMapping[value] = argumentAst;
                definitionMapping.TryAdd(value, argumentAst);
                return argumentAst;
            }

            // If it's a llvm::ConstantInt then lift it into an IntegerNode ast.
            if (value.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                var constAst = GetConstantIntAst(value);
                definitionMapping.TryAdd(value, constAst);
                return constAst;
            }

            if (value.Kind == LLVMValueKind.LLVMConstantPointerNullValueKind)
            {
                // LLVM has a special value type for nullptrs. When we encounter this,
                // we convert it to a constant integer of zero. 
                var nullPtr = astCtx.bv(0, 64);
                definitionMapping.TryAdd(value, nullPtr);
                return nullPtr;
            }

            if (value.Kind == LLVMValueKind.LLVMInstructionValueKind)
            {
                var instAst = GetInstructionAst(value);
                definitionMapping.TryAdd(value, instAst);
                return instAst;
            }

            throw new InvalidOperationException($"Unsupported value kind: {value.Kind} for value {value}");
        }

        // Return an ast for an LLVM function argument.
        private TemporaryNode GetArgumentAst(LLVMValueRef value)
        {
            var type = value.TypeOf.Kind;
            var intWidth = GetIntWidthForValue(value);
            var name = "%" + value.ToString().Split("%")[1];
            var temp = CreateTemp(intWidth, name);
            return temp;
        }

        private IntegerNode GetConstantIntAst(LLVMValueRef value)
        {
            if (value.TypeOf.IntWidth > 64)
                throw new InvalidOperationException($"Constant ints of width greater than 64 are not supported.");

            /*
            var constInt = astCtx.bv(value.ConstIntZExt, value.TypeOf.IntWidth);
            return constInt;
            */

            return new IntegerNode(value.ConstIntZExt, value.TypeOf.IntWidth);
        }

        private AbstractNode GetInstructionAst(LLVMValueRef value)
        {
            // TODO: Delete this. This is an artifact from porting over old code into this project.
            AbstractNode result = null;
            var emit = (AbstractNode node) =>
            {
                if (result is not null)
                    throw new InvalidOperationException($"Cannot assign multiple definitions to instruction {value}");
                result = node;
                definitionMapping.TryAdd(value, result);
            };

            var op1 = () =>
            {
                var val = GetAst(value.GetOperand(0));
                return val;
            };

            var op2 = () =>
            {
                var val = GetAst(value.GetOperand(1));
                return val;
            };

            var op3 = () =>
            {
                var val = GetAst(value.GetOperand(2));
                return val;
            };

            var inst = value;
            switch (inst.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                    emit(astCtx.bvadd(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMAnd:
                    emit(astCtx.bvand(op1(), op2()));
                    break;

#if NOTGAMBA
                case LLVMOpcode.LLVMAShr:
                    emit(astCtx.bvashr(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMLShr:
                    emit(astCtx.bvlshr(op1(), op2()));
                    break;
#endif
                case LLVMOpcode.LLVMMul:
                    emit(astCtx.bvmul(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMOr:
                    emit(astCtx.bvor(op1(), op2()));
                    break;

#if NOTGAMBA
                case LLVMOpcode.LLVMSDiv:
                    emit(astCtx.bvsdiv(op1(), op2()));
                    break;
#endif
                case LLVMOpcode.LLVMShl:
                    emit(astCtx.bvshl(op1(), op2()));
                    break;
#if NOTGAMBA
                case LLVMOpcode.LLVMICmp:
                    // Note: Since SMTLIB2 does not have an `NEQ` type node,
                    // we need return a negated equals.
                    if (inst.ICmpPredicate == LLVMIntPredicate.LLVMIntNE)
                    {
                        emit(astCtx.bvnot(astCtx.equal(op1(), op2())));
                        break;
                    }

                    var predicate = GetCondType(inst.ICmpPredicate);
                    AbstractNode cmp = predicate switch
                    {
                        CondType.Eq => astCtx.equal(op1(), op2()),
                        CondType.Sge => astCtx.bvsge(op1(), op2()),
                        CondType.Sgt => astCtx.bvsgt(op1(), op2()),
                        CondType.Sle => astCtx.bvsle(op1(), op2()),
                        //CondType.Slt => astCtx.bvslt(op1(), op2()),
                        CondType.Uge => astCtx.bvuge(op1(), op2()),
                        CondType.Ugt => astCtx.bvugt(op1(), op2()),
                        CondType.Ule => astCtx.bvule(op1(), op2()),
                        //CondType.Ult => astCtx.bvult(op1(), op2()),
                        _ => throw new InvalidOperationException(string.Format("CondType {0} is not valid.", predicate))
                    };

                    emit(cmp);
                    break;
                case LLVMOpcode.LLVMSRem:
                    emit(astCtx.bvurem(op1(), op2()));
                    break;
#endif

                case LLVMOpcode.LLVMSub:
                    emit(astCtx.bvsub(op1(), op2()));
                    break;
#if NOTGAMBA
                case LLVMOpcode.LLVMUDiv:
                    emit(astCtx.bvudiv(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMURem:
                    emit(astCtx.bvurem(op1(), op2()));
                    break;
#endif
                case LLVMOpcode.LLVMXor:
                    // Check for (x ^ -1) which is actually is actually just ~x.
                    var xorBy = inst.GetOperand(1);
                    if (xorBy.Kind == LLVMValueKind.LLVMConstantIntValueKind && xorBy.ConstIntZExt == (ulong.MaxValue & ModuloReducer.GetMask(inst.TypeOf.IntWidth)))
                    {
                        emit(astCtx.bvnot(op1()));
                        break;
                    }

                    emit(astCtx.bvxor(op1(), op2()));
                    break;
                case LLVMOpcode.LLVMTrunc:
                    // If we are truncating an i128 down to an i16 for example, symbolize the whole thing.
                    if (inst.GetOperand(0).TypeOf.IntWidth > 64)
                    {
                        var intWidth2 = GetIntWidthForValue(inst);
                        var name2 = value.ToString().Split(" = ")[0].Replace(" ", "");
                        var temp2 = CreateTemp(intWidth2, name2);
                        SubstitutionMapping.Add(value, temp2);
                        emit(temp2);
                        break;
                    }

                    var destTy = inst.TypeOf;
                    emit(astCtx.extract(destTy.IntWidth - 1u, 0, op1()));
                    break;
#if NOTGAMBA
                case LLVMOpcode.LLVMSelect:
                    emit(astCtx.ite(op1(), op2(), op3()));
                    break;
#endif
                case LLVMOpcode.LLVMSExt:
                    var sxTy = inst.TypeOf;
                    var srcTy = inst.GetOperand(0).TypeOf;
                    emit(astCtx.sx(astCtx.bv(sxTy.IntWidth - srcTy.IntWidth, sxTy.IntWidth), op1()));
                    break;
                case LLVMOpcode.LLVMZExt:
                    var zxTy = inst.TypeOf;
                    var srcZxTy = inst.GetOperand(0).TypeOf;
                    emit(astCtx.zx(astCtx.bv(zxTy.IntWidth - srcZxTy.IntWidth, zxTy.IntWidth), op1()));
                    break;
                // If the value is a load inst, create a substitution variable to represent it.
                // Do the same thing with phi nodes and function calls.
                case LLVMOpcode.LLVMLoad:
                case LLVMOpcode.LLVMPHI:
                case LLVMOpcode.LLVMCall:
                case LLVMOpcode.LLVMGetElementPtr:
                case LLVMOpcode.LLVMAlloca:
                default:
                    var intWidth = GetIntWidthForValue(inst);
                    var name = value.ToString().Split(" = ")[0].Replace(" ", "");
                    var temp = CreateTemp(intWidth, name);
                    SubstitutionMapping.Add(value, temp);
                    emit(temp);
                    break;
                case LLVMOpcode.LLVMFreeze:
                    if (inst.OperandCount > 1)
                        throw new InvalidOperationException($"Freeze inst {inst} has more than one operand.");
                    emit(op1());
                    break;
                /*
                case LLVMOpcode.LLVMPtrToInt:
                    var res = op1();
                    emit(res);
                    break;
                case LLVMOpcode.LLVMIntToPtr:
                    // TODO: Zero extend if the integer is not 64 bits.
                    if (inst.GetOperand(0).TypeOf.IntWidth != 64)
                        throw new InvalidOperationException($"Encountered inttoptr {inst} with width {inst.TypeOf.IntWidth}. Expected size 64.");

                    emit(op1());
                    break;
                */
            }

            return result;
        }

        private CondType GetCondType(LLVMIntPredicate type)
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

        private uint GetIntWidthForValue(LLVMValueRef loadInst)
        {
            var kind = loadInst.TypeOf.Kind;
            if (kind == LLVMTypeKind.LLVMIntegerTypeKind)
                return loadInst.TypeOf.IntWidth;
            // NOTE: This assumption is only safe on 64 bit code.
            if (kind == LLVMTypeKind.LLVMPointerTypeKind)
                return 64;
            else
                throw new InvalidOperationException($"Unsupported load result type: {kind} for load inst {loadInst}");
        }

        // Create a temporary node of the specified name and bit width.
        private TemporaryNode CreateTemp(uint bitWidth, string name = null)
        {
            if (name == "")
                Debugger.Break();
            if (name == null)
                name = $"t{tempCount}";

            var oldTempCount = tempCount;
            // Increase the temp count by two to allow a single slot to represent zero extended temporary variables.
            tempCount += 2;
            return new TemporaryNode((uint)tempCount, bitWidth, name);
            //return astCtx.temporary((uint)tempCount, bitWidth, name);
        }
    }
}
