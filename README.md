# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** for x86/x64 via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution
- Mixed boolean-arithmetic(MBA) simplification
- SMT solver integration
- Control flow graph recovery
- Lifting to LLVM IR
- Emulation 
- Parsing of executable file formats
- Visualization of control flow graphs(Graphviz)
- IR optimization passes 
- LLVM passes for simplifying obfuscated code
- APIs for writing LLVM passes in C# (e.g. bindings for PassManager, MemorySSA, LoopInfo)
- Function relocation(optionally across binaries, with some caveats)

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

 # Setup
 
The .NET component of Dna is supported on Windows, Linux, and Mac OSX. The C++ component(LLVM.Interop) has been used exclusively on windows. 

To get the C++ component building, extract [this precompiled version of llvm](https://github.com/LLVMParty/REVIDE/releases/download/libraries/llvm-15.0.3-win64.7z) to the root directory of Dna. 


# Status
Dna is now archived. It may be unarchived later on.