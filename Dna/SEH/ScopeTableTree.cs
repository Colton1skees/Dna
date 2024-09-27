using Dna.Binary.Windows;
using Dna.ControlFlow.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
namespace Dna.SEH
{
    public class ScopeTableTree
    {
        public IReadOnlyList<ScopeTableNode>[] DepthMapping;

        public ScopeTable ScopeTable { get; }

        /// <summary>
        /// Gets all root scope table nodes. I.e. all scope table entries that are not contained inside of another try catch.
        /// </summary>
        public IReadOnlyList<ScopeTableNode> RootNodes;

        /// <summary>
        /// Gets a list of all scope table entry nodes, regardless of their depth.
        /// </summary>
        public IReadOnlyList<ScopeTableNode> AllNodes;

        public ScopeTableTree(ScopeTable scopeTable)
        {
            ScopeTable = scopeTable;
            RootNodes = BuildScopeTableTree(scopeTable.Entries);

            var groupedByDepth = RootNodes.
                SelectMany(x => GetNodes(x, new()))
                .GroupBy(x => x.Depth)
                .ToDictionary(x => x.Key, x => x.ToList());

            // Create an ordered list of nodes by their depth.
            // Depth 0 scope table entries are placed at index 0,
            // depth 1 scope table entries are placed at index 1,
            // etc.
            DepthMapping = new List<ScopeTableNode>[groupedByDepth.Count];
            foreach((var depth, var nodes) in groupedByDepth)
                DepthMapping[depth] = nodes;

            // Get a list of all nodes.
            AllNodes = DepthMapping.SelectMany(x => x).ToList().AsReadOnly();
        }

        /// <summary>
        /// Takes a list of ScopeTableEntries and rebuild it into a hierarchical tree structure.
        /// Note that this would break if someone were to partially nest a try statement inside of another try statement.
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        private IReadOnlyList<ScopeTableNode> BuildScopeTableTree(IReadOnlyList<ScopeTableEntry> entries)
        {
            // Sort the entries in ascending orders.
            entries = entries.OrderBy(x => x.BeginAddr).ToList().AsReadOnly();

            /// Queue up all nodes to be processed.
            Stack<ScopeTableNode> parents = new();
            Queue<ScopeTableEntry> toVisit = new();
            List<ScopeTableNode> rootNodes = new();
            foreach(var entry in entries)
                toVisit.Enqueue(entry);

            // Process all available nodes.
            while(toVisit.Any())
            {
                // Pop the topmost available entry.
                var entry = toVisit.Dequeue();

                // If this node has a parent, then get it.
                parents.TryPeek(out var parent);

                // Create a node for the current scope table entry.
                var node = new ScopeTableNode(entry, parent, parents.Count);
                parent?.Children.Add(node);

                // If this is not nested under any other entry then add it to the root node list.
                if (parent == null)
                    rootNodes.Add(node);
                    
                // Get the next item to process.
                toVisit.TryPeek(out var next);

                // Break if there are no more nodes to process.
                if (next == null)
                    break;

                // Push the current node to the parent list.
                parents.Push(node);

                // Pop all parent nodes that don't own the next node.
                while(parents.Any())
                {
                    // Peek the current parent node.
                    parents.TryPeek(out parent);

                    // If the next node is not owned by the current parent then we pop it.
                    if (!parent.Entry.IsAddressInsideTryStatement(next.BeginAddr))
                        parents.Pop();
                    // Otherwise the node is owned by the current parent so it must stay.
                    else
                        break;
                }
            }

            return rootNodes.OrderBy(x => x.Entry.BeginAddr).ToList().AsReadOnly();
        }

        private static List<ScopeTableNode> GetNodes(ScopeTableNode node, List<ScopeTableNode> nodes)
        {
            nodes.Add(node);
            foreach (var child in node.Children)
                GetNodes(child, nodes);

            return nodes;
        }

        /// <summary>
        /// Gets the immediate scope table entry containing the address.
        /// Returns null if it is not inside any try statement.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ScopeTableNode GetNodeForAddress(ulong address)
        {
            for(int i = DepthMapping.Length - 1; i >= 0; i--)
            {
                var nodes = DepthMapping[i];
                ScopeTableNode owner = null;
                foreach(var node in nodes)
                {
                    if (!node.ContainsAddress(address))
                        continue;

                    // If multiple nodes at the current depth own this address, something is wrong.
                    if (owner != null)
                        throw new InvalidOperationException($"Invalid SEH entry: found multiple owners for address {address}");
                    owner = node;
                }

                // If we found an owner at the current depth then return it.
                if (owner != null)
                    return owner;
            }

            return null;
        }
    }

    /// <summary>
    /// Wrapper around ScopeTableEntry instances that's aware of the hierarchical structure of scope table.
    /// </summary>
    public class ScopeTableNode
    {
        public ScopeTableEntry Entry { get; }

        public ScopeTableNode? Parent { get; }

        public int Depth { get; }

        public List<ScopeTableNode> Children { get; } = new();

        public ScopeTableNode(ScopeTableEntry entry, ScopeTableNode? parent, int depth)
        {
            Entry = entry;
            Parent = parent;
            Depth = depth;
        }

        public bool ContainsAddress(ulong addr) => Entry.IsAddressInsideTryStatement(addr);
    }
}
