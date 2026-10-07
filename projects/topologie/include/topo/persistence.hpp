// Persistent homology of a height function on a mesh: every component, handle or cavity of the sublevel
// sets is born at one vertex and dies at another (or never); the gap between the two heights, its
// persistence, tells a real feature from noise (Edelsbrunner, Letscher and Zomorodian 2002).
#pragma once

#include "topo/mesh.hpp"

#include <cstdint>
#include <limits>
#include <vector>

namespace topo {

struct PersistencePair {
    int dimension;          // 0 a component, 1 a loop (a handle or a hole), 2 a cavity (a closed surface)
    uint32_t birth_vertex;  // the vertex whose arrival creates the class
    uint32_t death_vertex;  // the vertex whose arrival kills it; kNone for an essential class
    double birth;           // heights of those vertices
    double death;           // +infinity for an essential class

    [[nodiscard]] bool essential() const noexcept { return death_vertex == kNone; }
    [[nodiscard]] double persistence() const noexcept { return death - birth; }
};

struct Persistence {
    // Finite pairs first, by birth then death, then the essential classes by dimension and birth. Pairs born
    // and killed by the same vertex are left out; pairs between two vertices of equal height (a plateau,
    // ordered by index) are kept, with zero persistence.
    std::vector<PersistencePair> pairs;
    // Essential classes per dimension: the Betti numbers of the mesh over Z/2.
    std::array<int, 3> betti{};
};

// Lower-star filtration of `height` (one value per vertex, ties broken by index as in topo::elevation):
// a vertex enters with the edges and faces of which it is the highest vertex. The boundary matrix is
// reduced over Z/2, faces first, with clearing (Chen and Kerber 2011): an edge that kills a loop no longer
// needs reducing. Worst case cubic in the number of simplices, close to linear on meshes in practice.
// Throws std::invalid_argument if `height` does not have one value per vertex.
[[nodiscard]] Persistence persistence(const Mesh& mesh, const std::vector<double>& height);

}  // namespace topo
