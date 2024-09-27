#pragma once 

#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/Arch.h"
#include "remill/BC/ABI.h"
#include "remill/BC/Util.h"
#include "remill/BC/Version.h"
#include "remill/OS/OS.h"

#include "API/ExportDef.h"

namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetError(remill::IntrinsicTable* it)
	{
		return it->error;
	}
}

// Control-flow
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetFunctionCall(remill::IntrinsicTable* it)
	{
		return it->function_call;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetFunctionReturn(remill::IntrinsicTable* it)
	{
		return it->function_return;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetJump(remill::IntrinsicTable* it)
	{
		return it->jump;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetMissingBlock(remill::IntrinsicTable* it)
	{
		return it->missing_block;
	}
}

// OS interactions.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetSyncHyperCall(remill::IntrinsicTable* it)
	{
		return it->async_hyper_call;
	}
}

// Memory read intrinsics.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemory8(remill::IntrinsicTable* it)
	{
		return it->read_memory_8;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemory16(remill::IntrinsicTable* it)
	{
		return it->read_memory_16;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemory32(remill::IntrinsicTable* it)
	{
		return it->read_memory_32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemory64(remill::IntrinsicTable* it)
	{
		return it->read_memory_64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemoryF32(remill::IntrinsicTable* it)
	{
		return it->read_memory_f32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemoryF64(remill::IntrinsicTable* it)
	{
		return it->read_memory_f64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemoryF80(remill::IntrinsicTable* it)
	{
		return it->read_memory_f80;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetReadMemoryF128(remill::IntrinsicTable* it)
	{
		return it->read_memory_f128;
	}
}

// Memory write intrinsics.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemory8(remill::IntrinsicTable* it)
	{
		return it->write_memory_8;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemory16(remill::IntrinsicTable* it)
	{
		return it->write_memory_16;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemory32(remill::IntrinsicTable* it)
	{
		return it->write_memory_32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemory64(remill::IntrinsicTable* it)
	{
		return it->write_memory_64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemoryF32(remill::IntrinsicTable* it)
	{
		return it->write_memory_f32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemoryF64(remill::IntrinsicTable* it)
	{
		return it->write_memory_f64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemoryF80(remill::IntrinsicTable* it)
	{
		return it->write_memory_f80;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetWriteMemoryF128(remill::IntrinsicTable* it)
	{
		return it->write_memory_f128;
	}
}

// Memory barriers.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetBarrierLoadLoad(remill::IntrinsicTable* it)
	{
		return it->barrier_load_load;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetBarrierLoadStore(remill::IntrinsicTable* it)
	{
		return it->barrier_load_store;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetBarrierStoreLoad(remill::IntrinsicTable* it)
	{
		return it->barrier_store_load;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetBarrierStoreStore(remill::IntrinsicTable* it)
	{
		return it->barrier_store_store;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetAtomicBegin(remill::IntrinsicTable* it)
	{
		return it->atomic_begin;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetAtomicEnd(remill::IntrinsicTable* it)
	{
		return it->atomic_end;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetDelaySlotBegin(remill::IntrinsicTable* it)
	{
		return it->delay_slot_begin;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetDelaySlotEnd(remill::IntrinsicTable* it)
	{
		return it->delay_slot_end;
	}
}

// Optimization enabling.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefined8(remill::IntrinsicTable* it)
	{
		return it->undefined_8;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefined16(remill::IntrinsicTable* it)
	{
		return it->undefined_16;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefined32(remill::IntrinsicTable* it)
	{
		return it->undefined_32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefined64(remill::IntrinsicTable* it)
	{
		return it->undefined_64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefinedF32(remill::IntrinsicTable* it)
	{
		return it->undefined_f32;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefinedF64(remill::IntrinsicTable* it)
	{
		return it->undefined_f64;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetUndefinedF80(remill::IntrinsicTable* it)
	{
		return it->undefined_f80;
	}
}

// Flag markers.
namespace Dna::API {
	DNA_EXPORT llvm::Function* IntrinsicTable_GetFlagComputationZero(remill::IntrinsicTable* it)
	{
		return it->flag_computation_zero;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetFlagComputationSign(remill::IntrinsicTable* it)
	{
		return it->flag_computation_sign;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetFlagComputationOverflow(remill::IntrinsicTable* it)
	{
		return it->flag_computation_overflow;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetFlagComputationCarry(remill::IntrinsicTable* it)
	{
		return it->flag_computation_carry;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetCompareSle(remill::IntrinsicTable* it)
	{
		return it->compare_sle;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetCompareSgt(remill::IntrinsicTable* it)
	{
		return it->compare_sgt;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetCompareEq(remill::IntrinsicTable* it)
	{
		return it->compare_eq;
	}

	DNA_EXPORT llvm::Function* IntrinsicTable_GetCompareNeq(remill::IntrinsicTable* it)
	{
		return it->compare_neq;
	}
}

// Misc.
namespace Dna::API {
	DNA_EXPORT llvm::FunctionType* IntrinsicTable_GetLiftedFunctionType(remill::IntrinsicTable* it)
	{
		return it->lifted_function_type;
	}

	DNA_EXPORT llvm::PointerType* IntrinsicTable_GetStatePtrType(remill::IntrinsicTable* it)
	{
		return it->state_ptr_type;
	}

	DNA_EXPORT llvm::IntegerType* IntrinsicTable_GetPcType(remill::IntrinsicTable* it)
	{
		return it->pc_type;
	}

	DNA_EXPORT llvm::PointerType* IntrinsicTable_GetPointerType(remill::IntrinsicTable* it)
	{
		return it->mem_ptr_type;
	}
}