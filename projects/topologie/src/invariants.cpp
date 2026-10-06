#include "topo/invariants.hpp"

#include <algorithm>
#include <numeric>
#include <queue>

namespace topo {

namespace {

class UnionFind {
public:
    explicit UnionFind(std::size_t n) : parent_(n) { std::iota(parent_.begin(), parent_.end(), uint32_t{0}); }

    uint32_t find(uint32_t x) {
        while (parent_[x] != x) x = parent_[x] = parent_[parent_[x]];  // path halving
        return x;
    }
    void unite(uint32_t a, uint32_t b) { parent_[find(a)] = find(b); }

private:
    std::vector<uint32_t> parent_;
};

// Corner of face f at vertex v (its position in the triangle).
uint32_t corner(const Mesh& m, uint32_t f, uint32_t v) {
    const Triangle& t = m.triangles()[f];
    return 3 * f + (t[0] == v ? 0u : t[1] == v ? 1u : 2u);
}

}  // namespace

Invariants analyze(const Mesh& m) {
    Invariants inv;
    const auto& he = m.half_edges();
    const uint32_t nv = m.vertex_count(), nf = m.face_count();

    // Components and isolated vertices: union-find on vertices through the triangles.
    UnionFind vertices(nv);
    std::vector<bool> used(nv, false);
    for (const Triangle& t : m.triangles()) {
        for (uint32_t v : t) used[v] = true;
        vertices.unite(t[0], t[1]);
        vertices.unite(t[1], t[2]);
    }
    for (uint32_t v = 0; v < nv; ++v) {
        if (vertices.find(v) == v) ++inv.components;
        if (!used[v]) ++inv.isolated_vertices;
    }

    // Boundary loops: components of the graph made of boundary edges (single-face edges).
    UnionFind boundary(nv);
    std::vector<bool> on_boundary(nv, false);
    // non_manifold_edges() is sorted, as it is built from sorted edge keys.
    auto non_manifold = [&](uint32_t h) {
        const uint32_t a = std::min(he[h].origin, m.target(h)), b = std::max(he[h].origin, m.target(h));
        return std::ranges::binary_search(m.non_manifold_edges(), std::array<uint32_t, 2>{a, b});
    };
    for (uint32_t h = 0; h < he.size(); ++h) {
        if (he[h].twin != kNone || non_manifold(h)) continue;
        boundary.unite(he[h].origin, m.target(h));
        on_boundary[he[h].origin] = on_boundary[m.target(h)] = true;
    }
    for (uint32_t v = 0; v < nv; ++v)
        if (on_boundary[v] && boundary.find(v) == v) ++inv.boundary_loops;

    // Vertex manifoldness: around each vertex, corners joined across twinned edges must form one fan.
    UnionFind corners(he.size());
    for (uint32_t h = 0; h < he.size(); ++h) {
        const uint32_t t = he[h].twin;
        if (t == kNone || t < h) continue;
        for (uint32_t v : {he[h].origin, m.target(h)}) corners.unite(corner(m, he[h].face, v), corner(m, he[t].face, v));
    }
    std::vector<uint32_t> fans(nv, 0);
    for (uint32_t c = 0; c < he.size(); ++c)
        if (corners.find(c) == c) ++fans[m.triangles()[c / 3][c % 3]];
    for (uint32_t v = 0; v < nv; ++v)
        if (fans[v] > 1) ++inv.non_manifold_vertices;

    inv.non_manifold_edges = m.non_manifold_edges().size();
    inv.manifold = inv.non_manifold_edges == 0 && inv.non_manifold_vertices == 0;

    // Orientability: give each face a sign, flipping it across every `flipped` edge; a conflict means
    // no consistent orientation exists.
    std::vector<int8_t> sign(nf, 0);
    for (uint32_t start = 0; start < nf; ++start) {
        if (sign[start] != 0) continue;
        sign[start] = 1;
        std::queue<uint32_t> todo;
        todo.push(start);
        while (!todo.empty()) {
            const uint32_t f = todo.front();
            todo.pop();
            for (uint32_t h = 3 * f; h < 3 * f + 3; ++h) {
                const uint32_t t = he[h].twin;
                if (t == kNone) continue;
                if (he[h].flipped) inv.consistently_oriented = false;
                const int8_t expected = he[h].flipped ? static_cast<int8_t>(-sign[f]) : sign[f];
                const uint32_t g = he[t].face;
                if (sign[g] == 0) {
                    sign[g] = expected;
                    todo.push(g);
                } else if (sign[g] != expected) {
                    inv.orientable = false;
                }
            }
        }
    }

    inv.euler_characteristic = int64_t{nv} - static_cast<int64_t>(m.edge_count()) + int64_t{nf};
    if (inv.manifold && inv.orientable) {
        // Isolated vertices are components with chi = 1 but are not surfaces: leave them out.
        const auto c = static_cast<int64_t>(inv.components - inv.isolated_vertices);
        const int64_t chi = inv.euler_characteristic - static_cast<int64_t>(inv.isolated_vertices);
        inv.genus = (2 * c - static_cast<int64_t>(inv.boundary_loops) - chi) / 2;
    }
    return inv;
}

}  // namespace topo
