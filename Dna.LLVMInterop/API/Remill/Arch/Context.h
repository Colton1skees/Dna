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

// remill::DecodingContext.
// Note: Most of the APIs remain unchanged here, as the context API is only necessary for ARM.
namespace Dna::API {
	DNA_EXPORT remill::DecodingContext* DecodingContext_Constructor()
	{
		return new remill::DecodingContext();
	}
}
