using AsmResolver;
using AsmResolver.PE.File;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary.Windows
{
    public class PEInjector
    {
        private readonly PEFile srcBin;

        private readonly PEFile dstBin;

        public PEInjector(PEFile srcBin, PEFile dstBin)
        {
            this.srcBin = srcBin;
            this.dstBin = dstBin;
        }

        public void Inject()
        {
            // Get the source .text section as a byte array.
            var textSection = srcBin.Sections.Single(x => x.Name == ".text");
            var sectionBytes = textSection.ToArray();

            // Inject it into the target binary.
            SectionManager.AllocateNewSection(dstBin, ".dna", sectionBytes);
            dstBin.Write("dna_patched.exe");
            Console.WriteLine("done");
            Debugger.Break();
        }
    }
}
