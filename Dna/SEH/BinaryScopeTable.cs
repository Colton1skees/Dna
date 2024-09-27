using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.Exceptions.X64;
using Dna.Binary;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAssembly;

namespace Dna.SEH
{
    public record ScopeTableEntry(ulong BeginAddr, ulong EndAddr, ulong FilterAddr, ulong HandlerAddr)
    {
        public bool IsAddressInsideTryStatement(ulong ip)
        =>  ip >= BeginAddr && ip < EndAddr;
    }

    public record ScopeTable(ulong Addr, IReadOnlyList<ScopeTableEntry> Entries)
    {
        public bool IsAddressInsideTryStatement(ulong ip)
           => Entries.Any(x => ip >= x.BeginAddr && ip < x.EndAddr);
    };

    public class BinaryScopeTable : SegmentBase
    {
        public List<BinaryScopeTableEntry> Entries { get; set; }

        public static ScopeTable? TryGetFromFunctionAddress(IBinary binary, ulong addr)
        {
            // Parse the binary into a PEImage. TODO: Stop recomputing this for each individual function.
            var peImage = (SerializedPEImage)PEImage.FromBytes(binary.Bytes);
            if (peImage.Exceptions == null)
                return null;

            // Return null if the exceptions directory is empty.
            var exceptions = peImage.Exceptions.GetEntries().ToList();
            var target = exceptions.SingleOrDefault(x => (ulong)x.Begin.Rva + binary.BaseAddress == addr) as X64RuntimeFunction;
            if (target == null)
                return null;

            // Return null if the exception handler data is null.
            var segRef = target.UnwindInfo.ExceptionHandlerData;
            if (segRef == null || !segRef.CanRead)
                return null;

            // Parse the scope table entries.
            var reader = peImage.PEFile.CreateReaderAtRva(segRef.Rva);
            var binaryScopeTable = FromReader(peImage.ReaderContext, ref reader);

            var entries = new List<ScopeTableEntry>();    
            foreach(var entry in binaryScopeTable.Entries)
            {
                var beginAddr = entry.Begin.Rva + binary.BaseAddress;
                var endAddr = entry.End.Rva + binary.BaseAddress;
                var filterAddr = entry.Filter.Rva + binary.BaseAddress;
                var handlerAddr = entry.ExceptionHandler.Rva + binary.BaseAddress;

                // TODO: Handle scenario where the filter is null but the target is target is not. This would be an `__finally` statement.
                // https://blog.talosintelligence.com/exceptional-behavior-windows-81-x64-seh/
                if (entry.Filter.Rva == 0 || entry.ExceptionHandler.Rva == 0)
                    throw new InvalidOperationException($"Invalid scope table entry: filter {entry.Filter.Rva} or handler {entry.ExceptionHandler.Rva} is null.");
                entries.Add(new ScopeTableEntry(beginAddr, endAddr, filterAddr, handlerAddr));
            }

            return new ScopeTable(binaryScopeTable.Rva + binary.BaseAddress, entries.AsReadOnly());
        }

        public static BinaryScopeTable FromReader(PEReaderContext context, ref BinaryStreamReader reader)
        {
            var entry = new BinaryScopeTable();
            entry.UpdateOffsets(context.GetRelocation(reader.Offset, reader.Rva));

            var numEntries = reader.ReadUInt32();

            entry.Entries = new List<BinaryScopeTableEntry>((int)numEntries);
            for(int i = 0; i < (uint)numEntries; i++)
                entry.Entries.Add(BinaryScopeTableEntry.FromReader(context, ref reader));

            return entry;
        }

        public override uint GetPhysicalSize() => (1 + (uint)Entries.Count) * 4;

        public override void Write(IBinaryStreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
