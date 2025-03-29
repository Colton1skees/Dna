using Dna.Binary;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.Passes.Matchers;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Passes
{
    public static class PointerClassifier
    {
        public static bool print = false;

        public static HashSet<LLVMValueRef> Seen = new HashSet<LLVMValueRef>();

        public static PointerType GetPointerType(IBinary binary, LLVMValueRef gep)
        {
            // There are two types of values where LLVM will request alias analysis on:
            //  - GetElementPtr instructions
            //  - Global variables
            // If the value is not a getelementptr instruction, then LLVM's default
            // alias analysis is sufficient(it knows what is capable of aliasing with global variables).
            // So, if this is not a GEP, we return UNKNOWN to inform the caller that LLVMs default AA should be used.
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return PointerType.Unk;

            if (StackAccessMatcher.IsStackAccess(gep.GetOperand(1)))
            {
                return PointerType.LocalStack;
            }

            if (BinaryAccessMatcher.IsBinarySectionAccess(binary, gep.GetOperand(1)))
            {
                return PointerType.BinarySection;
            }

            if (SegmentAccessMatcher.IsSegmentAccess(gep.GetOperand(1)))
            {
                return PointerType.Segment;
            }

            // Debug logging
            if (!Seen.Contains(gep) && print)
            {
                Seen.Add(gep);

                var slice = InstructionSlicer.SliceInst(gep);
                foreach (var item in slice.Reverse())
                {
                    var text = item.ToString();
                    text = new string(text.SkipWhile(x => x == ' ').ToArray());
                    Console.WriteLine("    " + text);
                }
            }


            return PointerType.Unk;
        }
    }
}
