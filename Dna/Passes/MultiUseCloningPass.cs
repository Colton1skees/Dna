using Dna.Binary;
using Dna.ControlFlow.Extensions;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.Passes
{
    public class MultiUseCloningPass
    {
        private readonly bool debug = false;

        public dgMultiUseCloningPass PtrToStoreLoadPropagation { get; }

        public unsafe MultiUseCloningPass()
        {
            PtrToStoreLoadPropagation = new dgMultiUseCloningPass(MultiUseCloning);
        }

        private unsafe bool MultiUseCloning(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            return Run(function);
        }

        public static bool Run(LLVMValueRef function)
        {
            Console.WriteLine($"Running multi-use cloning pass on {function.Name}...");

            var builder = LLVMBuilderRef.Create(LLVMContextRef.Global);

            // Get all LLVM instructions that should be cloned.
            var instructions = GetInstructionsToClone(function, function.GlobalParent);

            // Return if there are no instructions to process.
            if (!instructions.Any())
                return false;

            // Create an LLVM IR -> ast translator.
            var astCtx = new AstContext();
            var llvmToTriton = new LLVMToTritonAst(astCtx, function);

            // Build an AST for each supported LLVM instruction.
            List<(LLVMValueRef From, LLVMValueRef To)> toReplace = new();
            foreach(var inst in instructions)
            {
                // Build an ast for the instruction.
                var ast = llvmToTriton.GetAst(inst);

                // TODO: Re-enable size check when the ast size change is pushed up to master
                Debugger.Break();
                /*
                if (ast.astSize > 2000)
                    continue;
                */

                // Create the inverse substitution mapping.
                var inverseSubstitutionMapping = new Dictionary<TemporaryNode, LLVMValueRef>();
                foreach ((LLVMValueRef llvmValue, TemporaryNode tempNode) in llvmToTriton.SubstitutionMapping)
                {
                    if (inverseSubstitutionMapping.TryGetValue(tempNode, out LLVMValueRef inverse))
                    {
                        Debug.Assert(inverse == llvmValue);
                        continue;
                    }

                    inverseSubstitutionMapping.Add(tempNode, llvmValue);
                }

                // Lower the AST back into LLVM IR.
                builder.PositionBefore(inst);
                var lowered = ToLlvm(new(), inverseSubstitutionMapping, builder, ast);

                toReplace.Add((inst, lowered));
            }
            
            foreach (var (before, after) in toReplace)
            {
                before.ReplaceAllUsesWith(after);
            }

            Console.WriteLine($"  -> replaced {toReplace.Count} instructions.");

            /*
            // Replace the instruction.
            foreach (var (before, after) in replacementMapping)
            {
                Console.WriteLine($"{before} -> {after}");
                before.ReplaceAllUsesWith(after);
            }
            */

            return toReplace.Count > 0;
        }

        private static bool ShouldCloneInstructionDefinition(LLVMValueRef inst)
        {
            if (inst.ToString().Contains("%new_val.0.i335.i.i = "))
                return true;
            if (inst.ToString().Contains("%253 = add nuw nsw i64 %252, 5368729271"))
                return true;
            if (inst.ToString().Contains("%add.i.i418.i.i = "))
                return true;
            if (inst.ToString().Contains("%add.i.i256.i.i10475 = add i64 %224, %sub.i.i.i114.i.i"))
                return true;
            if (inst.ToString().Contains("%193 = add nuw nsw i64 %192, 5368729271"))
                return true;
            if (inst.ToString().Contains("%185 = add nuw nsw i64 %184, 4294967295"))
                return true;
            if (inst.ToString().Contains("%185 = add nuw nsw i64 %184, 4294967295"))
                return true;
            if (inst.ToString().Contains("%181 = add nuw nsw i64 %180, 4294967295"))
                return true;

            if (inst.InstructionOpcode == LLVMOpcode.LLVMStore)
                return false;
            if (inst.InstructionOpcode == LLVMOpcode.LLVMCall)
                return false;
            if (inst.NextInstruction == null)
                return false;
            if (inst.TypeOf.IntWidth > 64)
                return false;
            // Skip any i128 operands.
            if (inst.GetOperands().Any(x => x.TypeOf.IntWidth > 64))
                return false;
            if (inst.TypeOf.Kind == LLVMTypeKind.LLVMVectorTypeKind)
                return false;

            // TODO: Heuristically check knownbits and the number of users.
            return false;
        }

        public static LLVMValueRef ToLlvm(Dictionary<AbstractNode, LLVMValueRef> cache, IReadOnlyDictionary<TemporaryNode, LLVMValueRef> substitutionMapping, LLVMBuilderRef builder, AbstractNode expr)
        {
            // If we've already lowered this subtree earlier, return it.
            if (cache.TryGetValue(expr, out LLVMValueRef cached))
                return cached;

            // Concise operand AST getter methods.
            var op1 = () => ToLlvm(cache, substitutionMapping, builder, expr.Children[0]);
            var op2 = () => ToLlvm(cache, substitutionMapping, builder, expr.Children[1]);
            var op3 = () => ToLlvm(cache, substitutionMapping, builder, expr.Children[2]);

            LLVMValueRef result = expr switch
            {
                IntegerNode intNode => LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(expr.BitSize), intNode.Value),
                TemporaryNode tempNode => substitutionMapping[tempNode],
                BvaddNode => builder.BuildAdd(op1(), op2()),
                BvsubNode => builder.BuildSub(op1(), op2()),
                BvmulNode => builder.BuildMul(op1(), op2()),
                BvandNode => builder.BuildAnd(op1(), op2()),
                BvorNode => builder.BuildOr(op1(), op2()),
                BvxorNode => builder.BuildXor(op1(), op2()),
                BvnotNode => builder.BuildNot(op1()),
                BvashrNode => builder.BuildAShr(op1(), op2()),
                BvlshrNode => builder.BuildLShr(op1(), op2()),
                BvshlNode => builder.BuildShl(op1(), op2()),
                ZxNode zxNode => builder.BuildZExt(op2(), LLVMTypeRef.CreateInt(zxNode.BitSize)), // Delete this! ZeroExt and all other uncommon operations should be removed by the 'TritonAstToRisc' class.
                SxNode sxNode => builder.BuildSExt(op2(), LLVMTypeRef.CreateInt(sxNode.BitSize)), // Delete this! ZeroExt and all other uncommon operations should be removed by the 'TritonAstToRisc' class.
                IteNode iteNode => builder.BuildSelect(op1(), op2(), op3()),
                BvultNode ultNode => builder.BuildICmp(LLVMIntPredicate.LLVMIntULT, op1(), op2()),
                BvsltNode sltNode => builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, op1(), op2()),
                BvugtNode bvugtNode => builder.BuildICmp(LLVMIntPredicate.LLVMIntUGT, op1(), op2()),
                ExtractNode extractNode => LowerExtract(cache, substitutionMapping, builder, extractNode),
                BvuremNode uremNode => builder.BuildURem(op1(), op2()),
                BvudivNode uremNode => builder.BuildUDiv(op1(), op2()),
                BvsdivNode sdivNode => builder.BuildSDiv(op1(), op2()),
                EqualNode => builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, op1(), op2()),
                _ => throw new InvalidOperationException($"Cannot lift ast kind: {expr.Type} to llvm.")

            };

            cache.Add(expr, result);
            return result;
        }

        private static LLVMValueRef LowerExtract(Dictionary<AbstractNode, LLVMValueRef> cache, IReadOnlyDictionary<TemporaryNode, LLVMValueRef> substitutionMapping, LLVMBuilderRef builder, ExtractNode inst)
        {
            // Concise operand AST getter methods.
            var op1 = () => ToLlvm(cache, substitutionMapping, builder, inst.Children[0]);
            var op2 = () => ToLlvm(cache, substitutionMapping, builder, inst.Children[1]);
            var op3 = () => ToLlvm(cache, substitutionMapping, builder, inst.Children[2]);

            var low = inst.Low;
            var value = op3();
            var intType = LLVMTypeRef.CreateInt(inst.BitSize);

            if (low.Value == 0)
            {
                var truncated = builder.BuildTrunc(value, intType);
                return truncated;
            }

            var shifted = builder.BuildLShr(value, op2());
            var result = builder.BuildTrunc(shifted, intType);
            return result;
        }

        public static bool IsValidInstrOpcode(LLVMOpcode opcode)
        {
            switch (opcode)
            {
                case LLVMOpcode.LLVMAdd:
                case LLVMOpcode.LLVMSub:
                case LLVMOpcode.LLVMMul:
                case LLVMOpcode.LLVMAnd:
                case LLVMOpcode.LLVMOr:
                case LLVMOpcode.LLVMXor:
                case LLVMOpcode.LLVMAShr:
                case LLVMOpcode.LLVMLShr:
                case LLVMOpcode.LLVMShl:
                case LLVMOpcode.LLVMICmp:
                case LLVMOpcode.LLVMExtractElement:
                case LLVMOpcode.LLVMExtractValue:
                case LLVMOpcode.LLVMSelect:
                case LLVMOpcode.LLVMSExt:
                case LLVMOpcode.LLVMZExt:
                    return true;
            }

            return false;
        }

        public static bool TryDFS(LLVMValueRef instr, int depth, HashSet<LLVMValueRef> visited)
        {
            if (!visited.Add(instr))
            {
                return false;
            }

            if (depth == 0)
            {
                return true;
            }

            foreach (var op in instr.GetOperands().Where(op => op.Kind == LLVMValueKind.LLVMInstructionValueKind))
            {
                if (IsValidInstrOpcode(op.InstructionOpcode) && TryDFS(op, depth - 1, visited))
                    return true;
            }

            return false;
        }

        public static List<LLVMValueRef> GetInstructionsToClone(LLVMValueRef func, LLVMModuleRef module)
        {
            HashSet<LLVMValueRef> toClone = new();
            
            foreach (var inst in func.GetInstructions())
            {

                HashSet<LLVMValueRef> ops = new();
                switch (inst.InstructionOpcode)
                {
                    case LLVMOpcode.LLVMGetElementPtr:
                        ops.Add(inst.GetOperand(1));
                        break;

                    case LLVMOpcode.LLVMCall:
                        {
                            if (inst.GetOperands().Last().Name != "vmp_maybe_jump")
                                continue;

                            ops.AddRange(inst.GetOperands().SkipLast(1));
                            break;
                        }

                    default:
                        continue;
                }

                foreach (var op in ops)
                {
                    if (IsValidInstrOpcode(op.InstructionOpcode) && op.Kind == LLVMValueKind.LLVMInstructionValueKind)
                    {
                        /*
                       var kb = NativeKnownBits.Get(op, module);
                       if (kb.GetUnknownBitCount() != 1)
                           continue;

                        if (TryDFS(op, 25, new()))
                            continue;
                       */

                        toClone.Add(op);
                    }
                }
            }

            return toClone.ToList();
        }
    }
}
