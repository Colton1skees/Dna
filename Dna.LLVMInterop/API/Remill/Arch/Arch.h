#pragma once 

#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/ArchBase.h"
#include "remill/Arch/Arch.h"
#include "remill/BC/ABI.h"
#include "remill/BC/Util.h"
#include "remill/BC/Version.h"
#include "remill/OS/OS.h"

#include "API/ExportDef.h"
#include <API/ImmutableManagedVector.h>
#include <iostream>

namespace Dna::API {
	DNA_EXPORT char* Register_GetName(remill::Register* reg)
	{
		return _strdup(reg->name.c_str());
	}

	DNA_EXPORT uint64_t Register_GetOffset(remill::Register* reg)
	{
		return reg->offset;
	}

	DNA_EXPORT uint64_t Register_GetSize(remill::Register* reg)
	{
		return reg->size;
	}

	DNA_EXPORT llvm::Type* Register_GetType(remill::Register* reg)
	{
		return reg->type;
	}

	DNA_EXPORT llvm::Constant* Register_GetConstantName(remill::Register* reg)
	{
		return reg->constant_name;
	}

	DNA_EXPORT ImmutableManagedVector* Register_GetGepIndexList(remill::Register* reg)
	{
		return ImmutableManagedVector::From(&reg->gep_index_list);
	}

	DNA_EXPORT uint64_t Register_GetGepOffset(remill::Register* reg)
	{
		return reg->gep_offset;
	}

	DNA_EXPORT llvm::Type* Register_GetGepTypeAtOffset(remill::Register* reg)
	{
		return reg->gep_type_at_offset;
	}

	DNA_EXPORT const remill::Register* Register_EnclosingRegisterOfSize(remill::Register* reg, uint64_t size)
	{
		return reg->EnclosingRegisterOfSize(size);
	}

	DNA_EXPORT const remill::Register* Register_EnclosingRegister(remill::Register* reg)
	{
		return reg->EnclosingRegister();
	}

	DNA_EXPORT ImmutableManagedVector* Register_EnclosedRegisters(remill::Register* reg)
	{
		return ImmutableManagedVector::From(&reg->EnclosedRegisters());
	}

	DNA_EXPORT llvm::Value* Register_AddressOf(remill::Register* reg, llvm::Value* statePtr, llvm::BasicBlock* addToEnd)
	{
		return reg->AddressOf(statePtr, addToEnd);
	}

	DNA_EXPORT llvm::Value* Register_AddressOfUsingBuilder(remill::Register* reg, llvm::Value* statePtr, llvm::IRBuilder<>* builder)
	{
		return reg->AddressOf(statePtr, *builder);
	}

	DNA_EXPORT const remill::Register* Register_GetParent(remill::Register* reg)
	{
		return reg->parent;
	}

	DNA_EXPORT const remill::Arch* Register_GetArch(remill::Register* reg)
	{
		return reg->arch;
	}

	DNA_EXPORT ImmutableManagedVector* Register_GetChildren(remill::Register* reg)
	{
		return ImmutableManagedVector::From(&reg->children);
	}
}

// remill::Arch
namespace Dna::API {
	typedef void(__cdecl* tRegisterCallback)(const remill::Register* reg);

	DNA_EXPORT const remill::Arch* __cdecl Arch_Constructor(llvm::LLVMContext* context, uint32_t osName, uint32_t archName)
	{
		// Create an remill architecture using the specified OS / arch ID.
		auto uniquePtr = remill::Arch::Get(*context, (remill::OSName)osName, (remill::ArchName)archName);

		auto ptr = uniquePtr.get();

		// Relinquish ownership to the managed caller.
		uniquePtr.release();

		// Since we're passing ownership of this object to managed code,
		// we need to relinquish ownership.
		return ptr;
	}

	DNA_EXPORT remill::DecodingContext* Arch_CreateInitialContext(remill::Arch* arch)
	{
		return new remill::DecodingContext(arch->CreateInitialContext());
	}

	DNA_EXPORT const remill::Arch* Arch_GetModuleArch(llvm::Module* module)
	{
		// While a smart pointer is used here, we don't
		// need to care about reference counting. The managed caller is assumed to own this object.
		return remill::Arch::GetModuleArch(*module).get();
	}

	DNA_EXPORT llvm::IntegerType* Arch_AddressType(remill::Arch* arch)
	{
		return arch->AddressType();
	}

	DNA_EXPORT llvm::StructType* Arch_StateStructType(remill::Arch* arch)
	{
		return arch->StateStructType();
	}

	DNA_EXPORT llvm::PointerType* Arch_StatePointerType(remill::Arch* arch)
	{
		return arch->StatePointerType();
	}

	DNA_EXPORT llvm::PointerType* Arch_MemoryPointerType(remill::Arch* arch)
	{
		return arch->MemoryPointerType();
	}

	DNA_EXPORT llvm::FunctionType* Arch_LiftedFunctionType(remill::Arch* arch)
	{
		return arch->LiftedFunctionType();
	}

	DNA_EXPORT llvm::StructType* Arch_RegisterWindowType(remill::Arch* arch)
	{
		return arch->RegisterWindowType();
	}

	DNA_EXPORT const remill::IntrinsicTable* Arch_GetIntrinsicTable(remill::Arch* arch)
	{
		return arch->GetInstrinsicTable();
	}

	DNA_EXPORT void Arch_ForEachRegister(remill::Arch* arch, tRegisterCallback cb)
	{
		arch->ForEachRegister(cb);
	}

	DNA_EXPORT const remill::Register* Arch_RegisterAtStateOffset(remill::Arch* arch, uint64_t offset)
	{
		return arch->RegisterAtStateOffset(offset);
	}

	DNA_EXPORT const remill::Register* Arch_RegisterByName(remill::Arch* arch, char* name)
	{
		return arch->RegisterByName(name);
	}

	DNA_EXPORT char* Arch_StackPointerRegisterName(remill::Arch* arch)
	{
		auto str = std::string(arch->StackPointerRegisterName());
		return _strdup(str.c_str());
	}

	DNA_EXPORT char* Arch_ProgramCounterRegisterName(remill::Arch* arch)
	{
		auto idk = arch->ProgramCounterRegisterName();
		auto str = std::string(arch->ProgramCounterRegisterName());
		return _strdup(str.c_str());
	}

	DNA_EXPORT llvm::Function* Arch_DeclareLiftedFunction(remill::Arch* arch, char* name, llvm::Module* mod)
	{
		return arch->DeclareLiftedFunction(name, mod);
	}

	DNA_EXPORT llvm::Function* Arch_DefineLiftedFunction(remill::Arch* arch, char* name, llvm::Module* mod)
	{
		return arch->DefineLiftedFunction(name, mod);
	}

	DNA_EXPORT void Arch_InitializeEmptyLiftedFunction(remill::Arch* arch, llvm::Function* func)
	{
		arch->InitializeEmptyLiftedFunction(func);
	}

	DNA_EXPORT void Arch_PrepareModule(remill::Arch* arch, llvm::Module* mod)
	{
		arch->PrepareModule(mod);
	}

	DNA_EXPORT void Arch_PrepareModuleDataLayout(remill::Arch* arch, llvm::Module* mod)
	{
		arch->PrepareModuleDataLayout(mod);
	}

	DNA_EXPORT const remill::OperandLifter* Arch_DefaultLifter(remill::Arch* arch, remill::IntrinsicTable* intrinsics)
	{
		auto ptr = arch->DefaultLifter(*intrinsics);

		// Since we're passing ownership of this object to managed code,
		// we need to relinquish ownership.
		auto reference = new remill::OperandLifter::OpLifterPtr(ptr);

		return ptr.get();
	}

	DNA_EXPORT bool Arch_DecodeInstruction(remill::Arch* arch, uint64_t address, char* instrBytes, int byteCount, remill::Instruction* inst, remill::DecodingContext* decodingContext)
	{
		auto view = std::string_view(instrBytes, byteCount);
		auto res = arch->DecodeInstruction(address, view, *inst, *decodingContext);
		return res;
	}

	DNA_EXPORT bool Arch_DecodeDelayedInstruction(remill::Arch* arch, uint64_t address, char* instrBytes, remill::Instruction* inst, remill::DecodingContext* decodingContext)
	{
		return arch->DecodeDelayedInstruction(address, instrBytes, *inst, *decodingContext);
	}

	DNA_EXPORT llvm::CallingConv::ID Arch_DefaultCallingConv(remill::Arch* arch)
	{
		return arch->DefaultCallingConv();
	}
}