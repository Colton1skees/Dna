using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public static class NativeRemillUtilApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void* InitFunctionAttributes(LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* AddCall(LLVMOpaqueBasicBlock* sourceBlock, LLVMOpaqueValue* destFunc, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* AddTerminatingTailCallOnFunc(LLVMOpaqueValue* sourceFunc, LLVMOpaqueValue* destFunc, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* AddTerminatingTailCallOnBlock(LLVMOpaqueBasicBlock* sourceBlock, LLVMOpaqueValue* destFunc, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* FindVarInFunctionFromBlock(LLVMOpaqueBasicBlock* block, sbyte* name, bool allowFailure);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* FindVarInFunctionFromFunc(LLVMOpaqueValue* func, sbyte* name, bool allowFailure);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadStatePointerFromFunc(LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadStatePointerFromBlock(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadProgramCounter(LLVMOpaqueBasicBlock* block, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadNextProgramCounter(LLVMOpaqueBasicBlock* block, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadProgramCounterRef(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadNextProgramCounterRef(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadReturnProgramCounterRef(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void StoreProgramCounterWithIntrinsics(LLVMOpaqueBasicBlock* block, ulong pc, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void StoreProgramCounter(LLVMOpaqueBasicBlock* block, LLVMOpaqueValue* pc);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void StoreNextProgramCounter(LLVMOpaqueBasicBlock* block, LLVMOpaqueValue* pc);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadMemoryPointerArg(LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadProgramCounterArg(LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadMemoryPointer(LLVMOpaqueBasicBlock* block, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadMemoryPointerRef(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadBranchTaken(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadBranchTakenRef(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* FindFunction(LLVMOpaqueModule* module, sbyte* name);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* FindGlobalVariable(LLVMOpaqueModule* module, sbyte* name);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool VerifyModule(LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueModule* LoadModuleFromFile(LLVMOpaqueContext* context, sbyte* fileName);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueModule* LoadArchSemantics(RemillOpaqueArch* arch, sbyte* path);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool StoreModuleToFile(LLVMOpaqueModule* module, sbyte* fileName, bool allowFailure);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool StoreModuleIRToFile(LLVMOpaqueModule* module, sbyte* fileName, bool allowFailure);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* NthArgument(LLVMOpaqueValue* func, ulong index);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueValue>* LiftedFunctionArgs(LLVMOpaqueBasicBlock* block, RemillOpaqueIntrinsicTable* intrinsics);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* LLVMValueToString(LLVMOpaqueValue* thing);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* LLVMTypeToString(LLVMOpaqueType* type);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* LLVMModuleToString(LLVMOpaqueModule* mod);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void CloneFunctionInto(LLVMOpaqueValue* sourceFunc, LLVMOpaqueValue* destFunc);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMValueRef>* CallersOf(LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* ModuleName(LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint ReplaceAllUsesOfConstant(LLVMOpaqueValue* oldConstant, LLVMOpaqueValue* newConstant, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MoveFunctionIntoModule(LLVMOpaqueValue* func, LLVMOpaqueModule* destModule);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* RecontextualizeType(LLVMOpaqueType* type, LLVMOpaqueContext* context);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* LoadFromMemory(RemillOpaqueIntrinsicTable* intrinsics, LLVMOpaqueBasicBlock* block, LLVMOpaqueType* type, LLVMOpaqueValue* memPtr, LLVMOpaqueValue* addr);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* StoreToMemory(RemillOpaqueIntrinsicTable* intrinsics, LLVMOpaqueBasicBlock* block, LLVMOpaqueValue* valToStore, LLVMOpaqueValue* memPtr, LLVMOpaqueValue* addr);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* BuildPointerToOffset(LLVMOpaqueBuilder* ir, LLVMOpaqueValue* ptr, ulong destElemOffset, LLVMOpaqueType* destPtrType);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedPair* StripAndAccumulateConstantOffsets(LLVMOpaqueValue* value);
    }
}
