using Dna.Synthesis.Miasm;
using Dna.Synthesis.Utils;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Jit
{
    public class LLVMJitter
    {
        private readonly LLVMModuleRef Module = LLVMModuleRef.CreateWithName("TritonTranslator");

        private readonly LLVMBuilderRef builder;

        private Dictionary<ExprId, LLVMValueRef> argumentMapping = new();

        private int functionCount;

        public LLVMJitter()
        {
            builder = Module.Context.CreateBuilder();
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            var engine = Module.CreateExecutionEngine();
        }

        public (LLVMValueRef functionPointer, Dictionary<ExprId, LLVMValueRef> argMapping) LiftAst(MiasmExpr expr, IEnumerable<ExprId> inputVariables)
        {
            // Create integer arguments for each input variable.
            var argumentTypes = inputVariables.Select(x => LLVMTypeRef.CreateInt(x.Size)).ToArray();

            // Create a function which evaluates to a single integer of the expression size.
            var prototype = LLVMTypeRef.CreateFunction(LLVMTypeRef.CreateInt(expr.Size), argumentTypes, false);
            var function = Module.AddFunction("Expr" + functionCount, prototype);
            
            // Create a single block and position the builder to emit instructions for this block.
            var block = function.AppendBasicBlock("entry");
            builder.PositionAtEnd(block);

            // Maintain a mapping of <inputVariable, llvmParam>.
            argumentMapping.Clear();
            var arguments = function.GetParams();
            int i = 0;
            foreach (var inputVariable in inputVariables)
            {
                argumentMapping.Add(inputVariable, arguments[i]);
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
            var intType = LLVMTypeRef.CreateInt(exprInt.Size);
            return LLVMValueRef.CreateConstInt(intType, exprInt.Value, false);
        }

        private LLVMValueRef FromExprSlice(ExprSlice exprSlice)
        {
            // Translate the source expression to LLVM IR.
            var src = Translate(exprSlice.Src);

            // Remove trailing bits.
            var sliceSize = LLVMTypeRef.CreateInt(exprSlice.Src.Size);
            LLVMValueRef? shifted = null;
            if (exprSlice.Start != 0)
            {
                var toShr = LLVMValueRef.CreateConstInt(sliceSize, exprSlice.Start, false);
                shifted = builder.BuildLShr(src, toShr, "shifted");
            }

            else
            {
                shifted = src;
            }

            // Remove leading bits.
            var shiftSize = (1u << (int)(exprSlice.Stop - exprSlice.Start)) - 1;
            var toAnd = LLVMValueRef.CreateConstInt(sliceSize, shiftSize, false);
            var anded = builder.BuildAnd(shifted.Value, toAnd, "anded");

            return builder.BuildTrunc(anded, LLVMTypeRef.CreateInt(exprSlice.Size), "sliced");
        }

        private LLVMValueRef FromExprOp(ExprOp expr)
        {
            switch(expr.Op)
            {
                case "-":
                    if (expr.Operands.Count != 1)
                        throw new InvalidOperationException();
                    var type = LLVMTypeRef.CreateInt(expr.Size);
                    var zero = LLVMValueRef.CreateConstInt(type, 0, false);
                    return builder.BuildSub(zero, Translate(expr.Operands[0]), "Sub");
                case "*":
                    return builder.BuildMul(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Mul");
                case "+":
                    return builder.BuildAdd(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Add");
                case "&":
                    return builder.BuildAnd(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "And");
                case "|":
                    return builder.BuildOr(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Or");
                case "^":
                    return builder.BuildXor(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Xor");
                case "<<":
                    return builder.BuildShl(Translate(expr.Operands[0]), Translate(expr.Operands[1]), "Shl");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
