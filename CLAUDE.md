## Building

To build the project:

```bash
msbuild.exe Dna.sln > build.log
```

Then check the tail in case of failure.

To build after modifying `Dna.LLVMInterop/dependencies/remill`:

```bash
cmake --build Dna.LLVMInterop/dependencies/build > build.log
```

Then check the tail in case of failure.