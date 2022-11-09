using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Optimization.Passes
{
    public interface IOptimizationPass
    {
        /// <summary>
        /// Executes the optimization pass.
        /// </summary>
        public void Run();
    }
}
