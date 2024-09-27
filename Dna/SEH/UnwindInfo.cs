using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver;
using AsmResolver.PE.Exceptions.X64;
using AsmResolver.PE.File;
using Dna.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Dna.SEH
{
    public class UnwindInfo : SegmentBase
    {
        public static UnwindInfo FromReader(PEReaderContext context, ref BinaryStreamReader reader)
        {
            var entry = new UnwindInfo();
            entry.UpdateOffsets(context.GetRelocation(reader.Offset, reader.Rva));

            var v = reader.ReadUInt32();

            var countOfCodes = (byte)((v & 0xff0000) >> 16);

            var flags = reader.ReadByte();

            var sizeOfProlog = reader.ReadByte();
            var countOfCodes_ = reader.ReadByte();
            var frameRegister = reader.ReadByte();

            var unwindCode = reader.ReadUInt16();

            var count = ((countOfCodes + 1) & ~1) - 1;
            for(int i = 0;  i < count; i++)
            {
                reader.ReadUInt16();
            }
            return entry;
        }

        public override uint GetPhysicalSize() => 12;

        public override void Write(IBinaryStreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
