# Dna

`Dna` is a static binary analysis framework built on top of LLVM. Notably it's written almost entirely in C#, including managed bindings for LLVM, Remill, and Souper.

# Functionality

`Dna` implements an iterative control flow graph reconstruction inspired heavily by the [SATURN](https://arxiv.org/pdf/1909.01752) paper. It iteratively applies recursive descent, lifting (using remill), and path solving until the complete control flow graph is recovered. In the case of jump tables, we use a recursive algorithm based on `Souper` and z3 to solve the set of possible jump table targets. You can find the iterative exploration algorithm [here](https://github.com/Colton1skees/Dna/blob/e70b48b1da4c9b3666cc2a218138c050ab6f9d8b/Dna.BinaryTranslator/Unsafe/IterativeFunctionTranslator.cs#L48), and the jump table solving algorithm [here](https://github.com/Colton1skees/Dna/blob/master/Dna.BinaryTranslator/JmpTables/Precise/SouperJumpTableSolver.cs#L41).

Once a control flow graph has been fully explored, it can then be recompiled to x86 and reinserted into the binary using the algorithms from [here](https://github.com/Colton1skees/Dna/blob/master/Dna.BinaryTranslator/Safe/SafeFunctionTranslator.cs#L46) and [here](https://github.com/Colton1skees/Dna/blob/master/Dna.BinaryTranslator/Safe/FunctionGroupCompiler.cs#L27). Though the compiled code is not pretty by *any* means, it should run so long as the recovered control flow graph is correct. That being said, it is still a research prototype - bugs and edge cases are expected. Control flow graph exploration may fail in the case of e.g. unbounded jump tables or unliftable instructions.

Some other notable features:
- Supports *most* jump tables, including MSVC's nested or so-called compressed jump tables.
- Supports lifting code with SEH to LLVM IR. When SEH is present, `try`/`catch` statements and `filter` intrinsics are inserted into the control flow graph. Though the recompiler does not (yet) support SEH (the SEH entries are not fixed up), so exceptions will cause crashes.
- Includes a strong API for writing LLVM passes natively in C#. We have bindings for e.g. `MemorySSA`, `LoopInfo`, dominator trees, pass pipeline management, etc. 
- Graph visualization for LLVM IR and binary control flow graphs using graphviz or alternatively a script generator for binary ninja.

Some caveats:
- Only x86_64 is supported
- Recompiled code is not CET compliant 

# Dependencies
- LLVM/LLVMSharp
- Remill
- Souper
- AsmResolver
- Rivers

Note that `Dna` is currently based on LLVM 17.

# VMProtect

`Dna` contains a VMProtect devirtualization plugin located in `Dna.BinaryTranslator/VMProtect`. See [this PR](https://github.com/Colton1skees/Dna/pull/8) for more info.

# Building

Dna currently targets LLVM 17 and is expected to be built on Windows x64 with Visual Studio 2022.
Build `Dna.LLVMInterop` in **Release** mode; the native dependency tree is Release-built and Debug interop builds are not supported.

## Prerequisites

- Visual Studio 2022 with C++/MSBuild tools
- CMake
- Ninja
- clang-cl / LLVM tools available from the VS toolchain
- Rust/Cargo, for the EqSat simplifier DLL
- .NET SDK 8+

Run the commands below from a VS x64 developer shell, or another shell with the VS C++ tools on `PATH`.

## 1. Build native dependencies

The dependency superbuild installs LLVM 17, Remill, Z3, XED, gflags/glog, and related native libraries into `Dna.LLVMInterop/dependencies/install`.

```powershell
cmake -S Dna.LLVMInterop/dependencies `
      -B Dna.LLVMInterop/dependencies/build `
      -G Ninja `
      -DCMAKE_BUILD_TYPE=Release `
      -DCMAKE_C_COMPILER=clang-cl `
      -DCMAKE_CXX_COMPILER=clang-cl

cmake --build Dna.LLVMInterop/dependencies/build
```

If changing compiler, build type, or CRT settings, delete both `Dna.LLVMInterop/dependencies/build` and `Dna.LLVMInterop/dependencies/install` before reconfiguring.

## 2. Build the Rust simplifier DLL

`Dna.Example` and the simplifier projects copy `eq_sat.dll` from the Cargo release output.

```powershell
cargo build --manifest-path Simplifier/EqSat/Cargo.toml --release
```

## 3. Build the solution

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  Dna.sln `
  /restore `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /m
```
