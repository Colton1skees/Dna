#pragma once
#pragma once

#include <hexrays.hpp>
#include "Minsn.hpp"

public ref class Mnumber
{
public:
	mnumber_t* mnumber;

	Mnumber(mnumber_t* mnumber)
	{
		this->mnumber = mnumber;
	}

	Mnumber(uint64 value, int ea, int n)
	{
		mnumber = new mnumber_t(value, ea, n);
	}

	uint64 GetValue()
	{
		return mnumber->value;
	}

	void SetValue(uint64 val64)
	{
		mnumber->update_value(val64);
	}

	uint64 GetValue()
	{
		return mnumber->org_value;
	}
};