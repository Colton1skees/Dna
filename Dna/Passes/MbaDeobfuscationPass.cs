using Dna.Extensions;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp.Interop;
using Mba.Simplifier.Bindings;
using Mba.Simplifier.Pipeline;
using Mba.Utility;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes
{
    record struct ValueWithSimplification(LLVMValueRef Inst, AstIdx Idx);

    /// <summary>
    /// This class implements a Mixed Boolean-Arithmetic simplification pass based on Simplifier (https://github.com/mazeworks-security/Simplifier)
    /// It makes no unsafe assumptions (e.g. by performing oracle synthesis or algebraic methods without validating the structure of the MBA).
    /// </summary>
    public class MbaDeobfuscationPass
    {
        private readonly LLVMValueRef function;

        private readonly AstCtx ctx;

        // Mapping of unsupported LLVM instructions to substituted values
        private Dictionary<LLVMValueRef, AstIdx> substMapping = new();

        private Dictionary<LLVMValueRef, AstIdx> defMap = new();

        public static void Run(LLVMValueRef function)
            => new MbaDeobfuscationPass(function).Run();

        private MbaDeobfuscationPass(LLVMValueRef function)
        {
            this.function = function;
            ctx = new AstCtx();
        }

        private void Run()
        {
            // Collect all instructions for deobfuscation.
            var targets = GetTargets(function).ToList();

            // Collect asts for each target
            var asts = targets.Select(x => new ValueWithSimplification(x, GetAst(x))).ToList();

            // Simplify the ASTs until a fixed-point or timeout is reached.
            SimplifyFixedPoint(asts);

            // Collect all pairs where we achieved either a simpler or equal result from the MBA simplifier.
            var profitablePairs = asts.Where(x => ctx.GetCost(x.Idx) <= ctx.GetCost(defMap[x.Inst])).ToList();

            // Update the definition of each instruction with it's simplification.
            foreach(var pair in profitablePairs)
            {
                defMap[pair.Inst] = pair.Idx;
            }

            // Lower all of the simplifications back down to LLVM IR, then replace the old instructions.
            // TODO: We are currently duplicating some code when there is node sharing among two different simplification candidates.
            // We should use the dominator tree to avoid this duplication when possible!
            var builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
            var invSubstMapping = substMapping.ToDictionary(x => x.Value, x => x.Key);
            foreach(var pair in profitablePairs)
            {
                builder.PositionBefore(pair.Inst);
                var lowered = Lower(ctx, builder, pair.Idx, invSubstMapping, new());
                if (lowered == pair.Inst)
                    continue;

                pair.Inst.ReplaceAllUsesWith(lowered);
            }
        }

        private static IReadOnlySet<LLVMValueRef> GetTargets(LLVMValueRef function)
        {
            // Collect all load / store / call instructions
            var candUsers = function
                .GetInstructions()
                .Where(x => IsTarget(x.InstructionOpcode));

            // Collect all integer instructions of width <= 64
            var targets = new HashSet<LLVMValueRef>();
            foreach (var user in candUsers)
            {
                foreach (var operand in user.GetOperands())
                {
                    if (operand.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                        continue;
                    if (operand.Kind != LLVMValueKind.LLVMInstructionValueKind)
                        continue;
                    if (operand.TypeOf.IntWidth > 64)
                        continue;
                    targets.Add(operand);
                }
            }

            return targets;
        }

        private static bool IsTarget(LLVMOpcode opcode)
        {
            switch(opcode)
            {
                case LLVMOpcode.LLVMLoad:
                case LLVMOpcode.LLVMStore:
                case LLVMOpcode.LLVMCall:
                case LLVMOpcode.LLVMZExt:
                case LLVMOpcode.LLVMSExt:
                case LLVMOpcode.LLVMGetElementPtr:
                    return true;

                default:
                    return false;
            }
        }
        
        private AstIdx GetAst(LLVMValueRef value)
        {
            // Only create the definition of an LLVM instruction once
            // This makes AST construction linear time for the whole function.
            if (substMapping.TryGetValue(value, out var subst))
                return subst;
            if (defMap.TryGetValue(value, out var existing))
                return existing;

            // Convert special cases(anything that is not an instruction)
            if (value.Kind == LLVMValueKind.LLVMArgumentValueKind)
                return GetArgAst(value);
            if (value.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                return GetConstIntAst(value);

            // We only concern ourselves with integer operations over variables and integer values
            // E.g. global variables and pointers should never be encountered in our ast traversal!!
            if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
                throw new InvalidOperationException($"Unsupported value kind: {value.Kind}!");

            return GetInstructionAst(value);
        }

        private AstIdx GetArgAst(LLVMValueRef value)
        {
            Debug.Assert(value.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind);
            var name = $"arg{function.GetParams().IndexOf(value)}";
            var ast = ctx.Symbol(name, (byte)value.TypeOf.IntWidth);
            defMap[value] = ast;
            substMapping[value] = ast;
            return ast;
        }

        private AstIdx GetConstIntAst(LLVMValueRef value)
        {
            var constDef = ctx.Constant(value.ConstIntZExt, (byte)value.TypeOf.IntWidth);
            defMap[value] = constDef;
            return constDef;
        }

        private AstIdx GetInstructionAst(LLVMValueRef inst)
        {
            if (inst.Kind != LLVMValueKind.LLVMInstructionValueKind)
                throw new InvalidOperationException($"{inst} is not an instruction!");

            var op1 = () =>
            {
                var val = GetAst(inst.GetOperand(0));
                return val;
            };

            var op2 = () =>
            {
                var val = GetAst(inst.GetOperand(1));
                return val;
            };

            var op3 = () =>
            {
                var val = GetAst(inst.GetOperand(2));
                return val;
            };

            AstIdx def = inst.InstructionOpcode switch
            {
                LLVMOpcode.LLVMAnd => ctx.And(op1(), op2()),
                LLVMOpcode.LLVMOr => ctx.Or(op1(), op2()),
                LLVMOpcode.LLVMXor => ctx.Xor(op1(), op2()),
                LLVMOpcode.LLVMShl => Shl(ctx, op1(), op2()),
                LLVMOpcode.LLVMSub => Sub(ctx, op1(), op2()),

                LLVMOpcode.LLVMAdd => ctx.Add(op1(), op2()),
                LLVMOpcode.LLVMMul => ctx.Mul(op1(), op2()),
                _ => GetUnsupportedInstruction(inst)
            };

            defMap[inst] = def;
            return def;
        }

        private AstIdx GetUnsupportedInstruction(LLVMValueRef inst)
        {
            var name = $"uns{substMapping.Count}";
            var def = ctx.Symbol(name, (byte)inst.TypeOf.IntWidth);
            substMapping[inst] = def;
            return def;
        }

        private static AstIdx Shl(AstCtx ctx, AstIdx a, AstIdx b)
        {
            return ctx.Mul(a, ctx.Pow(ctx.Constant(2, ctx.GetWidth(a)), b));
        }

        private static AstIdx Sub(AstCtx ctx, AstIdx a, AstIdx b)
        {
            return ctx.Add(a, ctx.Mul(b, ctx.Constant(ulong.MaxValue, ctx.GetWidth(b))));
        }

        private void SimplifyFixedPoint(List<ValueWithSimplification> pairs)
        {
            // Iteratively simplify each ast 
            var simplifier = new GeneralSimplifier(ctx);
            for (int count = 0; count < 5; count++)
            {
                bool changed = false;
                for (int i = 0; i < pairs.Count; i++)
                {
                    var pair = pairs[i];
                    // Simplify the AST
                    var simplified = simplifier.SimplifyGeneral(pair.Idx);

                    // Take note of whether anything changed
                    if (pairs[i].Idx != simplified)
                        changed = true;

                    // Save the most simple version
                    pairs[i] = new ValueWithSimplification(pair.Inst, simplified);
                }

                if (!changed)
                    break;
            }
        }

        // Recursively lower the AST instance to LLVM IR
        private static LLVMValueRef Lower(AstCtx ctx, LLVMBuilderRef builder, AstIdx idx, Dictionary<AstIdx, LLVMValueRef> substMapping, Dictionary<AstIdx, LLVMValueRef> cache)
        {
            var lower = (uint operand) =>
            {
                var val = Lower(ctx, builder, operand == 0 ? ctx.GetOp0(idx) : ctx.GetOp1(idx), substMapping, cache);
                return val;
            };

            if (cache.TryGetValue(idx, out var existing))
                return existing;
            if (substMapping.TryGetValue(idx, out var subst))
                return subst;

            var opcode = ctx.GetOpcode(idx);
            LLVMValueRef result = opcode switch
            {
                AstOp.Add => builder.BuildAdd(lower(0), lower(1)),
                AstOp.Mul => builder.BuildMul(lower(0), lower(1)),
                AstOp.And => builder.BuildAnd(lower(0), lower(1)),
                AstOp.Or => builder.BuildOr(lower(0), lower(1)),
                AstOp.Xor => builder.BuildXor(lower(0), lower(1)),
                AstOp.Neg => builder.BuildXor(lower(0), LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(ctx.GetWidth(idx)), ulong.MaxValue)), // TODO: Create TypeRef in context!
                AstOp.Constant => LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(ctx.GetWidth(idx)), ctx.GetConstantValue(idx)),
                _ => throw new InvalidOperationException($"Cannot lower opcode {opcode} to LLVM IR!"),
            };

            cache[idx] = result;
            return result;
        }
    }   
}
