using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicorn;
using Unicorn.X86;

namespace Dna.Emulation.Unicorn
{
    public class GdtHelper
    {
        private readonly X86Emulator emu;

        private readonly ulong gdtAddress;

        private readonly ulong gdtSize;

        private ulong entryCount;

        private ulong maxGdt = 0x10;

        private const ulong F_PAGE_GRANULARITY = 0x8;
        private const ulong F_PROT_32 = 0x4;
        private const ulong F_LONG = 0x2;
        private const ulong F_AVAILABLE = 0x1;

        private const ulong A_PRESENT = 0x80;

        private const ulong A_PRIV_3 = 0x60;
        private const ulong A_PRIV_2 = 0x40;
        private const ulong A_PRIV_1 = 0x20;
        private const ulong A_PRIV_0 = 0x0;

        private const ulong A_CODE = 0x18;
        private const ulong A_DATA = 0x10;
        private const ulong A_TSS = 0x0;
        private const ulong A_GATE = 0x0;

        private const ulong A_DATA_WRITABLE = 0x2;
        private const ulong A_CODE_READABLE = 0x2;

        private const ulong A_DIRECTION_UP = 0x0;
        private const ulong A_DIRECTION_DOWN = 0x4;
        private const ulong A_CONFORMING = 0x0;

        private const ulong S_GDT = 0x0;
        private const ulong S_LDT = 0x4;
        private const ulong S_PRIV_3 = 0x3;
        private const ulong S_PRIV_2 = 0x2;
        private const ulong S_PRIV_1 = 0x1;
        private const ulong S_PRIV_0 = 0x0;

        public GdtHelper(X86Emulator emu, ulong gdtAddress, ulong gdtSize)
        {
            this.emu = emu;
            this.gdtAddress = gdtAddress;
            this.gdtSize = gdtSize;
        }

        ulong CreateSelector(ulong idx, ulong flags)
        {
            var toRet = flags;
            toRet |= idx << 3;
            return toRet;
        }

        ulong CreateGdtEntry(ulong gBase, ulong limit, ulong access, ulong flags)
        {
            var to_ret = limit & 0xffff;
            to_ret |= (gBase & 0xffffff) << 16;
            to_ret |= (access & 0xff) << 40;
            to_ret |= ((limit >> 16) & 0xf) << 48;
            to_ret |= (flags & 0x0f) << 52;
            to_ret |= ((gBase >> 24) & 0xff) << 56;
            return to_ret;
        }

        ulong CreateSegmentSelector(int segReg, ulong segAddr, ulong segSize, ulong access)
        {
            if (entryCount > maxGdt)
                throw new InvalidOperationException();

            var gdtIdx = entryCount + 1;

            var gdtEntry = CreateGdtEntry(segAddr, segSize, access, F_PROT_32);

            var bytes = BitConverter.GetBytes(gdtEntry);
            emu.Memory.Write(gdtAddress + 8 * gdtIdx, bytes, (ulong)bytes.Length);

            var selector = CreateSelector(gdtIdx, S_GDT | S_PRIV_0);

            emu.Registers.Write(segReg, (long)selector);

            entryCount += 1;

            return selector;
        }

        public void Setup(ulong teb)
        {
            List<ulong> gdts = new List<ulong>();
            for(int i = 0; i < 32; i++)
            {
                gdts.Add(CreateGdtEntry(0, 0, 0, 0));
            }


            var ds = CreateSegmentSelector(17, 0x0, 0xfffff, A_PRESENT | A_PRIV_0 | A_DATA | A_DATA_WRITABLE | A_DIRECTION_UP);
            var es = CreateSegmentSelector(28, 0x0, 0xfffff, A_PRESENT | A_PRIV_0 | A_DATA | A_DATA_WRITABLE | A_DIRECTION_UP);
           // var fs = CreateSegmentSelector(32, teb, 1, A_PRESENT | A_PRIV_0 | A_DATA | A_DATA_WRITABLE);  // FIXME: Correct the limit
            //var gs = CreateSegmentSelector(33, 0x0, 0xfffff, A_PRESENT | A_PRIV_0 | A_DATA | A_DATA_WRITABLE | A_DIRECTION_UP);
            //var cs = CreateSegmentSelector(11, 0x0, 0xfffff, A_PRESENT | A_PRIV_0 | A_CODE | A_CODE_READABLE | A_CONFORMING);
        }
    }
}
