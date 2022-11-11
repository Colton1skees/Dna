#pragma once

#include <hexrays.hpp>
#include "Mblock.hpp"

public ref class Mba
{
private:
	mba_t* mba;

public:
	Mba(mba_t* mba)
	{
		this->mba = mba;
	}

	Mblock^ GetMblock(int n)
	{
		auto block = mba->get_mblock(n);
		return gcnew Mblock(block);
	}

	Mblock^ InsertBlock(int bblk)
	{
		auto block = mba->insert_block(bblk);
		return gcnew Mblock(block);
	}

	bool RemoveBlock(Mblock^ block)
	{
		return mba->remove_block(block->mblock);
	}
};
