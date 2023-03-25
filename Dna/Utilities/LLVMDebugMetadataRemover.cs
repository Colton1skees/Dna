using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Utilities
{
    public static class LLVMDebugMetadataRemover
    {
        public static List<string> RemoveMetadata(List<string> lines)
        {
            List<string> output = new List<string>();
            foreach (var line in lines)
            {
                // Discard all calls to lldm.dbg.value().
                if (line.Contains("call void @llvm.dbg.value"))
                    continue;

                // Emit the original string if there is no debug metadata.
                if (!line.Contains("!"))
                {
                    output.Add(line);
                    continue;
                }

                // Split the string, starting at the first instance of debug metadata.
                var split = line.Split('!')[0];

                if (split.EndsWith(", "))
                    split = split.Substring(0, split.Length - 2);

                // If this is the start of a function definition, then add back the bracket.
                if (line.Contains("{"))
                    split += "{";

                output.Add(split);
            }

            return output;
        }
    }
}
