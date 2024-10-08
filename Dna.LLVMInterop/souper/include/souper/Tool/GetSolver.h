// Copyright 2014 The Souper Authors. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#ifndef SOUPER_TOOL_GETSOLVER_H
#define SOUPER_TOOL_GETSOLVER_H

#include "llvm/Support/CommandLine.h"
#include "souper/Extractor/Solver.h"
#include "souper/KVStore/KVStore.h"
#include "souper/SMTLIB2/Solver.h"
// #include <unistd.h>
#include <memory>
#include <string>

namespace souper {

static constexpr const char *Z3Path = "@Z3@";
//static_assert(Z3Path[0] != 0 && Z3Path[0] != '@',
    //          "CMake does not seem to have rewritten the solver path correctly");

static llvm::cl::opt<bool> KeepSolverInputs(
    "keep-solver-inputs", llvm::cl::desc("Do not clean up solver inputs"),
    llvm::cl::init(false));

static llvm::cl::opt<bool> MemCache(
  "souper-internal-cache",
  llvm::cl::desc("Cache solver results in memory (default=true)"),
  llvm::cl::init(true));

static llvm::cl::opt<bool> ExternalCache(
  "souper-external-cache",
  llvm::cl::desc("Use external Redis-based cache (default=false)"),
  llvm::cl::init(false));

static llvm::cl::opt<int> SolverTimeout(
  "solver-timeout",
  llvm::cl::desc("Solver timeout in seconds (default=15)"),
  llvm::cl::init(15));

static bool exists_and_executable(const char *fn) {
    throw std::invalid_argument("This will not run on windows.");
    return false;
 // return access(fn, X_OK) != -1;
}

static std::unique_ptr<SMTLIBSolver> GetUnderlyingSolver() {
  std::string Z3PathStr(Z3Path);
  if (!exists_and_executable(Z3Path))
    llvm::report_fatal_error(((std::string)"Solver '" + Z3PathStr + "' does not exist or is not executable").c_str());
  return createZ3Solver(makeExternalSolverProgram(Z3PathStr),
                        KeepSolverInputs);
}

static std::unique_ptr<Solver> GetSolver(KVStore *&KV) {
  std::unique_ptr<SMTLIBSolver> US = GetUnderlyingSolver();
  if (!US)
    return NULL;
  std::unique_ptr<Solver> S = createBaseSolver (std::move(US), SolverTimeout);
  if (ExternalCache) {
    KV = new KVStore;
    S = createExternalCachingSolver (std::move(S), KV);
  }
  if (MemCache) {
    S = createMemCachingSolver (std::move(S));
  }
  return S;
}

}

#endif  // SOUPER_TOOL_GETSOLVER_H
