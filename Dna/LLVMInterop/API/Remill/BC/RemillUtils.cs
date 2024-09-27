using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public static class RemillUtils
    {
        /// <summary>
        /// Initializes the attributes for a lifted function.
        /// </summary>
        public unsafe static void InitFunctionAttributes(LLVMValueRef func) => NativeRemillUtilApi.InitFunctionAttributes(func);

        /// <summary>
        /// Create a call from one lifted function to another.
        /// </summary>
        public unsafe static LLVMValueRef AddCall(LLVMBasicBlockRef sourceBlock, LLVMValueRef destFunc, RemillIntrinsicTable intrinsics) => NativeRemillUtilApi.AddCall(sourceBlock, destFunc, intrinsics);

        /// <summary>
        /// Create a tail-call from one lifted function to another.
        /// </summary>
        public unsafe static LLVMValueRef AddTerminatingTailCall(LLVMValueRef sourceFunc, LLVMValueRef destFunc, RemillIntrinsicTable intrinsics)
        {
            return NativeRemillUtilApi.AddTerminatingTailCallOnFunc(sourceFunc, destFunc, intrinsics);
        }

        /// <summary>
        /// Create a tail-call from one lifted function to another.
        /// </summary>
        public unsafe static LLVMValueRef AddTerminatingTailCall(LLVMBasicBlockRef sourceBlock, LLVMValueRef destFunc, RemillIntrinsicTable intrinsics)
        {
            return NativeRemillUtilApi.AddTerminatingTailCallOnBlock(sourceBlock, destFunc, intrinsics);
        }

        /// <summary>
        /// Finds a local variable defined in the entry block of the function. We use this to find register variables.
        /// </summary>
        public unsafe static LLVMValueRef FindVarInFunction(LLVMBasicBlockRef block, string name, bool allowFailure)
        {
            return NativeRemillUtilApi.FindVarInFunctionFromBlock(block, new MarshaledString(name), allowFailure);
        }

        /// <summary>
        /// Finds a local variable defined in the entry block of the function. We use this to find register variables.
        /// </summary>
        public unsafe static LLVMValueRef FindVarInFunction(LLVMValueRef func, string name, bool allowFailure)
        {
            return NativeRemillUtilApi.FindVarInFunctionFromFunc(func, new MarshaledString(name), allowFailure);
        }

        /// <summary>
        /// Find the machine state pointer. The machine state pointer is, by convention,
        /// passed as the first argument to every lifted function.
        /// </summary>
        public unsafe static LLVMValueRef LoadStatePointer(LLVMValueRef func)
        {
            return NativeRemillUtilApi.LoadStatePointerFromFunc(func);
        }

        /// <summary>
        /// Find the machine state pointer. The machine state pointer is, by convention,
        /// passed as the first argument to every lifted function.
        /// </summary>
        public unsafe static LLVMValueRef LoadStatePointer(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadStatePointerFromBlock(block);
        }

        /// <summary>
        /// Returns the program counter.
        /// </summary>
        public unsafe static LLVMValueRef LoadProgramCounter(LLVMBasicBlockRef block, RemillIntrinsicTable intrinsics)
        {
            return NativeRemillUtilApi.LoadProgramCounter(block, intrinsics);
        }

        /// <summary>
        /// Returns the next program counter.
        /// </summary>
        public unsafe static LLVMValueRef LoadNextProgramCounter(LLVMBasicBlockRef block, RemillIntrinsicTable intrinsics)
        {
            return NativeRemillUtilApi.LoadNextProgramCounter(block, intrinsics);
        }

        /// <summary>
        /// Returns a reference to the current program counter.
        /// </summary>
        public unsafe static LLVMValueRef LoadProgramCounterRef(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadProgramCounterRef(block);
        }

        /// <summary>
        /// Returns a reference to the next program counter.
        /// </summary>
        public unsafe static LLVMValueRef LoadNextProgramCounterRef(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadNextProgramCounterRef(block);
        }

        /// <summary>
        /// Return a reference to the return program counter.
        /// </summary>
        public unsafe static LLVMValueRef LoadReturnProgramCounterRef(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadReturnProgramCounterRef(block);
        }

        /// <summary>
        /// Update the program counter in the state struct with a hard-coded value.
        /// </summary>
        public unsafe static void StoreProgramCounter(LLVMBasicBlockRef block, ulong pc, RemillIntrinsicTable intrinsics)
        {
            NativeRemillUtilApi.StoreProgramCounterWithIntrinsics(block, pc, intrinsics);
        }

        /// <summary>
        /// Update the program counter in the state struct with a new value.
        /// </summary>
        public unsafe static void StoreProgramCounter(LLVMBasicBlockRef block, LLVMValueRef pc)
        {
            NativeRemillUtilApi.StoreProgramCounter(block, pc);
        }

        /// <summary>
        /// Update the next program counter in the state struct with a new value.
        /// </summary>
        public unsafe static void StoreNextProgramCounter(LLVMBasicBlockRef block, LLVMValueRef pc)
        {
            NativeRemillUtilApi.StoreProgramCounter(block, pc);
        }

        /// <summary>
        /// Return the memory pointer argument.
        /// </summary>
        public unsafe static LLVMValueRef LoadMemoryPointerArg(LLVMValueRef func)
        {
            return NativeRemillUtilApi.LoadMemoryPointerArg(func);
        }

        /// <summary>
        /// Return the program counter argument.
        /// </summary>
        public unsafe static LLVMValueRef LoadProgramCounterArg(LLVMValueRef func)
        {
            return NativeRemillUtilApi.LoadProgramCounterArg(func);
        }

        public unsafe static LLVMValueRef LoadMemoryPointer(LLVMBasicBlockRef block, RemillIntrinsicTable intrinsics)
        {
            return NativeRemillUtilApi.LoadMemoryPointer(block, intrinsics);
        }

        /// <summary>
        /// Return a reference to the memory pointer.
        /// </summary>
        public unsafe static LLVMValueRef LoadMemoryPointerRef(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadMemoryPointerRef(block);
        }

        /// <summary>
        /// Return an `llvm::Value *` that is an `i1` (bool type) representing whether
        /// or not a conditional branch is taken.
        /// </summary>
        public unsafe static LLVMValueRef LoadBranchTaken(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadBranchTaken(block);
        }

        public unsafe static LLVMValueRef LoadBranchTakenRef(LLVMBasicBlockRef block)
        {
            return NativeRemillUtilApi.LoadBranchTakenRef(block);
        }

        /// <summary>
        /// Find a function with name `name` in the module `M`.
        /// </summary>
        public unsafe static LLVMValueRef FindFunction(LLVMModuleRef module, string name)
        {
            return NativeRemillUtilApi.FindFunction(module, new MarshaledString(name));
        }

        /// <summary>
        /// Find a global variable with name `name` in the module `M`.
        /// </summary>
        public unsafe static LLVMValueRef FindGlobalVariable(LLVMModuleRef module, string name)
        {
            return NativeRemillUtilApi.FindGlobalVariable(module, new MarshaledString(name));
        }

        /// <summary>
        /// Try to verify a module.
        /// </summary>
        public unsafe static bool VerifyModule(LLVMModuleRef module)
        {
            return NativeRemillUtilApi.VerifyModule(module);
        }

        public unsafe static LLVMModuleRef? LoadModuleFromFile(LLVMContextRef context, string name)
        {
            return NativeRemillUtilApi.LoadModuleFromFile(context, new MarshaledString(name));
        }

        public unsafe static LLVMModuleRef LoadArchSemantics(RemillArch arch, string path)
        {
            return NativeRemillUtilApi.LoadArchSemantics(arch, new MarshaledString(path));
        }

        /// <summary>
        /// Store an LLVM module into a file.
        /// </summary>
        public unsafe static bool StoreModuleToFile(LLVMModuleRef module, string fileName, bool allowFailure)
        {
            return NativeRemillUtilApi.StoreModuleToFile(module, new MarshaledString(fileName), allowFailure);
        }

        /// <summary>
        /// Store a module, serialized to LLVM IR, into a file.
        /// </summary>
        public unsafe static bool StoreModuleIRToFile(LLVMModuleRef module, string fileName, bool allowFailure)
        {
            return NativeRemillUtilApi.StoreModuleIRToFile(module, new MarshaledString(fileName), allowFailure);
        }

        /// <summary>
        /// Return a pointer to the Nth argument (N=0 is the first argument).
        /// </summary>
        public unsafe static LLVMValueRef NthArgument(LLVMValueRef func, ulong index)
        {
            return NativeRemillUtilApi.NthArgument(func, index);
        }

        /// <summary>
        /// Return a vector of arguments to pass to a lifted function, where the
        /// arguments are derived from `block`.
        /// </summary>
        public unsafe static IReadOnlyList<LLVMValueRef> LiftedFunctionArgs(LLVMBasicBlockRef block, RemillIntrinsicTable intrinsics)
        {
            var argsPtr = NativeRemillUtilApi.LiftedFunctionArgs(block, intrinsics);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)argsPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        /// <summary>
        /// Serialize an LLVM object into a string.
        /// </summary>
        public unsafe static string LLVMValueToString(LLVMValueRef value)
        {
            return StringMarshaler.AcquireString(NativeRemillUtilApi.LLVMValueToString(value));
        }

        /// <summary>
        /// Serialize an LLVM type into a string.
        /// </summary>
        public unsafe static string LLVMTypeToString(LLVMTypeRef type)
        {
            return StringMarshaler.AcquireString(NativeRemillUtilApi.LLVMTypeToString(type));
        }

        /// <summary>
        /// Serialize an LLVM type into a string.
        /// </summary>
        public unsafe static string LLVMModuleToString(LLVMModuleRef type)
        {
            return StringMarshaler.AcquireString(NativeRemillUtilApi.LLVMModuleToString(type));
        }

        /// <summary>
        /// Clone function `source_func` into `dest_func`. This will strip out debug
        /// info during the clone.
        /// </summary>
        public unsafe static void CloneFunctionInto(LLVMValueRef sourceFunc, LLVMValueRef destFunc)
        {
            NativeRemillUtilApi.CloneFunctionInto(sourceFunc, destFunc);
        }

        /// <summary>
        /// Returns a list of callers of a specific function.
        /// </summary>
        public unsafe static IReadOnlyList<LLVMValueRef> CallersOf(LLVMValueRef func)
        {
            var callersPtr = NativeRemillUtilApi.CallersOf(func);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)callersPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        /// <summary>
        /// Returns the name of a module.
        /// </summary>
        public unsafe static string ModuleName(LLVMModuleRef module)
        {
            return StringMarshaler.AcquireString(NativeRemillUtilApi.ModuleName(module));
        }

        /// <summary>
        /// Replace all uses of a constant `old_c` with `new_c` inside of `module`.
        /// Returns the number of constant uses of `old_c`.
        /// </summary>
        public unsafe static uint ReplaceAllUsesOfConstant(LLVMValueRef oldConstant, LLVMValueRef newConstant, LLVMModuleRef module)
        {
            return NativeRemillUtilApi.ReplaceAllUsesOfConstant(oldConstant, newConstant, module);
        }

        /// <summary>
        /// Move a function from one module into another module.
        /// </summary>
        public unsafe static void MoveFunctionIntoModule(LLVMValueRef func, LLVMModuleRef destModule)
        {
            NativeRemillUtilApi.MoveFunctionIntoModule(func, destModule);
        }

        /// <summary>
        /// Get an instance of `type` that belongs to `context`.
        /// </summary>
        public unsafe static LLVMTypeRef RecontextualizeType(LLVMTypeRef type, LLVMContextRef context)
        {
            return NativeRemillUtilApi.RecontextualizeType(type, context);
        }

        /// <summary>
        /// Produce a sequence of instructions that will load values from
        /// memory, building up the correct type. This will invoke the various
        /// memory read intrinsics in order to match the right type, or
        /// recursively build up the right type.
        /// </summary>
        public unsafe static LLVMValueRef LoadFromMemory(RemillIntrinsicTable intrinsics, LLVMBasicBlockRef block, LLVMTypeRef type, LLVMValueRef memPtr, LLVMValueRef addr)
        {
            return NativeRemillUtilApi.LoadFromMemory(intrinsics, block, type, memPtr, addr);
        }

        /// <summary>
        /// Produce a sequence of instructions that will store a value to
        /// memory. This will invoke the various memory write intrinsics
        /// in order to match the right type, or recursively destructure
        /// the type into components which can be written to memory.
        /// Returns the new value of the memory pointer.
        /// </summary>
        public unsafe static LLVMValueRef StoreToMemory(RemillIntrinsicTable intrinsics, LLVMBasicBlockRef block, LLVMValueRef valToStore, LLVMValueRef memPtr, LLVMValueRef addr)
        {
            return NativeRemillUtilApi.StoreToMemory(intrinsics, block, valToStore, memPtr, addr);
        }

        /// <summary>
        /// Given a pointer, `ptr`, and a goal byte offset to which we'd like to index,
        /// build either a constant expression or sequence of instructions that can
        /// index to that offset. `ir` is provided to support the instruction case
        /// and to give access to a module for data layouts.
        /// </summary>
        public unsafe static LLVMValueRef BuildPointerToOffset(LLVMBuilderRef builder, LLVMValueRef ptr, ulong destElemOffset, LLVMTypeRef destPtrType)
        {
            return NativeRemillUtilApi.BuildPointerToOffset(builder, ptr, destElemOffset, destPtrType);
        }

        /// <summary>
        /// Compute the total offset of a GEP chain.
        /// </summary>
        public unsafe static (LLVMValueRef, long) StripAndAccumulateConstantOffsets(LLVMValueRef gepBase)
        {
            var ptr = NativeRemillUtilApi.StripAndAccumulateConstantOffsets(gepBase);
            var managedPair = new ManagedPair<LLVMOpaqueValue, long>(ptr);

            LLVMValueRef value = managedPair.First;
            return (value, *managedPair.Second);
        }
    }
}
