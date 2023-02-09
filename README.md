# Dna

**Dna** is a static analysis framework for **x86/x64**, mainly geared towards deobfuscation. It offers:
- **Instruction semantics** via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Symbolic execution
- Mixed boolean-arithmetic(MBA) simplification
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
 * [X] Mixed boolean-arithmetic(MBA) simplifier
 * [ ] Go-to free control flow structuring
 * [ ] **SSA** form construction 
 * [ ] IR to x86 compiler
