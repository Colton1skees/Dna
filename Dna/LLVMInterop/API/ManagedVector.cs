using Dna.LLVMInterop.API.LLVMBindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API
{
    public class ManagedVector<T>
    {
        public readonly nint Handle;

        private readonly Func<nint, T> convertPtr;

        public int Count => NativeManagedVectorApi.GetCount(Handle);

        public IReadOnlyList<T> Items => GetItems();

        public ManagedVector(nint handle, Func<nint, T> convertPtr)
        {
            this.Handle = handle;
            this.convertPtr = convertPtr;
        }

        public unsafe static ManagedVector<T> From(nint[] items, Func<nint, T> convertPtr)
        {
            fixed (nint* pointerToFirst = items)
            {
                var ptr = NativeManagedVectorApi.FromManagedArray((nint)pointerToFirst, items.Length);
                return new ManagedVector<T>(ptr, convertPtr);
            }
        }

        private IReadOnlyList<T> GetItems()
        {
            List<T> output = new();
            for (int i = 0; i < Count; i++)
            {
                var element = NativeManagedVectorApi.GetElementAt(Handle, i);
                output.Add(convertPtr(element));
            }

            return output.AsReadOnly();
        }
    }
}
