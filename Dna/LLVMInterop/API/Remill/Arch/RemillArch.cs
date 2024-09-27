using Dna.LLVMInterop.API.Remill.BC;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void dgRegisterCallback(RemillOpaqueRegister* reg);

    public class RemillArch
    {
        public readonly nint Handle;

        private RemillDecodingContext defaultCtx = null;

        public unsafe LLVMTypeRef AddressType => NativeRemillArchApi.Arch_AddressType(this);

        public unsafe LLVMTypeRef StateStructType => NativeRemillArchApi.Arch_StateStructType(this);

        public unsafe LLVMTypeRef StatePointerType => NativeRemillArchApi.Arch_StatePointerType(this);

        public unsafe LLVMTypeRef MemoryPointerType => NativeRemillArchApi.Arch_MemoryPointerType(this);

        public unsafe LLVMTypeRef LiftedFunctionType => NativeRemillArchApi.Arch_LiftedFunctionType(this);

        public unsafe LLVMTypeRef RegisterWindowType => NativeRemillArchApi.Arch_RegisterWindowType(this);

        public unsafe RemillIntrinsicTable IntrinsicTable => NativeRemillArchApi.Arch_GetIntrinsicTable(this);

        public IReadOnlyList<RemillRegister> Registers => GetRegisters();

        public unsafe string StackPointerRegisterName => StringMarshaler.AcquireString(NativeRemillArchApi.Arch_StackPointerRegisterName(this));

        public unsafe string ProgramCounterRegisterName => StringMarshaler.AcquireString(NativeRemillArchApi.Arch_ProgramCounterRegisterName(this));

        public unsafe RemillArch(LLVMContextRef context, RemillOsId osId, RemillArchId archId)
        {
            Handle = (nint)NativeRemillArchApi.Arch_Constructor(context, osId, archId);
        }

        public RemillArch(nint handle)
        {
            Handle = handle;
        }

        public unsafe RemillDecodingContext CreateInitialContext()
        {
            return NativeRemillArchApi.Arch_CreateInitialContext(this);
        }

        private unsafe IReadOnlyList<RemillRegister> GetRegisters()
        {
            // Create a callback which adds the input register to the list.
            var output = new List<RemillRegister>();
            var callback = (RemillOpaqueRegister* reg) =>
            {
                output.Add(reg);
            };

            // Append each register to the list, using remill's `ForEachRegister` function.
            var callbackFuncPtr = Marshal.GetFunctionPointerForDelegate(new dgRegisterCallback(callback));
            NativeRemillArchApi.Arch_ForEachRegister(this, callbackFuncPtr);
            return output.AsReadOnly();
        }

        public unsafe RemillRegister? GetRegisterAtStateOffset(ulong offset)
        {
            var ptr = NativeRemillArchApi.Arch_RegisterAtStateOffset(this, offset);
            return ptr == null ? (RemillRegister?)null : ptr;
        }

        public unsafe RemillRegister? GetRegisterByName(string name)
        {
            var ptr = NativeRemillArchApi.Arch_RegisterByName(this, new MarshaledString(name));
            return ptr == null ? (RemillRegister?)null : ptr;
        }

        public unsafe LLVMValueRef DeclareLiftedFunction(string name, LLVMModuleRef module) => NativeRemillArchApi.Arch_DeclareLiftedFunction(this, new MarshaledString(name), module);

        public unsafe LLVMValueRef DefineLiftedFunction(string name, LLVMModuleRef module) => NativeRemillArchApi.Arch_DefineLiftedFunction(this, new MarshaledString(name), module);

        public unsafe void InitializeEmptyLiftedFunction(LLVMValueRef func) => NativeRemillArchApi.Arch_InitializeEmptyLiftedFunction(this, func);

        public unsafe void PrepareModule(LLVMModuleRef module) => NativeRemillArchApi.Arch_PrepareModule(this, module);

        public unsafe void PrepareModuleDataLayout(LLVMModuleRef module) => NativeRemillArchApi.Arch_PrepareModuleDataLayout(this, module);

        /// <summary>
        /// Decodes a single remill instruction. 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bytes"></param>
        /// <returns>The decoded instruction if successful, null otherwise.</returns>
        public unsafe RemillInstruction? DecodeInstruction(ulong address, byte[] bytes)
        {
            RemillInstruction instruction = new RemillInstruction();
            bool success = DecodeInstruction(address, bytes, instruction, GetDefaultDecodingContext());
            return success ? instruction : null;
        }

        public unsafe bool DecodeInstruction(ulong address, byte[] bytes, RemillInstruction inst, RemillDecodingContext decodingContext)
        {
            fixed (byte* pArr = bytes)
            {
                return NativeRemillArchApi.Arch_DecodeInstruction(this, address, pArr, bytes.Length, inst, decodingContext);
            };
        }

        private unsafe RemillDecodingContext GetDefaultDecodingContext()
        {
            // If a cached context does not exist, create one.
            if(defaultCtx == null)
                defaultCtx = CreateInitialContext();

            // Return the context.
            return defaultCtx;
        }

        public unsafe static RemillArch GetModuleArch(LLVMModuleRef module) => NativeRemillArchApi.Arch_GetModuleArch(module);

        public unsafe static RemillArch Get(LLVMContextRef context, RemillOsId osId, RemillArchId archId) => NativeRemillArchApi.Arch_Constructor(context, osId, archId);

        public unsafe static implicit operator RemillOpaqueArch*(RemillArch reg) => (RemillOpaqueArch*)reg.Handle;

        public unsafe static implicit operator RemillArch(RemillOpaqueArch* reg) => new RemillArch((nint)reg);
    }
}
