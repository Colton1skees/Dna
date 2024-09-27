#pragma once

#include <llvm/Transforms/Utils.h>
#include <API/ExportDef.h>
#
using namespace llvm;

//===----------------------------------------------------------------------===//
//
// AlignmentFromAssumptions - Use assume intrinsics to set load/store
// alignments.
//
DNA_EXPORT FunctionPass* CreateLowerSwitchPass()
{
    return createLowerSwitchPass();
}


//===----------------------------------------------------------------------===//
//
// AlignmentFromAssumptions - Use assume intrinsics to set load/store
// alignments.
//
DNA_EXPORT Pass* CreateLCSSAPass()
{
    return createLCSSAPass();
}


//===----------------------------------------------------------------------===//
//
// AlignmentFromAssumptions - Use assume intrinsics to set load/store
// alignments.
//
DNA_EXPORT Pass* CreateLoopSimplifyPass()
{
    return createLoopSimplifyPass();
}


//===----------------------------------------------------------------------===//
//
// AlignmentFromAssumptions - Use assume intrinsics to set load/store
// alignments.
//
DNA_EXPORT FunctionPass* CreateFixIrreduciblePass()
{
    return createFixIrreduciblePass();
}

