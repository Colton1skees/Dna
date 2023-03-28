using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Passes
{
    public enum AliasResult : byte
    {
        /// The two locations do not alias at all.
        ///
        /// This value is arranged to convert to false, while all other values
        /// convert to true. This allows a boolean context to convert the result to
        /// a binary flag indicating whether there is the possibility of aliasing.
        NoAlias = 0,
        /// The two locations may or may not alias. This is the least precise
        /// result.
        MayAlias,
        /// The two locations alias, but only due to a partial overlap.
        PartialAlias,
        /// The two locations precisely alias each other.
        MustAlias,

        /// <summary>
        /// If Dna cannot determine the pointer type, then we return this out of range value.
        /// Since this is not a valid option(e.g. llvm only has the above four alias types),
        /// the pass which consumes this API knows to default to LLVM's built in AA.
        /// </summary>
        UnknownAlias,
    };

    public enum PointerType
    {
        Unk,
        BinarySection,
        LocalStack,
        Segment,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate AliasResult dgGetAliasResult(LLVMOpaqueValue* ptrA, LLVMOpaqueValue* ptrB);

    public static class ClassifyingAliasAnalysisPass
    {
        private static readonly dgGetAliasResult getAliasResult;

        /// <summary>
        /// Unmanaged pointer to the `GetAliasKind` method, which is invoked by native code.
        /// </summary>
        public static nint PtrGetAliasResult { get;}

        public static bool print = false;

        static unsafe ClassifyingAliasAnalysisPass()
        {
            getAliasResult = new dgGetAliasResult(GetAliasResult);
            PtrGetAliasResult = Marshal.GetFunctionPointerForDelegate(getAliasResult);
        }

        public static unsafe AliasResult GetAliasResult(LLVMOpaqueValue* opaquePtrA, LLVMOpaqueValue* opaquePtrB)
        {
            return GetAliasResultInternal(opaquePtrA, opaquePtrB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe AliasResult GetAliasResultInternal(LLVMValueRef ptrA, LLVMValueRef ptrB)
        {
            /*
            var ptrAText = new string(ptrA.ToString().SkipWhile(x => x == ' ').ToArray());
            Console.WriteLine(ptrAText);
            var ptrBText = new string(ptrB.ToString().SkipWhile(x => x == ' ').ToArray());
            Console.WriteLine(ptrBText);
            */
            

            // Classify the type of pointer A.
            var typeA = PointerClassifier.GetPointerType(ptrA);

            // Classify the type of pointer B.
            var typeB = PointerClassifier.GetPointerType(ptrB);

            // If either pointer type is unknown, then we tell LLVM
            // to fall back to it's basic alias analysis.
            if (typeA == PointerType.Unk || typeB == PointerType.Unk)
            {
                if (print)
                {
                    var chainA = InstructionSlicer.SliceInst(ptrA);
                    Console.WriteLine("");
                    Console.WriteLine("Chain A: ");
                    foreach (var item in chainA.Reverse())
                    {
                        var text = item.ToString();
                        text = new string(text.SkipWhile(x => x == ' ').ToArray());
                        Console.WriteLine("    " + text);
                    }

                    Console.WriteLine("");
                    Console.WriteLine("Chain B: ");
                    var chainB = InstructionSlicer.SliceInst(ptrB);
                    foreach (var item in chainB.Reverse())
                    {
                        var text = item.ToString();
                        text = new string(text.SkipWhile(x => x == ' ').ToArray());
                        Console.WriteLine("    " + text);
                    }
                }


                return AliasResult.UnknownAlias;
            }

            // If the two pointer types are different(e.g. [rsp - 0x10] and [gs]),
            // then we tell LLVM that these cannot possibly alias.
            if (typeA != typeB)
                return AliasResult.NoAlias;

            // If the two pointer types are the same, then we fall back to LLVM's
            // built in alias analysis.
            return AliasResult.UnknownAlias;
        }
    }
}
