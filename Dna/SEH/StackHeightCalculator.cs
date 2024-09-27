using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.SEH
{
    public static class StackHeightCalculator
    {
        public static int Get(IEnumerable<UnwindCode> codes)
        {
            int height = 0;
            foreach(var code in codes)
            {
                height += code switch
                {
                    UwOpPushNonVol => 8,
                    UwOpAllocLarge large => large.Size,
                    UwOpAllocSmall small => small.Size,
                    UwOpSetFpReg or UwOpSaveNonVol or UwOpSaveNonVolFar or UwOpSaveXmm128 or UwOpSaveXmm128Far => 0,
                    _ => throw new InvalidOperationException($"TODO: Compute height for type {code}")
                };
            }

            return height;
        }
    }
}
