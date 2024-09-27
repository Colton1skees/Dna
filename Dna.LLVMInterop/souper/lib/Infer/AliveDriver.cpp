#include "souper/Extractor/ExprBuilder.h"
#include "souper/Infer/AliveDriver.h"
#include "souper/Inst/Inst.h"

#include "llvm/ADT/SmallString.h"
#include "llvm/Support/raw_ostream.h"
#include "llvm/Support/CommandLine.h"

#include <iostream>
#include <memory>
#include <set>
#include <sstream>
#include <string_view>
#include <unordered_set>
#include <z3.h>

extern unsigned DebugLevel;
static const int MaxTries = 30;

bool startsWith(const std::string &pre, const std::string &str) {
  return std::equal(pre.begin(), pre.end(), str.begin());
}

namespace {

	static llvm::cl::opt<bool> DisableUndefInput("alive-disable-undef-input",
		llvm::cl::desc("Assume inputs can not be undef (default = false)"),
		llvm::cl::init(false));

	static llvm::cl::opt<bool> SkipAliveSolver("alive-skip-solver",
		llvm::cl::desc("Omit Alive solver calls for performance testing (default = false)"),
		llvm::cl::init(false));

}
