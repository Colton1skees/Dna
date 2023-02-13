using Rivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Analysis
{
    public static class ReachabilityAnalysis
    {
        public static HashSet<Node> FindReachableNodes(Node source, Node target)
        {
            // The node for starting exploration must always exist.
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            // While the node for starting exploration should always exist,
            // it is not the case that a target node must exit.
            var targets = new HashSet<Node>();
            if(target != null)
                targets.Add(target);
        }
    }
}
