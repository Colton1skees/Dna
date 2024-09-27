using AsmResolver;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary.Windows
{
    /// <summary>
    /// Helper class for creating and manipulating sections in a binary.
    /// </summary>
    public static class SectionManager
    {
        public static PESection AllocateNewSection(PEFile peFile, string sectionName, uint virtualSize = 0x1000, SectionFlags sectionFlags = SectionFlags.MemoryRead | SectionFlags.MemoryWrite | SectionFlags.MemoryExecute)
        {
            var section = new PESection(sectionName, sectionFlags);
            var physicalContents = new DataSegment(new byte[virtualSize]);
            section.Contents = new VirtualSegment(physicalContents, virtualSize);
            peFile.Sections.Add(section);
            peFile.UpdateHeaders();
            return section;
        }

        public static PESection AllocateNewSection(PEFile peFile, string sectionName, byte[] bytes, uint virtualSize = 0x1000, SectionFlags sectionFlags = SectionFlags.MemoryRead | SectionFlags.MemoryExecute)
        {
            var section = AllocateNewSection(peFile, sectionName, virtualSize, sectionFlags);
            var physicalContents = new DataSegment(bytes);
            section.Contents = new VirtualSegment(physicalContents, virtualSize);
            peFile.UpdateHeaders();
            return section;
        }

        /// <summary>
        /// Overwrites bytes in the provided section.
        /// </summary>
        /// <param name="section">The section to be modified.</param>
        /// <param name="bytes">The source bytes to overwrite the section data with.</param>
        /// <param name="index">The start index of the bytes to overwrite.</param>
        /// <param name="count">The length of bytes to be overwritten.</param>
        public static void ModifySectionBytes(PEFile peFile, PESection section, byte[] bytes, int index, int count)
        {
            var sectionBytes = section.ToArray();
            Array.Copy(bytes, 0, sectionBytes, index, count);
            var physicalContents = new DataSegment(sectionBytes);
            section.Contents = new VirtualSegment(physicalContents, section.Contents.GetVirtualSize());
            peFile.UpdateHeaders();
        }

        /*
        /// <summary>
        /// Modifies a section's bytes at the given rva.
        /// </summary>
        /// <param name="section">The section to be modified.</param>
        /// <param name="rva">The RVA to start modifying at.</param>
        /// <param name="bytes"></param>
        /// <param name="count"></param>
        public static void ModifySectionBytes(PESection section, ulong rva, byte[] bytes, int count)
        {
            // Subtract the provided base address from the rva since AsmResolver seems to assume that the base address is always 0.
            rva -= binary.BaseAddress;

            // Utilizing the RVA to write to bytes like this might not work in some edge cases. TODO: Validate.
            var sectionBytes = section.ToArray();
            ModifySectionBytes(section, bytes, (int)rva, count);
        }

        public static PESection GetSectionFromRVA(ulong rva)
        {
            rva -= binary.BaseAddress;
            return binary.PEFile.Sections.Single(x => x.Rva <= rva && x.Rva + x.GetVirtualSize() > rva);
        }
        */
    }
}
