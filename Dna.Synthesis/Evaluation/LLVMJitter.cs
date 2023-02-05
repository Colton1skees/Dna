using Dna.Synthesis.Miasm;
using Dna.Synthesis.Utils;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Jit
{
    public class LLVMJitter
    {
        private readonly LLVMModuleRef module = LLVM.ModuleCreateWithName("TritonTranslator");

        private readonly LLVMBuilderRef builder = LLVM.CreateBuilder();

        private Dictionary<ExprId, LLVMValueRef> argumentMapping = new();

        private int functionCount;

        public LLVMJitter()
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            if (LLVM.CreateExecutionEngineForModule(out var engine, module, out var errorMessage).Value == 1)
            {
                throw new Exception(errorMessage);
            }
        }

        public (LLVMValueRef functionPointer, Dictionary<ExprId, LLVMValueRef> argMapping) LiftAst(MiasmExpr expr, IEnumerable<ExprId> inputVariables)
        {
            // Create integer arguments for each input variable.
            var argumentTypes = inputVariables.Select(x => LLVM.IntType(x.Size)).ToArray();

            // Create a function which evaluates to a single integer of the expression size.
            var prototype = LLVM.FunctionType(LLVM.IntType(expr.Size), argumentTypes, false);
            var function = LLVM.AddFunction(module, "Expr" + functionCount, prototype);
            
            // Create a single block and position the builder to emit instructions for this block.
            var block = LLVM.AppendBasicBlock(function, "entry");
            LLVM.PositionBuilderAtEnd(builder, block);

            // Maintain a mapping of <inputVariable, llvmParam>.
            argumentMapping.Clear();
            var arguments = LLVM.GetParams(function);
            int i = 0;
            foreach (var argument in inputVariables)
            {
                argumentMapping.Add(argument, arguments[i]);
                i++;
            }

            Translate(expr);

            // Iterate the function count so that each function gets a unique ID.
            functionCount++;

            return (function, argumentMapping.ToDictionary(x => x.Key, x => x.Value));
        }

        private LLVMValueRef Translate(MiasmExpr expr)
        {
            switch(expr)
            {
                case ExprId exprId:
                    return FromExprId(exprId);
                case ExprInt exprInt:
                    return FromExprInt(exprInt);
                case ExprSlice exprSlice:
                    return FromExprSlice(exprSlice);
                case ExprOp exprOp:
                    return FromExprOp(exprOp);
                default:
                    throw new NotImplementedException();
            }
        }

        private LLVMValueRef FromExprId(ExprId exprId)
        {
            return argumentMapping[exprId];
        }

        private LLVMValueRef FromExprInt(ExprInt exprInt)
        {
            var intType = LLVM.IntType(exprInt.Size);
            return LLVM.ConstInt(intType, exprInt.Value, new LLVMBool(0));
        }

        private LLVMValueRef FromExprSlice(ExprSlice exprSlice)
        {
            // Translate the source expression to LLVM IR.
            var src = Translate(exprSlice.Src);

            // Remove trailing bits.
            var sliceSize = LLVM.IntType(exprSlice.Src.Size);
            LLVMValueRef? shifted = null;
            if (exprSlice.Start != 0)
            {
                var toShr = LLVM.ConstInt(sliceSize, exprSlice.Start, new LLVMBool(0));
                shifted = LLVM.BuildLShr(builder, src, toShr, "shifted");
            }

            else
            {
                shifted = src;
            }

            // Remove leading bits.
            var shiftSize = (1u << (int)(exprSlice.Stop - exprSlice.Start)) - 1;
            var toAnd = LLVM.ConstInt(sliceSize, shiftSize, new LLVMBool(0));
            var anded = LLVM.BuildAnd(builder, shifted.Value, toAnd, "anded");

            return LLVM.BuildTrunc(builder, anded, LLVM.IntType(exprSlice.Size), "sliced");
        }

        private LLVMValueRef FromExprOp(ExprOp expr)
        {
            switch(expr.Op)
            {
                case "-":
                    if (expr.Operands.Count != 1)
                        throw new InvalidOperationException();
                    var type = LLVM.IntType(expr.Size);
                    var zero = LLVM.ConstInt(type, 0, new LLVMBool(0));
                    return LLVM.BuildSub(builder, zero, Translate(expr.Operands[0]), "Sub");
                case "*":
                    return LLVM.BuildMul(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Mul");
                case "+":
                    return LLVM.BuildAdd(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Add");
                case "&":
                    return LLVM.BuildAnd(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "And");
                case "|":
                    return LLVM.BuildOr(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Or");
                case "^":
                    return LLVM.BuildXor(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Xor");
                case "<<":
                    return LLVM.BuildShl(builder, Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Shl");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
