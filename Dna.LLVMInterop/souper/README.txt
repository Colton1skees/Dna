Dna.LLVMInterop Souper vendor notes
====================================

This directory is a limited, checked-in vendor copy of Souper source used by
Dna.LLVMInterop. It is intentionally not a git submodule and it is not a full
Souper checkout.

Likely upstream base
--------------------

The closest local upstream match found during dependency cleanup was:

  repository: https://github.com/LLVMParty/souper
  commit:     8648d11 ("Bump to llvm 17 (#875)")

Earlier local notes also referenced LLVMParty/souper commit 900fb8c
("cmake-package-v5"), but the current vendored files match the LLVM 17-era
8648d11 tree more closely.

Patch strategy
--------------

Keep the upstream Souper directory layout for the subset we vendor:

  include/souper/...
  include/klee/Config/config.h
  lib/...

Dna-specific changes are kept directly in this tree. Do not overwrite this
folder with a clean upstream checkout unless the Dna patches are reapplied.
Known Dna/custom areas include:

  * include/souper/Infer/Z3Driver.h and lib/Infer/Z3Driver.cpp
  * include/souper/Infer/Z3Expr.h and lib/Infer/Z3Expr.cpp
  * include/souper/Infer/Verification.h and lib/Infer/Verification.cpp
  * custom Z3 verification/query solving used by Dna's Souper interop APIs
  * local ExprBuilder/Candidates changes used for slicing and candidate filtering
  * Windows/LLVM 17 build adaptations

When refreshing Souper, diff this directory against the chosen upstream base,
rebase the Dna-specific changes, and keep the resulting limited source set in
this folder.
