# Dna

`Dna` is a static binary analysis framework built on top of LLVM. Notably it's written almost entirely in C#, including managed bindings for LLVM, Remill, and Souper.

# Functionality

`Dna` implements an iterative control flow graph reconstruction inspired heavily by the [SATURN](https://arxiv.org/pdf/1909.01752) paper. It iteratively applies recursive descent, lifting (using remill), and path solving until the complete control flow graph is recovered. In the case of jump tables, we use a recursive algorithm based on `Souper` and z3 to solve the set of possible jump table targets. You can find the iterative exploration algorithm [here](https://github.com/Colton1skees/Dna/blob/4a833fa197f777f985dde1b7bb8b27fd0801a991/Dna.BinaryTranslator/Unsafe/IterativeFunctionTranslator.cs#L48), and the jump table solving algorithm [here](https://github.com/Colton1skees/Dna/blob/master/Dna.BinaryTranslator/JmpTables/Precise/SouperJumpTableSolver.cs#L41).

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

# Building
`Dna` will not build out of the box. Custom patches to remill and souper were needed for this to build on windows. If you would like to work on Dna, open an issue or email me `colton1skees@gmail.com`. At some point I may publish proper build steps, but I make no guarantees.