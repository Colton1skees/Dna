# https://alexreinking.com/blog/how-to-use-cmake-without-the-agonizing-pain-part-1.html
cmake_minimum_required(VERSION 3.21)

# Default to a Release config. Required before project() because Windows defaults to Debug
set(CMAKE_BUILD_TYPE "RelWithDebInfo" CACHE STRING "")
if(CMAKE_BUILD_TYPE STREQUAL "")
    set(CMAKE_BUILD_TYPE "RelWithDebInfo" CACHE STRING "" FORCE)
endif()

project(dependencies)

option(USE_PRECOMPILED_LLVM "Do not compile LLVM as part of the superbuild, use a precompiled one instead" OFF)
option(USE_SANITIZERS "Use ASan and UBSan" OFF)

if(USE_PRECOMPILED_LLVM)
    # NOTE: We only search in ./install because that's where VS expects them
    find_package(LLVM CONFIG REQUIRED NO_DEFAULT_PATH
        PATHS ${PROJECT_SOURCE_DIR}/install
    )
    message(STATUS "LLVM ${LLVM_PACKAGE_VERSION}: ${LLVM_DIR}")
endif()

if(USE_SANITIZERS)
    list(APPEND CMAKE_C_FLAGS "-fsanitize=address,undefined")
    list(APPEND CMAKE_CXX_FLAGS "-fsanitize=address,undefined")
endif()

include(superbuild.cmake)

simple_git(https://github.com/Z3Prover/z3 z3-4.16.0
    "-DZ3_BUILD_LIBZ3_SHARED:STRING=OFF"
)

simple_git(https://github.com/gflags/gflags 52e94563eba1968783864942fedf6e87e3c611f4
)
simple_git(https://github.com/google/glog v0.7.1
    "-DGFLAGS_USE_TARGET_NAMESPACE:STRING=ON"
    "-DBUILD_TESTING:STRING=OFF"
)
simple_git(https://github.com/google/googletest v1.17.0
    "-Dgtest_force_shared_crt:STRING=ON"
    "-DGFLAGS_USE_TARGET_NAMESPACE:STRING=ON"
)

include(xed.cmake)

if(NOT USE_PRECOMPILED_LLVM)
    include(llvm.cmake)
endif()

simple_submodule(remill
    "-DREMILL_ENABLE_TESTING:STRING=OFF"
)