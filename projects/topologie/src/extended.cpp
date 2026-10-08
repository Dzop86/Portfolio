#include "topo/extended.hpp"

#include <algorithm>
#include <array>
#include <iterator>
#include <numeric>
#include <stdexcept>
#include <tuple>
#include <unordered_map>

namespace topo {

namespace {

// A simplex of the mesh: its vertices by decreasing rank (the first is the highest), padded with kNone.
struct Simplex {
    int dimension;
    std::array<uint32_t, 3> ranks;
    [[nodiscard]] uint32_t lowest() const { return ranks[static_cast<std::size_t>(dimension)]; }
};

using Column = std::vector<uint32_t>;

void add(Column& into, const Column& other) {
    Column out;
    out.reserve(into.size() + other.size());
    std::set_symmetric_difference(into.begin(), into.end(), other.begin(), other.end(), std::back_inserter(out));
    into.swap(out);
}

}  // namespace

ExtendedPersistence extended_persistence(const Mesh& mesh, const std::vector<double>& height) {
    const uint32_t n = mesh.vertex_count();
    if (height.size() != n) throw std::invalid_argument("extended_persistence: one height per vertex is needed");

    std::vector<uint32_t> order(n);
    std::iota(order.begin(), order.end(), 0u);
    std::sort(order.begin(), order.end(), [&](uint32_t a, uint32_t b) { return height[a] != height[b] ? height[a] < height[b] : a < b; });
    std::vector<uint32_t> rank(n);
    for (uint32_t r = 0; r < n; ++r) rank[order[r]] = r;

    // The mesh's simplices, as topo::persistence builds them.
    std::vector<Simplex> mesh_simplices;
    for (uint32_t r = 0; r < n; ++r) mesh_simplices.push_back({0, {r, kNone, kNone}});
    std::vector<std::array<uint32_t, 2>> edges;
    for (const auto& t : mesh.triangles()) {
        std::array<uint32_t, 3> r{rank[t[0]], rank[t[1]], rank[t[2]]};
        std::sort(r.begin(), r.end(), std::greater<>());
        mesh_simplices.push_back({2, r});
        for (std::size_t k = 0; k < 3; ++k) {
            const uint32_t a = rank[t[k]], b = rank[t[(k + 1) % 3]];
            edges.push_back({std::max(a, b), std::min(a, b)});
        }
    }
    std::sort(edges.begin(), edges.end());
    edges.erase(std::unique(edges.begin(), edges.end()), edges.end());
    for (const auto& [a, b] : edges) mesh_simplices.push_back({1, {a, b, kNone}});
    std::sort(mesh_simplices.begin(), mesh_simplices.end(), [](const Simplex& x, const Simplex& y) {
        if (x.ranks[0] != y.ranks[0]) return x.ranks[0] < y.ranks[0];
        if (x.dimension != y.dimension) return x.dimension < y.dimension;
        return x.ranks < y.ranks;
    });
    mesh_simplices.erase(std::unique(mesh_simplices.begin(), mesh_simplices.end(), [](const Simplex& x, const Simplex& y) {
        return x.dimension == y.dimension && x.ranks == y.ranks;
    }), mesh_simplices.end());
    const auto m = static_cast<uint32_t>(mesh_simplices.size());

    // The cone over each simplex, going down: by its lowest vertex, from the highest; faces first.
    std::vector<uint32_t> cone_order(m);
    std::iota(cone_order.begin(), cone_order.end(), 0u);
    std::sort(cone_order.begin(), cone_order.end(), [&](uint32_t i, uint32_t j) {
        const auto &x = mesh_simplices[i], &y = mesh_simplices[j];
        if (x.lowest() != y.lowest()) return x.lowest() > y.lowest();
        if (x.dimension != y.dimension) return x.dimension < y.dimension;
        return x.ranks > y.ranks;
    });
    // Global indices: the apex first (the oldest class, so that the mesh's components die on the cone; being the
    // smallest index and never alone in a column, it is never a pivot), then the mesh going up, then the cone going down.
    const uint32_t total = 1 + 2 * m;
    std::vector<uint32_t> cone_at(m);
    for (uint32_t p = 0; p < m; ++p) cone_at[cone_order[p]] = 1 + m + p;
    auto is_cone = [&](uint32_t g) { return g > m; };
    auto base_of = [&](uint32_t g) { return is_cone(g) ? cone_order[g - 1 - m] : g - 1; };
    auto dimension_of = [&](uint32_t g) { return g == 0 ? 0 : mesh_simplices[base_of(g)].dimension + (is_cone(g) ? 1 : 0); };

    std::vector<uint32_t> vertex_at(n);
    std::unordered_map<uint64_t, uint32_t> edge_at;
    const auto key = [](uint32_t hi, uint32_t lo) { return (uint64_t{hi} << 32) | lo; };
    for (uint32_t i = 0; i < m; ++i) {
        const auto& s = mesh_simplices[i];
        if (s.dimension == 0) vertex_at[s.ranks[0]] = i;
        else if (s.dimension == 1) edge_at.emplace(key(s.ranks[0], s.ranks[1]), i);
    }
    // Faces of a mesh simplex, as mesh indices.
    auto faces = [&](const Simplex& s) {
        std::vector<uint32_t> f;
        const auto& r = s.ranks;
        if (s.dimension == 1) f = {vertex_at[r[0]], vertex_at[r[1]]};
        if (s.dimension == 2) f = {edge_at.at(key(r[0], r[1])), edge_at.at(key(r[0], r[2])), edge_at.at(key(r[1], r[2]))};
        return f;
    };
    auto boundary = [&](uint32_t g) {
        Column c;
        const uint32_t b = base_of(g);
        if (!is_cone(g)) {
            for (const uint32_t f : faces(mesh_simplices[b])) c.push_back(1 + f);
        } else {
            // d(apex * s) = s + apex * d(s); the cone over a vertex is an edge to the apex.
            c.push_back(1 + b);
            const auto f = faces(mesh_simplices[b]);
            if (f.empty()) c.push_back(0);
            for (const uint32_t x : f) c.push_back(cone_at[x]);
        }
        std::sort(c.begin(), c.end());
        return c;
    };
    // The vertex where a simplex enters: the highest for the mesh (going up), the lowest for the cone (going down).
    auto vertex_of = [&](uint32_t g) {
        const auto& s = mesh_simplices[base_of(g)];
        return order[is_cone(g) ? s.lowest() : s.ranks[0]];
    };

    std::vector<uint32_t> pivot(total, kNone);
    std::vector<Column> reduced(total);
    std::vector<bool> cleared(total, false);
    ExtendedPersistence out;
    auto record = [&](uint32_t birth, uint32_t death) {
        const uint32_t bv = vertex_of(birth), dv = vertex_of(death);
        const ExtendedPair pair{dimension_of(birth), bv, dv, height[bv], height[dv]};
        if (!is_cone(death)) {
            if (bv != dv) out.ordinary.push_back(pair);
        } else if (!is_cone(birth)) {
            out.extended.push_back(pair);
        } else if (bv != dv) {
            // A class of H_p(M, M_{>=a}) is born by the cone over a (p - 1)-simplex, a p-simplex of the cone.
            out.relative.push_back(pair);
        }
    };

    // Dimensions 2 and 3 by cohomology (de Silva, Morozov and Vejdemo-Johansson 2011: the same pairs). The column
    // reduction of the cones over triangles filled columns with tens of thousands of entries (65 s for 300,000
    // triangles); a 2-simplex has at most two cofaces of dimension 3 (the cones over the triangles around it), so
    // its coboundary column is an edge of a graph and the reduction is a union-find with the elder rule, the
    // simplices taken in reverse order. A coface missing on one side (the mesh's own triangles, the boundary) goes
    // to a ground older than everything.
    {
        std::vector<std::vector<uint32_t>> triangles_of_edge(m);
        for (uint32_t i = 0; i < m; ++i) {
            if (mesh_simplices[i].dimension != 2) continue;
            for (const uint32_t e : faces(mesh_simplices[i])) triangles_of_edge[e].push_back(i);
        }
        const uint32_t ground = total;
        std::vector<uint32_t> parent(total + 1);
        std::iota(parent.begin(), parent.end(), 0u);
        auto find = [&](uint32_t x) {
            while (parent[x] != x) x = parent[x] = parent[parent[x]];
            return x;
        };
        // Going backwards, the oldest node is the highest global index, and the ground beats all. The younger root
        // always goes under the older one, so a root is the oldest node of its component.
        auto older = [&](uint32_t a, uint32_t b) { return a == ground || (b != ground && a > b); };
        for (uint32_t j = total; j-- > 1;) {
            if (dimension_of(j) != 2) continue;
            std::vector<uint32_t> cofaces;
            const uint32_t b = base_of(j);
            if (!is_cone(j)) {
                cofaces.push_back(cone_at[b]);
            } else {
                for (const uint32_t t : triangles_of_edge[b]) cofaces.push_back(cone_at[t]);
            }
            if (cofaces.size() < 2) cofaces.push_back(ground);
            const uint32_t ra = find(cofaces[0]), rb = find(cofaces[1]);
            if (ra == rb) continue;
            // The younger component dies: its root, its oldest 3-simplex, kills the class born by j.
            const uint32_t young = older(ra, rb) ? rb : ra;
            parent[young] = young == ra ? rb : ra;
            pivot[j] = young;
            cleared[j] = true;
            record(j, young);
        }
    }
    for (const int dimension : {2, 1}) {
        for (uint32_t j = 0; j < total; ++j) {
            if (j == 0 || cleared[j] || dimension_of(j) != dimension) continue;
            Column c = boundary(j);
            while (!c.empty() && pivot[c.back()] != kNone) add(c, reduced[pivot[c.back()]]);
            if (c.empty()) continue;
            const uint32_t low = c.back();
            pivot[low] = j;
            cleared[low] = true;
            reduced[j] = std::move(c);
            record(low, j);
        }
    }
    auto by_dimension = [](std::vector<ExtendedPair>& list) {
        std::sort(list.begin(), list.end(), [](const ExtendedPair& a, const ExtendedPair& b) {
            return std::tie(a.dimension, a.birth, a.death, a.birth_vertex, a.death_vertex) <
                   std::tie(b.dimension, b.birth, b.death, b.birth_vertex, b.death_vertex);
        });
    };
    by_dimension(out.ordinary);
    by_dimension(out.extended);
    by_dimension(out.relative);
    return out;
}

}  // namespace topo
