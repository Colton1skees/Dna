#pragma once

// LLVM's legacy PassManagerBuilder API was removed before LLVM 17.
// Dna.LLVMInterop now uses the new pass manager through llvm/Passes/PassBuilder.h.
// This placeholder remains only to preserve the old project file structure while
// managed callers are migrated away from Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO.

#include <API/ExportDef.h>
