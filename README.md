# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution
- Expression Simplification
- Parsing of executable file formats
- Control flow graph reconstruction
- Translation of routines to an intermediate representation
- Translation of routines to **LLVM** IR
- IR **optimization** passes
- Function relocation(optionally across binaries, with some caveats)
- .NET bindings for the **hex-rays microcode** API

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

# Roadmap
 * [X] SMT solver interface
 * [X] **Symbolic** execution engine
 * [X] Expression simplification engine
 * [ ] **SSA** form construction
 * [ ] Wide set of compiler optimization passes
 * [ ] Native IR -> x86 compiler
