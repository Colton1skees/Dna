using Dna.BinaryTranslator.JmpTables;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Utilities;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public  class VmpJmpTableSolver
    {
        public record HandlerEdge();
        public record ConstantJmpTableEdge(ulong bytecodePtr, ulong handlerRip) : HandlerEdge;
        public record TwoBytecodeOneHandlerEdge(ulong bytecodePtr1, ulong bytecodePtr2, ulong handlerRip) : HandlerEdge;
        public record TwoBytecodeTwoHandlerEdge(ulong bytecodePtr1, ulong handlerRip1, ulong bytecodePtr2, ulong handlerRip2) : HandlerEdge;

        public record JmpTablesWithHandlerRips(IReadOnlyList<VmpJmpTable> Tables, IReadOnlyDictionary<ulong, ulong> bytecodePtrToRip);

        private readonly LLVMValueRef function;

        public VmpJmpTableSolver(LLVMValueRef function)
        {
            this.function = function;
        }

        public JmpTablesWithHandlerRips Solve()
        {
            var targetFunc = function.GlobalParent.GetFunctions().SingleOrDefault(x => x.Name.Contains("vmp_maybe_unsolved_jump"));
            if(targetFunc == null)
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                throw new InvalidOperationException($"No jump tables to solve!");
            }

            var output = new List<VmpJmpTable>();
            var jmpCalls = RemillUtils.CallersOf(targetFunc).Where(x => x.InstructionParent.Parent == function);

            var bytecodePtrToRip = new Dictionary<ulong, ulong>();
            foreach(var jmpCall in jmpCalls)
            {
                if (jmpCall.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                {
                    function.GlobalParent.PrintToFile("translatedFunction.ll");
                    throw new InvalidOperationException($"Could not identify jump from address for call {jmpCall}");
                }

                // If we are jumping out of a vmexit, skip it.
                var constJmpFromAddress = jmpCall.GetOperand(2).ConstIntZExt;
                /*
                if(constJmpFromAddress == 0x14000690E || constJmpFromAddress == 0x1400066F4)
                {
                    Console.WriteLine("Skipping vmexit!");
                    continue;
                }
                */
                var edge = ClassifyHandlerEdge(jmpCall);

                if(edge is ConstantJmpTableEdge constantEdge)
                {
                    bytecodePtrToRip.Add(constantEdge.bytecodePtr, constantEdge.handlerRip);
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { constantEdge.bytecodePtr }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else if(edge is TwoBytecodeOneHandlerEdge twoBytecodeOneHandlerEdge)
                {
                    bytecodePtrToRip.Add(twoBytecodeOneHandlerEdge.bytecodePtr1, twoBytecodeOneHandlerEdge.handlerRip);
                    bytecodePtrToRip.Add(twoBytecodeOneHandlerEdge.bytecodePtr2, twoBytecodeOneHandlerEdge.handlerRip);
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { twoBytecodeOneHandlerEdge.bytecodePtr1, twoBytecodeOneHandlerEdge.bytecodePtr2 }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else if(edge is TwoBytecodeTwoHandlerEdge twoBytecodeTwoHandlerEdge)
                {
                    bytecodePtrToRip.Add(twoBytecodeTwoHandlerEdge.bytecodePtr1, twoBytecodeTwoHandlerEdge.handlerRip1);
                    bytecodePtrToRip.Add(twoBytecodeTwoHandlerEdge.bytecodePtr2, twoBytecodeTwoHandlerEdge.handlerRip2);
                    output.Add(new VmpJmpTable(constJmpFromAddress, new List<ulong>() { twoBytecodeTwoHandlerEdge.bytecodePtr1, twoBytecodeTwoHandlerEdge.bytecodePtr2 }, Enumerable.Empty<ulong>().ToList(), isComplete: false));
                }

                else
                {
                    // Otherwise this is probably an unsolved jump table. Error out.
                    jmpCall.InstructionParent.Parent.GlobalParent.PrintToFile("translatedFunction.ll");
                    throw new InvalidOperationException($"Failed to solve indirect jump! {jmpCall}");
                }
            }

            return new(output, bytecodePtrToRip);
        }

        private static HandlerEdge ClassifyHandlerEdge(LLVMValueRef jmpCall)
        {
            // Try to simple a constant to constant edge.
            var bytecodePtr = jmpCall.GetOperand(0);
            var nativeInstPtr = jmpCall.GetOperand(1);
            HandlerEdge edge = TryMatchConstantEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return edge;
            // Try to match a select of two possible bytecode pointers, where both handlers share the same RIP.
            edge = TryMatchTwoBytecodeOneHandlerEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return edge;
            edge = TryMatchTwoBytecodeTwoHandlerEdge(bytecodePtr, nativeInstPtr);
            if (edge != null)
                return edge;

            // Otherwise this is probably an unsolved jump table. Error out.
            var memPtr = jmpCall.InstructionParent.Parent.GlobalParent.GetNamedGlobal("memory");
            if (memPtr.Handle != 0)
            {
                memPtr.Linkage = LLVMLinkage.LLVMCommonLinkage;
                var memoryPtrNull = LLVMValueRef.CreateConstPointerNull(jmpCall.InstructionParent.Parent.GlobalParent.GetPtrType());
                memPtr.Initializer = memoryPtrNull;
            }

            jmpCall.InstructionParent.Parent.GlobalParent.PrintToFile("translatedFunction.ll");
            jmpCall.InstructionParent.Parent.GlobalParent.WriteBitcodeToFile("translatedFunction.bc");
            throw new InvalidOperationException($"Failed to solve indirect jump! {jmpCall}");
        }

        private static ConstantJmpTableEdge TryMatchConstantEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we're not dealing with a constant outgoing bytecode ptr and a constant handler rip, return null.
            if (bytecodePtr.Kind != LLVMValueKind.LLVMConstantIntValueKind || nativeInstPtr.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;
            return new ConstantJmpTableEdge(bytecodePtr.ConstIntZExt, nativeInstPtr.ConstIntZExt);
        }

        private static TwoBytecodeOneHandlerEdge TryMatchTwoBytecodeOneHandlerEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we are not selecting from two constant bytecode ptrs, or if native instruction point is not a constant, return null
            if (!IsSelectOfTwoConstants(bytecodePtr) || nativeInstPtr.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return null;
            return new TwoBytecodeOneHandlerEdge(bytecodePtr.GetOperand(1).ConstIntZExt, bytecodePtr.GetOperand(2).ConstIntZExt, nativeInstPtr.ConstIntZExt);
        }

        private static TwoBytecodeTwoHandlerEdge TryMatchTwoBytecodeTwoHandlerEdge(LLVMValueRef bytecodePtr, LLVMValueRef nativeInstPtr)
        {
            // If we are not selecting from two constant bytecode ptrs and two constant handler RIPs, return null.
            if (!IsSelectOfTwoConstants(bytecodePtr) || !IsSelectOfTwoConstants(nativeInstPtr))
                return null;
            // If the select statements are picking two different conditions, we cannot clearly map which bytecode pointer belongs to which handler.
            if (bytecodePtr.GetOperand(0) != nativeInstPtr.GetOperand(0))
                return null;
            return new TwoBytecodeTwoHandlerEdge(bytecodePtr.GetOperand(1).ConstIntZExt, nativeInstPtr.GetOperand(1).ConstIntZExt, bytecodePtr.GetOperand(2).ConstIntZExt, nativeInstPtr.GetOperand(2).ConstIntZExt);
        }

        private static bool IsSelectOfTwoConstants(LLVMValueRef inst)
        {
            // Return false if it's not a select inst.
            if (inst.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return false;
            // If either operand is not a constant, return false.
            if (inst.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind || inst.GetOperand(2).Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return false;
            return true;
        }
    }
}
