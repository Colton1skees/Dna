// dllmain.cpp : Defines the entry point for the DLL application.
#include <llvm/Support/JSON.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/IR/LegacyPassManager.h>
#include <llvm/Pass.h>
#include <llvm/Transforms/Scalar.h>
#include "llvm/Transforms/Scalar/SimpleLoopUnswitch.h"
#include "llvm/Transforms/Utils/LowerSwitch.h"
#include <ExportedApi.h>
#include "Passes/ClassifyingAliasAnalysisPass.h"
#include <Windows.h>
