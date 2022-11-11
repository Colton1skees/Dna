#pragma once
#pragma once

#include <hexrays.hpp>
#include "Minsn.hpp"

public ref class Mblock
{
public:
	mblock_t* mblock;

	Mblock(mblock_t* mba)
	{
		this->mblock = mba;
	}

	/// <summary>
	/// Gets the number of block predecessors.
	/// </summary>
	/// <returns></returns>
	int GetNumPredecessors()
	{
		return mblock->npred();
	}

	/// <summary>
	/// Gets the number of block successors.
	/// </summary>
	int GetNumSuccessors()
	{
		return mblock->nsucc();
	}

	/// <summary>
	/// Gets the precessor number N.
	/// </summary>
	int GetPredecessor(int n)
	{
		return mblock->pred(n);
	}

	/// <summary>
	/// Gets the successor number N.
	/// </summary>
	int GetSuccessor(int n)
	{
		return mblock->succ(n);
	}

	bool GetIsEmpty()
	{
		return mblock->empty();
	}

	Mblock^ GetNextBlock()
	{
		/// <summary>
		/// Gets the next block in the doubly linked list.
		/// </summary>
		/// <returns></returns>
		return gcnew Mblock(mblock->nextb);
	}

	/// <summary>
	/// Gets the previous block in the doubly linked list.
	/// </summary>
	/// <returns></returns>
	Mblock^ GetPrevBlock()
	{
		return gcnew Mblock(mblock->nextb);
	}

	/// <summary>
	/// Gets the first instruction of the block.
	/// </summary>
	Minsn^ GetHead()
	{
		return gcnew Minsn(mblock->head);
	}

	/// <summary>
	/// Gets the last instruction of the block.
	/// </summary>
	Minsn^ GetTail()
	{
		return gcnew Minsn(mblock->tail);
	}

	/// <summary>
	/// Gets the parent micro block array.
	/// </summary>
	Mba^ GetMba()
	{
		return gcnew Mba(mblock->mba);
	}
};