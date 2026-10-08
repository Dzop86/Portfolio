// Reeb graph of the height on a mesh: each connected component of each level set shrunk to a point. Its
// nodes are where level sets split, merge, appear or vanish; its arcs follow one level set component from
// one node to the next. On a closed orientable surface it has as many loops as the genus (Cole-McLaughlin
// et al. 2003).
#pragma once

#include "topo/mesh.hpp"
#include "topo/morse.hpp"

#include <cstdint>
#include <limits>
#include <vector>

namespace topo {

struct ReebNode {
    uint32_t vertex;
    uint32_t down = 0;  // arcs that end here from below
    uint32_t up = 0;    // arcs that leave upwards
};

struct ReebArc {
    uint32_t lower;  // node indices, the lower one first
    uint32_t upper;
    // Centroids of the level set component at the sampled heights in between, from bottom to top: the arc
    // drawn on the mesh. Their heights increase strictly (an arc of a Reeb graph is monotone).
    std::vector<Vec3> path;
};

struct ReebGraph {
    std::vector<ReebNode> nodes;  // in the order of the filtration
    std::vector<ReebArc> arcs;
    std::size_t components = 0;   // connected components of the graph (those of the mesh)
    // First Betti number of the graph: arcs - nodes + components.
    [[nodiscard]] std::size_t loops() const noexcept { return arcs.size() + components - nodes.size(); }
};

// Nodes are the vertices whose lower or upper link is not in one piece (the critical points, plus the top or
// bottom of a boundary); the mesh is cut at the nodes' heights and at `samples` more regular heights (for the
// drawing), each slab is split into its connected pieces, and pieces are glued across a cut unless the level
// set component there holds the node. The order of the vertices is `e`'s (topo::elevation).
// Close to O(T + k sqrt(T)) on a mesh, for T triangles and k cuts: a level set crosses few triangles.
// Throws std::length_error, before the slicing, if there are more than `max_nodes` nodes (a noisy height);
// std::invalid_argument if `e` is not an elevation of `mesh`.
[[nodiscard]] ReebGraph reeb_graph(const Mesh& mesh, const Elevation& e, uint32_t samples = 32,
                                   uint32_t max_nodes = std::numeric_limits<uint32_t>::max());

}  // namespace topo
