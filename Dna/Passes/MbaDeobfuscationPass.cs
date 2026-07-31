using Antlr4.Runtime.Misc;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp;
using LLVMSharp.Interop;
using Mba.Common.Ast;
using Mba.Common.MSiMBA;
using Mba.Simplifier;
using Mba.Simplifier.Bindings;
using Mba.Simplifier.Interpreter;
using Mba.Simplifier.Pipeline;
using Mba.Simplifier.Synth;
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

        private const bool SYNTHESIS = true;


        private readonly LLVMValueRef function;

        public readonly AstCtx ctx = new();
        //public static readonly AstCtx ctx = new();

        // Mapping of unsupported LLVM instructions to substituted values
        public Dictionary<LLVMValueRef, AstIdx> substMapping = new();

        public Dictionary<AstIdx, LLVMValueRef> inverseSubstMap = new();


        public Dictionary<LLVMValueRef, AstIdx> defMap = new();

        public static Dictionary<AstIdx, AstIdx> optimal = new();

        private readonly Random random = new();

        private readonly nint pagePtr1 = JitUtils.AllocateExecutablePage(1000);

        private readonly nint pagePtr2 = JitUtils.AllocateExecutablePage(1000);


        public static bool Run(LLVMValueRef function)
            => new MbaDeobfuscationPass(function).Run();

        public MbaDeobfuscationPass(LLVMValueRef function)
        {
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
            SimplifyFixedPointSynth(asts);

            // Collect all pairs where we achieved either a simpler or equal result from the MBA simplifier.
            //var profitablePairs = asts.Where(x => ctx.GetCost(x.Idx) <= ctx.GetCost(defMap[x.Inst])).ToList();
            var profitablePairs = asts;

            // Update the definition of each instruction with it's simplification.
            foreach (var pair in profitablePairs)
            {
                defMap[pair.Inst] = pair.Idx;
            }

            bool changed = false;
            //return changed;
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



            function.GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
            Console.WriteLine("Finished mba pass");
            return changed;
        }

        public static IReadOnlySet<LLVMValueRef> GetTargets(LLVMValueRef function)
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
        public AstIdx GetAst(LLVMValueRef value)
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

        public AstIdx GetArgAst(LLVMValueRef value)
        {
            Debug.Assert(value.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind);
            //var name = $"arg{function.GetParams().IndexOf(value)}";
            //var name = value.Name.Replace("%", "");
            var name = $"arg{function.GetParams().IndexOf(value)}";
            var ast = ctx.Symbol(name, (byte)value.TypeOf.IntWidth);
            defMap[value] = ast;
            substMapping[value] = ast;
            inverseSubstMap[ast] = value;

            //Console.WriteLine($"Var name: {value.Name} got {ast}");

            return ast;
        }

        public AstIdx GetConstIntAst(LLVMValueRef value)
        {
            var constDef = ctx.Constant(value.ConstIntZExt, (byte)value.TypeOf.IntWidth);
            defMap[value] = constDef;
            return constDef;
        }

        public AstIdx GetInstructionAst(LLVMValueRef inst)
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
                LLVMOpcode.LLVMAShr => AShr(ctx, op1(), op2()),
                LLVMOpcode.LLVMSub => Sub(ctx, op1(), op2()),
                LLVMOpcode.LLVMZExt => ctx.Zext(op1(), (byte)inst.TypeOf.IntWidth),
                LLVMOpcode.LLVMTrunc => inst.GetOperand(0).TypeOf.IntWidth > 64 ? GetUnsupportedInstruction(inst) : ctx.Trunc(op1(), (byte)inst.TypeOf.IntWidth),
                LLVMOpcode.LLVMSExt => inst.TypeOf.IntWidth > 64 ? GetUnsupportedInstruction(inst) : SExt(ctx, op1(), (byte)inst.TypeOf.IntWidth),
                LLVMOpcode.LLVMCall => GetCallAst(inst),
                LLVMOpcode.LLVMSelect => ctx.Select(op1(), op2(), op3()),
                LLVMOpcode.LLVMICmp => ctx.ICmp(ConvPredicate(inst.ICmpPredicate), op1(), op2()),
                LLVMOpcode.LLVMAdd => ctx.Add(op1(), op2()),
                LLVMOpcode.LLVMMul => ctx.Mul(op1(), op2()),
                LLVMOpcode.LLVMFreeze => op1(),
                _ => GetUnsupportedInstruction(inst)
            };

            defMap[inst] = def;
            return def;
        }

        public AstIdx GetUnsupportedInstruction(LLVMValueRef inst)
        {
            uniqueMnemonics.Add(inst.InstructionOpcode);
            var name = $"uns{substMapping.Count}";
            var def = ctx.Symbol(name, (byte)inst.TypeOf.IntWidth);


            substMapping[inst] = def;
            inverseSubstMap[def] = inst;
            return def;

            if (inst.GetUsers().Any(x => x.InstructionOpcode == LLVMOpcode.LLVMPHI))
                return def;

            /*
            var knownBits = NativeKnownBits.Get(inst, function.GlobalParent);
            var oldDef = def;
            if (knownBits.GetKnownBitCount() > 0)
            {
                var w = ctx.GetWidth(def);
                def = ctx.Or(ctx.Constant(knownBits.One, w), def);
                def = ctx.And(ctx.Constant(~knownBits.Zero, w), def);
            }




            substMapping[inst] = oldDef;
            return def;
            */
        }

        private static AstIdx Shl(AstCtx ctx, AstIdx a, AstIdx b)
        {
            return ctx.Mul(a, ctx.Pow(ctx.Constant(2, ctx.GetWidth(a)), b));
        }

        private static AstIdx Sub(AstCtx ctx, AstIdx a, AstIdx b)
        {
            return ctx.Add(a, ctx.Mul(b, ctx.Constant(ulong.MaxValue, ctx.GetWidth(b))));
        }

        // Lower the whitelisted integer intrinsics to their emulated equivalents.
        private AstIdx GetCallAst(LLVMValueRef inst)
        {
            if (inst.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind || inst.TypeOf.IntWidth > 64)
                return GetUnsupportedInstruction(inst);

            var callee = inst.GetCallInstTarget();
            if (callee.Kind != LLVMValueKind.LLVMFunctionValueKind)
                return GetUnsupportedInstruction(inst);

            var name = callee.Name;
            var width = inst.TypeOf.IntWidth;

            // The modulo of the funnel shift amount is only expressible as an AND when the width is a power of two.
            if (name.StartsWith("llvm.fshl") && BitOperations.IsPow2(width))
                return Fshl(ctx, GetAst(inst.GetOperand(0)), GetAst(inst.GetOperand(1)), GetAst(inst.GetOperand(2)));

            // LLVM only defines llvm.bswap for integers with an even number of bytes.
            if (name.StartsWith("llvm.bswap") && width >= 16 && width % 8 == 0)
                return Bswap(ctx, GetAst(inst.GetOperand(0)));

            if (name.StartsWith("llvm.ctpop"))
                return CtPop(ctx, GetAst(inst.GetOperand(0)));

            return GetUnsupportedInstruction(inst);
        }

        // sext(x) = (zext(x) ^ signBit) - signBit, where signBit is the sign bit of the source width.
        // Flipping the source sign bit before subtracting it back out broadcasts it across all of the high bits.
        private static AstIdx SExt(AstCtx ctx, AstIdx a, byte destWidth)
        {
            var srcWidth = ctx.GetWidth(a);
            var extended = srcWidth == destWidth ? a : ctx.Zext(a, destWidth);
            var signBit = ctx.Constant(1ul << (srcWidth - 1), destWidth);
            return Sub(ctx, ctx.Xor(extended, signBit), signBit);
        }

        // Let signMask be either zero or all ones depending on the sign bit of a.
        // For negative a, complementing before and after a logical shift fills the
        // vacated high bits with ones. For non-negative a this reduces to lshr.
        private static AstIdx AShr(AstCtx ctx, AstIdx a, AstIdx amount)
        {
            var width = ctx.GetWidth(a);
            var sign = ctx.Lshr(a, ctx.Constant((ulong)(width - 1), width));
            var signMask = Sub(ctx, ctx.Constant(0, width), sign);
            return ctx.Xor(ctx.Lshr(ctx.Xor(a, signMask), amount), signMask);
        }

        // Sum every isolated input bit. LLVM returns ctpop in the same integer
        // type as its operand, which is wide enough to represent [0, width].
        private static AstIdx CtPop(AstCtx ctx, AstIdx a)
        {
            var width = ctx.GetWidth(a);
            var one = ctx.Constant(1, width);
            var result = ctx.And(a, one);

            for (byte bit = 1; bit < width; bit++)
            {
                var shifted = ctx.Lshr(a, ctx.Constant(bit, width));
                result = ctx.Add(result, ctx.And(shifted, one));
            }

            return result;
        }

        // fshl(a, b, c) concatenates a:b into a 2N bit value, shifts it left by (c % N), and returns the high N bits.
        // This is equivalent to (a << s) | (b >> (N - s)), where s = c % N.
        private static AstIdx Fshl(AstCtx ctx, AstIdx a, AstIdx b, AstIdx c)
        {
            var width = ctx.GetWidth(a);

            // s = c % N. The width is a power of two, so the modulo is a bitwise AND.
            var s = ctx.And(c, ctx.Constant((ulong)(width - 1), width));

            var high = Shl(ctx, a, s);

            // `b >> (N - s)` would shift by N when s is zero, which is poison in LLVM and is masked
            // (rather than saturated to zero) by both the AST interpreter and the JIT. Shifting right by
            // one first keeps the remaining shift amount within [0, N-1] while producing the same value.
            var shift = Sub(ctx, ctx.Constant((ulong)(width - 1), width), s);
            var low = ctx.Lshr(ctx.Lshr(b, ctx.Constant(1, width)), shift);

            // The two halves never overlap, so OR is exact here.
            return ctx.Or(high, low);
        }

        // bswap(x) reverses the byte order by moving each byte to its mirrored position.
        private static AstIdx Bswap(AstCtx ctx, AstIdx a)
        {
            var width = ctx.GetWidth(a);
            var byteCount = width / 8;

            AstIdx? result = null;
            for (int i = 0; i < byteCount; i++)
            {
                var srcShift = 8 * i;
                var dstShift = 8 * (byteCount - 1 - i);

                // Isolate the i'th byte.
                var current = srcShift == 0 ? a : ctx.Lshr(a, ctx.Constant((ulong)srcShift, width));
                current = ctx.And(current, ctx.Constant(0xFF, width));

                // Then move it to the mirrored byte position.
                if (dstShift != 0)
                    current = ctx.Mul(current, ctx.Constant(1ul << dstShift, width));

                result = result == null ? current : ctx.Or(result.Value, current);
            }

            return result.Value;
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

        // Sign ext
        // Freeze
        // Ashr
        // Call intrinsics
        private void SimplifyFixedPointSynth(List<ValueWithSimplification> pairs)
        {
            //pairs.RemoveAll(x => !ShouldProcess(x.Inst));
            //pairs.RemoveAt(0);
            foreach (var pair in pairs)
            {
                //if (!pair.Inst.ToString().Contains("%.not107633 =")) // this simplifies to false
                if (!pair.Inst.ToString().Contains("%516 ="))
                    continue;
                var components = new List<SynthComponent>()
                {
                    new(SynthOpc.Not, SynthOpc.And, SynthOpc.Or, SynthOpc.Xor, SynthOpc.Mul),
                    new(SynthOpc.Add, SynthOpc.Sub),
                    new(SynthOpc.Eq, SynthOpc.Ult, SynthOpc.Select),
                    new(SynthOpc.Bitcast)
                };

                var config = new SynthConfig(components, 6, 3);
                var synth = new LineSynth(config, ctx, pair.Idx);

                synth.Run();
                Debugger.Break();
            }
            Debugger.Break();
        }

        private bool ShouldProcess(LLVMValueRef value)
        {
            // Skip freeze
            //if (value.InstructionOpcode == LLVMOpcode.LLVMFreeze)
            //    return false;

            var users = value.GetUsers();
            foreach (var user in users)
            {
                if (user.Is(LLVMOpcode.LLVMBr))
                    return true;
                if (user.Is(LLVMOpcode.LLVMGetElementPtr))
                    return true;
                //if (user.Is(LLVMOpcode.LLVMCall) && value.GetCallInstTarget().Name == "vmp_branch")
                //    return true;

                if (user.Is(LLVMOpcode.LLVMPHI))
                    return true;
            }

            var text = value.ToString();
            List<string> whitelist = new() { "%1048 = add i64 %396, " };
            if (whitelist.Any(x => text.Contains(x)))
                return true;

            return false;
        }

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

                    if (optimal.TryGetValue(pair.Idx, out var existing))
                    {
                        simplified = existing;
                    }


                    else if (UNSOUND)
                    {
                        optimal[pair.Idx] = pair.Idx;

                        var unsound = SimplifyUnsound(simplified);
                        if (unsound != null)
                        {
                            var c1 = ctx.GetCost(unsound.Value);
                            var c2 = ctx.GetCost(pair.Idx);

                            var part1 = pair.Idx;
                            //if (c1 < c2 && c1 < 50 && ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, part1, unsound.Value))
                            if (false) // broken
                            {
                                optimal[pair.Idx] = unsound.Value;
                                if (IsNeg(pair.Idx))
                                {
                                    simplSubst.Add(pair.Idx, unsound.Value);
                                    continue;
                                }

                                //var part1 = GeneralSimplifier.BackSubstitute(ctx, pair.Idx, simplSubst);

                                // for (int ii = 0; ii < 3; ii++)
                                //     part1 = ctx.RecursiveSimplify(part1);

                                //if (!ProbableEquivalenceChecker.ProbablyEquivalentZ3(ctx, part1, pair.Idx))
                                //    Debugger.Break();

                                var s1 = ctx.GetAstString(part1);
                                var s2 = ctx.GetAstString(unsound.Value);
                                Console.WriteLine($"{s1}\n    =>\n{s2}\n");



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

                        if (pair.Inst.ToString().Contains("%216 ="))
                            Debugger.Break();

                        var r = simplifier.SimplifyGeneral(pair.Idx);
                        r = simplifier.SimplifyGeneral(r);
                        r = simplifier.SimplifyGeneral(r);
                        optimal[pair.Idx] = pair.Idx;

                        // var guess = SimplifyUnsound(r);

                        if (!ProbableEquivalenceChecker.ProbablyEquivalent(ctx, pair.Idx, r, slowHeuristics: true, pagePtr1, pagePtr2))
                        {
                            Console.WriteLine("Not equiv!");
                            Debugger.Break();
                        }


                        var c1 = ctx.GetCost(pair.Idx);
                        var c2 = ctx.GetCost(r);

                        if (c2 < c1)
                        {
                            simplified = r;
                            optimal[pair.Idx] = r;


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