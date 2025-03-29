using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.Arch
{
    public class RemillRegister
    {
        public readonly nint Handle;

        public string Name => GetName();

        public unsafe ulong Offset => NativeRemillRegisterApi.Register_GetOffset(this);

        public unsafe ulong Size => NativeRemillRegisterApi.Register_GetSize(this);

        public unsafe LLVMTypeRef LLVMType => NativeRemillRegisterApi.Register_GetType(this);

        public unsafe LLVMValueRef ConstantName => NativeRemillRegisterApi.Register_GetConstantName(this);

        public IReadOnlyList<LLVMValueRef> GepIndexes => GetGepIndexes();

        public unsafe ulong GepOffset => NativeRemillRegisterApi.Register_GetGepOffset(this);

        public unsafe LLVMTypeRef TypeAtOffset => NativeRemillRegisterApi.Register_GetGepTypeAtOffset(this);

        public unsafe RemillRegister EnclosingRegister => NativeRemillRegisterApi.Register_EnclosingRegister(this);

        public IReadOnlyList<RemillRegister> EnclosedRegisters => GetEnclosedRegisters();

        public unsafe RemillRegister? Parent => GetParent();

        public unsafe RemillArch Arch => NativeRemillRegisterApi.Register_GetArch(this);

        public IReadOnlyList<RemillRegister> Children => GetChildren();

        public RemillRegister(nint handle)
        {
            Handle = handle;
        }

        private unsafe string GetName()
        {
            var pStr = NativeRemillRegisterApi.Register_GetName(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        private unsafe IReadOnlyList<LLVMValueRef> GetGepIndexes()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeRemillRegisterApi.Register_GetGepIndexList(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMValueRef>((nint)vecPtr,
                (nint ptr) => new LLVMValueRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe RemillRegister GetEnclosingRegisterOfSize(ulong size) => NativeRemillRegisterApi.Register_EnclosingRegisterOfSize(this, size);

        private unsafe IReadOnlyList<RemillRegister> GetEnclosedRegisters()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeRemillRegisterApi.Register_EnclosedRegisters(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<RemillRegister>((nint)vecPtr,
                (nint ptr) => new RemillRegister(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe RemillRegister? GetParent()
        {
            var ptr = NativeRemillRegisterApi.Register_GetParent(this);
            return ptr == null ? (RemillRegister?)null : ptr;
        }

        public unsafe LLVMValueRef GetAddressOf(LLVMValueRef statePtr, LLVMBasicBlockRef addToEnd) => NativeRemillRegisterApi.Register_AddressOf(this, statePtr, addToEnd);

        public unsafe LLVMValueRef GetAddressOf(LLVMValueRef statePtr, LLVMBuilderRef builder) => NativeRemillRegisterApi.Register_AddressOfUsingBuilder(this, statePtr, builder);

        private unsafe IReadOnlyList<RemillRegister> GetChildren()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeRemillRegisterApi.Register_GetChildren(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<RemillRegister>((nint)vecPtr,
                (nint ptr) => new RemillRegister(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not RemillRegister reg)
                return false;
            return reg.Name == Name;
        }

        public unsafe static implicit operator RemillOpaqueRegister*(RemillRegister reg)
        {
            return (RemillOpaqueRegister*)reg.Handle;
        }

        public unsafe static implicit operator RemillRegister(RemillOpaqueRegister* reg)
        {
            return new RemillRegister((nint)reg);
        }
    }
}
