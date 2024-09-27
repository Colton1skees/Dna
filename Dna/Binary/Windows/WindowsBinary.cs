using AsmResolver;
using AsmResolver.Patching;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary.Windows
{
    public class WindowsBinary : IBinary
    {
        public int Bitness { get; }

        public byte[] Bytes { get; set; }

        public ulong BaseAddress { get; }

        public PEFile PEFile { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsBinary"/> class.
        /// </summary>
        /// <param name="bitness">The bitness of the binary(i.e 32 or 64).</param>
        /// <param name="binaryBytes">The raw bytes of the PE file.</param>
        /// <param name="baseAddress"> 
        ///     The optional base address of the binary.
        ///     If no address is provided, then base address specified in the optional header is used.
        /// </param>
        public WindowsBinary(int bitness, byte[] binaryBytes, ulong? baseAddress = null)
        {
            Bitness = bitness;
            Bytes = binaryBytes;
            PEFile = PEFile.FromBytes(binaryBytes);
            BaseAddress = baseAddress.HasValue ? baseAddress.Value : PEFile.OptionalHeader.ImageBase;
        }

        /// <inheritdoc cref="IBinary.ReadBytes(ulong, int)"/>
        public byte[] ReadBytes(ulong address, int count = 15)
        {
            // Allocate a buffer to store the results in.
            byte[] buffer = new byte[count];

            // Compute the file offset of the virtual address.
            var offset = address - BaseAddress;
            int fileOffset = (int)PEFile.RvaToFileOffset((uint)offset);

            // Read the raw data from the binary.
            Array.Copy(Bytes, fileOffset, buffer, 0, count);
            return buffer;
        }

        /// <inheritdoc cref="IBinary.WriteBytes(ulong, byte[])"/>
        public void WriteBytes(ulong address, byte[] bytes)
        {
            // Compute the file offset.
            var offset = address - BaseAddress;

            // Write the raw data to the binary.
            Array.Copy(bytes, 0, Bytes, (int)PEFile.RvaToFileOffset((uint)offset), bytes.Length);
        }

        public void WriteMutableByte(ulong address, byte input)
        {
            var offset = address - BaseAddress;
            var seg = PEFile.GetSectionContainingRva((uint)offset);

            var segBytes = seg.ToArray();
            Array.Copy(new byte[] { input }, 0, segBytes, (int)(offset - seg.Rva), 1);
            var physicalContents = new DataSegment(segBytes);
            seg.Contents = new VirtualSegment(physicalContents, seg.Contents.GetVirtualSize());
            PEFile.UpdateHeaders();
        }

        public void WriteMutableBytes(ulong address, byte[] input)
        {
            for(int i = 0; i < input.Length; i++)
            {
                WriteMutableByte(address + (ulong)i, input[i]);
            }
        }

        public static WindowsBinary From(string filePath, ulong? baseAddress = null)
        {
            return new WindowsBinary(64, File.ReadAllBytes(filePath), baseAddress);
        }
    }
}
