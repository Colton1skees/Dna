using Dna.LLVMInterop.API;
using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.Inst
{
    public enum SouperInstKind : byte
    {
        Const,
        UntypedConst,
        Var,
        Phi,
        Hole,

        Add,
        AddNSW,
        AddNUW,
        AddNW,
        Sub,
        SubNSW,
        SubNUW,
        SubNW,
        Mul,
        MulNSW,
        MulNUW,
        MulNW,
        UDiv,
        SDiv,
        UDivExact,
        SDivExact,
        URem,
        SRem,
        And,
        Or,
        Xor,
        Shl,
        ShlNSW,
        ShlNUW,
        ShlNW,
        LShr,
        LShrExact,
        AShr,
        AShrExact,
        Select,
        ZExt,
        SExt,
        Trunc,
        Eq,
        Ne,
        Ult,
        Slt,
        Ule,
        Sle,
        CtPop,
        BSwap,
        Cttz,
        Ctlz,
        BitReverse,
        FShl,
        FShr,
        ExtractValue,
        SAddWithOverflow,
        SAddO,
        UAddWithOverflow,
        UAddO,
        SSubWithOverflow,
        SSubO,
        USubWithOverflow,
        USubO,
        SMulWithOverflow,
        SMulO,
        UMulWithOverflow,
        UMulO,
        SAddSat,
        UAddSat,
        SSubSat,
        USubSat,
        Freeze,

        ReservedConst,
        ReservedInst,

        None,
    }

    public static class NativeSouperBlockApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* BlockGetName(SouperOpaqueBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BlockGetPreds(SouperOpaqueBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BlockGetNumber(SouperOpaqueBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BlockGetConcretePred(SouperOpaqueBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueInst>* BlockGetPredVars(SouperOpaqueBlock* block);
    }

    public static class NativeSouperInstApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperInstKind InstGetKind(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint InstGetWidth(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueBlock* InstGetBlock(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong InstGetVal(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* InstGetName(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* InstGetString(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<SouperOpaqueInst>* InstGetOrderedOps(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueValue>* InstGetOrigins(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong InstGetRangeMin(SouperOpaqueInst* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ulong InstGetRangeMax(SouperOpaqueInst* inst);
    }

    public static class NativeSouperInstMappingApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInstMapping* InstMappingConstructor(SouperOpaqueInst* lhs, SouperOpaqueInst* rhs);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstMappingGetLhs(SouperOpaqueInstMapping* mapping);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void InstMappingSetLhs(SouperOpaqueInstMapping* mapping, SouperOpaqueInst* lhs);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstMappingGetRhs(SouperOpaqueInstMapping* mapping);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void InstMappingSetRhs(SouperOpaqueInstMapping* mapping, SouperOpaqueInst* rhs);
    }

    public static class NativeSouperBlockPCMappingApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueBlockPCMapping* BlockPcMappingConstructor(SouperOpaqueBlock* block, uint index, SouperOpaqueInstMapping* pc);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueBlock* BlockPcMappingGetBlock(SouperOpaqueBlockPCMapping* mapping);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BlockPcMappingGetPrexIdx(SouperOpaqueBlockPCMapping* mapping);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInstMapping* BlockPcMappingGetPc(SouperOpaqueBlockPCMapping* mapping);
    }

    public static class NativeSouperReplacementContextApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueReplacementContext* ReplacementContextConstructor();
    }

    public static class NativeSouperInstContextApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInstContext* InstContextConstructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstContextGetConst(SouperOpaqueInstContext* ctx, uint width, ulong value);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstContextCreateVar(SouperOpaqueInstContext* ctx, uint width, sbyte* name);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstContextGetInst(SouperOpaqueInstContext* ctx, SouperInstKind kind, uint width, OpaqueManagedVector<SouperOpaqueInst>* operands, bool available);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstContextGetInstWithSingleArg(SouperOpaqueInstContext* ctx, SouperInstKind kind, uint width, SouperOpaqueInst* op1, bool available);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* InstContextGetInstWithDoubleArg(SouperOpaqueInstContext* ctx, SouperInstKind kind, uint width, SouperOpaqueInst* op1, SouperOpaqueInst* op2, bool available);
    }

}
