#pragma once
#pragma once

#include <hexrays.hpp>
#include "Minsn.hpp"
#include "Mnumber.hpp"


enum class MopKind : uint8
{
	mop_z = 0,  ///< none
	mop_r = 1,  ///< register (they exist until MMAT_LVARS)
	mop_n = 2,  ///< immediate number constant
	mop_str = 3,  ///< immediate string constant (user representation)
	mop_d = 4,  ///< result of another instruction
	mop_S = 5,  ///< local stack variable (they exist until MMAT_LVARS)
	mop_v = 6,  ///< global variable
	mop_b = 7,  ///< micro basic block (mblock_t)
	mop_f = 8,  ///< list of arguments
	mop_l = 9,  ///< local variable
	mop_a = 10, ///< mop_addr_t: address of operand (mop_l, mop_v, mop_S, mop_r)
	mop_h = 11, ///< helper function
	mop_c = 12, ///< mcases
	mop_fn = 13, ///< floating point constant
	mop_p = 14, ///< operand pair
	mop_sc = 15 ///< scattered
};

public ref class Mop
{
public:
	mop_t* mop;

	Mop(mop_t* mop)
	{
		this->mop = mop;
	}

	Mop(int reg, int size)
	{
		this->mop = new mop_t(reg, size);
	}

	/// <summary>
	/// Get displayable text without tags in a static buffer.
	/// </summary>
	System::String^ Dstr()
	{
		return gcnew System::String(mop->dstr());
	}

	MopKind GetOpKind()
	{
		return (MopKind)mop->t;
	}

	void SetOpKind(MopKind kind)
	{
		mop->t = (mopt_t)kind;
	}

	uint8 GetOpProperties()
	{
		return mop->oprops;
	}

	void SetOpKind(uint8 props)
	{
		mop->oprops = props;
	}

	uint16 GetValueNumber()
	{
		return mop->valnum;
	}

	void SetValueNumber(uint16 props)
	{
		mop->valnum = props;
	}

	int GetSize()
	{
		return mop->size;
	}

	void SetValueNumber(int size)
	{
		mop->size = size;
	}

	int GetUnionReg()
	{
		return mop->r;
	}

	Mnumber^ GetUnionNumber()
	{
		return gcnew Mnumber(mop->nnn);
	}

	Minsn^ GetUnionMinsn()
	{
		return gcnew Minsn(mop->d);
	}

	int GetUnionG()
	{
		return mop->g;
	}

	int GetUnionB()
	{
		return mop->b;
	}

	System::String^ GetUnionHelper()
	{
		return gcnew System::String(mop->helper);
	}

	System::String^ GetUnionCstr()
	{
		return gcnew System::String(mop->cstr);
	}

	void SetUnionReg(int reg)
	{
		mop->r = reg;
	}

	void SetUnionNumber(Mnumber^ num)
	{
		mop->nnn = num->mnumber;
	}

	void SetUnionMinsn(Minsn^ num)
	{
		mop->d = num->inst;
	}

	void SetUnionG(int g)
	{
		mop->g = g;
	}

	void SetUnionB(int b)
	{
		mop->b = b;
	}


};