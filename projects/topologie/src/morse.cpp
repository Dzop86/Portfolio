#include "topo/morse.hpp"

#include <algorithm>
#include <array>
#include <cmath>
#include <numeric>
#include <stdexcept>
#include <utility>

namespace topo {

Elevation elevation(const Mesh& mesh, Vec3 direction) {
    const double norm = std::sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);
    if (!(norm > 0)) throw std::invalid_argument("elevation: the direction must not be zero");
    const Vec3 d{direction.x / norm, direction.y / norm, direction.z / norm};

    const uint32_t n = mesh.vertex_count();
    Elevation e;
    e.height.reserve(n);
    for (const auto& p : mesh.positions()) e.height.push_back(p.x * d.x + p.y * d.y + p.z * d.z);

    // Filtration order: height, then index for equal heights.
    e.order.resize(n);
    std::iota(e.order.begin(), e.order.end(), 0u);
    std::sort(e.order.begin(), e.order.end(), [&](uint32_t a, uint32_t b) {
        return e.height[a] != e.height[b] ? e.height[a] < e.height[b] : a < b;
    });
    e.rank.resize(n);
    for (uint32_t r = 0; r < n; ++r) e.rank[e.order[r]] = r;
    const auto below = [&](uint32_t a, uint32_t b) { return e.rank[a] < e.rank[b]; };

    // Link of each vertex: its neighbours, and the edge opposite to it in each of its triangles. An edge
    // shared by two triangles around the same vertex cannot happen (it would repeat the vertex), so the
    // link edges of a vertex are distinct unless two triangles are identical; they are deduplicated.
    std::vector<std::vector<uint32_t>> neighbours(n);
    std::vector<std::vector<std::array<uint32_t, 2>>> link_edges(n);
    for (const auto& t : mesh.triangles()) {
        for (std::size_t k = 0; k < 3; ++k) {
            const uint32_t v = t[k], a = t[(k + 1) % 3], b = t[(k + 2) % 3];
            neighbours[v].push_back(a);
            neighbours[v].push_back(b);
            link_edges[v].push_back({std::min(a, b), std::max(a, b)});
        }
    }

    // Sublevel Euler characteristic, vertex by vertex: a vertex brings itself, the edges to its lower
    // neighbours and the faces whose two other vertices are lower; that is 1 - chi(lower link).
    e.euler.resize(n);
    int chi = 0;
    for (uint32_t r = 0; r < n; ++r) {
        const uint32_t v = e.order[r];
        auto& nb = neighbours[v];
        std::sort(nb.begin(), nb.end());
        nb.erase(std::unique(nb.begin(), nb.end()), nb.end());
        auto& le = link_edges[v];
        std::sort(le.begin(), le.end());
        le.erase(std::unique(le.begin(), le.end()), le.end());

        int lower_vertices = 0, lower_edges = 0;
        for (const uint32_t w : nb) lower_vertices += below(w, v) ? 1 : 0;
        for (const auto& [a, b] : le) lower_edges += below(a, v) && below(b, v) ? 1 : 0;
        const int index = 1 - (lower_vertices - lower_edges);
        chi += index;
        e.euler[r] = chi;

        if (index == 0) continue;
        CriticalKind kind = CriticalKind::Other;
        if (lower_vertices == 0) kind = CriticalKind::Minimum;
        else if (index < 0) kind = CriticalKind::Saddle;
        else if (lower_vertices == static_cast<int>(nb.size())) kind = CriticalKind::Maximum;
        e.critical.push_back({v, kind, index});
    }
    return e;
}

}  // namespace topo
