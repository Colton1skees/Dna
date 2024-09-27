 
#include <API/ExportDef.h>
#include <API/ImmutableManagedVector.h>
#include <llvm/Analysis/MemorySSA.h>
#include <llvm/Analysis/MemoryLocation.h>
// MemoryAccess. TODO: Expose iterators for defs / uses.
namespace Dna::API {
	DNA_EXPORT llvm::BasicBlock* MemoryAccess_GetBlock(llvm::MemoryAccess* memAccess)
	{
		return memAccess->getBlock();
	}

	DNA_EXPORT char* MemoryAccess_ToString(llvm::MemoryAccess* memAccess)
	{
		std::string buf;
		llvm::raw_string_ostream os(buf);

		memAccess->print(os);
		os.flush();

		return _strdup(buf.c_str());
	}
}

// MemorySSAWalker
namespace Dna::API {
	DNA_EXPORT llvm::MemoryAccess* MemorySSAWalker_GetInstClobberingMemoryAccess(llvm::MemorySSAWalker* walker, llvm::Instruction* instruction)
	{
		return walker->getClobberingMemoryAccess(instruction);
	}

	DNA_EXPORT llvm::MemoryAccess* MemorySSAWalker_GetAccessClobberingMemoryAccess1(llvm::MemorySSAWalker* walker, llvm::MemoryAccess* access)
	{
		return walker->getClobberingMemoryAccess(access);
	}

	DNA_EXPORT llvm::MemoryAccess* MemorySSAWalker_GetLocClobberingMemoryAccess(llvm::MemorySSAWalker* walker, llvm::MemoryAccess* access, llvm::MemoryLocation* location)
	{
		return walker->getClobberingMemoryAccess(access, *location);
	}
}

// LocationSize
namespace Dna::API {
	DNA_EXPORT bool LocationSize_GetHasValue(llvm::LocationSize* size)
	{
		return size->hasValue();
	}

	DNA_EXPORT uint64_t LocationSize_GetValue(llvm::LocationSize* size)
	{
		return size->getValue();
	}

	DNA_EXPORT bool LocationSize_GetIsPrecise(llvm::LocationSize* size)
	{
		return size->isPrecise();
	}

	DNA_EXPORT bool LocationSize_GetIsZero(llvm::LocationSize* size)
	{
		return size->isZero();
	}

	DNA_EXPORT bool LocationSize_GetMayBeBeforePointer(llvm::LocationSize* size)
	{
		return size->mayBeBeforePointer();
	}

	DNA_EXPORT bool LocationSize_GetIsEqual(llvm::LocationSize* size, llvm::LocationSize* other)
	{
		return *size == *other;
	}

	DNA_EXPORT uint64_t LocationSize_GetRaw(llvm::LocationSize* size)
	{
		return size->toRaw();
	}

	DNA_EXPORT char* LocationSize_ToString(llvm::LocationSize* size)
	{
		std::string buf;
		llvm::raw_string_ostream os(buf);

		size->print(os);
		os.flush();

		return _strdup(buf.c_str());
	}
}

// MemoryUseOrDef
namespace Dna::API {
	DNA_EXPORT llvm::Instruction* MemoryUseOrDef_GetMemoryInst(llvm::MemoryUseOrDef* mem)
	{
		return mem->getMemoryInst();
	}

	DNA_EXPORT llvm::MemoryAccess* MemoryUseOrDef_GetDefiningAccess(llvm::MemoryUseOrDef* mem)
	{
		return mem->getDefiningAccess();
	}

	DNA_EXPORT bool MemoryUseOrDef_IsOptimized(llvm::MemoryUseOrDef* mem)
	{
		return mem->isOptimized();
	}
}

// MemoryPhi
namespace Dna::API {
	DNA_EXPORT ImmutableManagedVector* MemoryPhi_GetBlocks(llvm::MemoryPhi* memPhi)
	{
		auto output = new std::vector<llvm::BasicBlock*>();

		auto blocks = memPhi->blocks();
		for (auto block : blocks)
		{
			output->push_back(block);
		}

		return ImmutableManagedVector::NonCopyingFrom(output);
	}

	DNA_EXPORT ImmutableManagedVector* MemoryPhi_GetIncomingValues(llvm::MemoryPhi* memPhi)
	{
		auto output = new std::vector<llvm::Use*>();

		auto incomingValues = memPhi->incoming_values();
		for (auto& value : incomingValues)
		{
			output->push_back(&value);
		}

		return ImmutableManagedVector::NonCopyingFrom(output);
	}

	DNA_EXPORT unsigned int MemoryPhi_GetNumIncomingValues(llvm::MemoryPhi* memPhi)
	{
		return memPhi->getNumIncomingValues();
	}

	DNA_EXPORT llvm::MemoryAccess* MemoryPhi_GetIncomingValue(llvm::MemoryPhi* memPhi, unsigned int index)
	{
		return memPhi->getIncomingValue(index);
	}

	DNA_EXPORT llvm::MemoryAccess* MemoryPhi_GetIncomingValueForBlock(llvm::MemoryPhi* memPhi, llvm::BasicBlock* block)
	{
		return memPhi->getIncomingValueForBlock(block);
	}
}

// MemoryLocation
namespace Dna::API {
	DNA_EXPORT llvm::MemoryLocation* MemoryLocation_GetOrNone(llvm::Instruction* instruction)
	{
		auto result = llvm::MemoryLocation::getOrNone(instruction);
		if (result.has_value())
			return new llvm::MemoryLocation(result.value());
		return nullptr;
	}

	DNA_EXPORT char* MemoryLocation_ToString(llvm::MemoryLocation* loc)
	{
		std::string buf;
		llvm::raw_string_ostream os(buf);

		loc->print(os);
		os.flush();

		return _strdup(buf.c_str());
	}

	DNA_EXPORT const llvm::Value* MemoryLocation_GetPtr(llvm::MemoryLocation* loc)
	{
		return loc->Ptr;
	}

	DNA_EXPORT llvm::LocationSize* MemoryLocation_GetLocationSize(llvm::MemoryLocation* loc)
	{
		return new llvm::LocationSize(loc->Size);
	}
}

// MemorySSA. TODO: Export dominator tree.
namespace Dna::API {
	DNA_EXPORT llvm::MemorySSAWalker* MemorySSA_GetWalker(llvm::MemorySSA* memSsa)
	{
		return memSsa->getWalker();
	}

	DNA_EXPORT llvm::MemorySSAWalker* MemorySSA_GetSkipSelfWalker(llvm::MemorySSA* memSsa)
	{
		return memSsa->getSkipSelfWalker();
	}

	DNA_EXPORT llvm::MemoryUseOrDef* MemorySSA_GetMemoryAccessFromInstruction(llvm::MemorySSA* memSsa, llvm::Instruction* inst)
	{
		return memSsa->getMemoryAccess(inst);
	}

	DNA_EXPORT llvm::MemoryPhi* MemorySSA_GetMemoryAccessFromBlock(llvm::MemorySSA* memSsa, llvm::BasicBlock* block)
	{
		return memSsa->getMemoryAccess(block);
	}

	DNA_EXPORT bool MemorySSA_IsLiveOnEntryDef(llvm::MemorySSA* memSsa, llvm::MemoryAccess* ma)
	{
		return memSsa->isLiveOnEntryDef(ma);
	}

	DNA_EXPORT bool MemorySSA_GetLiveOnEntryDef(llvm::MemorySSA* memSsa)
	{
		return memSsa->getLiveOnEntryDef();
	}

	DNA_EXPORT ImmutableManagedVector* MemorySSA_GetBlockAccesses(llvm::MemorySSA* memSsa, llvm::BasicBlock* block)
	{
		auto output = new std::vector<const llvm::MemoryAccess*>();
		for (auto& access : *memSsa->getBlockAccesses(block))
		{
			output->push_back(&access);
		}

		return ImmutableManagedVector::NonCopyingFrom(output);
	}

	DNA_EXPORT ImmutableManagedVector* MemorySSA_GetBlockDefs(llvm::MemorySSA* memSsa, llvm::BasicBlock* block)
	{
		auto output = new std::vector<const llvm::MemoryAccess*>();
		for (auto& def : *memSsa->getBlockDefs(block))
		{
			output->push_back(&def);
		}

		return ImmutableManagedVector::NonCopyingFrom(output);
	}

	DNA_EXPORT bool MemorySSA_LocallyDominates(llvm::MemorySSA* memSsa, llvm::MemoryAccess* a, llvm::MemoryAccess* b)
	{
		return memSsa->locallyDominates(a, b);
	}

	DNA_EXPORT bool MemorySSA_Dominates(llvm::MemorySSA* memSsa, llvm::MemoryAccess* a, llvm::MemoryAccess* b)
	{
		return memSsa->dominates(a, b);
	}

	DNA_EXPORT bool MemorySSA_DominatesUse(llvm::MemorySSA* memSsa, llvm::MemoryAccess* a, llvm::Use* b)
	{
		return memSsa->dominates(a, *b);
	}
}