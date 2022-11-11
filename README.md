# Dna

Dna is a static analysis framework for x86/x64, mainly geared towards deobfuscation. It offers:
- Instruction semantics(both AST & 3 address code representations) via [TritonTranslator](https://github.com/Colton1skees/TritonTranslator)
- Integration with a PE parser, aswell as abstractions to support other file format parsers.
- Control flow graph reconstruction
- x86 cfg -> IR cfg translation
- IR cfg -> LLVM translation
- IR cfg compiler optimization passes
- Function relocation(optionally across binaries, with some caveats)
- .NET bindings for the hex-rays microcode API

You can find an example usage [here](https://github.com/Colton1skees/Dna/blob/master/Dna.Example/Program.cs).

# Roadmap
 * [] Decompilation via translating IR cfgs to IR Hex-Rays microcode
 * [] SMT solver interface
 * [] Symbolic execution engine
 * [] Expression simplification engine
 * [] SSA form construction
 * [] Wide set of compiler optimization passes
 * [] Native IR -> x86 compiler
