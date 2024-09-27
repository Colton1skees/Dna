#include <set>
#include <map>
#include <llvm/IR/BasicBlock.h>
#include <API/ImmutableManagedVector.h>
#include "API/ExportDef.h"

namespace Dna::API {
    inline bool alreadyOnStackQuick(std::set<llvm::BasicBlock*>& StackSet,
        llvm::BasicBlock* Node) {
        if (StackSet.count(Node)) {
            return true;
        }
        else {
            return false;
        }
    }

    using Edge = std::pair<llvm::BasicBlock*, llvm::BasicBlock*>;
    using Stack = std::vector<std::pair<llvm::BasicBlock*, size_t>>;
    // Helper function to find all nodes on paths between a source and a target
    // node
    DNA_EXPORT ImmutableManagedVector*
        FindReachableNodes(llvm::BasicBlock* Source,
            llvm::BasicBlock* Target) {

        // The node starting the exploration should always exist (the same does not
        // hold for the target node).
        assert(Source != nullptr);

        // Add to the Targets set the original target node, if we actually have a
        // target node as a parameter.
        std::set<llvm::BasicBlock*> Targets;
        if (Target != nullptr)
            Targets.insert(Target);

        // Exploration stack initialization.
        Stack Stack;
        std::set<llvm::BasicBlock*> StackSet;
        Stack.push_back(std::make_pair(Source, 0));


        // Visited nodes to avoid entering in a loop.
        std::set<Edge> VisitedEdges;

        // Additional data structure to keep nodes that need to be added only if a
        // certain node will be added to the set of reachable nodes.
        std::map<llvm::BasicBlock*, std::set<llvm::BasicBlock*>> AdditionalNodes;

        // Exploration.
        while (!Stack.empty()) {
            auto StackElem = Stack.back();
            Stack.pop_back();
            llvm::BasicBlock* Vertex = StackElem.first;
            if (StackElem.second == 0) {

                // Stop condition for the exploration. If a `Target` is provided, then we
                // can only stop once we hit a node in `Targets`. If, instead, no `Target`
                // is provided, we must also stop at a node that has no successors (which,
                // usually, means that we invoked the helper function on a graph where we
                // computed a filtered post dominator tree, and the `nullptr` passed as
                // argument represents exactly the `VirtualRoot` node which acts as a sink
                // needed for the tree computation.
                if ((Targets.count(Vertex) != 0)
                    || (Target == nullptr && Vertex->getTerminator()->getNumSuccessors() == 0)) {
                    for (auto StackE : Stack) {
                        Targets.insert(StackE.first);
                    }
                    continue;
                }
                else if (alreadyOnStackQuick(StackSet, Vertex)) {
                    // Add all the nodes on the stack to the set of additional nodes.
                    std::set<llvm::BasicBlock*>& AdditionalSet = AdditionalNodes[Vertex];
                    for (auto StackE : Stack) {
                        AdditionalSet.insert(StackE.first);
                    }
                    continue;
                }
            }
            StackSet.insert(Vertex);

            size_t Index = StackElem.second;
            if (Index < StackElem.first->getTerminator()->getNumSuccessors()) {
                llvm::BasicBlock* NextSuccessor = Vertex->getTerminator()->getSuccessor(Index);
                Index++;
                Stack.push_back(std::make_pair(Vertex, Index));
                if (VisitedEdges.count(std::make_pair(Vertex, NextSuccessor)) == 0
                    && NextSuccessor != Source
                    && !alreadyOnStackQuick(StackSet, NextSuccessor)) {
                    Stack.push_back(std::make_pair(NextSuccessor, 0));
                    VisitedEdges.insert(std::make_pair(Vertex, NextSuccessor));
                }
            }
            else {
                StackSet.erase(Vertex);
            }
        }

        // Add additional nodes.
        std::set<llvm::BasicBlock*> OldTargets;

        do {
            // At each iteration obtain a copy of the old set, so that we are able to
            // exit from the loop as soon no change is made to the `Targets` set.

            OldTargets = Targets;

            // Temporary storage for the nodes to add at each iteration, to avoid
            // invalidation on the `Targets` set.
            std::set<llvm::BasicBlock*> NodesToAdd;

            for (llvm::BasicBlock* Node : Targets) {
                std::set<llvm::BasicBlock*>& AdditionalSet = AdditionalNodes[Node];
                NodesToAdd.insert(AdditionalSet.begin(), AdditionalSet.end());
            }

            // Add all the additional nodes found in this step.
            Targets.insert(NodesToAdd.begin(), NodesToAdd.end());
            NodesToAdd.clear();

        } while (Targets != OldTargets);

        auto vec = new std::vector<llvm::BasicBlock*>();
        for (auto tgt : Targets)
            vec->push_back(tgt);

        return ImmutableManagedVector::NonCopyingFrom(vec);
    }
}