using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.SEH
{
    public class BinaryScopeTableEntry : SegmentBase
    {
        /// <summary>
        /// The beginning of the guarded code block.
        /// </summary>
        public ISegmentReference Begin { get; set; }

        /// <summary>
        /// The end of the target / guarded code block.
        /// </summary>
        public ISegmentReference End { get; set; }

        /// <summary>
        /// The exception filter function (or "__finally").
        /// </summary>
        public ISegmentReference Filter { get; set; }

        /// <summary>
        /// The exception handler pointer(the code inside the __except block).
        /// </summary>
        public ISegmentReference ExceptionHandler { get; set; }

        public static BinaryScopeTableEntry FromReader(PEReaderContext context, ref BinaryStreamReader reader)
        {
            var entry = new BinaryScopeTableEntry();
            entry.UpdateOffsets(context.GetRelocation(reader.Offset, reader.Rva));

            entry.Begin = context.File.GetReferenceToRva(reader.ReadUInt32());
            entry.End = context.File.GetReferenceToRva(reader.ReadUInt32());
            entry.Filter = context.File.GetReferenceToRva(reader.ReadUInt32());
            entry.ExceptionHandler = context.File.GetReferenceToRva(reader.ReadUInt32());

            return entry;
        }

        public override uint GetPhysicalSize() => 12;

        public override void Write(IBinaryStreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
