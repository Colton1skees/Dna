# Dna.LLVMInterop native dependency superbuild

This folder is for CMake superbuild scripts that build/install the external
native dependencies consumed directly by Dna.LLVMInterop.dll, such as LLVM,
Remill, Z3, XED, gflags, glog, and sleigh libraries.

The intended generated prefix is:

```
Dna.LLVMInterop/dependencies/install
```

Dna.LLVMInterop.vcxproj expects headers and libraries at:

```
$(ProjectDir)dependencies\install\include
$(ProjectDir)dependencies\install\lib
```

Generated build/install/download folders are gitignored. Commit only the
superbuild scripts, patches, lock/version notes, and documentation needed to
reproduce the dependency prefix.

This setup is based on [LLVMParty/packages](https://github.com/LLVMParty/packages).

## Building

From a Visual Studio x64 command prompt:

```bash
cmake -B build -G Ninja -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl
cmake --build build
```

Or if you have a precompiled LLVM, you need to copy it into the `install` folder to get this layout:

```
install/bin/clang.exe
install/include/llvm/IR/Value.h
install/lib/LLVMPasses.lib
install/lib/cmake/llvm/LLVMConfig.cmake
```

Then configure:

```bash
cmake -B build -G Ninja -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl -DUSE_PRECOMPILED_LLVM=ON
cmake --build build
```

## Configuration

You need to enable long paths:

```ini
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
"LongPathsEnabled"=dword:00000001
```

And then enable it in git as well:

```
git config --global core.longpaths true
```