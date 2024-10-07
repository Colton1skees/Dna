#pragma once

#include <API/ExportDef.h>

#include "llvm-c/Analysis.h"
#include "llvm-c/BitReader.h"
#include "llvm-c/BitWriter.h"
// Missing blake3.h
#include "llvm-c/Comdat.h"
#include "llvm-c/Core.h"
#include "llvm-c/DataTypes.h"
#include "llvm-c/DebugInfo.h"
#include "llvm-c/Deprecated.h"
#include "llvm-c/Disassembler.h"
#include "llvm-c/DisassemblerTypes.h"
#include "llvm-c/Error.h"
#include "llvm-c/ErrorHandling.h"
#include "llvm-c/ExecutionEngine.h"
#include "llvm-c/ExternC.h"
#include "llvm-c/IRReader.h"
#include "llvm-c/Linker.h"
#include "llvm-c/LLJIT.h"
#include "llvm-c/lto.h"
#include "llvm-c/Object.h"
#include "llvm-c/Orc.h"
#include "llvm-c/OrcEE.h"
#include "llvm-c/Remarks.h"
#include "llvm-c/Support.h"
#include "llvm-c/Target.h"
#include "llvm-c/TargetMachine.h"
#include "llvm-c/Types.h"

DNA_EXPORT LLVMBool  FfiLLVMVerifyModule(LLVMModuleRef M, LLVMVerifierFailureAction Action, char** OutMessage)
{
    return LLVMVerifyModule(M, Action, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMVerifyFunction(LLVMValueRef Fn, LLVMVerifierFailureAction Action)
{
    return LLVMVerifyFunction(Fn, Action);
}


DNA_EXPORT void  FfiLLVMViewFunctionCFG(LLVMValueRef Fn)
{
    return LLVMViewFunctionCFG(Fn);
}


DNA_EXPORT void  FfiLLVMViewFunctionCFGOnly(LLVMValueRef Fn)
{
    return LLVMViewFunctionCFGOnly(Fn);
}


DNA_EXPORT LLVMBool  FfiLLVMParseBitcode(LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutModule, char** OutMessage)
{
    return LLVMParseBitcode(MemBuf, OutModule, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMParseBitcode2(LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutModule)
{
    return LLVMParseBitcode2(MemBuf, OutModule);
}


DNA_EXPORT LLVMBool  FfiLLVMParseBitcodeInContext(LLVMContextRef ContextRef, LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutModule, char** OutMessage)
{
    return LLVMParseBitcodeInContext(ContextRef, MemBuf, OutModule, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMParseBitcodeInContext2(LLVMContextRef ContextRef, LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutModule)
{
    return LLVMParseBitcodeInContext2(ContextRef, MemBuf, OutModule);
}


DNA_EXPORT LLVMBool  FfiLLVMGetBitcodeModuleInContext(LLVMContextRef ContextRef, LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutM, char** OutMessage)
{
    return LLVMGetBitcodeModuleInContext(ContextRef, MemBuf, OutM, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMGetBitcodeModuleInContext2(LLVMContextRef ContextRef, LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutM)
{
    return LLVMGetBitcodeModuleInContext2(ContextRef, MemBuf, OutM);
}


DNA_EXPORT LLVMBool  FfiLLVMGetBitcodeModule(LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutM, char** OutMessage)
{
    return LLVMGetBitcodeModule(MemBuf, OutM, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMGetBitcodeModule2(LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutM)
{
    return LLVMGetBitcodeModule2(MemBuf, OutM);
}


DNA_EXPORT int  FfiLLVMWriteBitcodeToFile(LLVMModuleRef M, const char* Path)
{
    return LLVMWriteBitcodeToFile(M, Path);
}


DNA_EXPORT int  FfiLLVMWriteBitcodeToFD(LLVMModuleRef M, int FD, int ShouldClose, int Unbuffered)
{
    return LLVMWriteBitcodeToFD(M, FD, ShouldClose, Unbuffered);
}


DNA_EXPORT int  FfiLLVMWriteBitcodeToFileHandle(LLVMModuleRef M, int Handle)
{
    return LLVMWriteBitcodeToFileHandle(M, Handle);
}


DNA_EXPORT LLVMMemoryBufferRef  FfiLLVMWriteBitcodeToMemoryBuffer(LLVMModuleRef M)
{
    return LLVMWriteBitcodeToMemoryBuffer(M);
}


DNA_EXPORT LLVMComdatRef  FfiLLVMGetOrInsertComdat(LLVMModuleRef M, const char* Name)
{
    return LLVMGetOrInsertComdat(M, Name);
}


DNA_EXPORT LLVMComdatRef  FfiLLVMGetComdat(LLVMValueRef V)
{
    return LLVMGetComdat(V);
}


DNA_EXPORT void  FfiLLVMSetComdat(LLVMValueRef V, LLVMComdatRef C)
{
    return LLVMSetComdat(V, C);
}


DNA_EXPORT LLVMComdatSelectionKind  FfiLLVMGetComdatSelectionKind(LLVMComdatRef C)
{
    return LLVMGetComdatSelectionKind(C);
}


DNA_EXPORT void  FfiLLVMSetComdatSelectionKind(LLVMComdatRef C, LLVMComdatSelectionKind Kind)
{
    return LLVMSetComdatSelectionKind(C, Kind);
}


DNA_EXPORT void  FfiLLVMShutdown()
{
    return LLVMShutdown();
}


DNA_EXPORT void  FfiLLVMGetVersion(unsigned* Major, unsigned* Minor, unsigned* Patch)
{
    return LLVMGetVersion(Major, Minor, Patch);
}


DNA_EXPORT char* FfiLLVMCreateMessage(const char* Message)
{
    return LLVMCreateMessage(Message);
}


DNA_EXPORT void  FfiLLVMDisposeMessage(char* Message)
{
    return LLVMDisposeMessage(Message);
}


DNA_EXPORT LLVMContextRef  FfiLLVMContextCreate()
{
    return LLVMContextCreate();
}


DNA_EXPORT LLVMContextRef  FfiLLVMGetGlobalContext()
{
    return LLVMGetGlobalContext();
}


DNA_EXPORT void  FfiLLVMContextSetDiagnosticHandler(LLVMContextRef C, LLVMDiagnosticHandler Handler, void* DiagnosticContext)
{
    return LLVMContextSetDiagnosticHandler(C, Handler, DiagnosticContext);
}


DNA_EXPORT LLVMDiagnosticHandler  FfiLLVMContextGetDiagnosticHandler(LLVMContextRef C)
{
    return LLVMContextGetDiagnosticHandler(C);
}


DNA_EXPORT void* FfiLLVMContextGetDiagnosticContext(LLVMContextRef C)
{
    return LLVMContextGetDiagnosticContext(C);
}


DNA_EXPORT void  FfiLLVMContextSetYieldCallback(LLVMContextRef C, LLVMYieldCallback Callback, void* OpaqueHandle)
{
    return LLVMContextSetYieldCallback(C, Callback, OpaqueHandle);
}


DNA_EXPORT LLVMBool  FfiLLVMContextShouldDiscardValueNames(LLVMContextRef C)
{
    return LLVMContextShouldDiscardValueNames(C);
}


DNA_EXPORT void  FfiLLVMContextSetDiscardValueNames(LLVMContextRef C, LLVMBool Discard)
{
    return LLVMContextSetDiscardValueNames(C, Discard);
}


DNA_EXPORT void  FfiLLVMContextDispose(LLVMContextRef C)
{
    return LLVMContextDispose(C);
}


DNA_EXPORT char* FfiLLVMGetDiagInfoDescription(LLVMDiagnosticInfoRef DI)
{
    return LLVMGetDiagInfoDescription(DI);
}


DNA_EXPORT LLVMDiagnosticSeverity  FfiLLVMGetDiagInfoSeverity(LLVMDiagnosticInfoRef DI)
{
    return LLVMGetDiagInfoSeverity(DI);
}


DNA_EXPORT unsigned  FfiLLVMGetMDKindIDInContext(LLVMContextRef C, const char* Name, unsigned SLen)
{
    return LLVMGetMDKindIDInContext(C, Name, SLen);
}


DNA_EXPORT unsigned  FfiLLVMGetMDKindID(const char* Name, unsigned SLen)
{
    return LLVMGetMDKindID(Name, SLen);
}


DNA_EXPORT unsigned  FfiLLVMGetEnumAttributeKindForName(const char* Name, size_t SLen)
{
    return LLVMGetEnumAttributeKindForName(Name, SLen);
}


DNA_EXPORT unsigned  FfiLLVMGetLastEnumAttributeKind()
{
    return LLVMGetLastEnumAttributeKind();
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMCreateEnumAttribute(LLVMContextRef C, unsigned KindID, uint64_t Val)
{
    return LLVMCreateEnumAttribute(C, KindID, Val);
}


DNA_EXPORT unsigned  FfiLLVMGetEnumAttributeKind(LLVMAttributeRef A)
{
    return LLVMGetEnumAttributeKind(A);
}


DNA_EXPORT uint64_t  FfiLLVMGetEnumAttributeValue(LLVMAttributeRef A)
{
    return LLVMGetEnumAttributeValue(A);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMCreateTypeAttribute(LLVMContextRef C, unsigned KindID, LLVMTypeRef type_ref)
{
    return LLVMCreateTypeAttribute(C, KindID, type_ref);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetTypeAttributeValue(LLVMAttributeRef A)
{
    return LLVMGetTypeAttributeValue(A);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMCreateStringAttribute(LLVMContextRef C, const char* K, unsigned KLength, const char* V, unsigned VLength)
{
    return LLVMCreateStringAttribute(C, K, KLength, V, VLength);
}


DNA_EXPORT const char* FfiLLVMGetStringAttributeKind(LLVMAttributeRef A, unsigned* Length)
{
    return LLVMGetStringAttributeKind(A, Length);
}


DNA_EXPORT const char* FfiLLVMGetStringAttributeValue(LLVMAttributeRef A, unsigned* Length)
{
    return LLVMGetStringAttributeValue(A, Length);
}


DNA_EXPORT LLVMBool  FfiLLVMIsEnumAttribute(LLVMAttributeRef A)
{
    return LLVMIsEnumAttribute(A);
}


DNA_EXPORT LLVMBool  FfiLLVMIsStringAttribute(LLVMAttributeRef A)
{
    return LLVMIsStringAttribute(A);
}


DNA_EXPORT LLVMBool  FfiLLVMIsTypeAttribute(LLVMAttributeRef A)
{
    return LLVMIsTypeAttribute(A);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetTypeByName2(LLVMContextRef C, const char* Name)
{
    return LLVMGetTypeByName2(C, Name);
}


DNA_EXPORT LLVMModuleRef  FfiLLVMModuleCreateWithName(const char* ModuleID)
{
    return LLVMModuleCreateWithName(ModuleID);
}


DNA_EXPORT LLVMModuleRef  FfiLLVMModuleCreateWithNameInContext(const char* ModuleID, LLVMContextRef C)
{
    return LLVMModuleCreateWithNameInContext(ModuleID, C);
}


DNA_EXPORT LLVMModuleRef  FfiLLVMCloneModule(LLVMModuleRef M)
{
    return LLVMCloneModule(M);
}


DNA_EXPORT void  FfiLLVMDisposeModule(LLVMModuleRef M)
{
    return LLVMDisposeModule(M);
}


DNA_EXPORT const char* FfiLLVMGetModuleIdentifier(LLVMModuleRef M, size_t* Len)
{
    return LLVMGetModuleIdentifier(M, Len);
}


DNA_EXPORT void  FfiLLVMSetModuleIdentifier(LLVMModuleRef M, const char* Ident, size_t Len)
{
    return LLVMSetModuleIdentifier(M, Ident, Len);
}


DNA_EXPORT const char* FfiLLVMGetSourceFileName(LLVMModuleRef M, size_t* Len)
{
    return LLVMGetSourceFileName(M, Len);
}


DNA_EXPORT void  FfiLLVMSetSourceFileName(LLVMModuleRef M, const char* Name, size_t Len)
{
    return LLVMSetSourceFileName(M, Name, Len);
}


DNA_EXPORT const char* FfiLLVMGetDataLayoutStr(LLVMModuleRef M)
{
    return LLVMGetDataLayoutStr(M);
}


DNA_EXPORT const char* FfiLLVMGetDataLayout(LLVMModuleRef M)
{
    return LLVMGetDataLayout(M);
}


DNA_EXPORT void  FfiLLVMSetDataLayout(LLVMModuleRef M, const char* DataLayoutStr)
{
    return LLVMSetDataLayout(M, DataLayoutStr);
}


DNA_EXPORT const char* FfiLLVMGetTarget(LLVMModuleRef M)
{
    return LLVMGetTarget(M);
}


DNA_EXPORT void  FfiLLVMSetTarget(LLVMModuleRef M, const char* Triple)
{
    return LLVMSetTarget(M, Triple);
}


DNA_EXPORT LLVMModuleFlagEntry* FfiLLVMCopyModuleFlagsMetadata(LLVMModuleRef M, size_t* Len)
{
    return LLVMCopyModuleFlagsMetadata(M, Len);
}


DNA_EXPORT void  FfiLLVMDisposeModuleFlagsMetadata(LLVMModuleFlagEntry* Entries)
{
    return LLVMDisposeModuleFlagsMetadata(Entries);
}


DNA_EXPORT const char* FfiLLVMModuleFlagEntriesGetKey(LLVMModuleFlagEntry* Entries, unsigned Index, size_t* Len)
{
    return LLVMModuleFlagEntriesGetKey(Entries, Index, Len);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMModuleFlagEntriesGetMetadata(LLVMModuleFlagEntry* Entries, unsigned Index)
{
    return LLVMModuleFlagEntriesGetMetadata(Entries, Index);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMGetModuleFlag(LLVMModuleRef M, const char* Key, size_t KeyLen)
{
    return LLVMGetModuleFlag(M, Key, KeyLen);
}


DNA_EXPORT void  FfiLLVMAddModuleFlag(LLVMModuleRef M, LLVMModuleFlagBehavior Behavior, const char* Key, size_t KeyLen, LLVMMetadataRef Val)
{
    return LLVMAddModuleFlag(M, Behavior, Key, KeyLen, Val);
}


DNA_EXPORT void  FfiLLVMDumpModule(LLVMModuleRef M)
{
    return LLVMDumpModule(M);
}


DNA_EXPORT LLVMBool  FfiLLVMPrintModuleToFile(LLVMModuleRef M, const char* Filename, char** ErrorMessage)
{
    return LLVMPrintModuleToFile(M, Filename, ErrorMessage);
}


DNA_EXPORT char* FfiLLVMPrintModuleToString(LLVMModuleRef M)
{
    return LLVMPrintModuleToString(M);
}


DNA_EXPORT const char* FfiLLVMGetModuleInlineAsm(LLVMModuleRef M, size_t* Len)
{
    return LLVMGetModuleInlineAsm(M, Len);
}


DNA_EXPORT void  FfiLLVMSetModuleInlineAsm2(LLVMModuleRef M, const char* Asm, size_t Len)
{
    return LLVMSetModuleInlineAsm2(M, Asm, Len);
}


DNA_EXPORT void  FfiLLVMAppendModuleInlineAsm(LLVMModuleRef M, const char* Asm, size_t Len)
{
    return LLVMAppendModuleInlineAsm(M, Asm, Len);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetInlineAsm(LLVMTypeRef Ty, char* AsmString, size_t AsmStringSize, char* Constraints, size_t ConstraintsSize, LLVMBool HasSideEffects, LLVMBool IsAlignStack, LLVMInlineAsmDialect Dialect, LLVMBool CanThrow)
{
    return LLVMGetInlineAsm(Ty, AsmString, AsmStringSize, Constraints, ConstraintsSize, HasSideEffects, IsAlignStack, Dialect, CanThrow);
}


DNA_EXPORT LLVMContextRef  FfiLLVMGetModuleContext(LLVMModuleRef M)
{
    return LLVMGetModuleContext(M);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetTypeByName(LLVMModuleRef M, const char* Name)
{
    return LLVMGetTypeByName(M, Name);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetFirstNamedMetadata(LLVMModuleRef M)
{
    return LLVMGetFirstNamedMetadata(M);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetLastNamedMetadata(LLVMModuleRef M)
{
    return LLVMGetLastNamedMetadata(M);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetNextNamedMetadata(LLVMNamedMDNodeRef NamedMDNode)
{
    return LLVMGetNextNamedMetadata(NamedMDNode);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetPreviousNamedMetadata(LLVMNamedMDNodeRef NamedMDNode)
{
    return LLVMGetPreviousNamedMetadata(NamedMDNode);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetNamedMetadata(LLVMModuleRef M, const char* Name, size_t NameLen)
{
    return LLVMGetNamedMetadata(M, Name, NameLen);
}


DNA_EXPORT LLVMNamedMDNodeRef  FfiLLVMGetOrInsertNamedMetadata(LLVMModuleRef M, const char* Name, size_t NameLen)
{
    return LLVMGetOrInsertNamedMetadata(M, Name, NameLen);
}


DNA_EXPORT const char* FfiLLVMGetNamedMetadataName(LLVMNamedMDNodeRef NamedMD, size_t* NameLen)
{
    return LLVMGetNamedMetadataName(NamedMD, NameLen);
}


DNA_EXPORT unsigned  FfiLLVMGetNamedMetadataNumOperands(LLVMModuleRef M, const char* Name)
{
    return LLVMGetNamedMetadataNumOperands(M, Name);
}


DNA_EXPORT void  FfiLLVMGetNamedMetadataOperands(LLVMModuleRef M, const char* Name, LLVMValueRef* Dest)
{
    return LLVMGetNamedMetadataOperands(M, Name, Dest);
}


DNA_EXPORT void  FfiLLVMAddNamedMetadataOperand(LLVMModuleRef M, const char* Name, LLVMValueRef Val)
{
    return LLVMAddNamedMetadataOperand(M, Name, Val);
}


DNA_EXPORT const char* FfiLLVMGetDebugLocDirectory(LLVMValueRef Val, unsigned* Length)
{
    return LLVMGetDebugLocDirectory(Val, Length);
}


DNA_EXPORT const char* FfiLLVMGetDebugLocFilename(LLVMValueRef Val, unsigned* Length)
{
    return LLVMGetDebugLocFilename(Val, Length);
}


DNA_EXPORT unsigned  FfiLLVMGetDebugLocLine(LLVMValueRef Val)
{
    return LLVMGetDebugLocLine(Val);
}


DNA_EXPORT unsigned  FfiLLVMGetDebugLocColumn(LLVMValueRef Val)
{
    return LLVMGetDebugLocColumn(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAddFunction(LLVMModuleRef M, const char* Name, LLVMTypeRef FunctionTy)
{
    return LLVMAddFunction(M, Name, FunctionTy);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNamedFunction(LLVMModuleRef M, const char* Name)
{
    return LLVMGetNamedFunction(M, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstFunction(LLVMModuleRef M)
{
    return LLVMGetFirstFunction(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastFunction(LLVMModuleRef M)
{
    return LLVMGetLastFunction(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextFunction(LLVMValueRef Fn)
{
    return LLVMGetNextFunction(Fn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousFunction(LLVMValueRef Fn)
{
    return LLVMGetPreviousFunction(Fn);
}


DNA_EXPORT void  FfiLLVMSetModuleInlineAsm(LLVMModuleRef M, const char* Asm)
{
    return LLVMSetModuleInlineAsm(M, Asm);
}


DNA_EXPORT LLVMTypeKind  FfiLLVMGetTypeKind(LLVMTypeRef Ty)
{
    return LLVMGetTypeKind(Ty);
}


DNA_EXPORT LLVMBool  FfiLLVMTypeIsSized(LLVMTypeRef Ty)
{
    return LLVMTypeIsSized(Ty);
}


DNA_EXPORT LLVMContextRef  FfiLLVMGetTypeContext(LLVMTypeRef Ty)
{
    return LLVMGetTypeContext(Ty);
}


DNA_EXPORT void  FfiLLVMDumpType(LLVMTypeRef Val)
{
    return LLVMDumpType(Val);
}


DNA_EXPORT char* FfiLLVMPrintTypeToString(LLVMTypeRef Val)
{
    return LLVMPrintTypeToString(Val);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt1TypeInContext(LLVMContextRef C)
{
    return LLVMInt1TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt8TypeInContext(LLVMContextRef C)
{
    return LLVMInt8TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt16TypeInContext(LLVMContextRef C)
{
    return LLVMInt16TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt32TypeInContext(LLVMContextRef C)
{
    return LLVMInt32TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt64TypeInContext(LLVMContextRef C)
{
    return LLVMInt64TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt128TypeInContext(LLVMContextRef C)
{
    return LLVMInt128TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntTypeInContext(LLVMContextRef C, unsigned NumBits)
{
    return LLVMIntTypeInContext(C, NumBits);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt1Type()
{
    return LLVMInt1Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt8Type()
{
    return LLVMInt8Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt16Type()
{
    return LLVMInt16Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt32Type()
{
    return LLVMInt32Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt64Type()
{
    return LLVMInt64Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMInt128Type()
{
    return LLVMInt128Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntType(unsigned NumBits)
{
    return LLVMIntType(NumBits);
}


DNA_EXPORT unsigned  FfiLLVMGetIntTypeWidth(LLVMTypeRef IntegerTy)
{
    return LLVMGetIntTypeWidth(IntegerTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMHalfTypeInContext(LLVMContextRef C)
{
    return LLVMHalfTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMBFloatTypeInContext(LLVMContextRef C)
{
    return LLVMBFloatTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMFloatTypeInContext(LLVMContextRef C)
{
    return LLVMFloatTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMDoubleTypeInContext(LLVMContextRef C)
{
    return LLVMDoubleTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86FP80TypeInContext(LLVMContextRef C)
{
    return LLVMX86FP80TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMFP128TypeInContext(LLVMContextRef C)
{
    return LLVMFP128TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMPPCFP128TypeInContext(LLVMContextRef C)
{
    return LLVMPPCFP128TypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMHalfType()
{
    return LLVMHalfType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMBFloatType()
{
    return LLVMBFloatType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMFloatType()
{
    return LLVMFloatType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMDoubleType()
{
    return LLVMDoubleType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86FP80Type()
{
    return LLVMX86FP80Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMFP128Type()
{
    return LLVMFP128Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMPPCFP128Type()
{
    return LLVMPPCFP128Type();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMFunctionType(LLVMTypeRef ReturnType, LLVMTypeRef* ParamTypes, unsigned ParamCount, LLVMBool IsVarArg)
{
    return LLVMFunctionType(ReturnType, ParamTypes, ParamCount, IsVarArg);
}


DNA_EXPORT LLVMBool  FfiLLVMIsFunctionVarArg(LLVMTypeRef FunctionTy)
{
    return LLVMIsFunctionVarArg(FunctionTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetReturnType(LLVMTypeRef FunctionTy)
{
    return LLVMGetReturnType(FunctionTy);
}


DNA_EXPORT unsigned  FfiLLVMCountParamTypes(LLVMTypeRef FunctionTy)
{
    return LLVMCountParamTypes(FunctionTy);
}


DNA_EXPORT void  FfiLLVMGetParamTypes(LLVMTypeRef FunctionTy, LLVMTypeRef* Dest)
{
    return LLVMGetParamTypes(FunctionTy, Dest);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMStructTypeInContext(LLVMContextRef C, LLVMTypeRef* ElementTypes, unsigned ElementCount, LLVMBool Packed)
{
    return LLVMStructTypeInContext(C, ElementTypes, ElementCount, Packed);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMStructType(LLVMTypeRef* ElementTypes, unsigned ElementCount, LLVMBool Packed)
{
    return LLVMStructType(ElementTypes, ElementCount, Packed);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMStructCreateNamed(LLVMContextRef C, const char* Name)
{
    return LLVMStructCreateNamed(C, Name);
}


DNA_EXPORT const char* FfiLLVMGetStructName(LLVMTypeRef Ty)
{
    return LLVMGetStructName(Ty);
}


DNA_EXPORT void  FfiLLVMStructSetBody(LLVMTypeRef StructTy, LLVMTypeRef* ElementTypes, unsigned ElementCount, LLVMBool Packed)
{
    return LLVMStructSetBody(StructTy, ElementTypes, ElementCount, Packed);
}


DNA_EXPORT unsigned  FfiLLVMCountStructElementTypes(LLVMTypeRef StructTy)
{
    return LLVMCountStructElementTypes(StructTy);
}


DNA_EXPORT void  FfiLLVMGetStructElementTypes(LLVMTypeRef StructTy, LLVMTypeRef* Dest)
{
    return LLVMGetStructElementTypes(StructTy, Dest);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMStructGetTypeAtIndex(LLVMTypeRef StructTy, unsigned i)
{
    return LLVMStructGetTypeAtIndex(StructTy, i);
}


DNA_EXPORT LLVMBool  FfiLLVMIsPackedStruct(LLVMTypeRef StructTy)
{
    return LLVMIsPackedStruct(StructTy);
}


DNA_EXPORT LLVMBool  FfiLLVMIsOpaqueStruct(LLVMTypeRef StructTy)
{
    return LLVMIsOpaqueStruct(StructTy);
}


DNA_EXPORT LLVMBool  FfiLLVMIsLiteralStruct(LLVMTypeRef StructTy)
{
    return LLVMIsLiteralStruct(StructTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetElementType(LLVMTypeRef Ty)
{
    return LLVMGetElementType(Ty);
}


DNA_EXPORT void  FfiLLVMGetSubtypes(LLVMTypeRef Tp, LLVMTypeRef* Arr)
{
    return LLVMGetSubtypes(Tp, Arr);
}


DNA_EXPORT unsigned  FfiLLVMGetNumContainedTypes(LLVMTypeRef Tp)
{
    return LLVMGetNumContainedTypes(Tp);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMArrayType(LLVMTypeRef ElementType, unsigned ElementCount)
{
    return LLVMArrayType(ElementType, ElementCount);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMArrayType2(LLVMTypeRef ElementType, uint64_t ElementCount)
{
    return LLVMArrayType2(ElementType, ElementCount);
}


DNA_EXPORT unsigned  FfiLLVMGetArrayLength(LLVMTypeRef ArrayTy)
{
    return LLVMGetArrayLength(ArrayTy);
}


DNA_EXPORT uint64_t  FfiLLVMGetArrayLength2(LLVMTypeRef ArrayTy)
{
    return LLVMGetArrayLength2(ArrayTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMPointerType(LLVMTypeRef ElementType, unsigned AddressSpace)
{
    return LLVMPointerType(ElementType, AddressSpace);
}


DNA_EXPORT LLVMBool  FfiLLVMPointerTypeIsOpaque(LLVMTypeRef Ty)
{
    return LLVMPointerTypeIsOpaque(Ty);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMPointerTypeInContext(LLVMContextRef C, unsigned AddressSpace)
{
    return LLVMPointerTypeInContext(C, AddressSpace);
}


DNA_EXPORT unsigned  FfiLLVMGetPointerAddressSpace(LLVMTypeRef PointerTy)
{
    return LLVMGetPointerAddressSpace(PointerTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMVectorType(LLVMTypeRef ElementType, unsigned ElementCount)
{
    return LLVMVectorType(ElementType, ElementCount);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMScalableVectorType(LLVMTypeRef ElementType, unsigned ElementCount)
{
    return LLVMScalableVectorType(ElementType, ElementCount);
}


DNA_EXPORT unsigned  FfiLLVMGetVectorSize(LLVMTypeRef VectorTy)
{
    return LLVMGetVectorSize(VectorTy);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMVoidTypeInContext(LLVMContextRef C)
{
    return LLVMVoidTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMLabelTypeInContext(LLVMContextRef C)
{
    return LLVMLabelTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86MMXTypeInContext(LLVMContextRef C)
{
    return LLVMX86MMXTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86AMXTypeInContext(LLVMContextRef C)
{
    return LLVMX86AMXTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMTokenTypeInContext(LLVMContextRef C)
{
    return LLVMTokenTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMMetadataTypeInContext(LLVMContextRef C)
{
    return LLVMMetadataTypeInContext(C);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMVoidType()
{
    return LLVMVoidType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMLabelType()
{
    return LLVMLabelType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86MMXType()
{
    return LLVMX86MMXType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMX86AMXType()
{
    return LLVMX86AMXType();
}


DNA_EXPORT LLVMTypeRef  FfiLLVMTargetExtTypeInContext(LLVMContextRef C, const char* Name, LLVMTypeRef* TypeParams, unsigned TypeParamCount, unsigned* IntParams, unsigned IntParamCount)
{
    return LLVMTargetExtTypeInContext(C, Name, TypeParams, TypeParamCount, IntParams, IntParamCount);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMTypeOf(LLVMValueRef Val)
{
    return LLVMTypeOf(Val);
}


DNA_EXPORT LLVMValueKind  FfiLLVMGetValueKind(LLVMValueRef Val)
{
    return LLVMGetValueKind(Val);
}


DNA_EXPORT const char* FfiLLVMGetValueName2(LLVMValueRef Val, size_t* Length)
{
    return LLVMGetValueName2(Val, Length);
}


DNA_EXPORT void  FfiLLVMSetValueName2(LLVMValueRef Val, const char* Name, size_t NameLen)
{
    return LLVMSetValueName2(Val, Name, NameLen);
}


DNA_EXPORT void  FfiLLVMDumpValue(LLVMValueRef Val)
{
    return LLVMDumpValue(Val);
}


DNA_EXPORT char* FfiLLVMPrintValueToString(LLVMValueRef Val)
{
    return LLVMPrintValueToString(Val);
}


DNA_EXPORT void  FfiLLVMReplaceAllUsesWith(LLVMValueRef OldVal, LLVMValueRef NewVal)
{
    return LLVMReplaceAllUsesWith(OldVal, NewVal);
}


DNA_EXPORT LLVMBool  FfiLLVMIsConstant(LLVMValueRef Val)
{
    return LLVMIsConstant(Val);
}


DNA_EXPORT LLVMBool  FfiLLVMIsUndef(LLVMValueRef Val)
{
    return LLVMIsUndef(Val);
}


DNA_EXPORT LLVMBool  FfiLLVMIsPoison(LLVMValueRef Val)
{
    return LLVMIsPoison(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMIsAMDNode(LLVMValueRef Val)
{
    return LLVMIsAMDNode(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMIsAValueAsMetadata(LLVMValueRef Val)
{
    return LLVMIsAValueAsMetadata(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMIsAMDString(LLVMValueRef Val)
{
    return LLVMIsAMDString(Val);
}


DNA_EXPORT const char* FfiLLVMGetValueName(LLVMValueRef Val)
{
    return LLVMGetValueName(Val);
}


DNA_EXPORT void  FfiLLVMSetValueName(LLVMValueRef Val, const char* Name)
{
    return LLVMSetValueName(Val, Name);
}


DNA_EXPORT LLVMUseRef  FfiLLVMGetFirstUse(LLVMValueRef Val)
{
    return LLVMGetFirstUse(Val);
}


DNA_EXPORT LLVMUseRef  FfiLLVMGetNextUse(LLVMUseRef U)
{
    return LLVMGetNextUse(U);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetUser(LLVMUseRef U)
{
    return LLVMGetUser(U);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetUsedValue(LLVMUseRef U)
{
    return LLVMGetUsedValue(U);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetOperand(LLVMValueRef Val, unsigned Index)
{
    return LLVMGetOperand(Val, Index);
}


DNA_EXPORT LLVMUseRef  FfiLLVMGetOperandUse(LLVMValueRef Val, unsigned Index)
{
    return LLVMGetOperandUse(Val, Index);
}


DNA_EXPORT void  FfiLLVMSetOperand(LLVMValueRef User, unsigned Index, LLVMValueRef Val)
{
    return LLVMSetOperand(User, Index, Val);
}


DNA_EXPORT int  FfiLLVMGetNumOperands(LLVMValueRef Val)
{
    return LLVMGetNumOperands(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstAllOnes(LLVMTypeRef Ty)
{
    return LLVMConstAllOnes(Ty);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetUndef(LLVMTypeRef Ty)
{
    return LLVMGetUndef(Ty);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPoison(LLVMTypeRef Ty)
{
    return LLVMGetPoison(Ty);
}


DNA_EXPORT LLVMBool  FfiLLVMIsNull(LLVMValueRef Val)
{
    return LLVMIsNull(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstPointerNull(LLVMTypeRef Ty)
{
    return LLVMConstPointerNull(Ty);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstInt(LLVMTypeRef IntTy, unsigned long long N, LLVMBool SignExtend)
{
    return LLVMConstInt(IntTy, N, SignExtend);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstIntOfArbitraryPrecision(LLVMTypeRef IntTy, unsigned NumWords, const uint64_t Words[])
{
    return LLVMConstIntOfArbitraryPrecision(IntTy, NumWords, Words);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstIntOfString(LLVMTypeRef IntTy, const char* Text, uint8_t Radix)
{
    return LLVMConstIntOfString(IntTy, Text, Radix);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstIntOfStringAndSize(LLVMTypeRef IntTy, const char* Text, unsigned SLen, uint8_t Radix)
{
    return LLVMConstIntOfStringAndSize(IntTy, Text, SLen, Radix);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstReal(LLVMTypeRef RealTy, double N)
{
    return LLVMConstReal(RealTy, N);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstRealOfString(LLVMTypeRef RealTy, const char* Text)
{
    return LLVMConstRealOfString(RealTy, Text);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstRealOfStringAndSize(LLVMTypeRef RealTy, const char* Text, unsigned SLen)
{
    return LLVMConstRealOfStringAndSize(RealTy, Text, SLen);
}


DNA_EXPORT unsigned long long  FfiLLVMConstIntGetZExtValue(LLVMValueRef ConstantVal)
{
    return LLVMConstIntGetZExtValue(ConstantVal);
}


DNA_EXPORT long long  FfiLLVMConstIntGetSExtValue(LLVMValueRef ConstantVal)
{
    return LLVMConstIntGetSExtValue(ConstantVal);
}


DNA_EXPORT double  FfiLLVMConstRealGetDouble(LLVMValueRef ConstantVal, LLVMBool* losesInfo)
{
    return LLVMConstRealGetDouble(ConstantVal, losesInfo);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstStringInContext(LLVMContextRef C, const char* Str, unsigned Length, LLVMBool DontNullTerminate)
{
    return LLVMConstStringInContext(C, Str, Length, DontNullTerminate);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstString(const char* Str, unsigned Length, LLVMBool DontNullTerminate)
{
    return LLVMConstString(Str, Length, DontNullTerminate);
}


DNA_EXPORT LLVMBool  FfiLLVMIsConstantString(LLVMValueRef c)
{
    return LLVMIsConstantString(c);
}


DNA_EXPORT const char* FfiLLVMGetAsString(LLVMValueRef c, size_t* Length)
{
    return LLVMGetAsString(c, Length);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstStructInContext(LLVMContextRef C, LLVMValueRef* ConstantVals, unsigned Count, LLVMBool Packed)
{
    return LLVMConstStructInContext(C, ConstantVals, Count, Packed);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstStruct(LLVMValueRef* ConstantVals, unsigned Count, LLVMBool Packed)
{
    return LLVMConstStruct(ConstantVals, Count, Packed);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstArray(LLVMTypeRef ElementTy, LLVMValueRef* ConstantVals, unsigned Length)
{
    return LLVMConstArray(ElementTy, ConstantVals, Length);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstArray2(LLVMTypeRef ElementTy, LLVMValueRef* ConstantVals, uint64_t Length)
{
    return LLVMConstArray2(ElementTy, ConstantVals, Length);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNamedStruct(LLVMTypeRef StructTy, LLVMValueRef* ConstantVals, unsigned Count)
{
    return LLVMConstNamedStruct(StructTy, ConstantVals, Count);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetAggregateElement(LLVMValueRef C, unsigned Idx)
{
    return LLVMGetAggregateElement(C, Idx);
}


DNA_EXPORT  LLVMValueRef  FfiLLVMGetElementAsConstant(LLVMValueRef C, unsigned idx)
{
    return LLVMGetElementAsConstant(C, idx);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstVector(LLVMValueRef* ScalarConstantVals, unsigned Size)
{
    return LLVMConstVector(ScalarConstantVals, Size);
}


DNA_EXPORT LLVMOpcode  FfiLLVMGetConstOpcode(LLVMValueRef ConstantVal)
{
    return LLVMGetConstOpcode(ConstantVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAlignOf(LLVMTypeRef Ty)
{
    return LLVMAlignOf(Ty);
}


DNA_EXPORT LLVMValueRef  FfiLLVMSizeOf(LLVMTypeRef Ty)
{
    return LLVMSizeOf(Ty);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNeg(LLVMValueRef ConstantVal)
{
    return LLVMConstNeg(ConstantVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNSWNeg(LLVMValueRef ConstantVal)
{
    return LLVMConstNSWNeg(ConstantVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNUWNeg(LLVMValueRef ConstantVal)
{
    return LLVMConstNUWNeg(ConstantVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNot(LLVMValueRef ConstantVal)
{
    return LLVMConstNot(ConstantVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstAdd(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstAdd(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNSWAdd(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNSWAdd(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNUWAdd(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNUWAdd(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstSub(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstSub(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNSWSub(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNSWSub(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNUWSub(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNUWSub(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstMul(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstMul(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNSWMul(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNSWMul(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstNUWMul(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstNUWMul(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstAnd(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstAnd(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstOr(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstOr(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstXor(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstXor(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstICmp(LLVMIntPredicate Predicate, LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstICmp(Predicate, LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFCmp(LLVMRealPredicate Predicate, LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstFCmp(Predicate, LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstShl(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstShl(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstLShr(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstLShr(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstAShr(LLVMValueRef LHSConstant, LLVMValueRef RHSConstant)
{
    return LLVMConstAShr(LHSConstant, RHSConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstGEP2(LLVMTypeRef Ty, LLVMValueRef ConstantVal, LLVMValueRef* ConstantIndices, unsigned NumIndices)
{
    return LLVMConstGEP2(Ty, ConstantVal, ConstantIndices, NumIndices);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstInBoundsGEP2(LLVMTypeRef Ty, LLVMValueRef ConstantVal, LLVMValueRef* ConstantIndices, unsigned NumIndices)
{
    return LLVMConstInBoundsGEP2(Ty, ConstantVal, ConstantIndices, NumIndices);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstTrunc(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstTrunc(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstSExt(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstSExt(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstZExt(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstZExt(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFPTrunc(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstFPTrunc(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFPExt(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstFPExt(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstUIToFP(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstUIToFP(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstSIToFP(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstSIToFP(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFPToUI(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstFPToUI(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFPToSI(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstFPToSI(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstPtrToInt(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstPtrToInt(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstIntToPtr(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstIntToPtr(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstBitCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstBitCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstAddrSpaceCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstAddrSpaceCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstZExtOrBitCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstZExtOrBitCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstSExtOrBitCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstSExtOrBitCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstTruncOrBitCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstTruncOrBitCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstPointerCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstPointerCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstIntCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType, LLVMBool isSigned)
{
    return LLVMConstIntCast(ConstantVal, ToType, isSigned);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstFPCast(LLVMValueRef ConstantVal, LLVMTypeRef ToType)
{
    return LLVMConstFPCast(ConstantVal, ToType);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstExtractElement(LLVMValueRef VectorConstant, LLVMValueRef IndexConstant)
{
    return LLVMConstExtractElement(VectorConstant, IndexConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstInsertElement(LLVMValueRef VectorConstant, LLVMValueRef ElementValueConstant, LLVMValueRef IndexConstant)
{
    return LLVMConstInsertElement(VectorConstant, ElementValueConstant, IndexConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstShuffleVector(LLVMValueRef VectorAConstant, LLVMValueRef VectorBConstant, LLVMValueRef MaskConstant)
{
    return LLVMConstShuffleVector(VectorAConstant, VectorBConstant, MaskConstant);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBlockAddress(LLVMValueRef F, LLVMBasicBlockRef BB)
{
    return LLVMBlockAddress(F, BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMConstInlineAsm(LLVMTypeRef Ty, const char* AsmString, const char* Constraints, LLVMBool HasSideEffects, LLVMBool IsAlignStack)
{
    return LLVMConstInlineAsm(Ty, AsmString, Constraints, HasSideEffects, IsAlignStack);
}


DNA_EXPORT LLVMModuleRef  FfiLLVMGetGlobalParent(LLVMValueRef Global)
{
    return LLVMGetGlobalParent(Global);
}


DNA_EXPORT LLVMBool  FfiLLVMIsDeclaration(LLVMValueRef Global)
{
    return LLVMIsDeclaration(Global);
}


DNA_EXPORT LLVMLinkage  FfiLLVMGetLinkage(LLVMValueRef Global)
{
    return LLVMGetLinkage(Global);
}


DNA_EXPORT void  FfiLLVMSetLinkage(LLVMValueRef Global, LLVMLinkage Linkage)
{
    return LLVMSetLinkage(Global, Linkage);
}


DNA_EXPORT const char* FfiLLVMGetSection(LLVMValueRef Global)
{
    return LLVMGetSection(Global);
}


DNA_EXPORT void  FfiLLVMSetSection(LLVMValueRef Global, const char* Section)
{
    return LLVMSetSection(Global, Section);
}


DNA_EXPORT LLVMVisibility  FfiLLVMGetVisibility(LLVMValueRef Global)
{
    return LLVMGetVisibility(Global);
}


DNA_EXPORT void  FfiLLVMSetVisibility(LLVMValueRef Global, LLVMVisibility Viz)
{
    return LLVMSetVisibility(Global, Viz);
}


DNA_EXPORT LLVMDLLStorageClass  FfiLLVMGetDLLStorageClass(LLVMValueRef Global)
{
    return LLVMGetDLLStorageClass(Global);
}


DNA_EXPORT void  FfiLLVMSetDLLStorageClass(LLVMValueRef Global, LLVMDLLStorageClass Class)
{
    return LLVMSetDLLStorageClass(Global, Class);
}


DNA_EXPORT LLVMUnnamedAddr  FfiLLVMGetUnnamedAddress(LLVMValueRef Global)
{
    return LLVMGetUnnamedAddress(Global);
}


DNA_EXPORT void  FfiLLVMSetUnnamedAddress(LLVMValueRef Global, LLVMUnnamedAddr UnnamedAddr)
{
    return LLVMSetUnnamedAddress(Global, UnnamedAddr);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGlobalGetValueType(LLVMValueRef Global)
{
    return LLVMGlobalGetValueType(Global);
}


DNA_EXPORT LLVMBool  FfiLLVMHasUnnamedAddr(LLVMValueRef Global)
{
    return LLVMHasUnnamedAddr(Global);
}


DNA_EXPORT void  FfiLLVMSetUnnamedAddr(LLVMValueRef Global, LLVMBool HasUnnamedAddr)
{
    return LLVMSetUnnamedAddr(Global, HasUnnamedAddr);
}


DNA_EXPORT unsigned  FfiLLVMGetAlignment(LLVMValueRef V)
{
    return LLVMGetAlignment(V);
}


DNA_EXPORT void  FfiLLVMSetAlignment(LLVMValueRef V, unsigned Bytes)
{
    return LLVMSetAlignment(V, Bytes);
}


DNA_EXPORT void  FfiLLVMGlobalSetMetadata(LLVMValueRef Global, unsigned Kind, LLVMMetadataRef MD)
{
    return LLVMGlobalSetMetadata(Global, Kind, MD);
}


DNA_EXPORT void  FfiLLVMGlobalEraseMetadata(LLVMValueRef Global, unsigned Kind)
{
    return LLVMGlobalEraseMetadata(Global, Kind);
}


DNA_EXPORT void  FfiLLVMGlobalClearMetadata(LLVMValueRef Global)
{
    return LLVMGlobalClearMetadata(Global);
}


DNA_EXPORT LLVMValueMetadataEntry* FfiLLVMGlobalCopyAllMetadata(LLVMValueRef Value, size_t* NumEntries)
{
    return LLVMGlobalCopyAllMetadata(Value, NumEntries);
}


DNA_EXPORT void  FfiLLVMDisposeValueMetadataEntries(LLVMValueMetadataEntry* Entries)
{
    return LLVMDisposeValueMetadataEntries(Entries);
}


DNA_EXPORT unsigned  FfiLLVMValueMetadataEntriesGetKind(LLVMValueMetadataEntry* Entries, unsigned Index)
{
    return LLVMValueMetadataEntriesGetKind(Entries, Index);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAddGlobal(LLVMModuleRef M, LLVMTypeRef Ty, const char* Name)
{
    return LLVMAddGlobal(M, Ty, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAddGlobalInAddressSpace(LLVMModuleRef M, LLVMTypeRef Ty, const char* Name, unsigned AddressSpace)
{
    return LLVMAddGlobalInAddressSpace(M, Ty, Name, AddressSpace);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNamedGlobal(LLVMModuleRef M, const char* Name)
{
    return LLVMGetNamedGlobal(M, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstGlobal(LLVMModuleRef M)
{
    return LLVMGetFirstGlobal(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastGlobal(LLVMModuleRef M)
{
    return LLVMGetLastGlobal(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextGlobal(LLVMValueRef GlobalVar)
{
    return LLVMGetNextGlobal(GlobalVar);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousGlobal(LLVMValueRef GlobalVar)
{
    return LLVMGetPreviousGlobal(GlobalVar);
}


DNA_EXPORT void  FfiLLVMDeleteGlobal(LLVMValueRef GlobalVar)
{
    return LLVMDeleteGlobal(GlobalVar);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetInitializer(LLVMValueRef GlobalVar)
{
    return LLVMGetInitializer(GlobalVar);
}


DNA_EXPORT void  FfiLLVMSetInitializer(LLVMValueRef GlobalVar, LLVMValueRef ConstantVal)
{
    return LLVMSetInitializer(GlobalVar, ConstantVal);
}


DNA_EXPORT LLVMBool  FfiLLVMIsThreadLocal(LLVMValueRef GlobalVar)
{
    return LLVMIsThreadLocal(GlobalVar);
}


DNA_EXPORT void  FfiLLVMSetThreadLocal(LLVMValueRef GlobalVar, LLVMBool IsThreadLocal)
{
    return LLVMSetThreadLocal(GlobalVar, IsThreadLocal);
}


DNA_EXPORT LLVMBool  FfiLLVMIsGlobalConstant(LLVMValueRef GlobalVar)
{
    return LLVMIsGlobalConstant(GlobalVar);
}


DNA_EXPORT void  FfiLLVMSetGlobalConstant(LLVMValueRef GlobalVar, LLVMBool IsConstant)
{
    return LLVMSetGlobalConstant(GlobalVar, IsConstant);
}


DNA_EXPORT LLVMThreadLocalMode  FfiLLVMGetThreadLocalMode(LLVMValueRef GlobalVar)
{
    return LLVMGetThreadLocalMode(GlobalVar);
}


DNA_EXPORT void  FfiLLVMSetThreadLocalMode(LLVMValueRef GlobalVar, LLVMThreadLocalMode Mode)
{
    return LLVMSetThreadLocalMode(GlobalVar, Mode);
}


DNA_EXPORT LLVMBool  FfiLLVMIsExternallyInitialized(LLVMValueRef GlobalVar)
{
    return LLVMIsExternallyInitialized(GlobalVar);
}


DNA_EXPORT void  FfiLLVMSetExternallyInitialized(LLVMValueRef GlobalVar, LLVMBool IsExtInit)
{
    return LLVMSetExternallyInitialized(GlobalVar, IsExtInit);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAddAlias2(LLVMModuleRef M, LLVMTypeRef ValueTy, unsigned AddrSpace, LLVMValueRef Aliasee, const char* Name)
{
    return LLVMAddAlias2(M, ValueTy, AddrSpace, Aliasee, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNamedGlobalAlias(LLVMModuleRef M, const char* Name, size_t NameLen)
{
    return LLVMGetNamedGlobalAlias(M, Name, NameLen);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstGlobalAlias(LLVMModuleRef M)
{
    return LLVMGetFirstGlobalAlias(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastGlobalAlias(LLVMModuleRef M)
{
    return LLVMGetLastGlobalAlias(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextGlobalAlias(LLVMValueRef GA)
{
    return LLVMGetNextGlobalAlias(GA);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousGlobalAlias(LLVMValueRef GA)
{
    return LLVMGetPreviousGlobalAlias(GA);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAliasGetAliasee(LLVMValueRef Alias)
{
    return LLVMAliasGetAliasee(Alias);
}


DNA_EXPORT void  FfiLLVMAliasSetAliasee(LLVMValueRef Alias, LLVMValueRef Aliasee)
{
    return LLVMAliasSetAliasee(Alias, Aliasee);
}


DNA_EXPORT void  FfiLLVMDeleteFunction(LLVMValueRef Fn)
{
    return LLVMDeleteFunction(Fn);
}


DNA_EXPORT LLVMBool  FfiLLVMHasPersonalityFn(LLVMValueRef Fn)
{
    return LLVMHasPersonalityFn(Fn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPersonalityFn(LLVMValueRef Fn)
{
    return LLVMGetPersonalityFn(Fn);
}


DNA_EXPORT void  FfiLLVMSetPersonalityFn(LLVMValueRef Fn, LLVMValueRef PersonalityFn)
{
    return LLVMSetPersonalityFn(Fn, PersonalityFn);
}


DNA_EXPORT unsigned  FfiLLVMLookupIntrinsicID(const char* Name, size_t NameLen)
{
    return LLVMLookupIntrinsicID(Name, NameLen);
}


DNA_EXPORT unsigned  FfiLLVMGetIntrinsicID(LLVMValueRef Fn)
{
    return LLVMGetIntrinsicID(Fn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetIntrinsicDeclaration(LLVMModuleRef Mod, unsigned ID, LLVMTypeRef* ParamTypes, size_t ParamCount)
{
    return LLVMGetIntrinsicDeclaration(Mod, ID, ParamTypes, ParamCount);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntrinsicGetType(LLVMContextRef Ctx, unsigned ID, LLVMTypeRef* ParamTypes, size_t ParamCount)
{
    return LLVMIntrinsicGetType(Ctx, ID, ParamTypes, ParamCount);
}


DNA_EXPORT const char* FfiLLVMIntrinsicGetName(unsigned ID, size_t* NameLength)
{
    return LLVMIntrinsicGetName(ID, NameLength);
}


DNA_EXPORT const char* FfiLLVMIntrinsicCopyOverloadedName(unsigned ID, LLVMTypeRef* ParamTypes, size_t ParamCount, size_t* NameLength)
{
    return LLVMIntrinsicCopyOverloadedName(ID, ParamTypes, ParamCount, NameLength);
}


DNA_EXPORT const char* FfiLLVMIntrinsicCopyOverloadedName2(LLVMModuleRef Mod, unsigned ID, LLVMTypeRef* ParamTypes, size_t ParamCount, size_t* NameLength)
{
    return LLVMIntrinsicCopyOverloadedName2(Mod, ID, ParamTypes, ParamCount, NameLength);
}


DNA_EXPORT LLVMBool  FfiLLVMIntrinsicIsOverloaded(unsigned ID)
{
    return LLVMIntrinsicIsOverloaded(ID);
}


DNA_EXPORT unsigned  FfiLLVMGetFunctionCallConv(LLVMValueRef Fn)
{
    return LLVMGetFunctionCallConv(Fn);
}


DNA_EXPORT void  FfiLLVMSetFunctionCallConv(LLVMValueRef Fn, unsigned CC)
{
    return LLVMSetFunctionCallConv(Fn, CC);
}


DNA_EXPORT const char* FfiLLVMGetGC(LLVMValueRef Fn)
{
    return LLVMGetGC(Fn);
}


DNA_EXPORT void  FfiLLVMSetGC(LLVMValueRef Fn, const char* Name)
{
    return LLVMSetGC(Fn, Name);
}


DNA_EXPORT void  FfiLLVMAddAttributeAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, LLVMAttributeRef A)
{
    return LLVMAddAttributeAtIndex(F, Idx, A);
}


DNA_EXPORT unsigned  FfiLLVMGetAttributeCountAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx)
{
    return LLVMGetAttributeCountAtIndex(F, Idx);
}


DNA_EXPORT void  FfiLLVMGetAttributesAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, LLVMAttributeRef* Attrs)
{
    return LLVMGetAttributesAtIndex(F, Idx, Attrs);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMGetEnumAttributeAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, unsigned KindID)
{
    return LLVMGetEnumAttributeAtIndex(F, Idx, KindID);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMGetStringAttributeAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, const char* K, unsigned KLen)
{
    return LLVMGetStringAttributeAtIndex(F, Idx, K, KLen);
}


DNA_EXPORT void  FfiLLVMRemoveEnumAttributeAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, unsigned KindID)
{
    return LLVMRemoveEnumAttributeAtIndex(F, Idx, KindID);
}


DNA_EXPORT void  FfiLLVMRemoveStringAttributeAtIndex(LLVMValueRef F, LLVMAttributeIndex Idx, const char* K, unsigned KLen)
{
    return LLVMRemoveStringAttributeAtIndex(F, Idx, K, KLen);
}


DNA_EXPORT void  FfiLLVMAddTargetDependentFunctionAttr(LLVMValueRef Fn, const char* A, const char* V)
{
    return LLVMAddTargetDependentFunctionAttr(Fn, A, V);
}


DNA_EXPORT unsigned  FfiLLVMCountParams(LLVMValueRef Fn)
{
    return LLVMCountParams(Fn);
}


DNA_EXPORT void  FfiLLVMGetParams(LLVMValueRef Fn, LLVMValueRef* Params)
{
    return LLVMGetParams(Fn, Params);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetParam(LLVMValueRef Fn, unsigned Index)
{
    return LLVMGetParam(Fn, Index);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetParamParent(LLVMValueRef Inst)
{
    return LLVMGetParamParent(Inst);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstParam(LLVMValueRef Fn)
{
    return LLVMGetFirstParam(Fn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastParam(LLVMValueRef Fn)
{
    return LLVMGetLastParam(Fn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextParam(LLVMValueRef Arg)
{
    return LLVMGetNextParam(Arg);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousParam(LLVMValueRef Arg)
{
    return LLVMGetPreviousParam(Arg);
}


DNA_EXPORT void  FfiLLVMSetParamAlignment(LLVMValueRef Arg, unsigned Align)
{
    return LLVMSetParamAlignment(Arg, Align);
}


DNA_EXPORT LLVMValueRef  FfiLLVMAddGlobalIFunc(LLVMModuleRef M, const char* Name, size_t NameLen, LLVMTypeRef Ty, unsigned AddrSpace, LLVMValueRef Resolver)
{
    return LLVMAddGlobalIFunc(M, Name, NameLen, Ty, AddrSpace, Resolver);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNamedGlobalIFunc(LLVMModuleRef M, const char* Name, size_t NameLen)
{
    return LLVMGetNamedGlobalIFunc(M, Name, NameLen);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstGlobalIFunc(LLVMModuleRef M)
{
    return LLVMGetFirstGlobalIFunc(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastGlobalIFunc(LLVMModuleRef M)
{
    return LLVMGetLastGlobalIFunc(M);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextGlobalIFunc(LLVMValueRef IFunc)
{
    return LLVMGetNextGlobalIFunc(IFunc);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousGlobalIFunc(LLVMValueRef IFunc)
{
    return LLVMGetPreviousGlobalIFunc(IFunc);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetGlobalIFuncResolver(LLVMValueRef IFunc)
{
    return LLVMGetGlobalIFuncResolver(IFunc);
}


DNA_EXPORT void  FfiLLVMSetGlobalIFuncResolver(LLVMValueRef IFunc, LLVMValueRef Resolver)
{
    return LLVMSetGlobalIFuncResolver(IFunc, Resolver);
}


DNA_EXPORT void  FfiLLVMEraseGlobalIFunc(LLVMValueRef IFunc)
{
    return LLVMEraseGlobalIFunc(IFunc);
}


DNA_EXPORT void  FfiLLVMRemoveGlobalIFunc(LLVMValueRef IFunc)
{
    return LLVMRemoveGlobalIFunc(IFunc);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMMDStringInContext2(LLVMContextRef C, const char* Str, size_t SLen)
{
    return LLVMMDStringInContext2(C, Str, SLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMMDNodeInContext2(LLVMContextRef C, LLVMMetadataRef* MDs, size_t Count)
{
    return LLVMMDNodeInContext2(C, MDs, Count);
}


DNA_EXPORT LLVMValueRef  FfiLLVMMetadataAsValue(LLVMContextRef C, LLVMMetadataRef MD)
{
    return LLVMMetadataAsValue(C, MD);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMValueAsMetadata(LLVMValueRef Val)
{
    return LLVMValueAsMetadata(Val);
}


DNA_EXPORT const char* FfiLLVMGetMDString(LLVMValueRef V, unsigned* Length)
{
    return LLVMGetMDString(V, Length);
}


DNA_EXPORT unsigned  FfiLLVMGetMDNodeNumOperands(LLVMValueRef V)
{
    return LLVMGetMDNodeNumOperands(V);
}


DNA_EXPORT void  FfiLLVMGetMDNodeOperands(LLVMValueRef V, LLVMValueRef* Dest)
{
    return LLVMGetMDNodeOperands(V, Dest);
}


DNA_EXPORT void  FfiLLVMReplaceMDNodeOperandWith(LLVMValueRef V, unsigned Index, LLVMMetadataRef Replacement)
{
    return LLVMReplaceMDNodeOperandWith(V, Index, Replacement);
}


DNA_EXPORT LLVMValueRef  FfiLLVMMDStringInContext(LLVMContextRef C, const char* Str, unsigned SLen)
{
    return LLVMMDStringInContext(C, Str, SLen);
}


DNA_EXPORT LLVMValueRef  FfiLLVMMDString(const char* Str, unsigned SLen)
{
    return LLVMMDString(Str, SLen);
}


DNA_EXPORT LLVMValueRef  FfiLLVMMDNodeInContext(LLVMContextRef C, LLVMValueRef* Vals, unsigned Count)
{
    return LLVMMDNodeInContext(C, Vals, Count);
}


DNA_EXPORT LLVMValueRef  FfiLLVMMDNode(LLVMValueRef* Vals, unsigned Count)
{
    return LLVMMDNode(Vals, Count);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBasicBlockAsValue(LLVMBasicBlockRef BB)
{
    return LLVMBasicBlockAsValue(BB);
}


DNA_EXPORT LLVMBool  FfiLLVMValueIsBasicBlock(LLVMValueRef Val)
{
    return LLVMValueIsBasicBlock(Val);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMValueAsBasicBlock(LLVMValueRef Val)
{
    return LLVMValueAsBasicBlock(Val);
}


DNA_EXPORT const char* FfiLLVMGetBasicBlockName(LLVMBasicBlockRef BB)
{
    return LLVMGetBasicBlockName(BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetBasicBlockParent(LLVMBasicBlockRef BB)
{
    return LLVMGetBasicBlockParent(BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetBasicBlockTerminator(LLVMBasicBlockRef BB)
{
    return LLVMGetBasicBlockTerminator(BB);
}


DNA_EXPORT unsigned  FfiLLVMCountBasicBlocks(LLVMValueRef Fn)
{
    return LLVMCountBasicBlocks(Fn);
}


DNA_EXPORT void  FfiLLVMGetBasicBlocks(LLVMValueRef Fn, LLVMBasicBlockRef* BasicBlocks)
{
    return LLVMGetBasicBlocks(Fn, BasicBlocks);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetFirstBasicBlock(LLVMValueRef Fn)
{
    return LLVMGetFirstBasicBlock(Fn);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetLastBasicBlock(LLVMValueRef Fn)
{
    return LLVMGetLastBasicBlock(Fn);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetNextBasicBlock(LLVMBasicBlockRef BB)
{
    return LLVMGetNextBasicBlock(BB);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetPreviousBasicBlock(LLVMBasicBlockRef BB)
{
    return LLVMGetPreviousBasicBlock(BB);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetEntryBasicBlock(LLVMValueRef Fn)
{
    return LLVMGetEntryBasicBlock(Fn);
}


DNA_EXPORT void  FfiLLVMInsertExistingBasicBlockAfterInsertBlock(LLVMBuilderRef Builder, LLVMBasicBlockRef BB)
{
    return LLVMInsertExistingBasicBlockAfterInsertBlock(Builder, BB);
}


DNA_EXPORT void  FfiLLVMAppendExistingBasicBlock(LLVMValueRef Fn, LLVMBasicBlockRef BB)
{
    return LLVMAppendExistingBasicBlock(Fn, BB);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMCreateBasicBlockInContext(LLVMContextRef C, const char* Name)
{
    return LLVMCreateBasicBlockInContext(C, Name);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMAppendBasicBlockInContext(LLVMContextRef C, LLVMValueRef Fn, const char* Name)
{
    return LLVMAppendBasicBlockInContext(C, Fn, Name);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMAppendBasicBlock(LLVMValueRef Fn, const char* Name)
{
    return LLVMAppendBasicBlock(Fn, Name);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMInsertBasicBlockInContext(LLVMContextRef C, LLVMBasicBlockRef BB, const char* Name)
{
    return LLVMInsertBasicBlockInContext(C, BB, Name);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMInsertBasicBlock(LLVMBasicBlockRef InsertBeforeBB, const char* Name)
{
    return LLVMInsertBasicBlock(InsertBeforeBB, Name);
}


DNA_EXPORT void  FfiLLVMDeleteBasicBlock(LLVMBasicBlockRef BB)
{
    return LLVMDeleteBasicBlock(BB);
}


DNA_EXPORT void  FfiLLVMRemoveBasicBlockFromParent(LLVMBasicBlockRef BB)
{
    return LLVMRemoveBasicBlockFromParent(BB);
}


DNA_EXPORT void  FfiLLVMMoveBasicBlockBefore(LLVMBasicBlockRef BB, LLVMBasicBlockRef MovePos)
{
    return LLVMMoveBasicBlockBefore(BB, MovePos);
}


DNA_EXPORT void  FfiLLVMMoveBasicBlockAfter(LLVMBasicBlockRef BB, LLVMBasicBlockRef MovePos)
{
    return LLVMMoveBasicBlockAfter(BB, MovePos);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetFirstInstruction(LLVMBasicBlockRef BB)
{
    return LLVMGetFirstInstruction(BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetLastInstruction(LLVMBasicBlockRef BB)
{
    return LLVMGetLastInstruction(BB);
}


DNA_EXPORT int  FfiLLVMHasMetadata(LLVMValueRef Val)
{
    return LLVMHasMetadata(Val);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetMetadata(LLVMValueRef Val, unsigned KindID)
{
    return LLVMGetMetadata(Val, KindID);
}


DNA_EXPORT void  FfiLLVMSetMetadata(LLVMValueRef Val, unsigned KindID, LLVMValueRef Node)
{
    return LLVMSetMetadata(Val, KindID, Node);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetInstructionParent(LLVMValueRef Inst)
{
    return LLVMGetInstructionParent(Inst);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetNextInstruction(LLVMValueRef Inst)
{
    return LLVMGetNextInstruction(Inst);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetPreviousInstruction(LLVMValueRef Inst)
{
    return LLVMGetPreviousInstruction(Inst);
}


DNA_EXPORT void  FfiLLVMInstructionRemoveFromParent(LLVMValueRef Inst)
{
    return LLVMInstructionRemoveFromParent(Inst);
}


DNA_EXPORT void  FfiLLVMInstructionEraseFromParent(LLVMValueRef Inst)
{
    return LLVMInstructionEraseFromParent(Inst);
}


DNA_EXPORT void  FfiLLVMDeleteInstruction(LLVMValueRef Inst)
{
    return LLVMDeleteInstruction(Inst);
}


DNA_EXPORT LLVMOpcode  FfiLLVMGetInstructionOpcode(LLVMValueRef Inst)
{
    return LLVMGetInstructionOpcode(Inst);
}


DNA_EXPORT LLVMIntPredicate  FfiLLVMGetICmpPredicate(LLVMValueRef Inst)
{
    return LLVMGetICmpPredicate(Inst);
}


DNA_EXPORT LLVMRealPredicate  FfiLLVMGetFCmpPredicate(LLVMValueRef Inst)
{
    return LLVMGetFCmpPredicate(Inst);
}


DNA_EXPORT LLVMValueRef  FfiLLVMInstructionClone(LLVMValueRef Inst)
{
    return LLVMInstructionClone(Inst);
}


DNA_EXPORT LLVMValueRef  FfiLLVMIsATerminatorInst(LLVMValueRef Inst)
{
    return LLVMIsATerminatorInst(Inst);
}


DNA_EXPORT unsigned  FfiLLVMGetNumArgOperands(LLVMValueRef Instr)
{
    return LLVMGetNumArgOperands(Instr);
}


DNA_EXPORT void  FfiLLVMSetInstructionCallConv(LLVMValueRef Instr, unsigned CC)
{
    return LLVMSetInstructionCallConv(Instr, CC);
}


DNA_EXPORT unsigned  FfiLLVMGetInstructionCallConv(LLVMValueRef Instr)
{
    return LLVMGetInstructionCallConv(Instr);
}


DNA_EXPORT void  FfiLLVMSetInstrParamAlignment(LLVMValueRef Instr, LLVMAttributeIndex Idx, unsigned Align)
{
    return LLVMSetInstrParamAlignment(Instr, Idx, Align);
}


DNA_EXPORT void  FfiLLVMAddCallSiteAttribute(LLVMValueRef C, LLVMAttributeIndex Idx, LLVMAttributeRef A)
{
    return LLVMAddCallSiteAttribute(C, Idx, A);
}


DNA_EXPORT unsigned  FfiLLVMGetCallSiteAttributeCount(LLVMValueRef C, LLVMAttributeIndex Idx)
{
    return LLVMGetCallSiteAttributeCount(C, Idx);
}


DNA_EXPORT void  FfiLLVMGetCallSiteAttributes(LLVMValueRef C, LLVMAttributeIndex Idx, LLVMAttributeRef* Attrs)
{
    return LLVMGetCallSiteAttributes(C, Idx, Attrs);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMGetCallSiteEnumAttribute(LLVMValueRef C, LLVMAttributeIndex Idx, unsigned KindID)
{
    return LLVMGetCallSiteEnumAttribute(C, Idx, KindID);
}


DNA_EXPORT LLVMAttributeRef  FfiLLVMGetCallSiteStringAttribute(LLVMValueRef C, LLVMAttributeIndex Idx, const char* K, unsigned KLen)
{
    return LLVMGetCallSiteStringAttribute(C, Idx, K, KLen);
}


DNA_EXPORT void  FfiLLVMRemoveCallSiteEnumAttribute(LLVMValueRef C, LLVMAttributeIndex Idx, unsigned KindID)
{
    return LLVMRemoveCallSiteEnumAttribute(C, Idx, KindID);
}


DNA_EXPORT void  FfiLLVMRemoveCallSiteStringAttribute(LLVMValueRef C, LLVMAttributeIndex Idx, const char* K, unsigned KLen)
{
    return LLVMRemoveCallSiteStringAttribute(C, Idx, K, KLen);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetCalledFunctionType(LLVMValueRef C)
{
    return LLVMGetCalledFunctionType(C);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetCalledValue(LLVMValueRef Instr)
{
    return LLVMGetCalledValue(Instr);
}


DNA_EXPORT LLVMBool  FfiLLVMIsTailCall(LLVMValueRef CallInst)
{
    return LLVMIsTailCall(CallInst);
}


DNA_EXPORT void  FfiLLVMSetTailCall(LLVMValueRef CallInst, LLVMBool IsTailCall)
{
    return LLVMSetTailCall(CallInst, IsTailCall);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetNormalDest(LLVMValueRef InvokeInst)
{
    return LLVMGetNormalDest(InvokeInst);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetUnwindDest(LLVMValueRef InvokeInst)
{
    return LLVMGetUnwindDest(InvokeInst);
}


DNA_EXPORT void  FfiLLVMSetNormalDest(LLVMValueRef InvokeInst, LLVMBasicBlockRef B)
{
    return LLVMSetNormalDest(InvokeInst, B);
}


DNA_EXPORT void  FfiLLVMSetUnwindDest(LLVMValueRef InvokeInst, LLVMBasicBlockRef B)
{
    return LLVMSetUnwindDest(InvokeInst, B);
}


DNA_EXPORT unsigned  FfiLLVMGetNumSuccessors(LLVMValueRef Term)
{
    return LLVMGetNumSuccessors(Term);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetSuccessor(LLVMValueRef Term, unsigned i)
{
    return LLVMGetSuccessor(Term, i);
}


DNA_EXPORT void  FfiLLVMSetSuccessor(LLVMValueRef Term, unsigned i, LLVMBasicBlockRef block)
{
    return LLVMSetSuccessor(Term, i, block);
}


DNA_EXPORT LLVMBool  FfiLLVMIsConditional(LLVMValueRef Branch)
{
    return LLVMIsConditional(Branch);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetCondition(LLVMValueRef Branch)
{
    return LLVMGetCondition(Branch);
}


DNA_EXPORT void  FfiLLVMSetCondition(LLVMValueRef Branch, LLVMValueRef Cond)
{
    return LLVMSetCondition(Branch, Cond);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetSwitchDefaultDest(LLVMValueRef SwitchInstr)
{
    return LLVMGetSwitchDefaultDest(SwitchInstr);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetAllocatedType(LLVMValueRef Alloca)
{
    return LLVMGetAllocatedType(Alloca);
}


DNA_EXPORT LLVMBool  FfiLLVMIsInBounds(LLVMValueRef GEP)
{
    return LLVMIsInBounds(GEP);
}


DNA_EXPORT void  FfiLLVMSetIsInBounds(LLVMValueRef GEP, LLVMBool InBounds)
{
    return LLVMSetIsInBounds(GEP, InBounds);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMGetGEPSourceElementType(LLVMValueRef GEP)
{
    return LLVMGetGEPSourceElementType(GEP);
}


DNA_EXPORT void  FfiLLVMAddIncoming(LLVMValueRef PhiNode, LLVMValueRef* IncomingValues, LLVMBasicBlockRef* IncomingBlocks, unsigned Count)
{
    return LLVMAddIncoming(PhiNode, IncomingValues, IncomingBlocks, Count);
}


DNA_EXPORT unsigned  FfiLLVMCountIncoming(LLVMValueRef PhiNode)
{
    return LLVMCountIncoming(PhiNode);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetIncomingValue(LLVMValueRef PhiNode, unsigned Index)
{
    return LLVMGetIncomingValue(PhiNode, Index);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetIncomingBlock(LLVMValueRef PhiNode, unsigned Index)
{
    return LLVMGetIncomingBlock(PhiNode, Index);
}


DNA_EXPORT unsigned  FfiLLVMGetNumIndices(LLVMValueRef Inst)
{
    return LLVMGetNumIndices(Inst);
}


DNA_EXPORT const unsigned* FfiLLVMGetIndices(LLVMValueRef Inst)
{
    return LLVMGetIndices(Inst);
}


DNA_EXPORT LLVMBuilderRef  FfiLLVMCreateBuilderInContext(LLVMContextRef C)
{
    return LLVMCreateBuilderInContext(C);
}


DNA_EXPORT LLVMBuilderRef  FfiLLVMCreateBuilder()
{
    return LLVMCreateBuilder();
}


DNA_EXPORT void  FfiLLVMPositionBuilder(LLVMBuilderRef Builder, LLVMBasicBlockRef Block, LLVMValueRef Instr)
{
    return LLVMPositionBuilder(Builder, Block, Instr);
}


DNA_EXPORT void  FfiLLVMPositionBuilderBefore(LLVMBuilderRef Builder, LLVMValueRef Instr)
{
    return LLVMPositionBuilderBefore(Builder, Instr);
}


DNA_EXPORT void  FfiLLVMPositionBuilderAtEnd(LLVMBuilderRef Builder, LLVMBasicBlockRef Block)
{
    return LLVMPositionBuilderAtEnd(Builder, Block);
}


DNA_EXPORT LLVMBasicBlockRef  FfiLLVMGetInsertBlock(LLVMBuilderRef Builder)
{
    return LLVMGetInsertBlock(Builder);
}


DNA_EXPORT void  FfiLLVMClearInsertionPosition(LLVMBuilderRef Builder)
{
    return LLVMClearInsertionPosition(Builder);
}


DNA_EXPORT void  FfiLLVMInsertIntoBuilder(LLVMBuilderRef Builder, LLVMValueRef Instr)
{
    return LLVMInsertIntoBuilder(Builder, Instr);
}


DNA_EXPORT void  FfiLLVMInsertIntoBuilderWithName(LLVMBuilderRef Builder, LLVMValueRef Instr, const char* Name)
{
    return LLVMInsertIntoBuilderWithName(Builder, Instr, Name);
}


DNA_EXPORT void  FfiLLVMDisposeBuilder(LLVMBuilderRef Builder)
{
    return LLVMDisposeBuilder(Builder);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMGetCurrentDebugLocation2(LLVMBuilderRef Builder)
{
    return LLVMGetCurrentDebugLocation2(Builder);
}


DNA_EXPORT void  FfiLLVMSetCurrentDebugLocation2(LLVMBuilderRef Builder, LLVMMetadataRef Loc)
{
    return LLVMSetCurrentDebugLocation2(Builder, Loc);
}


DNA_EXPORT void  FfiLLVMSetInstDebugLocation(LLVMBuilderRef Builder, LLVMValueRef Inst)
{
    return LLVMSetInstDebugLocation(Builder, Inst);
}


DNA_EXPORT void  FfiLLVMAddMetadataToInst(LLVMBuilderRef Builder, LLVMValueRef Inst)
{
    return LLVMAddMetadataToInst(Builder, Inst);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMBuilderGetDefaultFPMathTag(LLVMBuilderRef Builder)
{
    return LLVMBuilderGetDefaultFPMathTag(Builder);
}


DNA_EXPORT void  FfiLLVMBuilderSetDefaultFPMathTag(LLVMBuilderRef Builder, LLVMMetadataRef FPMathTag)
{
    return LLVMBuilderSetDefaultFPMathTag(Builder, FPMathTag);
}


DNA_EXPORT void  FfiLLVMSetCurrentDebugLocation(LLVMBuilderRef Builder, LLVMValueRef L)
{
    return LLVMSetCurrentDebugLocation(Builder, L);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetCurrentDebugLocation(LLVMBuilderRef Builder)
{
    return LLVMGetCurrentDebugLocation(Builder);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildRetVoid(LLVMBuilderRef builder)
{
    return LLVMBuildRetVoid(builder);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildRet(LLVMBuilderRef builder, LLVMValueRef V)
{
    return LLVMBuildRet(builder, V);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAggregateRet(LLVMBuilderRef builder, LLVMValueRef* RetVals, unsigned N)
{
    return LLVMBuildAggregateRet(builder, RetVals, N);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildBr(LLVMBuilderRef builder, LLVMBasicBlockRef Dest)
{
    return LLVMBuildBr(builder, Dest);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCondBr(LLVMBuilderRef builder, LLVMValueRef If, LLVMBasicBlockRef Then, LLVMBasicBlockRef Else)
{
    return LLVMBuildCondBr(builder, If, Then, Else);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSwitch(LLVMBuilderRef builder, LLVMValueRef V, LLVMBasicBlockRef Else, unsigned NumCases)
{
    return LLVMBuildSwitch(builder, V, Else, NumCases);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildIndirectBr(LLVMBuilderRef B, LLVMValueRef Addr, unsigned NumDests)
{
    return LLVMBuildIndirectBr(B, Addr, NumDests);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildInvoke2(LLVMBuilderRef builder, LLVMTypeRef Ty, LLVMValueRef Fn, LLVMValueRef* Args, unsigned NumArgs, LLVMBasicBlockRef Then, LLVMBasicBlockRef Catch, const char* Name)
{
    return LLVMBuildInvoke2(builder, Ty, Fn, Args, NumArgs, Then, Catch, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildUnreachable(LLVMBuilderRef builder)
{
    return LLVMBuildUnreachable(builder);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildResume(LLVMBuilderRef B, LLVMValueRef Exn)
{
    return LLVMBuildResume(B, Exn);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildLandingPad(LLVMBuilderRef B, LLVMTypeRef Ty, LLVMValueRef PersFn, unsigned NumClauses, const char* Name)
{
    return LLVMBuildLandingPad(B, Ty, PersFn, NumClauses, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCleanupRet(LLVMBuilderRef B, LLVMValueRef CatchPad, LLVMBasicBlockRef BB)
{
    return LLVMBuildCleanupRet(B, CatchPad, BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCatchRet(LLVMBuilderRef B, LLVMValueRef CatchPad, LLVMBasicBlockRef BB)
{
    return LLVMBuildCatchRet(B, CatchPad, BB);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCatchPad(LLVMBuilderRef B, LLVMValueRef ParentPad, LLVMValueRef* Args, unsigned NumArgs, const char* Name)
{
    return LLVMBuildCatchPad(B, ParentPad, Args, NumArgs, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCleanupPad(LLVMBuilderRef B, LLVMValueRef ParentPad, LLVMValueRef* Args, unsigned NumArgs, const char* Name)
{
    return LLVMBuildCleanupPad(B, ParentPad, Args, NumArgs, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCatchSwitch(LLVMBuilderRef B, LLVMValueRef ParentPad, LLVMBasicBlockRef UnwindBB, unsigned NumHandlers, const char* Name)
{
    return LLVMBuildCatchSwitch(B, ParentPad, UnwindBB, NumHandlers, Name);
}


DNA_EXPORT void  FfiLLVMAddCase(LLVMValueRef Switch, LLVMValueRef OnVal, LLVMBasicBlockRef Dest)
{
    return LLVMAddCase(Switch, OnVal, Dest);
}


DNA_EXPORT void  FfiLLVMAddDestination(LLVMValueRef IndirectBr, LLVMBasicBlockRef Dest)
{
    return LLVMAddDestination(IndirectBr, Dest);
}


DNA_EXPORT unsigned  FfiLLVMGetNumClauses(LLVMValueRef LandingPad)
{
    return LLVMGetNumClauses(LandingPad);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetClause(LLVMValueRef LandingPad, unsigned Idx)
{
    return LLVMGetClause(LandingPad, Idx);
}


DNA_EXPORT void  FfiLLVMAddClause(LLVMValueRef LandingPad, LLVMValueRef ClauseVal)
{
    return LLVMAddClause(LandingPad, ClauseVal);
}


DNA_EXPORT LLVMBool  FfiLLVMIsCleanup(LLVMValueRef LandingPad)
{
    return LLVMIsCleanup(LandingPad);
}


DNA_EXPORT void  FfiLLVMSetCleanup(LLVMValueRef LandingPad, LLVMBool Val)
{
    return LLVMSetCleanup(LandingPad, Val);
}


DNA_EXPORT void  FfiLLVMAddHandler(LLVMValueRef CatchSwitch, LLVMBasicBlockRef Dest)
{
    return LLVMAddHandler(CatchSwitch, Dest);
}


DNA_EXPORT unsigned  FfiLLVMGetNumHandlers(LLVMValueRef CatchSwitch)
{
    return LLVMGetNumHandlers(CatchSwitch);
}


DNA_EXPORT void  FfiLLVMGetHandlers(LLVMValueRef CatchSwitch, LLVMBasicBlockRef* Handlers)
{
    return LLVMGetHandlers(CatchSwitch, Handlers);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetArgOperand(LLVMValueRef Funclet, unsigned i)
{
    return LLVMGetArgOperand(Funclet, i);
}


DNA_EXPORT void  FfiLLVMSetArgOperand(LLVMValueRef Funclet, unsigned i, LLVMValueRef value)
{
    return LLVMSetArgOperand(Funclet, i, value);
}


DNA_EXPORT LLVMValueRef  FfiLLVMGetParentCatchSwitch(LLVMValueRef CatchPad)
{
    return LLVMGetParentCatchSwitch(CatchPad);
}


DNA_EXPORT void  FfiLLVMSetParentCatchSwitch(LLVMValueRef CatchPad, LLVMValueRef CatchSwitch)
{
    return LLVMSetParentCatchSwitch(CatchPad, CatchSwitch);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAdd(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildAdd(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNSWAdd(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNSWAdd(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNUWAdd(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNUWAdd(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFAdd(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFAdd(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSub(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildSub(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNSWSub(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNSWSub(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNUWSub(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNUWSub(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFSub(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFSub(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildMul(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildMul(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNSWMul(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNSWMul(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNUWMul(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildNUWMul(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFMul(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFMul(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildUDiv(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildUDiv(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildExactUDiv(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildExactUDiv(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSDiv(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildSDiv(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildExactSDiv(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildExactSDiv(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFDiv(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFDiv(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildURem(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildURem(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSRem(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildSRem(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFRem(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFRem(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildShl(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildShl(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildLShr(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildLShr(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAShr(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildAShr(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAnd(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildAnd(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildOr(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildOr(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildXor(LLVMBuilderRef builder, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildXor(builder, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildBinOp(LLVMBuilderRef B, LLVMOpcode Op, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildBinOp(B, Op, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNeg(LLVMBuilderRef builder, LLVMValueRef V, const char* Name)
{
    return LLVMBuildNeg(builder, V, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNSWNeg(LLVMBuilderRef B, LLVMValueRef V, const char* Name)
{
    return LLVMBuildNSWNeg(B, V, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNUWNeg(LLVMBuilderRef B, LLVMValueRef V, const char* Name)
{
    return LLVMBuildNUWNeg(B, V, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFNeg(LLVMBuilderRef builder, LLVMValueRef V, const char* Name)
{
    return LLVMBuildFNeg(builder, V, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildNot(LLVMBuilderRef builder, LLVMValueRef V, const char* Name)
{
    return LLVMBuildNot(builder, V, Name);
}


DNA_EXPORT LLVMBool  FfiLLVMGetNUW(LLVMValueRef ArithInst)
{
    return LLVMGetNUW(ArithInst);
}


DNA_EXPORT void  FfiLLVMSetNUW(LLVMValueRef ArithInst, LLVMBool HasNUW)
{
    return LLVMSetNUW(ArithInst, HasNUW);
}


DNA_EXPORT LLVMBool  FfiLLVMGetNSW(LLVMValueRef ArithInst)
{
    return LLVMGetNSW(ArithInst);
}


DNA_EXPORT void  FfiLLVMSetNSW(LLVMValueRef ArithInst, LLVMBool HasNSW)
{
    return LLVMSetNSW(ArithInst, HasNSW);
}


DNA_EXPORT LLVMBool  FfiLLVMGetExact(LLVMValueRef DivOrShrInst)
{
    return LLVMGetExact(DivOrShrInst);
}


DNA_EXPORT void  FfiLLVMSetExact(LLVMValueRef DivOrShrInst, LLVMBool IsExact)
{
    return LLVMSetExact(DivOrShrInst, IsExact);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildMalloc(LLVMBuilderRef builder, LLVMTypeRef Ty, const char* Name)
{
    return LLVMBuildMalloc(builder, Ty, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildArrayMalloc(LLVMBuilderRef builder, LLVMTypeRef Ty, LLVMValueRef Val, const char* Name)
{
    return LLVMBuildArrayMalloc(builder, Ty, Val, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildMemSet(LLVMBuilderRef B, LLVMValueRef Ptr, LLVMValueRef Val, LLVMValueRef Len, unsigned Align)
{
    return LLVMBuildMemSet(B, Ptr, Val, Len, Align);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildMemCpy(LLVMBuilderRef B, LLVMValueRef Dst, unsigned DstAlign, LLVMValueRef Src, unsigned SrcAlign, LLVMValueRef Size)
{
    return LLVMBuildMemCpy(B, Dst, DstAlign, Src, SrcAlign, Size);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildMemMove(LLVMBuilderRef B, LLVMValueRef Dst, unsigned DstAlign, LLVMValueRef Src, unsigned SrcAlign, LLVMValueRef Size)
{
    return LLVMBuildMemMove(B, Dst, DstAlign, Src, SrcAlign, Size);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAlloca(LLVMBuilderRef builder, LLVMTypeRef Ty, const char* Name)
{
    return LLVMBuildAlloca(builder, Ty, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildArrayAlloca(LLVMBuilderRef builder, LLVMTypeRef Ty, LLVMValueRef Val, const char* Name)
{
    return LLVMBuildArrayAlloca(builder, Ty, Val, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFree(LLVMBuilderRef builder, LLVMValueRef PointerVal)
{
    return LLVMBuildFree(builder, PointerVal);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildLoad2(LLVMBuilderRef builder, LLVMTypeRef Ty, LLVMValueRef PointerVal, const char* Name)
{
    return LLVMBuildLoad2(builder, Ty, PointerVal, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildStore(LLVMBuilderRef builder, LLVMValueRef Val, LLVMValueRef Ptr)
{
    return LLVMBuildStore(builder, Val, Ptr);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildGEP2(LLVMBuilderRef B, LLVMTypeRef Ty, LLVMValueRef Pointer, LLVMValueRef* Indices, unsigned NumIndices, const char* Name)
{
    return LLVMBuildGEP2(B, Ty, Pointer, Indices, NumIndices, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildInBoundsGEP2(LLVMBuilderRef B, LLVMTypeRef Ty, LLVMValueRef Pointer, LLVMValueRef* Indices, unsigned NumIndices, const char* Name)
{
    return LLVMBuildInBoundsGEP2(B, Ty, Pointer, Indices, NumIndices, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildStructGEP2(LLVMBuilderRef B, LLVMTypeRef Ty, LLVMValueRef Pointer, unsigned Idx, const char* Name)
{
    return LLVMBuildStructGEP2(B, Ty, Pointer, Idx, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildGlobalString(LLVMBuilderRef B, const char* Str, const char* Name)
{
    return LLVMBuildGlobalString(B, Str, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildGlobalStringPtr(LLVMBuilderRef B, const char* Str, const char* Name)
{
    return LLVMBuildGlobalStringPtr(B, Str, Name);
}


DNA_EXPORT LLVMBool  FfiLLVMGetVolatile(LLVMValueRef MemoryAccessInst)
{
    return LLVMGetVolatile(MemoryAccessInst);
}


DNA_EXPORT void  FfiLLVMSetVolatile(LLVMValueRef MemoryAccessInst, LLVMBool IsVolatile)
{
    return LLVMSetVolatile(MemoryAccessInst, IsVolatile);
}


DNA_EXPORT LLVMBool  FfiLLVMGetWeak(LLVMValueRef CmpXchgInst)
{
    return LLVMGetWeak(CmpXchgInst);
}


DNA_EXPORT void  FfiLLVMSetWeak(LLVMValueRef CmpXchgInst, LLVMBool IsWeak)
{
    return LLVMSetWeak(CmpXchgInst, IsWeak);
}


DNA_EXPORT LLVMAtomicOrdering  FfiLLVMGetOrdering(LLVMValueRef MemoryAccessInst)
{
    return LLVMGetOrdering(MemoryAccessInst);
}


DNA_EXPORT void  FfiLLVMSetOrdering(LLVMValueRef MemoryAccessInst, LLVMAtomicOrdering Ordering)
{
    return LLVMSetOrdering(MemoryAccessInst, Ordering);
}


DNA_EXPORT LLVMAtomicRMWBinOp  FfiLLVMGetAtomicRMWBinOp(LLVMValueRef AtomicRMWInst)
{
    return LLVMGetAtomicRMWBinOp(AtomicRMWInst);
}


DNA_EXPORT void  FfiLLVMSetAtomicRMWBinOp(LLVMValueRef AtomicRMWInst, LLVMAtomicRMWBinOp BinOp)
{
    return LLVMSetAtomicRMWBinOp(AtomicRMWInst, BinOp);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildTrunc(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildTrunc(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildZExt(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildZExt(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSExt(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildSExt(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFPToUI(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildFPToUI(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFPToSI(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildFPToSI(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildUIToFP(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildUIToFP(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSIToFP(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildSIToFP(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFPTrunc(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildFPTrunc(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFPExt(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildFPExt(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildPtrToInt(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildPtrToInt(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildIntToPtr(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildIntToPtr(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildBitCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildBitCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAddrSpaceCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildAddrSpaceCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildZExtOrBitCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildZExtOrBitCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSExtOrBitCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildSExtOrBitCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildTruncOrBitCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildTruncOrBitCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCast(LLVMBuilderRef B, LLVMOpcode Op, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildCast(B, Op, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildPointerCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildPointerCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildIntCast2(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, LLVMBool IsSigned, const char* Name)
{
    return LLVMBuildIntCast2(builder, Val, DestTy, IsSigned, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFPCast(LLVMBuilderRef builder, LLVMValueRef Val, LLVMTypeRef DestTy, const char* Name)
{
    return LLVMBuildFPCast(builder, Val, DestTy, Name);
}


DNA_EXPORT LLVMOpcode  FfiLLVMGetCastOpcode(LLVMValueRef Src, LLVMBool SrcIsSigned, LLVMTypeRef DestTy, LLVMBool DestIsSigned)
{
    return LLVMGetCastOpcode(Src, SrcIsSigned, DestTy, DestIsSigned);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildICmp(LLVMBuilderRef builder, LLVMIntPredicate Op, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildICmp(builder, Op, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFCmp(LLVMBuilderRef builder, LLVMRealPredicate Op, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildFCmp(builder, Op, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildPhi(LLVMBuilderRef builder, LLVMTypeRef Ty, const char* Name)
{
    return LLVMBuildPhi(builder, Ty, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildCall2(LLVMBuilderRef builder, LLVMTypeRef typeRef, LLVMValueRef Fn, LLVMValueRef* Args, unsigned NumArgs, const char* Name)
{
    return LLVMBuildCall2(builder, typeRef, Fn, Args, NumArgs, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildSelect(LLVMBuilderRef builder, LLVMValueRef If, LLVMValueRef Then, LLVMValueRef Else, const char* Name)
{
    return LLVMBuildSelect(builder, If, Then, Else, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildVAArg(LLVMBuilderRef builder, LLVMValueRef List, LLVMTypeRef Ty, const char* Name)
{
    return LLVMBuildVAArg(builder, List, Ty, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildExtractElement(LLVMBuilderRef builder, LLVMValueRef VecVal, LLVMValueRef Index, const char* Name)
{
    return LLVMBuildExtractElement(builder, VecVal, Index, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildInsertElement(LLVMBuilderRef builder, LLVMValueRef VecVal, LLVMValueRef EltVal, LLVMValueRef Index, const char* Name)
{
    return LLVMBuildInsertElement(builder, VecVal, EltVal, Index, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildShuffleVector(LLVMBuilderRef builder, LLVMValueRef V1, LLVMValueRef V2, LLVMValueRef Mask, const char* Name)
{
    return LLVMBuildShuffleVector(builder, V1, V2, Mask, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildExtractValue(LLVMBuilderRef builder, LLVMValueRef AggVal, unsigned Index, const char* Name)
{
    return LLVMBuildExtractValue(builder, AggVal, Index, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildInsertValue(LLVMBuilderRef builder, LLVMValueRef AggVal, LLVMValueRef EltVal, unsigned Index, const char* Name)
{
    return LLVMBuildInsertValue(builder, AggVal, EltVal, Index, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFreeze(LLVMBuilderRef builder, LLVMValueRef Val, const char* Name)
{
    return LLVMBuildFreeze(builder, Val, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildIsNull(LLVMBuilderRef builder, LLVMValueRef Val, const char* Name)
{
    return LLVMBuildIsNull(builder, Val, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildIsNotNull(LLVMBuilderRef builder, LLVMValueRef Val, const char* Name)
{
    return LLVMBuildIsNotNull(builder, Val, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildPtrDiff2(LLVMBuilderRef builder, LLVMTypeRef ElemTy, LLVMValueRef LHS, LLVMValueRef RHS, const char* Name)
{
    return LLVMBuildPtrDiff2(builder, ElemTy, LHS, RHS, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildFence(LLVMBuilderRef B, LLVMAtomicOrdering ordering, LLVMBool singleThread, const char* Name)
{
    return LLVMBuildFence(B, ordering, singleThread, Name);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAtomicRMW(LLVMBuilderRef B, LLVMAtomicRMWBinOp op, LLVMValueRef PTR, LLVMValueRef Val, LLVMAtomicOrdering ordering, LLVMBool singleThread)
{
    return LLVMBuildAtomicRMW(B, op, PTR, Val, ordering, singleThread);
}


DNA_EXPORT LLVMValueRef  FfiLLVMBuildAtomicCmpXchg(LLVMBuilderRef B, LLVMValueRef Ptr, LLVMValueRef Cmp, LLVMValueRef New, LLVMAtomicOrdering SuccessOrdering, LLVMAtomicOrdering FailureOrdering, LLVMBool SingleThread)
{
    return LLVMBuildAtomicCmpXchg(B, Ptr, Cmp, New, SuccessOrdering, FailureOrdering, SingleThread);
}


DNA_EXPORT unsigned  FfiLLVMGetNumMaskElements(LLVMValueRef ShuffleVectorInst)
{
    return LLVMGetNumMaskElements(ShuffleVectorInst);
}


DNA_EXPORT int  FfiLLVMGetUndefMaskElem()
{
    return LLVMGetUndefMaskElem();
}


DNA_EXPORT int  FfiLLVMGetMaskValue(LLVMValueRef ShuffleVectorInst, unsigned Elt)
{
    return LLVMGetMaskValue(ShuffleVectorInst, Elt);
}


DNA_EXPORT LLVMBool  FfiLLVMIsAtomicSingleThread(LLVMValueRef AtomicInst)
{
    return LLVMIsAtomicSingleThread(AtomicInst);
}


DNA_EXPORT void  FfiLLVMSetAtomicSingleThread(LLVMValueRef AtomicInst, LLVMBool SingleThread)
{
    return LLVMSetAtomicSingleThread(AtomicInst, SingleThread);
}


DNA_EXPORT LLVMAtomicOrdering  FfiLLVMGetCmpXchgSuccessOrdering(LLVMValueRef CmpXchgInst)
{
    return LLVMGetCmpXchgSuccessOrdering(CmpXchgInst);
}


DNA_EXPORT void  FfiLLVMSetCmpXchgSuccessOrdering(LLVMValueRef CmpXchgInst, LLVMAtomicOrdering Ordering)
{
    return LLVMSetCmpXchgSuccessOrdering(CmpXchgInst, Ordering);
}


DNA_EXPORT LLVMAtomicOrdering  FfiLLVMGetCmpXchgFailureOrdering(LLVMValueRef CmpXchgInst)
{
    return LLVMGetCmpXchgFailureOrdering(CmpXchgInst);
}


DNA_EXPORT void  FfiLLVMSetCmpXchgFailureOrdering(LLVMValueRef CmpXchgInst, LLVMAtomicOrdering Ordering)
{
    return LLVMSetCmpXchgFailureOrdering(CmpXchgInst, Ordering);
}


DNA_EXPORT void  FfiLLVMDisposeModuleProvider(LLVMModuleProviderRef M)
{
    return LLVMDisposeModuleProvider(M);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateMemoryBufferWithContentsOfFile(const char* Path, LLVMMemoryBufferRef* OutMemBuf, char** OutMessage)
{
    return LLVMCreateMemoryBufferWithContentsOfFile(Path, OutMemBuf, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateMemoryBufferWithSTDIN(LLVMMemoryBufferRef* OutMemBuf, char** OutMessage)
{
    return LLVMCreateMemoryBufferWithSTDIN(OutMemBuf, OutMessage);
}


DNA_EXPORT LLVMMemoryBufferRef  FfiLLVMCreateMemoryBufferWithMemoryRange(const char* InputData, size_t InputDataLength, const char* BufferName, LLVMBool RequiresNullTerminator)
{
    return LLVMCreateMemoryBufferWithMemoryRange(InputData, InputDataLength, BufferName, RequiresNullTerminator);
}


DNA_EXPORT LLVMMemoryBufferRef  FfiLLVMCreateMemoryBufferWithMemoryRangeCopy(const char* InputData, size_t InputDataLength, const char* BufferName)
{
    return LLVMCreateMemoryBufferWithMemoryRangeCopy(InputData, InputDataLength, BufferName);
}


DNA_EXPORT const char* FfiLLVMGetBufferStart(LLVMMemoryBufferRef MemBuf)
{
    return LLVMGetBufferStart(MemBuf);
}


DNA_EXPORT size_t  FfiLLVMGetBufferSize(LLVMMemoryBufferRef MemBuf)
{
    return LLVMGetBufferSize(MemBuf);
}


DNA_EXPORT void  FfiLLVMDisposeMemoryBuffer(LLVMMemoryBufferRef MemBuf)
{
    return LLVMDisposeMemoryBuffer(MemBuf);
}


DNA_EXPORT LLVMPassManagerRef  FfiLLVMCreatePassManager()
{
    return LLVMCreatePassManager();
}


DNA_EXPORT LLVMPassManagerRef  FfiLLVMCreateFunctionPassManagerForModule(LLVMModuleRef M)
{
    return LLVMCreateFunctionPassManagerForModule(M);
}


DNA_EXPORT LLVMPassManagerRef  FfiLLVMCreateFunctionPassManager(LLVMModuleProviderRef MP)
{
    return LLVMCreateFunctionPassManager(MP);
}


DNA_EXPORT LLVMBool  FfiLLVMRunPassManager(LLVMPassManagerRef PM, LLVMModuleRef M)
{
    return LLVMRunPassManager(PM, M);
}


DNA_EXPORT LLVMBool  FfiLLVMInitializeFunctionPassManager(LLVMPassManagerRef FPM)
{
    return LLVMInitializeFunctionPassManager(FPM);
}


DNA_EXPORT LLVMBool  FfiLLVMRunFunctionPassManager(LLVMPassManagerRef FPM, LLVMValueRef F)
{
    return LLVMRunFunctionPassManager(FPM, F);
}


DNA_EXPORT LLVMBool  FfiLLVMFinalizeFunctionPassManager(LLVMPassManagerRef FPM)
{
    return LLVMFinalizeFunctionPassManager(FPM);
}


DNA_EXPORT void  FfiLLVMDisposePassManager(LLVMPassManagerRef PM)
{
    return LLVMDisposePassManager(PM);
}


DNA_EXPORT LLVMBool  FfiLLVMStartMultithreaded()
{
    return LLVMStartMultithreaded();
}


DNA_EXPORT void  FfiLLVMStopMultithreaded()
{
    return LLVMStopMultithreaded();
}


DNA_EXPORT LLVMBool  FfiLLVMIsMultithreaded()
{
    return LLVMIsMultithreaded();
}


DNA_EXPORT unsigned  FfiLLVMDebugMetadataVersion()
{
    return LLVMDebugMetadataVersion();
}


DNA_EXPORT unsigned  FfiLLVMGetModuleDebugMetadataVersion(LLVMModuleRef Module)
{
    return LLVMGetModuleDebugMetadataVersion(Module);
}


DNA_EXPORT LLVMBool  FfiLLVMStripModuleDebugInfo(LLVMModuleRef Module)
{
    return LLVMStripModuleDebugInfo(Module);
}


DNA_EXPORT LLVMDIBuilderRef  FfiLLVMCreateDIBuilderDisallowUnresolved(LLVMModuleRef M)
{
    return LLVMCreateDIBuilderDisallowUnresolved(M);
}


DNA_EXPORT LLVMDIBuilderRef  FfiLLVMCreateDIBuilder(LLVMModuleRef M)
{
    return LLVMCreateDIBuilder(M);
}


DNA_EXPORT void  FfiLLVMDisposeDIBuilder(LLVMDIBuilderRef Builder)
{
    return LLVMDisposeDIBuilder(Builder);
}


DNA_EXPORT void  FfiLLVMDIBuilderFinalize(LLVMDIBuilderRef Builder)
{
    return LLVMDIBuilderFinalize(Builder);
}


DNA_EXPORT void  FfiLLVMDIBuilderFinalizeSubprogram(LLVMDIBuilderRef Builder, LLVMMetadataRef Subprogram)
{
    return LLVMDIBuilderFinalizeSubprogram(Builder, Subprogram);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateCompileUnit(LLVMDIBuilderRef Builder, LLVMDWARFSourceLanguage Lang, LLVMMetadataRef FileRef, const char* Producer, size_t ProducerLen, LLVMBool isOptimized, const char* Flags, size_t FlagsLen, unsigned RuntimeVer, const char* SplitName, size_t SplitNameLen, LLVMDWARFEmissionKind Kind, unsigned DWOId, LLVMBool SplitDebugInlining, LLVMBool DebugInfoForProfiling, const char* SysRoot, size_t SysRootLen, const char* SDK, size_t SDKLen)
{
    return LLVMDIBuilderCreateCompileUnit(Builder, Lang, FileRef, Producer, ProducerLen, isOptimized, Flags, FlagsLen, RuntimeVer, SplitName, SplitNameLen, Kind, DWOId, SplitDebugInlining, DebugInfoForProfiling, SysRoot, SysRootLen, SDK, SDKLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateFunction(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, const char* LinkageName, size_t LinkageNameLen, LLVMMetadataRef File, unsigned LineNo, LLVMMetadataRef Ty, LLVMBool IsLocalToUnit, LLVMBool IsDefinition, unsigned ScopeLine, LLVMDIFlags Flags, LLVMBool IsOptimized)
{
    return LLVMDIBuilderCreateFunction(Builder, Scope, Name, NameLen, LinkageName, LinkageNameLen, File, LineNo, Ty, IsLocalToUnit, IsDefinition, ScopeLine, Flags, IsOptimized);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateLexicalBlock(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, LLVMMetadataRef File, unsigned Line, unsigned Column)
{
    return LLVMDIBuilderCreateLexicalBlock(Builder, Scope, File, Line, Column);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateImportedModuleFromAlias(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, LLVMMetadataRef ImportedEntity, LLVMMetadataRef File, unsigned Line, LLVMMetadataRef* Elements, unsigned NumElements)
{
    return LLVMDIBuilderCreateImportedModuleFromAlias(Builder, Scope, ImportedEntity, File, Line, Elements, NumElements);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateImportedModuleFromModule(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, LLVMMetadataRef M, LLVMMetadataRef File, unsigned Line, LLVMMetadataRef* Elements, unsigned NumElements)
{
    return LLVMDIBuilderCreateImportedModuleFromModule(Builder, Scope, M, File, Line, Elements, NumElements);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateImportedDeclaration(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, LLVMMetadataRef Decl, LLVMMetadataRef File, unsigned Line, const char* Name, size_t NameLen, LLVMMetadataRef* Elements, unsigned NumElements)
{
    return LLVMDIBuilderCreateImportedDeclaration(Builder, Scope, Decl, File, Line, Name, NameLen, Elements, NumElements);
}


DNA_EXPORT unsigned  FfiLLVMDILocationGetLine(LLVMMetadataRef Location)
{
    return LLVMDILocationGetLine(Location);
}


DNA_EXPORT unsigned  FfiLLVMDILocationGetColumn(LLVMMetadataRef Location)
{
    return LLVMDILocationGetColumn(Location);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDILocationGetScope(LLVMMetadataRef Location)
{
    return LLVMDILocationGetScope(Location);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDILocationGetInlinedAt(LLVMMetadataRef Location)
{
    return LLVMDILocationGetInlinedAt(Location);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIScopeGetFile(LLVMMetadataRef Scope)
{
    return LLVMDIScopeGetFile(Scope);
}


DNA_EXPORT const char* FfiLLVMDIFileGetDirectory(LLVMMetadataRef File, unsigned* Len)
{
    return LLVMDIFileGetDirectory(File, Len);
}


DNA_EXPORT const char* FfiLLVMDIFileGetFilename(LLVMMetadataRef File, unsigned* Len)
{
    return LLVMDIFileGetFilename(File, Len);
}


DNA_EXPORT const char* FfiLLVMDIFileGetSource(LLVMMetadataRef File, unsigned* Len)
{
    return LLVMDIFileGetSource(File, Len);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderGetOrCreateTypeArray(LLVMDIBuilderRef Builder, LLVMMetadataRef* Data, size_t NumElements)
{
    return LLVMDIBuilderGetOrCreateTypeArray(Builder, Data, NumElements);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateMacro(LLVMDIBuilderRef Builder, LLVMMetadataRef ParentMacroFile, unsigned Line, LLVMDWARFMacinfoRecordType RecordType, const char* Name, size_t NameLen, const char* Value, size_t ValueLen)
{
    return LLVMDIBuilderCreateMacro(Builder, ParentMacroFile, Line, RecordType, Name, NameLen, Value, ValueLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateEnumerator(LLVMDIBuilderRef Builder, const char* Name, size_t NameLen, int64_t Value, LLVMBool IsUnsigned)
{
    return LLVMDIBuilderCreateEnumerator(Builder, Name, NameLen, Value, IsUnsigned);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateEnumerationType(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNumber, uint64_t SizeInBits, uint32_t AlignInBits, LLVMMetadataRef* Elements, unsigned NumElements, LLVMMetadataRef ClassTy)
{
    return LLVMDIBuilderCreateEnumerationType(Builder, Scope, Name, NameLen, File, LineNumber, SizeInBits, AlignInBits, Elements, NumElements, ClassTy);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateUnionType(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNumber, uint64_t SizeInBits, uint32_t AlignInBits, LLVMDIFlags Flags, LLVMMetadataRef* Elements, unsigned NumElements, unsigned RunTimeLang, const char* UniqueId, size_t UniqueIdLen)
{
    return LLVMDIBuilderCreateUnionType(Builder, Scope, Name, NameLen, File, LineNumber, SizeInBits, AlignInBits, Flags, Elements, NumElements, RunTimeLang, UniqueId, UniqueIdLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreatePointerType(LLVMDIBuilderRef Builder, LLVMMetadataRef PointeeTy, uint64_t SizeInBits, uint32_t AlignInBits, unsigned AddressSpace, const char* Name, size_t NameLen)
{
    return LLVMDIBuilderCreatePointerType(Builder, PointeeTy, SizeInBits, AlignInBits, AddressSpace, Name, NameLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateStructType(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNumber, uint64_t SizeInBits, uint32_t AlignInBits, LLVMDIFlags Flags, LLVMMetadataRef DerivedFrom, LLVMMetadataRef* Elements, unsigned NumElements, unsigned RunTimeLang, LLVMMetadataRef VTableHolder, const char* UniqueId, size_t UniqueIdLen)
{
    return LLVMDIBuilderCreateStructType(Builder, Scope, Name, NameLen, File, LineNumber, SizeInBits, AlignInBits, Flags, DerivedFrom, Elements, NumElements, RunTimeLang, VTableHolder, UniqueId, UniqueIdLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateMemberType(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNo, uint64_t SizeInBits, uint32_t AlignInBits, uint64_t OffsetInBits, LLVMDIFlags Flags, LLVMMetadataRef Ty)
{
    return LLVMDIBuilderCreateMemberType(Builder, Scope, Name, NameLen, File, LineNo, SizeInBits, AlignInBits, OffsetInBits, Flags, Ty);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateForwardDecl(LLVMDIBuilderRef Builder, unsigned Tag, const char* Name, size_t NameLen, LLVMMetadataRef Scope, LLVMMetadataRef File, unsigned Line, unsigned RuntimeLang, uint64_t SizeInBits, uint32_t AlignInBits, const char* UniqueIdentifier, size_t UniqueIdentifierLen)
{
    return LLVMDIBuilderCreateForwardDecl(Builder, Tag, Name, NameLen, Scope, File, Line, RuntimeLang, SizeInBits, AlignInBits, UniqueIdentifier, UniqueIdentifierLen);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateClassType(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNumber, uint64_t SizeInBits, uint32_t AlignInBits, uint64_t OffsetInBits, LLVMDIFlags Flags, LLVMMetadataRef DerivedFrom, LLVMMetadataRef* Elements, unsigned NumElements, LLVMMetadataRef VTableHolder, LLVMMetadataRef TemplateParamsNode, const char* UniqueIdentifier, size_t UniqueIdentifierLen)
{
    return LLVMDIBuilderCreateClassType(Builder, Scope, Name, NameLen, File, LineNumber, SizeInBits, AlignInBits, OffsetInBits, Flags, DerivedFrom, Elements, NumElements, VTableHolder, TemplateParamsNode, UniqueIdentifier, UniqueIdentifierLen);
}


DNA_EXPORT const char* FfiLLVMDITypeGetName(LLVMMetadataRef DType, size_t* Length)
{
    return LLVMDITypeGetName(DType, Length);
}


DNA_EXPORT uint64_t  FfiLLVMDITypeGetSizeInBits(LLVMMetadataRef DType)
{
    return LLVMDITypeGetSizeInBits(DType);
}


DNA_EXPORT uint64_t  FfiLLVMDITypeGetOffsetInBits(LLVMMetadataRef DType)
{
    return LLVMDITypeGetOffsetInBits(DType);
}


DNA_EXPORT uint32_t  FfiLLVMDITypeGetAlignInBits(LLVMMetadataRef DType)
{
    return LLVMDITypeGetAlignInBits(DType);
}


DNA_EXPORT unsigned  FfiLLVMDITypeGetLine(LLVMMetadataRef DType)
{
    return LLVMDITypeGetLine(DType);
}


DNA_EXPORT LLVMDIFlags  FfiLLVMDITypeGetFlags(LLVMMetadataRef DType)
{
    return LLVMDITypeGetFlags(DType);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderGetOrCreateSubrange(LLVMDIBuilderRef Builder, int64_t LowerBound, int64_t Count)
{
    return LLVMDIBuilderGetOrCreateSubrange(Builder, LowerBound, Count);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderGetOrCreateArray(LLVMDIBuilderRef Builder, LLVMMetadataRef* Data, size_t NumElements)
{
    return LLVMDIBuilderGetOrCreateArray(Builder, Data, NumElements);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateExpression(LLVMDIBuilderRef Builder, uint64_t* Addr, size_t Length)
{
    return LLVMDIBuilderCreateExpression(Builder, Addr, Length);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateGlobalVariableExpression(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, const char* Linkage, size_t LinkLen, LLVMMetadataRef File, unsigned LineNo, LLVMMetadataRef Ty, LLVMBool LocalToUnit, LLVMMetadataRef Expr, LLVMMetadataRef Decl, uint32_t AlignInBits)
{
    return LLVMDIBuilderCreateGlobalVariableExpression(Builder, Scope, Name, NameLen, Linkage, LinkLen, File, LineNo, Ty, LocalToUnit, Expr, Decl, AlignInBits);
}


DNA_EXPORT uint16_t  FfiLLVMGetDINodeTag(LLVMMetadataRef MD)
{
    return LLVMGetDINodeTag(MD);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIGlobalVariableExpressionGetVariable(LLVMMetadataRef GVE)
{
    return LLVMDIGlobalVariableExpressionGetVariable(GVE);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIGlobalVariableExpressionGetExpression(LLVMMetadataRef GVE)
{
    return LLVMDIGlobalVariableExpressionGetExpression(GVE);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIVariableGetFile(LLVMMetadataRef Var)
{
    return LLVMDIVariableGetFile(Var);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIVariableGetScope(LLVMMetadataRef Var)
{
    return LLVMDIVariableGetScope(Var);
}


DNA_EXPORT unsigned  FfiLLVMDIVariableGetLine(LLVMMetadataRef Var)
{
    return LLVMDIVariableGetLine(Var);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMTemporaryMDNode(LLVMContextRef Ctx, LLVMMetadataRef* Data, size_t NumElements)
{
    return LLVMTemporaryMDNode(Ctx, Data, NumElements);
}


DNA_EXPORT void  FfiLLVMDisposeTemporaryMDNode(LLVMMetadataRef TempNode)
{
    return LLVMDisposeTemporaryMDNode(TempNode);
}


DNA_EXPORT void  FfiLLVMMetadataReplaceAllUsesWith(LLVMMetadataRef TempTargetMetadata, LLVMMetadataRef Replacement)
{
    return LLVMMetadataReplaceAllUsesWith(TempTargetMetadata, Replacement);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateTempGlobalVariableFwdDecl(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, const char* Linkage, size_t LnkLen, LLVMMetadataRef File, unsigned LineNo, LLVMMetadataRef Ty, LLVMBool LocalToUnit, LLVMMetadataRef Decl, uint32_t AlignInBits)
{
    return LLVMDIBuilderCreateTempGlobalVariableFwdDecl(Builder, Scope, Name, NameLen, Linkage, LnkLen, File, LineNo, Ty, LocalToUnit, Decl, AlignInBits);
}


DNA_EXPORT LLVMValueRef  FfiLLVMDIBuilderInsertDeclareBefore(LLVMDIBuilderRef Builder, LLVMValueRef Storage, LLVMMetadataRef VarInfo, LLVMMetadataRef Expr, LLVMMetadataRef DebugLoc, LLVMValueRef Instr)
{
    return LLVMDIBuilderInsertDeclareBefore(Builder, Storage, VarInfo, Expr, DebugLoc, Instr);
}


DNA_EXPORT LLVMValueRef  FfiLLVMDIBuilderInsertDeclareAtEnd(LLVMDIBuilderRef Builder, LLVMValueRef Storage, LLVMMetadataRef VarInfo, LLVMMetadataRef Expr, LLVMMetadataRef DebugLoc, LLVMBasicBlockRef Block)
{
    return LLVMDIBuilderInsertDeclareAtEnd(Builder, Storage, VarInfo, Expr, DebugLoc, Block);
}


DNA_EXPORT LLVMValueRef  FfiLLVMDIBuilderInsertDbgValueBefore(LLVMDIBuilderRef Builder, LLVMValueRef Val, LLVMMetadataRef VarInfo, LLVMMetadataRef Expr, LLVMMetadataRef DebugLoc, LLVMValueRef Instr)
{
    return LLVMDIBuilderInsertDbgValueBefore(Builder, Val, VarInfo, Expr, DebugLoc, Instr);
}


DNA_EXPORT LLVMValueRef  FfiLLVMDIBuilderInsertDbgValueAtEnd(LLVMDIBuilderRef Builder, LLVMValueRef Val, LLVMMetadataRef VarInfo, LLVMMetadataRef Expr, LLVMMetadataRef DebugLoc, LLVMBasicBlockRef Block)
{
    return LLVMDIBuilderInsertDbgValueAtEnd(Builder, Val, VarInfo, Expr, DebugLoc, Block);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateAutoVariable(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, LLVMMetadataRef File, unsigned LineNo, LLVMMetadataRef Ty, LLVMBool AlwaysPreserve, LLVMDIFlags Flags, uint32_t AlignInBits)
{
    return LLVMDIBuilderCreateAutoVariable(Builder, Scope, Name, NameLen, File, LineNo, Ty, AlwaysPreserve, Flags, AlignInBits);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMDIBuilderCreateParameterVariable(LLVMDIBuilderRef Builder, LLVMMetadataRef Scope, const char* Name, size_t NameLen, unsigned ArgNo, LLVMMetadataRef File, unsigned LineNo, LLVMMetadataRef Ty, LLVMBool AlwaysPreserve, LLVMDIFlags Flags)
{
    return LLVMDIBuilderCreateParameterVariable(Builder, Scope, Name, NameLen, ArgNo, File, LineNo, Ty, AlwaysPreserve, Flags);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMGetSubprogram(LLVMValueRef Func)
{
    return LLVMGetSubprogram(Func);
}


DNA_EXPORT void  FfiLLVMSetSubprogram(LLVMValueRef Func, LLVMMetadataRef SP)
{
    return LLVMSetSubprogram(Func, SP);
}


DNA_EXPORT unsigned  FfiLLVMDISubprogramGetLine(LLVMMetadataRef Subprogram)
{
    return LLVMDISubprogramGetLine(Subprogram);
}


DNA_EXPORT LLVMMetadataRef  FfiLLVMInstructionGetDebugLoc(LLVMValueRef Inst)
{
    return LLVMInstructionGetDebugLoc(Inst);
}


DNA_EXPORT void  FfiLLVMInstructionSetDebugLoc(LLVMValueRef Inst, LLVMMetadataRef Loc)
{
    return LLVMInstructionSetDebugLoc(Inst, Loc);
}


DNA_EXPORT LLVMMetadataKind  FfiLLVMGetMetadataKind(LLVMMetadataRef Metadata)
{
    return LLVMGetMetadataKind(Metadata);
}


DNA_EXPORT LLVMDisasmContextRef  FfiLLVMCreateDisasm(const char* TripleName, void* DisInfo, int TagType, LLVMOpInfoCallback GetOpInfo, LLVMSymbolLookupCallback SymbolLookUp)
{
    return LLVMCreateDisasm(TripleName, DisInfo, TagType, GetOpInfo, SymbolLookUp);
}


DNA_EXPORT LLVMDisasmContextRef  FfiLLVMCreateDisasmCPU(const char* Triple, const char* CPU, void* DisInfo, int TagType, LLVMOpInfoCallback GetOpInfo, LLVMSymbolLookupCallback SymbolLookUp)
{
    return LLVMCreateDisasmCPU(Triple, CPU, DisInfo, TagType, GetOpInfo, SymbolLookUp);
}


DNA_EXPORT int  FfiLLVMSetDisasmOptions(LLVMDisasmContextRef DC, uint64_t Options)
{
    return LLVMSetDisasmOptions(DC, Options);
}


DNA_EXPORT void  FfiLLVMDisasmDispose(LLVMDisasmContextRef DC)
{
    return LLVMDisasmDispose(DC);
}


DNA_EXPORT size_t  FfiLLVMDisasmInstruction(LLVMDisasmContextRef DC, uint8_t* Bytes, uint64_t BytesSize, uint64_t PC, char* OutString, size_t OutStringSize)
{
    return LLVMDisasmInstruction(DC, Bytes, BytesSize, PC, OutString, OutStringSize);
}


DNA_EXPORT LLVMErrorTypeId  FfiLLVMGetErrorTypeId(LLVMErrorRef Err)
{
    return LLVMGetErrorTypeId(Err);
}


DNA_EXPORT void  FfiLLVMConsumeError(LLVMErrorRef Err)
{
    return LLVMConsumeError(Err);
}


DNA_EXPORT char* FfiLLVMGetErrorMessage(LLVMErrorRef Err)
{
    return LLVMGetErrorMessage(Err);
}


DNA_EXPORT void  FfiLLVMDisposeErrorMessage(char* ErrMsg)
{
    return LLVMDisposeErrorMessage(ErrMsg);
}


DNA_EXPORT LLVMErrorTypeId  FfiLLVMGetStringErrorTypeId()
{
    return LLVMGetStringErrorTypeId();
}


DNA_EXPORT LLVMErrorRef  FfiLLVMCreateStringError(const char* ErrMsg)
{
    return LLVMCreateStringError(ErrMsg);
}


DNA_EXPORT void  FfiLLVMInstallFatalErrorHandler(LLVMFatalErrorHandler Handler)
{
    return LLVMInstallFatalErrorHandler(Handler);
}


DNA_EXPORT void  FfiLLVMResetFatalErrorHandler()
{
    return LLVMResetFatalErrorHandler();
}


DNA_EXPORT void  FfiLLVMEnablePrettyStackTrace()
{
    return LLVMEnablePrettyStackTrace();
}


DNA_EXPORT void  FfiLLVMLinkInMCJIT()
{
    return LLVMLinkInMCJIT();
}


DNA_EXPORT void  FfiLLVMLinkInInterpreter()
{
    return LLVMLinkInInterpreter();
}


DNA_EXPORT LLVMGenericValueRef  FfiLLVMCreateGenericValueOfInt(LLVMTypeRef Ty, unsigned long long N, LLVMBool IsSigned)
{
    return LLVMCreateGenericValueOfInt(Ty, N, IsSigned);
}


DNA_EXPORT LLVMGenericValueRef  FfiLLVMCreateGenericValueOfPointer(void* P)
{
    return LLVMCreateGenericValueOfPointer(P);
}


DNA_EXPORT LLVMGenericValueRef  FfiLLVMCreateGenericValueOfFloat(LLVMTypeRef Ty, double N)
{
    return LLVMCreateGenericValueOfFloat(Ty, N);
}


DNA_EXPORT unsigned  FfiLLVMGenericValueIntWidth(LLVMGenericValueRef GenValRef)
{
    return LLVMGenericValueIntWidth(GenValRef);
}


DNA_EXPORT unsigned long long  FfiLLVMGenericValueToInt(LLVMGenericValueRef GenVal, LLVMBool IsSigned)
{
    return LLVMGenericValueToInt(GenVal, IsSigned);
}


DNA_EXPORT void* FfiLLVMGenericValueToPointer(LLVMGenericValueRef GenVal)
{
    return LLVMGenericValueToPointer(GenVal);
}


DNA_EXPORT double  FfiLLVMGenericValueToFloat(LLVMTypeRef TyRef, LLVMGenericValueRef GenVal)
{
    return LLVMGenericValueToFloat(TyRef, GenVal);
}


DNA_EXPORT void  FfiLLVMDisposeGenericValue(LLVMGenericValueRef GenVal)
{
    return LLVMDisposeGenericValue(GenVal);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateExecutionEngineForModule(LLVMExecutionEngineRef* OutEE, LLVMModuleRef M, char** OutError)
{
    return LLVMCreateExecutionEngineForModule(OutEE, M, OutError);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateInterpreterForModule(LLVMExecutionEngineRef* OutInterp, LLVMModuleRef M, char** OutError)
{
    return LLVMCreateInterpreterForModule(OutInterp, M, OutError);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateJITCompilerForModule(LLVMExecutionEngineRef* OutJIT, LLVMModuleRef M, unsigned OptLevel, char** OutError)
{
    return LLVMCreateJITCompilerForModule(OutJIT, M, OptLevel, OutError);
}


DNA_EXPORT LLVMBool  FfiLLVMCreateMCJITCompilerForModule(LLVMExecutionEngineRef* OutJIT, LLVMModuleRef M, struct LLVMMCJITCompilerOptions* Options, size_t SizeOfOptions, char** OutError)
{
    return LLVMCreateMCJITCompilerForModule(OutJIT, M, Options, SizeOfOptions, OutError);
}


DNA_EXPORT void  FfiLLVMDisposeExecutionEngine(LLVMExecutionEngineRef EE)
{
    return LLVMDisposeExecutionEngine(EE);
}


DNA_EXPORT void  FfiLLVMRunStaticConstructors(LLVMExecutionEngineRef EE)
{
    return LLVMRunStaticConstructors(EE);
}


DNA_EXPORT void  FfiLLVMRunStaticDestructors(LLVMExecutionEngineRef EE)
{
    return LLVMRunStaticDestructors(EE);
}


DNA_EXPORT int  FfiLLVMRunFunctionAsMain(LLVMExecutionEngineRef EE, LLVMValueRef F, unsigned ArgC, const char* const* ArgV, const char* const* EnvP)
{
    return LLVMRunFunctionAsMain(EE, F, ArgC, ArgV, EnvP);
}


DNA_EXPORT LLVMGenericValueRef  FfiLLVMRunFunction(LLVMExecutionEngineRef EE, LLVMValueRef F, unsigned NumArgs, LLVMGenericValueRef* Args)
{
    return LLVMRunFunction(EE, F, NumArgs, Args);
}


DNA_EXPORT void  FfiLLVMFreeMachineCodeForFunction(LLVMExecutionEngineRef EE, LLVMValueRef F)
{
    return LLVMFreeMachineCodeForFunction(EE, F);
}


DNA_EXPORT void  FfiLLVMAddModule(LLVMExecutionEngineRef EE, LLVMModuleRef M)
{
    return LLVMAddModule(EE, M);
}


DNA_EXPORT LLVMBool  FfiLLVMRemoveModule(LLVMExecutionEngineRef EE, LLVMModuleRef M, LLVMModuleRef* OutMod, char** OutError)
{
    return LLVMRemoveModule(EE, M, OutMod, OutError);
}


DNA_EXPORT LLVMBool  FfiLLVMFindFunction(LLVMExecutionEngineRef EE, const char* Name, LLVMValueRef* OutFn)
{
    return LLVMFindFunction(EE, Name, OutFn);
}


DNA_EXPORT void* FfiLLVMRecompileAndRelinkFunction(LLVMExecutionEngineRef EE, LLVMValueRef Fn)
{
    return LLVMRecompileAndRelinkFunction(EE, Fn);
}


DNA_EXPORT LLVMTargetDataRef  FfiLLVMGetExecutionEngineTargetData(LLVMExecutionEngineRef EE)
{
    return LLVMGetExecutionEngineTargetData(EE);
}


DNA_EXPORT void  FfiLLVMAddGlobalMapping(LLVMExecutionEngineRef EE, LLVMValueRef Global, void* Addr)
{
    return LLVMAddGlobalMapping(EE, Global, Addr);
}


DNA_EXPORT void* FfiLLVMGetPointerToGlobal(LLVMExecutionEngineRef EE, LLVMValueRef Global)
{
    return LLVMGetPointerToGlobal(EE, Global);
}


DNA_EXPORT uint64_t  FfiLLVMGetGlobalValueAddress(LLVMExecutionEngineRef EE, const char* Name)
{
    return LLVMGetGlobalValueAddress(EE, Name);
}


DNA_EXPORT uint64_t  FfiLLVMGetFunctionAddress(LLVMExecutionEngineRef EE, const char* Name)
{
    return LLVMGetFunctionAddress(EE, Name);
}


DNA_EXPORT LLVMBool  FfiLLVMExecutionEngineGetErrMsg(LLVMExecutionEngineRef EE, char** OutError)
{
    return LLVMExecutionEngineGetErrMsg(EE, OutError);
}


DNA_EXPORT LLVMMCJITMemoryManagerRef  FfiLLVMCreateSimpleMCJITMemoryManager(void* Opaque, LLVMMemoryManagerAllocateCodeSectionCallback AllocateCodeSection, LLVMMemoryManagerAllocateDataSectionCallback AllocateDataSection, LLVMMemoryManagerFinalizeMemoryCallback FinalizeMemory, LLVMMemoryManagerDestroyCallback Destroy)
{
    return LLVMCreateSimpleMCJITMemoryManager(Opaque, AllocateCodeSection, AllocateDataSection, FinalizeMemory, Destroy);
}


DNA_EXPORT void  FfiLLVMDisposeMCJITMemoryManager(LLVMMCJITMemoryManagerRef MM)
{
    return LLVMDisposeMCJITMemoryManager(MM);
}


DNA_EXPORT LLVMJITEventListenerRef  FfiLLVMCreateGDBRegistrationListener()
{
    return LLVMCreateGDBRegistrationListener();
}


DNA_EXPORT LLVMJITEventListenerRef  FfiLLVMCreateIntelJITEventListener()
{
    return LLVMCreateIntelJITEventListener();
}


DNA_EXPORT LLVMJITEventListenerRef  FfiLLVMCreateOProfileJITEventListener()
{
    return LLVMCreateOProfileJITEventListener();
}


DNA_EXPORT LLVMJITEventListenerRef  FfiLLVMCreatePerfJITEventListener()
{
    return LLVMCreatePerfJITEventListener();
}


DNA_EXPORT LLVMBool  FfiLLVMParseIRInContext(LLVMContextRef ContextRef, LLVMMemoryBufferRef MemBuf, LLVMModuleRef* OutM, char** OutMessage)
{
    return LLVMParseIRInContext(ContextRef, MemBuf, OutM, OutMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMLinkModules2(LLVMModuleRef Dest, LLVMModuleRef Src)
{
    return LLVMLinkModules2(Dest, Src);
}


DNA_EXPORT LLVMBinaryRef  FfiLLVMCreateBinary(LLVMMemoryBufferRef MemBuf, LLVMContextRef Context, char** ErrorMessage)
{
    return LLVMCreateBinary(MemBuf, Context, ErrorMessage);
}


DNA_EXPORT void  FfiLLVMDisposeBinary(LLVMBinaryRef BR)
{
    return LLVMDisposeBinary(BR);
}


DNA_EXPORT LLVMMemoryBufferRef  FfiLLVMBinaryCopyMemoryBuffer(LLVMBinaryRef BR)
{
    return LLVMBinaryCopyMemoryBuffer(BR);
}


DNA_EXPORT LLVMBinaryType  FfiLLVMBinaryGetType(LLVMBinaryRef BR)
{
    return LLVMBinaryGetType(BR);
}


DNA_EXPORT LLVMBinaryRef  FfiLLVMMachOUniversalBinaryCopyObjectForArch(LLVMBinaryRef BR, const char* Arch, size_t ArchLen, char** ErrorMessage)
{
    return LLVMMachOUniversalBinaryCopyObjectForArch(BR, Arch, ArchLen, ErrorMessage);
}


DNA_EXPORT LLVMSectionIteratorRef  FfiLLVMObjectFileCopySectionIterator(LLVMBinaryRef BR)
{
    return LLVMObjectFileCopySectionIterator(BR);
}


DNA_EXPORT LLVMBool  FfiLLVMObjectFileIsSectionIteratorAtEnd(LLVMBinaryRef BR, LLVMSectionIteratorRef SI)
{
    return LLVMObjectFileIsSectionIteratorAtEnd(BR, SI);
}


DNA_EXPORT LLVMSymbolIteratorRef  FfiLLVMObjectFileCopySymbolIterator(LLVMBinaryRef BR)
{
    return LLVMObjectFileCopySymbolIterator(BR);
}


DNA_EXPORT LLVMBool  FfiLLVMObjectFileIsSymbolIteratorAtEnd(LLVMBinaryRef BR, LLVMSymbolIteratorRef SI)
{
    return LLVMObjectFileIsSymbolIteratorAtEnd(BR, SI);
}


DNA_EXPORT void  FfiLLVMDisposeSectionIterator(LLVMSectionIteratorRef SI)
{
    return LLVMDisposeSectionIterator(SI);
}


DNA_EXPORT void  FfiLLVMMoveToNextSection(LLVMSectionIteratorRef SI)
{
    return LLVMMoveToNextSection(SI);
}


DNA_EXPORT void  FfiLLVMMoveToContainingSection(LLVMSectionIteratorRef Sect, LLVMSymbolIteratorRef Sym)
{
    return LLVMMoveToContainingSection(Sect, Sym);
}


DNA_EXPORT void  FfiLLVMDisposeSymbolIterator(LLVMSymbolIteratorRef SI)
{
    return LLVMDisposeSymbolIterator(SI);
}


DNA_EXPORT void  FfiLLVMMoveToNextSymbol(LLVMSymbolIteratorRef SI)
{
    return LLVMMoveToNextSymbol(SI);
}


DNA_EXPORT const char* FfiLLVMGetSectionName(LLVMSectionIteratorRef SI)
{
    return LLVMGetSectionName(SI);
}


DNA_EXPORT uint64_t  FfiLLVMGetSectionSize(LLVMSectionIteratorRef SI)
{
    return LLVMGetSectionSize(SI);
}


DNA_EXPORT const char* FfiLLVMGetSectionContents(LLVMSectionIteratorRef SI)
{
    return LLVMGetSectionContents(SI);
}


DNA_EXPORT uint64_t  FfiLLVMGetSectionAddress(LLVMSectionIteratorRef SI)
{
    return LLVMGetSectionAddress(SI);
}


DNA_EXPORT LLVMBool  FfiLLVMGetSectionContainsSymbol(LLVMSectionIteratorRef SI, LLVMSymbolIteratorRef Sym)
{
    return LLVMGetSectionContainsSymbol(SI, Sym);
}


DNA_EXPORT LLVMRelocationIteratorRef  FfiLLVMGetRelocations(LLVMSectionIteratorRef Section)
{
    return LLVMGetRelocations(Section);
}


DNA_EXPORT void  FfiLLVMDisposeRelocationIterator(LLVMRelocationIteratorRef RI)
{
    return LLVMDisposeRelocationIterator(RI);
}


DNA_EXPORT LLVMBool  FfiLLVMIsRelocationIteratorAtEnd(LLVMSectionIteratorRef Section, LLVMRelocationIteratorRef RI)
{
    return LLVMIsRelocationIteratorAtEnd(Section, RI);
}


DNA_EXPORT void  FfiLLVMMoveToNextRelocation(LLVMRelocationIteratorRef RI)
{
    return LLVMMoveToNextRelocation(RI);
}


DNA_EXPORT const char* FfiLLVMGetSymbolName(LLVMSymbolIteratorRef SI)
{
    return LLVMGetSymbolName(SI);
}


DNA_EXPORT uint64_t  FfiLLVMGetSymbolAddress(LLVMSymbolIteratorRef SI)
{
    return LLVMGetSymbolAddress(SI);
}


DNA_EXPORT uint64_t  FfiLLVMGetSymbolSize(LLVMSymbolIteratorRef SI)
{
    return LLVMGetSymbolSize(SI);
}


DNA_EXPORT uint64_t  FfiLLVMGetRelocationOffset(LLVMRelocationIteratorRef RI)
{
    return LLVMGetRelocationOffset(RI);
}


DNA_EXPORT LLVMSymbolIteratorRef  FfiLLVMGetRelocationSymbol(LLVMRelocationIteratorRef RI)
{
    return LLVMGetRelocationSymbol(RI);
}


DNA_EXPORT uint64_t  FfiLLVMGetRelocationType(LLVMRelocationIteratorRef RI)
{
    return LLVMGetRelocationType(RI);
}


DNA_EXPORT const char* FfiLLVMGetRelocationTypeName(LLVMRelocationIteratorRef RI)
{
    return LLVMGetRelocationTypeName(RI);
}


DNA_EXPORT const char* FfiLLVMGetRelocationValueString(LLVMRelocationIteratorRef RI)
{
    return LLVMGetRelocationValueString(RI);
}


DNA_EXPORT LLVMObjectFileRef  FfiLLVMCreateObjectFile(LLVMMemoryBufferRef MemBuf)
{
    return LLVMCreateObjectFile(MemBuf);
}


DNA_EXPORT void  FfiLLVMDisposeObjectFile(LLVMObjectFileRef ObjectFile)
{
    return LLVMDisposeObjectFile(ObjectFile);
}


DNA_EXPORT LLVMSectionIteratorRef  FfiLLVMGetSections(LLVMObjectFileRef ObjectFile)
{
    return LLVMGetSections(ObjectFile);
}


DNA_EXPORT LLVMBool  FfiLLVMIsSectionIteratorAtEnd(LLVMObjectFileRef ObjectFile, LLVMSectionIteratorRef SI)
{
    return LLVMIsSectionIteratorAtEnd(ObjectFile, SI);
}


DNA_EXPORT LLVMSymbolIteratorRef  FfiLLVMGetSymbols(LLVMObjectFileRef ObjectFile)
{
    return LLVMGetSymbols(ObjectFile);
}


DNA_EXPORT LLVMBool  FfiLLVMIsSymbolIteratorAtEnd(LLVMObjectFileRef ObjectFile, LLVMSymbolIteratorRef SI)
{
    return LLVMIsSymbolIteratorAtEnd(ObjectFile, SI);
}


DNA_EXPORT LLVMBool  FfiLLVMLoadLibraryPermanently(const char* Filename)
{
    return LLVMLoadLibraryPermanently(Filename);
}


DNA_EXPORT void  FfiLLVMParseCommandLineOptions(int argc, const char* const* argv, const char* Overview)
{
    return LLVMParseCommandLineOptions(argc, argv, Overview);
}


DNA_EXPORT void* FfiLLVMSearchForAddressOfSymbol(const char* symbolName)
{
    return LLVMSearchForAddressOfSymbol(symbolName);
}


DNA_EXPORT void  FfiLLVMAddSymbol(const char* symbolName, void* symbolValue)
{
    return LLVMAddSymbol(symbolName, symbolValue);
}


DNA_EXPORT LLVMTargetDataRef  FfiLLVMGetModuleDataLayout(LLVMModuleRef M)
{
    return LLVMGetModuleDataLayout(M);
}


DNA_EXPORT void  FfiLLVMSetModuleDataLayout(LLVMModuleRef M, LLVMTargetDataRef DL)
{
    return LLVMSetModuleDataLayout(M, DL);
}


DNA_EXPORT LLVMTargetDataRef  FfiLLVMCreateTargetData(const char* StringRep)
{
    return LLVMCreateTargetData(StringRep);
}


DNA_EXPORT void  FfiLLVMDisposeTargetData(LLVMTargetDataRef TD)
{
    return LLVMDisposeTargetData(TD);
}


DNA_EXPORT void  FfiLLVMAddTargetLibraryInfo(LLVMTargetLibraryInfoRef TLI, LLVMPassManagerRef PM)
{
    return LLVMAddTargetLibraryInfo(TLI, PM);
}


DNA_EXPORT char* FfiLLVMCopyStringRepOfTargetData(LLVMTargetDataRef TD)
{
    return LLVMCopyStringRepOfTargetData(TD);
}


DNA_EXPORT unsigned  FfiLLVMPointerSize(LLVMTargetDataRef TD)
{
    return LLVMPointerSize(TD);
}


DNA_EXPORT unsigned  FfiLLVMPointerSizeForAS(LLVMTargetDataRef TD, unsigned AS)
{
    return LLVMPointerSizeForAS(TD, AS);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntPtrType(LLVMTargetDataRef TD)
{
    return LLVMIntPtrType(TD);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntPtrTypeForAS(LLVMTargetDataRef TD, unsigned AS)
{
    return LLVMIntPtrTypeForAS(TD, AS);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntPtrTypeInContext(LLVMContextRef C, LLVMTargetDataRef TD)
{
    return LLVMIntPtrTypeInContext(C, TD);
}


DNA_EXPORT LLVMTypeRef  FfiLLVMIntPtrTypeForASInContext(LLVMContextRef C, LLVMTargetDataRef TD, unsigned AS)
{
    return LLVMIntPtrTypeForASInContext(C, TD, AS);
}


DNA_EXPORT unsigned long long  FfiLLVMSizeOfTypeInBits(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMSizeOfTypeInBits(TD, Ty);
}


DNA_EXPORT unsigned long long  FfiLLVMStoreSizeOfType(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMStoreSizeOfType(TD, Ty);
}


DNA_EXPORT unsigned long long  FfiLLVMABISizeOfType(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMABISizeOfType(TD, Ty);
}


DNA_EXPORT unsigned  FfiLLVMABIAlignmentOfType(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMABIAlignmentOfType(TD, Ty);
}


DNA_EXPORT unsigned  FfiLLVMCallFrameAlignmentOfType(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMCallFrameAlignmentOfType(TD, Ty);
}


DNA_EXPORT unsigned  FfiLLVMPreferredAlignmentOfType(LLVMTargetDataRef TD, LLVMTypeRef Ty)
{
    return LLVMPreferredAlignmentOfType(TD, Ty);
}


DNA_EXPORT unsigned  FfiLLVMPreferredAlignmentOfGlobal(LLVMTargetDataRef TD, LLVMValueRef GlobalVar)
{
    return LLVMPreferredAlignmentOfGlobal(TD, GlobalVar);
}


DNA_EXPORT unsigned  FfiLLVMElementAtOffset(LLVMTargetDataRef TD, LLVMTypeRef StructTy, unsigned long long Offset)
{
    return LLVMElementAtOffset(TD, StructTy, Offset);
}


DNA_EXPORT unsigned long long  FfiLLVMOffsetOfElement(LLVMTargetDataRef TD, LLVMTypeRef StructTy, unsigned Element)
{
    return LLVMOffsetOfElement(TD, StructTy, Element);
}


DNA_EXPORT LLVMTargetRef  FfiLLVMGetFirstTarget()
{
    return LLVMGetFirstTarget();
}


DNA_EXPORT LLVMTargetRef  FfiLLVMGetNextTarget(LLVMTargetRef T)
{
    return LLVMGetNextTarget(T);
}


DNA_EXPORT LLVMTargetRef  FfiLLVMGetTargetFromName(const char* Name)
{
    return LLVMGetTargetFromName(Name);
}


DNA_EXPORT LLVMBool  FfiLLVMGetTargetFromTriple(const char* Triple, LLVMTargetRef* T, char** ErrorMessage)
{
    return LLVMGetTargetFromTriple(Triple, T, ErrorMessage);
}


DNA_EXPORT const char* FfiLLVMGetTargetName(LLVMTargetRef T)
{
    return LLVMGetTargetName(T);
}


DNA_EXPORT const char* FfiLLVMGetTargetDescription(LLVMTargetRef T)
{
    return LLVMGetTargetDescription(T);
}


DNA_EXPORT LLVMBool  FfiLLVMTargetHasJIT(LLVMTargetRef T)
{
    return LLVMTargetHasJIT(T);
}


DNA_EXPORT LLVMBool  FfiLLVMTargetHasTargetMachine(LLVMTargetRef T)
{
    return LLVMTargetHasTargetMachine(T);
}


DNA_EXPORT LLVMBool  FfiLLVMTargetHasAsmBackend(LLVMTargetRef T)
{
    return LLVMTargetHasAsmBackend(T);
}


DNA_EXPORT LLVMTargetMachineRef  FfiLLVMCreateTargetMachine(LLVMTargetRef T, const char* Triple, const char* CPU, const char* Features, LLVMCodeGenOptLevel Level, LLVMRelocMode Reloc, LLVMCodeModel CodeModel)
{
    return LLVMCreateTargetMachine(T, Triple, CPU, Features, Level, Reloc, CodeModel);
}


DNA_EXPORT void  FfiLLVMDisposeTargetMachine(LLVMTargetMachineRef T)
{
    return LLVMDisposeTargetMachine(T);
}


DNA_EXPORT LLVMTargetRef  FfiLLVMGetTargetMachineTarget(LLVMTargetMachineRef T)
{
    return LLVMGetTargetMachineTarget(T);
}


DNA_EXPORT char* FfiLLVMGetTargetMachineTriple(LLVMTargetMachineRef T)
{
    return LLVMGetTargetMachineTriple(T);
}


DNA_EXPORT char* FfiLLVMGetTargetMachineCPU(LLVMTargetMachineRef T)
{
    return LLVMGetTargetMachineCPU(T);
}


DNA_EXPORT char* FfiLLVMGetTargetMachineFeatureString(LLVMTargetMachineRef T)
{
    return LLVMGetTargetMachineFeatureString(T);
}


DNA_EXPORT LLVMTargetDataRef  FfiLLVMCreateTargetDataLayout(LLVMTargetMachineRef T)
{
    return LLVMCreateTargetDataLayout(T);
}


DNA_EXPORT void  FfiLLVMSetTargetMachineAsmVerbosity(LLVMTargetMachineRef T, LLVMBool VerboseAsm)
{
    return LLVMSetTargetMachineAsmVerbosity(T, VerboseAsm);
}


DNA_EXPORT LLVMBool  FfiLLVMTargetMachineEmitToFile(LLVMTargetMachineRef T, LLVMModuleRef M, const char* Filename, LLVMCodeGenFileType codegen, char** ErrorMessage)
{
    return LLVMTargetMachineEmitToFile(T, M, Filename, codegen, ErrorMessage);
}


DNA_EXPORT LLVMBool  FfiLLVMTargetMachineEmitToMemoryBuffer(LLVMTargetMachineRef T, LLVMModuleRef M, LLVMCodeGenFileType codegen, char** ErrorMessage, LLVMMemoryBufferRef* OutMemBuf)
{
    return LLVMTargetMachineEmitToMemoryBuffer(T, M, codegen, ErrorMessage, OutMemBuf);
}


DNA_EXPORT char* FfiLLVMGetDefaultTargetTriple()
{
    return LLVMGetDefaultTargetTriple();
}


DNA_EXPORT char* FfiLLVMNormalizeTargetTriple(const char* triple)
{
    return LLVMNormalizeTargetTriple(triple);
}


DNA_EXPORT char* FfiLLVMGetHostCPUName()
{
    return LLVMGetHostCPUName();
}


DNA_EXPORT char* FfiLLVMGetHostCPUFeatures()
{
    return LLVMGetHostCPUFeatures();
}


DNA_EXPORT void  FfiLLVMAddAnalysisPasses(LLVMTargetMachineRef T, LLVMPassManagerRef PM)
{
    return LLVMAddAnalysisPasses(T, PM);
}

// The above exports were generated by a script. Below are manually written exports that were (for some unknown reason) not handled by my script.
// TODO: Regenerate more complete bindings.
DNA_EXPORT LLVMValueRef FfiLLVMIsAFunction(LLVMValueRef Val)
{
    return LLVMIsAFunction(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAGlobalValue(LLVMValueRef Val)
{
    return LLVMIsAGlobalValue(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAInstruction(LLVMValueRef Val)
{
    return LLVMIsAInstruction(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAUser(LLVMValueRef Val)
{
    return LLVMIsAUser(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAConstantInt(LLVMValueRef Val)
{
    return LLVMIsAConstantInt(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAGlobalVariable(LLVMValueRef Val)
{
    return LLVMIsAGlobalVariable(Val);
}

DNA_EXPORT LLVMValueRef FfiLLVMIsAConstantExpr(LLVMValueRef Val)
{
    return LLVMIsAConstantExpr(Val);
}
