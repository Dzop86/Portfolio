#include "par/mesh.hpp"

#include <algorithm>
#include <array>
#include <cmath>
#include <numbers>

namespace par {

Adjacency build_adjacency(const Mesh& mesh)
{
    const uint32_t nv = mesh.vertex_count();
    const auto ncorners = static_cast<uint32_t>(mesh.tri.size());
    Adjacency a;
    // Counting sort of the corners by vertex: corners come in increasing order within each vertex.
    a.offsets.assign(nv + 1, 0);
    for (uint32_t v : mesh.tri)
        ++a.offsets[v + 1];
    for (uint32_t v = 0; v < nv; ++v)
        a.offsets[v + 1] += a.offsets[v];
    a.corners.resize(ncorners);
    std::vector<uint32_t> next(a.offsets.begin(), a.offsets.end() - 1);
    for (uint32_t c = 0; c < ncorners; ++c)
        a.corners[next[mesh.tri[c]]++] = c;

    // An edge used once is a boundary edge: sort the edges and count the repeats.
    std::vector<std::array<uint32_t, 2>> edges;
    edges.reserve(ncorners);
    for (uint32_t f = 0; f < mesh.face_count(); ++f)
        for (uint32_t c = 0; c < 3; ++c) {
            const uint32_t u = mesh.tri[3 * f + c], w = mesh.tri[3 * f + (c + 1) % 3];
            edges.push_back({std::min(u, w), std::max(u, w)});
        }
    std::sort(edges.begin(), edges.end());
    a.boundary.assign(nv, 0);
    for (std::size_t i = 0; i < edges.size();) {
        std::size_t j = i;
        while (j < edges.size() && edges[j] == edges[i])
            ++j;
        if (j - i == 1)
            a.boundary[edges[i][0]] = a.boundary[edges[i][1]] = 1;
        i = j;
    }
    return a;
}

Mesh torus(uint32_t rings, uint32_t segments, double major, double minor)
{
    constexpr double tau = 2 * std::numbers::pi;
    Mesh m;
    m.xyz.reserve(3 * static_cast<std::size_t>(rings) * segments);
    for (uint32_t i = 0; i < rings; ++i)
        for (uint32_t j = 0; j < segments; ++j) {
            const double u = tau * i / rings, v = tau * j / segments;
            const double r = major + minor * std::cos(v);
            m.xyz.insert(m.xyz.end(), {r * std::cos(u), r * std::sin(u), minor * std::sin(v)});
        }
    auto id = [&](uint32_t i, uint32_t j) { return (i % rings) * segments + (j % segments); };
    m.tri.reserve(6 * static_cast<std::size_t>(rings) * segments);
    for (uint32_t i = 0; i < rings; ++i)
        for (uint32_t j = 0; j < segments; ++j)
            m.tri.insert(m.tri.end(), {id(i, j), id(i + 1, j), id(i + 1, j + 1), id(i, j), id(i + 1, j + 1), id(i, j + 1)});
    return m;
}

}  // namespace par
