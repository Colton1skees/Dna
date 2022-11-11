#pragma once
#pragma once

#include <hexrays.hpp>

public enum class MOpCode : int
{
	m_nop = 0x00,
	m_stx = 0x01,
	m_ldx = 0x02,
	m_ldc = 0x03,
	m_mov = 0x04,
	m_neg = 0x05,
	m_lnot = 0x06,
	m_bnot = 0x07,
	m_xds = 0x08,
	m_xdu = 0x09,
	m_low = 0x0A,
	m_high = 0x0B,
	m_add = 0x0C,
	m_sub = 0x0D,
	m_mul = 0x0E, 
	m_udiv = 0x0F,
	m_sdiv = 0x10,
	m_umod = 0x11,
	m_smod = 0x12,
	m_or = 0x13,
	m_and = 0x14,
	m_xor = 0x15,
	m_shl = 0x16,
	m_shr = 0x17,
	m_sar = 0x18,
	m_cfadd = 0x19, 
	m_ofadd = 0x1A,
	m_cfshl = 0x1B,
	m_cfshr = 0x1C,
	m_sets = 0x1D, 
	m_seto = 0x1E,
	m_setp = 0x1F,
	m_setnz = 0x20,
	m_setz = 0x21,
	m_setae = 0x22, 
	m_setb = 0x23,
	m_seta = 0x24,
	m_setbe = 0x25, 
	m_setg = 0x26, 
	m_setge = 0x27,
	m_setl = 0x28,
	m_setle = 0x29,
	m_jcnd = 0x2A,
	m_jnz = 0x2B,
	m_jz = 0x2C,
	m_jae = 0x2D,
	m_jb = 0x2E,
	m_ja = 0x2F,
	m_jbe = 0x30,
	m_jg = 0x31, 
	m_jge = 0x32,
	m_jl = 0x33,
	m_jle = 0x34, 
	m_jtbl = 0x35,
	m_ijmp = 0x36,
	m_goto = 0x37,
	m_call = 0x38,
	m_icall = 0x39,
	m_ret = 0x3A, 
	m_push = 0x3B,
	m_pop = 0x3C, 
	m_und = 0x3D, 
	m_ext = 0x3E, 
	m_f2i = 0x3F,
	m_f2u = 0x40,
	m_i2f = 0x41,
	m_u2f = 0x42,
	m_f2f = 0x43,
	m_fneg = 0x44,
	m_fadd = 0x45,
	m_fsub = 0x46,
	m_fmul = 0x47,
	m_fdiv = 0x48
};

public ref class Minsn
{
public:
	minsn_t* inst;

	Minsn(minsn_t* inst)
	{
		this->inst = inst;
	}

	Minsn(ea_t address)
	{
		inst = new minsn_t(address);
	}

	/// <summary>
	/// Swap two instructions.
	/// </summary>
	void Swap(Minsn^ m)
	{
		inst->swap(*m->inst);
	}

	/// <summary>
	/// Get displayable text without tags in a static buffer.
	/// </summary>
	System::String^ Dstr()
	{
		return gcnew System::String(inst->dstr());
	}

	MOpCode GetOpCode()
	{
		return (MOpCode)inst->opcode;
	}

	void SetOpCode(MOpCode code)
	{
		inst->opcode = (mcode_t)code;
	}

	Minsn^ GetNext()
	{
		return gcnew Minsn(inst->next);
	}

	void SetNext(Minsn^ next)
	{
		inst->next = next->inst;
	}

	Minsn^ GetPrev()
	{
		return gcnew Minsn(inst->prev);
	}

	void SetPrev(Minsn^ prev)
	{
		inst->prev = prev->inst;
	}

	int GetAddress()
	{
		return inst->ea;
	}

	void SetAddress(int address)
	{
		inst->setaddr(address);
	}
};