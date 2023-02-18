# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** for x86/x64 via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution
- Mixed boolean-arithmetic(MBA) simplification
- SMT solver integration
- Control flow graph recovery
- Lifting to LLVM IR
- Decompilation(via [Rellic](https://github.com/lifting-bits/rellic))
- Emulation 
- Parsing of executable file formats
- Visualization of control flow graphs
- IR **optimization** passes
- Function relocation(optionally across binaries, with some caveats)

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

# Roadmap
 * [X] SMT solver interface
 * [X] **Symbolic** execution engine
 * [X] Mixed boolean-arithmetic(MBA) simplifier
 * [X] Decompilation to pseudo C
 * [ ] Jump table recovery
 * [ ] Compiler back to x86/x64
