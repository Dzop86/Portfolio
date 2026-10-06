// Topological invariants of a triangle mesh.
#pragma once

#include "topo/mesh.hpp"

#include <cstdint>
#include <optional>

namespace topo {

struct Invariants {
    std::size_t components = 0;            // connected components, isolated vertices included
    std::size_t isolated_vertices = 0;     // vertices used by no triangle
    std::size_t boundary_loops = 0;        // connected components of the boundary edges
    std::size_t non_manifold_edges = 0;    // edges shared by three faces or more
    std::size_t non_manifold_vertices = 0; // vertices whose faces form several fans
    bool manifold = true;                  // no non-manifold edge or vertex
    bool orientable = true;                // faces can be reoriented consistently
    bool consistently_oriented = true;     // they already are
    int64_t euler_characteristic = 0;      // V - E + F
    // Total genus of an orientable manifold: chi = 2c - 2g - b over its surface components.
    // Empty for non-manifold or non-orientable meshes.
    std::optional<int64_t> genus;
};

// O(T log T) for building the mesh, then near-linear (union-find) here.
[[nodiscard]] Invariants analyze(const Mesh& mesh);

}  // namespace topo
