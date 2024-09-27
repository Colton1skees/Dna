#pragma once

#include <vector>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/SmallVector.h>
#include <API/ExportDef.h>

namespace Dna::API {
	class ImmutableManagedPair
	{
	public:
		ImmutableManagedPair(void* first, void* second)
		{
			this->first = first;
			this->second = second;
		}

		void* GetFirst()
		{
			return first;
		}

		void* GetSecond()
		{
			return second;
		}

	private:
		void* first = nullptr;

		void* second = nullptr;
	};

	DNA_EXPORT void* ImmutableManagedPair_GetFirst(ImmutableManagedPair* pair)
	{
		return pair->GetFirst();
	}

	DNA_EXPORT void* ImmutableManagedPair_GetSecond(ImmutableManagedPair* pair)
	{
		return pair->GetSecond();
	}
}