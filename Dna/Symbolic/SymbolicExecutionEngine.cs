using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TritonTranslator.Ast;
using TritonTranslator.Intermediate;
using TritonTranslator.Intermediate.Operands;

namespace Dna.Symbolic
{
    public class SymbolicExecutionEngine : ISymbolicExecutionEngine
    {
        private Context ctx = new Context();

        private Dictionary<IOperand, Expr> variableDefinitions = new();

        private Dictionary<MemoryNode, Expr> memoryDefinitions = new();

        public IReadOnlyDictionary<IOperand, Expr> VariableDefinitions => variableDefinitions;

        public IReadOnlyDictionary<MemoryNode, Expr> MemoryDefinitions => memoryDefinitions;

        public void ExecuteInstruction(AbstractInst instruction)
        {
           switch(instruction)
           {
                case InstZx inst:
                    FromZx(inst);
                    break;
                case InstSx inst:
                    FromSx(inst);
                    break;
                case InstSub inst:
                    FromSub(inst);
                    break;
                case InstXor inst:
                    FromXor(inst);
                    break;
                case InstCopy inst:
                    FromCopy(inst);
                    break;
                case InstSelect inst:
                    FromSelect(inst);
                    break;
                case InstExtract inst:
                    FromExtract(inst);
                    break;
                case InstCond inst:
                    FromCond(inst);
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Cannot handle inst: {0}", instruction));
           }
        }

        private void FromZx(InstZx inst)
        {
            var op2 = LoadSourceOperand(inst.Op2);
            var result = ctx.MkZeroExt((uint)inst.Size.Value, (BitVecExpr)op2);
            StoreToOperand(inst.Dest, result);
        }

        private void FromSx(InstSx inst)
        {
            var op2 = LoadSourceOperand(inst.Op2);
            var result = ctx.MkSignExt((uint)inst.Size.Value, (BitVecExpr)op2);
            StoreToOperand(inst.Dest, result);
        }

        private void FromSub(InstSub inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = ctx.MkBVSub((BitVecExpr)op1, (BitVecExpr)op2);
            StoreToOperand(inst.Dest, result);
        }

        private void FromXor(InstXor inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var result = ctx.MkBVXOR((BitVecExpr)op1, (BitVecExpr)op2);
            StoreToOperand(inst.Dest, result);
        }

        private void FromCopy(InstCopy inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            StoreToOperand(inst.Dest, (BitVecExpr)op1);
        }

        private void FromSelect(InstSelect inst)
        {
            var op1 = LoadSourceOperand(inst.Op1);
            var op2 = LoadSourceOperand(inst.Op2);
            var op3 = LoadSourceOperand(inst.Op3);
            var result =  ctx.MkITE(ctx.MkEq(op1, ctx.MkBV(1, 1)), op2, op3);
            StoreToOperand(inst.Dest, result);
        }

        private void FromExtract(InstExtract inst)
        {
            var op1 = inst.Op1 as ImmediateOperand;
            var op2 = inst.Op2 as ImmediateOperand;
            var op3 = LoadSourceOperand(inst.Op3);
            var result = ctx.MkExtract((uint)op1.Value, (uint)op2.Value, (BitVecExpr)op3);
            StoreToOperand(inst.Dest, result);
        }

        private void FromCond(InstCond inst)
        {
            BitVecExpr op1 = (BitVecExpr)LoadSourceOperand(inst.Op1);
            BitVecExpr op2 = (BitVecExpr)LoadSourceOperand(inst.Op2);
            Expr result = null;
            switch (inst.CondType)
            {
                case CondType.Eq:
                    result = ctx.MkEq(op1, op2);
                    break;
                case CondType.Sge:
                    result = ctx.MkBVSGE(op1, op2);
                    break;
                case CondType.Sgt:
                    result = ctx.MkBVSGT(op1, op2);
                    break;
                case CondType.Sle:
                    result = ctx.MkBVSLE(op1, op2);
                    break;
                case CondType.Slt:
                    result = ctx.MkBVSLT(op1, op2);
                    break;
                case CondType.Uge:
                    result = ctx.MkBVUGE(op1, op2);
                    break;
                case CondType.Ugt:
                    result = ctx.MkBVUGT(op1, op2);
                    break;
                case CondType.Ule:
                    result = ctx.MkBVULE(op1, op2);
                    break;
                case CondType.Ult:
                    result = ctx.MkBVULT(op1, op2);
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Cond type {0} is invalid.", inst.CondType));
            }

            result = ctx.MkITE((BoolExpr)result, ctx.MkBV(1, 1), ctx.MkBV(0, 1));
            StoreToOperand(inst.Dest, result);
        }

        private Expr LoadSourceOperand(IOperand operand)
        {
            if(variableDefinitions.ContainsKey(operand))
            {
                return variableDefinitions[operand];
            }

            BitVecExpr op = null;
            if (operand is ImmediateOperand imm)
            {
                op = ctx.MkBV(imm.Value, operand.Bitsize);
            }

            else
            {
                op = ctx.MkBVConst(operand.ToString(), operand.Bitsize);
            }
            variableDefinitions.Add(operand, op);
            return op;
        }

        private void StoreToOperand(IOperand operand, Expr expr)
        {
            variableDefinitions[operand] = expr;
        }

        public void CompareExpression(int bitSize, BitVecExpr wholeExpr, IOperand inputOperand)
        {
            var expr = variableDefinitions[inputOperand];
         //   expr = expr.Simplify();
          //  wholeExpr = (BitVecExpr)wholeExpr.Simplify();

            BitVecExpr inputExpr = null;
            if (expr is BitVecExpr)
            {
                inputExpr = (BitVecExpr)expr;
            }

            else if (expr is BoolExpr boolExpr)
            {
                Console.WriteLine("Hadnling bool expr.");
                inputExpr = (BitVecExpr)ctx.MkITE(boolExpr, ctx.MkBV(1, 1), ctx.MkBV(0, 1));
            }

            // Skip if the bit sizes don't match.
            if (32 != inputOperand.Bitsize)
                return;

            var equivalentExpr = ctx.MkITE(
                    ctx.MkEq(ctx.MkExtract(0x1F, 0x0, inputExpr), ctx.MkBV(0, (uint)inputOperand.Bitsize)),
                    ctx.MkBV(1, 1),
                    ctx.MkBV(0, 1)

                );

            BitVecNum idk = null;



            equivalentExpr = ctx.MkITE(ctx.MkEq(equivalentExpr, ctx.MkBV(1, 1)), ctx.MkBV(1, 1), ctx.MkBV(0, 1));

            //bool isEqual = Prove(ctx, ctx.MkEq(wholeExpr, wholeExpr));
            // Console.WriteLine("IsEqual: {0}", isEqual);

            var expr1 = ctx.MkBVConst("x", 32);
            var expr2 = ctx.MkBVConst("y", 32);

            bool isEqual = Prove(ctx, ctx.MkEq(expr1, expr2));
            Console.WriteLine("IsEqual: {0}", isEqual);

            BitVecSort bvs = ctx.MkBitVecSort(32);
            Expr x = expr1;
            Expr y = expr2;


            BoolExpr eq = ctx.MkNot(ctx.MkEq(x, y));

            // Use a solver for QF_BV
            Solver s = ctx.MkSolver("QF_BV");
            s.Assert(eq);
            Status res = s.Check();
            Console.WriteLine("solver result: " + res);

            bool finalIsEq = IsEqual(wholeExpr, equivalentExpr);
            Console.WriteLine("FinalIsEq: {0}", finalIsEq);
            Console.WriteLine("adsa");
        }

        // Unsat means expressions are equal
        private bool IsEqual(Expr expr1, Expr expr2)
        {
            Expr x = expr1;
            Expr y = expr2;


            BoolExpr eq = ctx.MkNot(ctx.MkEq(x, y));

            // Use a solver for QF_BV
            Solver s = ctx.MkSolver("QF_BV");
            s.Assert(eq);
            Status res = s.Check();

            //Console.WriteLine("Model: {0}", s.Model);

            return res == Status.UNSATISFIABLE;
        }



        bool Prove(Context ctx, BoolExpr f, bool useMBQI = false, params BoolExpr[] assumptions)
        {
            //Console.WriteLine("Proving: " + f);
            Solver s = ctx.MkSolver("QF_BV");
            foreach (BoolExpr a in assumptions)
            {
                //s.Add(a);
            }

            s.Add(f);

            Status q = s.Check();


            switch (q)
            {
                case Status.UNKNOWN:
                    Console.WriteLine("Unknown because: ");
                    return false;
                    break;
                case Status.SATISFIABLE:
                    Console.WriteLine("SATISFIABLE: " + f);
                    Console.WriteLine("Model: " + s.Model);
                    return false;
                    break;
                case Status.UNSATISFIABLE:
                    Console.WriteLine("Unsatisfiable query");
                    Console.WriteLine("Model: {0}", s.Model);
                    Console.WriteLine("OK, proof: " + f);
                    return true;
                    break;
            }

            Console.WriteLine("AAAAA");
            return false;
        } 
    }
}
