#pragma once

#include <vector>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/SmallVector.h>
#include <API/ExportDef.h>

namespace Dna::API {
	class ImmutableManagedVector
	{
	public:
		template <class T>
		static ImmutableManagedVector* From(const std::vector<T*>* input)
		{
			auto managedVector = new ImmutableManagedVector();
			for (auto item : *input)
			{
				managedVector->items->push_back(item);
			}

			return managedVector;
		}

		template <class T>
		static ImmutableManagedVector* NonCopyingFrom(const std::vector<T*>* input)
		{
			auto managedVector = new ImmutableManagedVector();
			managedVector->items = (std::vector<void*>*)input;
			return managedVector;
		}

		template <class T>
		static ImmutableManagedVector* From(const llvm::ArrayRef<T*>* input)
		{
			auto managedVector = new ImmutableManagedVector();
			for (auto item : *input)
			{
				managedVector->items->push_back(item);
			}

			return managedVector;
		}

		template <class T>
		static ImmutableManagedVector* From(llvm::SmallVectorImpl<T*>* input)
		{
			auto managedVector = new ImmutableManagedVector();
			for (auto item : *input)
			{
				managedVector->items->push_back(item);
			}

			return managedVector;
		}

		int GetCount()
		{
			return items->size();
		}

		void* GetElement(int index)
		{
			return items->at(index);
		}

	private:
		std::vector<void*>* items = new std::vector<void*>();
	};

	DNA_EXPORT unsigned int ImmutableManagedVector_GetCount(ImmutableManagedVector* vec)
	{
		return vec->GetCount();
	}

	DNA_EXPORT void* ImmutableManagedVector_GetElementAt(ImmutableManagedVector* vec, int index)
	{
		return vec->GetElement(index);
	}
}