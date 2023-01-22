# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution
- Expression Simplification
- Parsing of executable file formats
- Control flow graph reconstruction
- Control flow graph visualization
- Translation of routines to an intermediate representation
- Translation of routines to **LLVM** IR
- IR **optimization** passes
- Function relocation(optionally across binaries, with some caveats)

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

# Roadmap
 * [X] SMT solver interface
 * [X] **Symbolic** execution engine
 * [ ] Expression simplification engine
 * [ ] **SSA** form construction
 * [ ] IR to x86 compiler
