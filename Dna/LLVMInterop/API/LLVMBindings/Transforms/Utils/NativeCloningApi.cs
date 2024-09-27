using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils
{
    public enum AttrKind : uint
    {
        FirstEnumAttr = 1,
        AllocAlign = 1,
        AllocatedPointer = 2,
        AlwaysInline = 3,
        ArgMemOnly = 4,
        Builtin = 5,
        Cold = 6,
        Convergent = 7,
        DisableSanitizerInstrumentation = 8,
        FnRetThunkExtern = 9,
        Hot = 10,
        ImmArg = 11,
        InReg = 12,
        InaccessibleMemOnly = 13,
        InaccessibleMemOrArgMemOnly = 14,
        InlineHint = 15,
        JumpTable = 16,
        MinSize = 17,
        MustProgress = 18,
        Naked = 19,
        Nest = 20,
        NoAlias = 21,
        NoBuiltin = 22,
        NoCallback = 23,
        NoCapture = 24,
        NoCfCheck = 25,
        NoDuplicate = 26,
        NoFree = 27,
        NoImplicitFloat = 28,
        NoInline = 29,
        NoMerge = 30,
        NoProfile = 31,
        NoRecurse = 32,
        NoRedZone = 33,
        NoReturn = 34,
        NoSanitizeBounds = 35,
        NoSanitizeCoverage = 36,
        NoSync = 37,
        NoUndef = 38,
        NoUnwind = 39,
        NonLazyBind = 40,
        NonNull = 41,
        NullPointerIsValid = 42,
        OptForFuzzing = 43,
        OptimizeForSize = 44,
        OptimizeNone = 45,
        PresplitCoroutine = 46,
        ReadNone = 47,
        ReadOnly = 48,
        Returned = 49,
        ReturnsTwice = 50,
        SExt = 51,
        SafeStack = 52,
        SanitizeAddress = 53,
        SanitizeHWAddress = 54,
        SanitizeMemTag = 55,
        SanitizeMemory = 56,
        SanitizeThread = 57,
        ShadowCallStack = 58,
        Speculatable = 59,
        SpeculativeLoadHardening = 60,
        StackProtect = 61,
        StackProtectReq = 62,
        StackProtectStrong = 63,
        StrictFP = 64,
        SwiftAsync = 65,
        SwiftError = 66,
        SwiftSelf = 67,
        WillReturn = 68,
        WriteOnly = 69,
        ZExt = 70,
        LastEnumAttr = 70,
        FirstTypeAttr = 71,
        ByRef = 71,
        ByVal = 72,
        ElementType = 73,
        InAlloca = 74,
        Preallocated = 75,
        StructRet = 76,
        LastTypeAttr = 76,
        FirstIntAttr = 77,
        Alignment = 77,
        AllocKind = 78,
        AllocSize = 79,
        Dereferenceable = 80,
        DereferenceableOrNull = 81,
        StackAlignment = 82,
        UWTable = 83,
        VScaleRange = 84,
        LastIntAttr = 84,
    }

    public static class NativeCloningApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* InlineFunction(LLVMOpaqueValue* callInst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void AddParamAttr(LLVMOpaqueValue* function, uint index, AttrKind kind);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MakeMustTail(LLVMOpaqueValue* callInst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MakeDsoLocal(LLVMOpaqueValue* function, bool dsoLocal);
    }
}
