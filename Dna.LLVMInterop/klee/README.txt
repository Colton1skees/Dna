Dna.LLVMInterop KLEE vendor notes
=================================

This directory is a limited, checked-in vendor copy of KLEE source used by the
vendored Souper KLEE expression/SMTLIB backend. It is intentionally not a git
submodule and it is not a full KLEE checkout.

Likely upstream base
--------------------

The local source used for this vendor refresh was:

  repository: https://github.com/Colton1skees/klee
  branch:     klee-for-souper-13
  commit:     63a8acc1bf8d113aed72560708a505ae569b91fe
              ("make klee compile on windows")

Vendored subset
---------------

Keep the upstream KLEE directory layout for the subset we vendor:

  include/klee/...
  lib/Expr/...

Dna currently vendors the KLEE headers and Expr implementation files needed by
Souper's KLEEBuilder/SMTLIB printer. Dna.LLVMInterop compiles these sources
directly into Dna.LLVMInterop.dll rather than linking a separate KLEE library.

The upstream KLEE tree generates include/klee/Config/config.h from
config.h.in. Because this is a partial checked-in vendor copy, Dna also checks
in a generated config.h configured for the LLVM 17 build used by this project.

When refreshing KLEE, copy only the needed files while preserving their upstream
relative paths, then reapply any Dna build/configuration adjustments.
