// Height along a direction and its critical points, by piecewise-linear Morse theory (Banchoff 1967):
// the sublevel sets of the height, grown vertex by vertex, change topology only at critical vertices.
#pragma once

#include "topo/mesh.hpp"

#include <cstdint>
#include <vector>

namespace topo {

enum class CriticalKind { Minimum, Saddle, Maximum, Other };

struct CriticalPoint {
    uint32_t vertex;
    CriticalKind kind;
    // What the vertex adds to the Euler characteristic of the sublevel set: +1 for a minimum or an
    // interior maximum, 1 - k for a saddle whose lower link has k pieces (a monkey saddle: -2). `Other`
    // only happens at non-manifold vertices, where the link is not a single loop or path.
    int index;
};

struct Elevation {
    std::vector<double> height;    // per vertex: dot(position, direction)
    std::vector<uint32_t> rank;    // per vertex: its place in the filtration, 0 = lowest
    std::vector<uint32_t> order;   // the vertices from lowest to highest (order[rank[v]] == v)
    std::vector<CriticalPoint> critical;  // in the order of the filtration
    // Euler characteristic of the sublevel set after each vertex (the vertices up to that rank, with the
    // edges and faces whose highest vertex is among them): euler[order.size() - 1] is the mesh's chi.
    std::vector<int> euler;
};

// O(V log V + T). Equal heights are ordered by vertex index (simulation of simplicity), so that every
// direction gives a valid filtration. A vertex is critical when its lower link (the neighbours below it
// and the link edges between them) does not have the Euler characteristic of a point: then the index
// 1 - chi(lower link) is not 0. The indices sum to the Euler characteristic of the mesh.
// Throws std::invalid_argument if the direction is zero.
[[nodiscard]] Elevation elevation(const Mesh& mesh, Vec3 direction = {0, 1, 0});

}  // namespace topo
