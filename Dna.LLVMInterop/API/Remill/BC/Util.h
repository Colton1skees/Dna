#pragma once

#include <filesystem>
#include <llvm/IR/LLVMContext.h>
#include "remill/Arch/Arch.h"
#include "remill/BC/ABI.h"
#include "remill/BC/Version.h"
#include "remill/BC/InstructionLifter.h"

#include <API/ImmutableManagedVector.h>
#include <API/ImmutableManagedPair.h>
#include "API/ExportDef.h"
#include <remill/BC/Util.h>
#include <iostream>

// remill::OperandLifter
namespace Dna::API {
    // Initialize the attributes for a lifted function.
    DNA_EXPORT void InitFunctionAttributes(llvm::Function* F)
    {
        remill::InitFunctionAttributes(F);
    }

    // Create a call from one lifted function to another.
    DNA_EXPORT llvm::CallInst* AddCall(llvm::BasicBlock* source_block, llvm::Value* dest_func,
        const remill::IntrinsicTable* intrinsics)
    {
        return remill::AddCall(source_block, dest_func, *intrinsics);
    }

    // Create a tail-call from one lifted function to another.
    DNA_EXPORT llvm::CallInst* AddTerminatingTailCallOnFunc(llvm::Function* source_func,
        llvm::Value* dest_func,
        const remill::IntrinsicTable* intrinsics)
    {
        auto ret = remill::AddTerminatingTailCall(source_func, dest_func, *intrinsics);
        ret->setTailCallKind(llvm::CallInst::TCK_MustTail);
        return ret;
    }

    DNA_EXPORT llvm::CallInst* AddTerminatingTailCallOnBlock(llvm::BasicBlock* source_block,
        llvm::Value* dest_func,
        const remill::IntrinsicTable* intrinsics)
    {
        auto ret = remill::AddTerminatingTailCall(source_block, dest_func, *intrinsics);
        ret->setTailCallKind(llvm::CallInst::TCK_MustTail);
        return ret;
    }

    // Find a local variable defined in the entry block of the function. We use
    // this to find register variables.
    DNA_EXPORT llvm::Value*
        FindVarInFunctionFromBlock(llvm::BasicBlock* block, char* name,
            bool allow_failure = false)
    {
        return remill::FindVarInFunction(block, name, allow_failure).first;
    }

    // Find a local variable defined in the entry block of the function. We use
    // this to find register variables.
    DNA_EXPORT llvm::Value*
        FindVarInFunctionFromFunc(llvm::Function* func, char* name,
            bool allow_failure = false)
    {
        remill::FindVarInFunction(func, name, allow_failure).first;
    }

    // Find the machine state pointer. The machine state pointer is, by convention,
    // passed as the first argument to every lifted function.
    DNA_EXPORT llvm::Value* LoadStatePointerFromFunc(llvm::Function* function)
    {
        return remill::LoadStatePointer(function);
    }
    DNA_EXPORT llvm::Value* LoadStatePointerFromBlock(llvm::BasicBlock* block)
    {
        return remill::LoadStatePointer(block);
    }

    DNA_EXPORT llvm::Value* LoadProgramCounter(llvm::BasicBlock* block,
        const remill::IntrinsicTable* intrinsics)
    {
        return remill::LoadProgramCounter(block, *intrinsics);
    }

    // Return the next program counter.
    DNA_EXPORT llvm::Value* LoadNextProgramCounter(llvm::BasicBlock* block,
        const remill::IntrinsicTable* intrinsics)
    {
        return remill::LoadNextProgramCounter(block, *intrinsics);
    }

    // Return a reference to the current program counter.
    DNA_EXPORT llvm::Value* LoadProgramCounterRef(llvm::BasicBlock* block)
    {
        return remill::LoadProgramCounterRef(block);
    }

    // Return a reference to the next program counter.
    DNA_EXPORT llvm::Value* LoadNextProgramCounterRef(llvm::BasicBlock* block)
    {
        return remill::LoadNextProgramCounterRef(block);
    }

    // Return a reference to the return program counter.
    DNA_EXPORT llvm::Value* LoadReturnProgramCounterRef(llvm::BasicBlock* block)
    {
        return remill::LoadReturnProgramCounterRef(block);
    }

    // Update the program counter in the state struct with a hard-coded value.
    DNA_EXPORT void StoreProgramCounterWithIntrinsics(llvm::BasicBlock* block, uint64_t pc,
        const remill::IntrinsicTable* intrinsics)
    {
        remill::StoreProgramCounter(block, pc, *intrinsics);
    }

    // Update the program counter in the state struct with a new value.
    DNA_EXPORT void StoreProgramCounter(llvm::BasicBlock* block, llvm::Value* pc)
    {
        remill::StoreProgramCounter(block, pc);
    }

    // Update the next program counter in the state struct with a new value.
    DNA_EXPORT void StoreNextProgramCounter(llvm::BasicBlock* block, llvm::Value* pc)
    {
        remill::StoreNextProgramCounter(block, pc);
    }

    // Return the memory pointer argument.
    DNA_EXPORT llvm::Value* LoadMemoryPointerArg(llvm::Function* func)
    {
        return remill::LoadMemoryPointerArg(func);
    }

    // Return the program counter argument.
    DNA_EXPORT llvm::Value* LoadProgramCounterArg(llvm::Function* function)
    {
        return remill::LoadProgramCounterArg(function);
    }


    DNA_EXPORT llvm::Value* LoadMemoryPointer(llvm::BasicBlock* block,
        const remill::IntrinsicTable* intrinsics)
    {
        return remill::LoadMemoryPointer(block, *intrinsics);
    }

    // Return a reference to the memory pointer.
    DNA_EXPORT llvm::Value* LoadMemoryPointerRef(llvm::BasicBlock* block)
    {
        return remill::LoadMemoryPointerRef(block);
    }

    // Return an `llvm::Value *` that is an `i1` (bool type) representing whether
    // or not a conditional branch is taken.
    DNA_EXPORT llvm::Value* LoadBranchTaken(llvm::BasicBlock* block)
    {
        return remill::LoadBranchTaken(block);
    }

    DNA_EXPORT llvm::Value* LoadBranchTakenRef(llvm::BasicBlock* block)
    {
        return remill::LoadBranchTakenRef(block);
    }

    // Find a function with name `name` in the module `M`.
    DNA_EXPORT llvm::Function* FindFunction(llvm::Module* M, char* name)
    {
        return remill::FindFunction(M, name);
    }

    // Find a global variable with name `name` in the module `M`.
    DNA_EXPORT llvm::GlobalVariable* FindGlobalVariable(llvm::Module* M, char* name)
    {
        return remill::FindGlobaVariable(M, name);
    }

    // Try to verify a module.
    DNA_EXPORT bool VerifyModule(llvm::Module* module)
    {
        return remill::VerifyModule(module);
    }

    DNA_EXPORT llvm::Module*
        LoadModuleFromFile(llvm::LLVMContext* context, char* fileName)
    {
        auto ptr = remill::LoadModuleFromFile(context, fileName);

        // Relinquish ownership to the managed caller.
        auto get = ptr.get();

        return ptr.release();
    }

    // `sem_dirs` is forwarded to `FindSemanticsBitcodeFile`.
    DNA_EXPORT llvm::Module* __cdecl
        LoadArchSemantics(const remill::Arch* arch,
            char* path)
    {

        auto foo = llvm::DataLayout("e-m:e-p:32:32-f64:32:64-f80:32-n8:16:32-S128");

        auto strRep = foo.getStringRepresentation();

        std::cout << strRep.c_str() << std::endl;

        auto os = arch->os_name;
        

        auto archName = arch->arch_name;

        auto isX86 = arch->IsX86();

        auto win = arch->IsWindows();

        auto name = std::string(arch->ProgramCounterRegisterName());

        auto dl = arch->DataLayout();

        auto str = dl.getStringRepresentation();

        printf(str.c_str());

        auto paths = std::vector<std::filesystem::path>();
        paths.push_back(path);
        auto smartPtr = remill::LoadArchSemantics(arch, paths);

        auto ptr = smartPtr.get();

        // Relinquish ownership to the managed caller.
        smartPtr.release();

        return ptr;
    }

    // Store an LLVM module into a file.
    DNA_EXPORT bool StoreModuleToFile(llvm::Module* module, char* file_name,
        bool allow_failure = false)
    {
        return remill::StoreModuleToFile(module, file_name);
    }

    // Store a module, serialized to LLVM IR, into a file.
    DNA_EXPORT bool StoreModuleIRToFile(llvm::Module* module, char* file_name,
        bool allow_failure = false)
    {
        return remill::StoreModuleIRToFile(module, file_name, allow_failure);
    }

    // Return a pointer to the Nth argument (N=0 is the first argument).
    DNA_EXPORT llvm::Argument* NthArgument(llvm::Function* func, size_t index)
    {
        return remill::NthArgument(func, index);
    }

    // Return a vector of arguments to pass to a lifted function, where the
    // arguments are derived from `block`.
    DNA_EXPORT ImmutableManagedVector*
        LiftedFunctionArgs(llvm::BasicBlock* block, const remill::IntrinsicTable* intrinsics)
    {
        auto args = remill::LiftedFunctionArgs(block, *intrinsics);
        
        auto vec = std::vector<llvm::Value*>(args.begin(), args.end());

        return ImmutableManagedVector::From(&vec);
    }

    // Serialize an LLVM object into a string.
    DNA_EXPORT char* LLVMValueToString(llvm::Value* thing)
    {
        return _strdup(remill::LLVMThingToString(thing).c_str());
    }

    DNA_EXPORT char* LLVMTypeToString(llvm::Type* thing)
    {
        return _strdup(remill::LLVMThingToString(thing).c_str());
    }

    DNA_EXPORT char* LLVMModuleToString(llvm::Module* mod)
    {
        std::string str;
        raw_string_ostream os(str);
        os << *mod;
        os.flush();
        return _strdup(str.c_str());
    }

    // Clone function `source_func` into `dest_func`. This will strip out debug
    // info during the clone.
    //
    // Note: this will try to clone globals referenced from the module of
    //       `source_func` into the module of `dest_func`.
    DNA_EXPORT void CloneFunctionInto(llvm::Function* source_func, llvm::Function* dest_func)
    {
        return remill::CloneFunctionInto(source_func, dest_func);
    }

    // Returns a list of callers of a specific function.
    DNA_EXPORT ImmutableManagedVector* CallersOf(llvm::Function* func)
    {
        std::vector<llvm::CallInst*> callers = remill::CallersOf(func);

        return ImmutableManagedVector::From(&callers);
    }

    // Returns the name of a module.
    DNA_EXPORT std::string ModuleName(llvm::Module* module)
    {
        return _strdup(remill::ModuleName(module).c_str());
    }

    // Replace all uses of a constant `old_c` with `new_c` inside of `module`.
    //
    // Returns the number of constant uses of `old_c`.
    DNA_EXPORT unsigned ReplaceAllUsesOfConstant(llvm::Constant* old_c, llvm::Constant* new_c,
        llvm::Module* module)
    {
        return remill::ReplaceAllUsesOfConstant(old_c, new_c, module);
    }

    // Move a function from one module into another module.
    DNA_EXPORT void MoveFunctionIntoModule(llvm::Function* func, llvm::Module* dest_module)
    {
        return remill::MoveFunctionIntoModule(func, dest_module);
    }

    // Get an instance of `type` that belongs to `context`.
    DNA_EXPORT llvm::Type* RecontextualizeType(llvm::Type* type, llvm::LLVMContext* context)
    {
        return remill::RecontextualizeType(type, *context);
    }

    // Produce a sequence of instructions that will load values from
    // memory, building up the correct type. This will invoke the various
    // memory read intrinsics in order to match the right type, or
    // recursively build up the right type.
    //
    // Returns the loaded value.
    DNA_EXPORT llvm::Value* LoadFromMemory(const remill::IntrinsicTable* intrinsics,
        llvm::BasicBlock* block, llvm::Type* type,
        llvm::Value* mem_ptr, llvm::Value* addr)
    {
        return remill::LoadFromMemory(*intrinsics, block, type, mem_ptr, addr);
    }

    // Produce a sequence of instructions that will store a value to
    // memory. This will invoke the various memory write intrinsics
    // in order to match the right type, or recursively destructure
    // the type into components which can be written to memory.
    //
    // Returns the new value of the memory pointer.
    DNA_EXPORT llvm::Value* StoreToMemory(const remill::IntrinsicTable* intrinsics,
        llvm::BasicBlock* block, llvm::Value* val_to_store,
        llvm::Value* mem_ptr, llvm::Value* addr)
    {
        return remill::StoreToMemory(*intrinsics, block, val_to_store, mem_ptr, addr);
    }

    // Create an array of index values to pass to a GetElementPtr instruction
    // that will let us locate a particular register. Returns the final offset
    // into `type` which was reached as the first value in the pair, and the type
    // of what is at that offset in the second value of the pair.
    DNA_EXPORT ImmutableManagedPair*
        BuildIndexes(const llvm::DataLayout* dl, llvm::Type* type, size_t offset,
            const size_t goal_offset,
            llvm::SmallVectorImpl<llvm::Value*>* indexes_out)
    {
        std::pair<uint64_t, llvm::Type*> indexes = remill::BuildIndexes(*dl, type, offset, goal_offset, *indexes_out);

        // Store the uint64_t to a pointer. Normally this would be insane,
        // but it's easier to use a pointer here than to try to handle mixing pointers 
        // and value types within C#'s type system.
        auto pOffset = new uint64_t();
        *pOffset = indexes.first;
        // I'm not a fan of the std::pair wrapper here. It's easy to mess up
        // when you mix pointers and value types.
        // TODO: Find a better abstraction.
        return new ImmutableManagedPair(pOffset, indexes.second);
    }

    // Given a pointer, `ptr`, and a goal byte offset to which we'd like to index,
    // build either a constant expression or sequence of instructions that can
    // index to that offset. `ir` is provided to support the instruction case
    // and to give access to a module for data layouts.
    DNA_EXPORT llvm::Value* BuildPointerToOffset(llvm::IRBuilder<>* ir, llvm::Value* ptr,
        size_t dest_elem_offset,
        llvm::Type* dest_ptr_type)
    {
        return remill::BuildPointerToOffset(*ir, ptr, dest_elem_offset, dest_ptr_type);
    }

    // Compute the total offset of a GEP chain.
    DNA_EXPORT ImmutableManagedPair*
        StripAndAccumulateConstantOffsets(llvm::Instruction* base)
    {
        std::pair<llvm::Value*, int64_t> offsets = remill::StripAndAccumulateConstantOffsets(base->getModule()->getDataLayout(), base);

        // Store the uint64_t to a pointer. Normally this would be insane,
        // but it's easier to use a pointer here than to try to handle mixing pointers 
        // and value types within C#'s type system.
        auto pOffset = new int64_t();
        *pOffset = offsets.second;

        return new ImmutableManagedPair(offsets.first, pOffset);
    }
}
