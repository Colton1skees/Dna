using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAIL.IL.Type
{
    public class IntegerType : ILType
    {
        public int Size { get; }

        public IntegerType(int size)
        {
            if(size % 8 != 0)
                throw new ArgumentException(String.Format("Cannot create IntegerType of size {0}", size));
            Size = size;
        }
    }
}
