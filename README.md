# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution engine
- Integration with a PE parser, aswell as abstractions to support other executable format parsers.
- Control flow graph reconstruction
- x86 cfg -> IR cfg translation
- IR cfg -> **LLVM** translation
- IR **optimization** passes
- Function relocation(optionally across binaries, with some caveats)
- .NET bindings for the **hex-rays microcode** API

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

# Roadmap
 * [X] SMT solver interface
 * [X] **Symbolic** execution engine
 * [ ] Expression simplification engine
 * [ ] **SSA** form construction
 * [ ] Wide set of compiler optimization passes
 * [ ] Native IR -> x86 compiler
