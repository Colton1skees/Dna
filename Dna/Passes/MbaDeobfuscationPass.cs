using Antlr4.Runtime.Misc;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp;
using LLVMSharp.Interop;
using Mba.Common.Ast;
using Mba.Common.MSiMBA;
using Mba.Simplifier.Bindings;
using Mba.Simplifier.Interpreter;
using Mba.Simplifier.Pipeline;
using Mba.Simplifier.Utility;
using Mba.Utility;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes.Mba
{
    record struct ValueWithSimplification(LLVMValueRef Inst, AstIdx Idx);


    /// <summary>
    /// This class implements a Mixed Boolean-Arithmetic simplification pass based on Simplifier (https://github.com/mazeworks-security/Simplifier)
    /// It makes no unsafe assumptions (e.g. by performing oracle synthesis or algebraic methods without validating the structure of the MBA).
    /// </summary>
    public class MbaDeobfuscationPass
    {
        public static Dictionary<string, string> database = new();

        // Substantially improve the chance of finding simplifications, at the cost of unsoundness
        private const bool UNSOUND = true;

        private readonly LLVMValueRef function;

        //private readonly AstCtx ctx;
        public static readonly AstCtx ctx = new();

        // Mapping of unsupported LLVM instructions to substituted values
        private Dictionary<LLVMValueRef, AstIdx> substMapping = new();

        private Dictionary<LLVMValueRef, AstIdx> defMap = new();

        public static Dictionary<AstIdx, AstIdx> optimal = new();

        private readonly Random random = new();

        private readonly nint pagePtr1 = JitUtils.AllocateExecutablePage(1000);

        private readonly nint pagePtr2 = JitUtils.AllocateExecutablePage(1000);

        public static bool Run(LLVMValueRef function)
            => new MbaDeobfuscationPass(function).Run();

        private MbaDeobfuscationPass(LLVMValueRef function)
        {
            AstIdx.ctx = ctx;
            this.function = function;
            //ctx = new AstCtx();
        }

        private bool Run()
        {
            Console.WriteLine("Running mba deob pass");
            function.GlobalParent.PrintToFile("translatedFunction.ll");

            // Collect all instructions for deobfuscation.
            var targets = GetTargets(function).ToList();

            // Collect asts for each target
            var asts = targets.Select(x => new ValueWithSimplification(x, GetAst(x))).ToList();

            // Simplify the ASTs until a fixed-point or timeout is reached.
            SimplifyFixedPoint(asts);

            // Collect all pairs where we achieved either a simpler or equal result from the MBA simplifier.
            //var profitablePairs = asts.Where(x => ctx.GetCost(x.Idx) <= ctx.GetCost(defMap[x.Inst])).ToList();
            var profitablePairs = asts;

            // Update the definition of each instruction with it's simplification.
            foreach (var pair in profitablePairs)
            {
                defMap[pair.Inst] = pair.Idx;
            }

            bool changed = false;
            return changed;
            //return;

            // Lower all of the simplifications back down to LLVM IR, then replace the old instructions.
            // TODO: We are currently duplicating some code when there is node sharing among two different simplification candidates.
            // We should use the dominator tree to avoid this duplication when possible!
            var builder = LLVMBuilderRef.Create(function.GetFunctionCtx());
            var invSubstMapping = substMapping.ToDictionary(x => x.Value, x => x.Key);
            //var invSubstMapping = new Dictionary<AstIdx, LLVMValueRef>();
            //foreach (var (k, v) in substMapping)
            //{
            //    Console.WriteLine($"Adding: ({v}, {k})");
            //    invSubstMapping.Add(v, k);
            //}

            foreach (var pair in profitablePairs)
            {

                builder.PositionBefore(pair.Inst);
                var lowered = Lower(ctx, builder, pair.Idx, invSubstMapping, new());
                if (lowered == pair.Inst)
                    continue;

                pair.Inst.ReplaceAllUsesWith(lowered);
                changed = true;


                //invSubstMapping.Clear();
                //Console.WriteLine($"Replaced {pair.Idx}");
            }

       
            //function.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
            Console.WriteLine("Finished mba pass");
            return changed;
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
                    if (operand.InstructionOpcode == LLVMOpcode.LLVMTrunc && operand.TypeOf.IntWidth > 64)
                        continue;
                    targets.Add(operand);
                }
            }

            return targets;
        }

        private static bool IsTarget(LLVMOpcode opcode)
        {
            switch (opcode)
            {
                case LLVMOpcode.LLVMLoad:
                case LLVMOpcode.LLVMStore:
                case LLVMOpcode.LLVMCall:
                case LLVMOpcode.LLVMZExt:
                case LLVMOpcode.LLVMSExt:
                case LLVMOpcode.LLVMGetElementPtr:
                case LLVMOpcode.LLVMICmp:
                case LLVMOpcode.LLVMIntToPtr:
                case LLVMOpcode.LLVMPHI:
                case LLVMOpcode.LLVMOr:
                case LLVMOpcode.LLVMAnd:
                case LLVMOpcode.LLVMXor:
                case LLVMOpcode.LLVMAdd:
                case LLVMOpcode.LLVMSub:
                case LLVMOpcode.LLVMMul:
                case LLVMOpcode.LLVMSelect:
                case LLVMOpcode.LLVMShl:
                case LLVMOpcode.LLVMLShr:
                    return true;

                default:
                    return false;
            }
        }

        private HashSet<LLVMOpcode> uniqueMnemonics = new();
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
            //var name = $"arg{function.GetParams().IndexOf(value)}";
            //var name = value.Name.Replace("%", "");
            var name = $"arg{function.GetParams().IndexOf(value)}";
            var ast = ctx.Symbol(name, (byte)value.TypeOf.IntWidth);
            defMap[value] = ast;
            substMapping[value] = ast;

            //Console.WriteLine($"Var name: {value.Name} got {ast}");

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
                LLVMOpcode.LLVMLShr => ctx.Lshr(op1(), op2()),
                LLVMOpcode.LLVMSub => Sub(ctx, op1(), op2()),
                LLVMOpcode.LLVMZExt => ctx.Zext(op1(), (byte)inst.TypeOf.IntWidth),
                LLVMOpcode.LLVMTrunc => inst.GetOperand(0).TypeOf.IntWidth > 64 ? GetUnsupportedInstruction(inst) : ctx.Trunc(op1(), (byte)inst.TypeOf.IntWidth),
                LLVMOpcode.LLVMSelect => ctx.Select(op1(), op2(), op3()),
                LLVMOpcode.LLVMICmp => ctx.ICmp(ConvPredicate(inst.ICmpPredicate), op1(), op2()),
                LLVMOpcode.LLVMAdd => ctx.Add(op1(), op2()),
                LLVMOpcode.LLVMMul => ctx.Mul(op1(), op2()),
                _ => GetUnsupportedInstruction(inst)
            };

            defMap[inst] = def;
            return def;
        }

        private AstIdx GetUnsupportedInstruction(LLVMValueRef inst)
        {
            uniqueMnemonics.Add(inst.InstructionOpcode);
            var name = $"uns{substMapping.Count}";
            var def = ctx.Symbol(name, (byte)inst.TypeOf.IntWidth);

            var knownBits = NativeKnownBits.Get(inst, function.GlobalParent);

            if (knownBits.GetKnownBitCount() > 0)
            {
                var w = ctx.GetWidth(def);
                def = ctx.Or(ctx.Constant(knownBits.One, w), def);
                def = ctx.And(ctx.Constant(~knownBits.Zero, w), def);
            }

            

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

        private static Predicate ConvPredicate(LLVMIntPredicate pred)
        {
            return pred switch
            {
                LLVMIntPredicate.LLVMIntEQ => Predicate.Eq,
                LLVMIntPredicate.LLVMIntNE => Predicate.Ne,
                LLVMIntPredicate.LLVMIntUGT => Predicate.Ugt,
                LLVMIntPredicate.LLVMIntUGE => Predicate.Uge,
                LLVMIntPredicate.LLVMIntULT => Predicate.Ult,
                LLVMIntPredicate.LLVMIntULE => Predicate.Ule,
                LLVMIntPredicate.LLVMIntSGT => Predicate.Sgt,
                LLVMIntPredicate.LLVMIntSGE => Predicate.Sge,
                LLVMIntPredicate.LLVMIntSLT => Predicate.Slt,
                LLVMIntPredicate.LLVMIntSLE => Predicate.Sle,
                _ => throw new NotSupportedException($"Unsupported LLVM integer predicate: {pred}"),
            };
        }

        private static LLVMIntPredicate ConvPredicate(Predicate pred)
        {
            return pred switch
            {
                Predicate.Eq => LLVMIntPredicate.LLVMIntEQ,
                Predicate.Ne => LLVMIntPredicate.LLVMIntNE,
                Predicate.Ugt => LLVMIntPredicate.LLVMIntUGT,
                Predicate.Uge => LLVMIntPredicate.LLVMIntUGE,
                Predicate.Ult => LLVMIntPredicate.LLVMIntULT,
                Predicate.Ule => LLVMIntPredicate.LLVMIntULE,
                Predicate.Sgt => LLVMIntPredicate.LLVMIntSGT,
                Predicate.Sge => LLVMIntPredicate.LLVMIntSGE,
                Predicate.Slt => LLVMIntPredicate.LLVMIntSLT,
                Predicate.Sle => LLVMIntPredicate.LLVMIntSLE,
                _ => throw new NotSupportedException($"Unsupported predicate: {pred}"),
            };
        }

        Dictionary<AstIdx, AstIdx> simplSubst = new();

        private void SimplifyFixedPoint(List<ValueWithSimplification> pairs)
        {

            // Iteratively simplify each ast 
            var simplifier = new GeneralSimplifier(ctx);
            for (int count = 0; count < 1; count++)
            {
                bool changed = false;
                for (int i = 0; i < pairs.Count; i++)
                {
                    var pair = pairs[i];

                    //if (ctx.GetCost(pair.Idx) >= 1000)
                    //    continue;

                    // Simplify the AST
                    //var simplified = simplifier.SimplifyGeneral(pair.Idx);
                    var simplified = pair.Idx;

                    //if (optimal.TryGetValue(pair.Idx, out var existing))
                    if (false)
                    {
                       // simplified = existing;
                    }


                    else if (UNSOUND)
                    {
                       

                        var unsound = SimplifyUnsound(simplified);
                        if (unsound != null)
                        {
                            var c1 = ctx.GetCost(unsound.Value);
                            var c2 = ctx.GetCost(pair.Idx);


                            if (c1 < c2 && c1 < 50)
                            {

                                if (IsNeg(pair.Idx))
                                {
                                    simplSubst.Add(pair.Idx, unsound.Value);
                                    continue;
                                }
                                    
                                //var part1 = GeneralSimplifier.BackSubstitute(ctx, pair.Idx, simplSubst);
                                var part1 = pair.Idx;
                               // for (int ii = 0; ii < 3; ii++)
                               //     part1 = ctx.RecursiveSimplify(part1);

                                //if (!ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, part1, pair.Idx))
                                //    Debugger.Break();

                                var s1 = ctx.GetAstString(part1);
                                var s2 = ctx.GetAstString(unsound.Value);
                                Console.WriteLine($"{s1} => {s2}");

                                ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, part1, unsound.Value);

                                Console.WriteLine("\n\n\n");


                                database[s1] = s2;
                                simplSubst.TryAdd(pair.Idx, unsound.Value);

                                if (database.Count % 1000 == 0)
                                {
                                    var map = MbaDeobfuscationPass.database.Select(x => $"{x.Key}, {x.Value}");
                                    var dbString = String.Join("\n", map);
                                    File.WriteAllText("mba_database.txt", dbString);

                                    Console.WriteLine("Hit");
                                    //Console.ReadLine();
                                }
                            }


                            simplified = unsound.Value;

                        }

                        else
                        {
                            /*
                            var noDead =IdentifyDeadVariables(pair.Idx);
                            if (noDead != null)
                                simplified = noDead.Value;
                            */
                        }
                    }

                    else
                    {
              
                        var r = simplifier.SimplifyGeneral(pair.Idx);
                        r = simplifier.SimplifyGeneral(r);
                        r = simplifier.SimplifyGeneral(r);
                        optimal[pair.Idx] = r;

                        var c1 = ctx.GetCost(pair.Idx);
                        var c2 = ctx.GetCost(r);

                        if (c2 < c1)
                        {
                            simplified = r;

                            var s1 = ctx.GetAstString(pair.Idx);
                            var s2 = ctx.GetAstString(simplified);

                            //if (s1.Contains("(72057594037927936*(255&(uns30>>24)))"))

                            ////ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, pair.Idx, simplified);

                            Console.WriteLine($"{s1}\n  =>\n{s2}\n");
                        }


                        //Console.WriteLine($"{s1}\n  =>\n{s2}\n");


                        //ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, pair.Idx, simplified);

                    }

                    
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

        private bool IsNeg(AstIdx idx)
        {
            return ctx.GetOpcode(idx) == AstOp.Xor && ctx.IsSymbol(ctx.GetOp0(idx)) && ctx.IsConstant(ctx.GetOp1(idx)) && ctx.GetConstantValue(ctx.GetOp1(idx)) == (ulong)ModuloReducer.GetMask(ctx.GetWidth(idx));
        }

        private unsafe AstIdx? SimplifyUnsound(AstIdx idx)
        {
            var variables = ctx.CollectVariables(idx);
            if (variables.Count >= 11)
                return null;

            var before = idx;
            var after = LinearSimplifier.Run(ctx.GetWidth(idx), ctx, idx, false, true, false, variables);

            if (!ProbableEquivalenceChecker.ProbablyEquivalent(ctx, before, after, true, pagePtr1, pagePtr2))
                return null;

            //ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, before, after);
            //var checker = new EquivalenceChecker(ctx, variables, before, after, pagePtr1, pagePtr2);
            //if (!checker.ProbablyEquivalent())
            //    return null;

            //var trans = new Z3Translator(ctx);
            

            return after;
        }

        private unsafe AstIdx? IdentifyDeadVariables(AstIdx idx)
        {
            var variables = ctx.CollectVariables(idx);

            var jit1 = new Amd64OptimizingJit(ctx);
            jit1.Compile(idx, variables, pagePtr1, false);
            var func1 = (delegate* unmanaged[SuppressGCTransition]<ulong*, ulong>)pagePtr1;

            var liveVars = new HashSet<AstIdx>();
            var vArray = stackalloc ulong[variables.Count];
            for (int _ = 0; _ < 100; _++)
            {
                // Assign random values to each variable
                for (int vidx = 0; vidx < variables.Count; vidx++)
                    vArray[vidx] = GetRandUlong();

                var before = func1(vArray);
                for (int vIdx = 0; vIdx < variables.Count; vIdx++)
                {
                    var v = variables[vIdx];
                    if (liveVars.Contains(v))
                        continue;

                    var old = vArray[vIdx];
                    vArray[vIdx] = GetRandUlong();
                    var after = func1(vArray);
                    if (before != after)
                    {
                        liveVars.Add(v);
                        vArray[vIdx] = old;
                        break;
                    }

                    vArray[vIdx] = old;

                    // Otherwise behavior stayed the same, this variable is dead
                }
            }

            var deadVars = variables.Where(x => !liveVars.Contains(x)).ToDictionary(x => x, x => ctx.Constant(0, ctx.GetWidth(x)));
            if (deadVars.Any())
            {
                return GeneralSimplifier.BackSubstitute(ctx, idx, deadVars);
                //Debugger.Break();
            }

            return null;


        }

        public ulong GetRandUlong()
        {
            // The builtin random API can only return positive int64s,
            // so the sign-bit must be flipped at random manually.
            var value = (ulong)random.NextInt64(0, long.MaxValue);
            ulong signBit = GetRandBool() ? 0x8000000000000000 : 0;
            value |= signBit;
            return value;
        }

        public bool GetRandBool()
            => random.Next(0, 2) == 1;

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
                AstOp.Zext => builder.BuildZExt(lower(0), LLVMTypeRef.CreateInt(ctx.GetWidth(idx))),
                AstOp.Trunc => builder.BuildTrunc(lower(0), LLVMTypeRef.CreateInt(ctx.GetWidth(idx))),
                AstOp.Constant => LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(ctx.GetWidth(idx)), ctx.GetConstantValue(idx)),
                AstOp.Lshr => builder.BuildLShr(lower(0), lower(1)),
                AstOp.ICmp => builder.BuildICmp(ConvPredicate(ctx.GetPredicate(idx)), lower(0), lower(1)),
                AstOp.Select => builder.BuildSelect(lower(0), lower(1), lower(2)),
                _ => throw new InvalidOperationException($"Cannot lower opcode {opcode} to LLVM IR! with inst {ctx.GetAstString(idx)}"),
            };

            cache[idx] = result;
            return result;
        }
    }
}