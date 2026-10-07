#include "topo/persistence.hpp"

#include <algorithm>
#include <array>
#include <iterator>
#include <limits>
#include <numeric>
#include <stdexcept>
#include <tuple>
#include <unordered_map>

namespace topo {

namespace {

// A simplex of the filtration: its vertices sorted by decreasing rank (the first is the highest, the one
// that brings it in), padded with kNone.
struct Simplex {
    int dimension;
    std::array<uint32_t, 3> ranks;  // ranks of its vertices, decreasing
};

// Z/2 column: sorted indices of the boundary; adding two columns is their symmetric difference.
using Column = std::vector<uint32_t>;

void add(Column& into, const Column& other) {
    Column out;
    out.reserve(into.size() + other.size());
    std::set_symmetric_difference(into.begin(), into.end(), other.begin(), other.end(), std::back_inserter(out));
    into.swap(out);
}

}  // namespace

Persistence persistence(const Mesh& mesh, const std::vector<double>& height) {
    const uint32_t n = mesh.vertex_count();
    if (height.size() != n) throw std::invalid_argument("persistence: one height per vertex is needed");

    // Ranks as in topo::elevation: height, then index.
    std::vector<uint32_t> order(n);
    std::iota(order.begin(), order.end(), 0u);
    std::sort(order.begin(), order.end(), [&](uint32_t a, uint32_t b) { return height[a] != height[b] ? height[a] < height[b] : a < b; });
    std::vector<uint32_t> rank(n);
    for (uint32_t r = 0; r < n; ++r) rank[order[r]] = r;

    // Simplices, by ranks: vertices, edges (from the triangles), triangles.
    std::vector<Simplex> simplices;
    simplices.reserve(n + 3 * std::size_t{mesh.face_count()});
    for (uint32_t r = 0; r < n; ++r) simplices.push_back({0, {r, kNone, kNone}});
    auto sorted = [](std::array<uint32_t, 3> r) {
        std::sort(r.begin(), r.end(), [](uint32_t a, uint32_t b) {
            return a == kNone ? false : b == kNone ? true : a > b;
        });
        return r;
    };
    std::vector<std::array<uint32_t, 2>> edges;
    for (const auto& t : mesh.triangles()) {
        for (std::size_t k = 0; k < 3; ++k) {
            const uint32_t a = rank[t[k]], b = rank[t[(k + 1) % 3]];
            edges.push_back({std::max(a, b), std::min(a, b)});
        }
        simplices.push_back({2, sorted({rank[t[0]], rank[t[1]], rank[t[2]]})});
    }
    std::sort(edges.begin(), edges.end());
    edges.erase(std::unique(edges.begin(), edges.end()), edges.end());
    for (const auto& [a, b] : edges) simplices.push_back({1, {a, b, kNone}});

    // Filtration: by highest vertex (lower star), then dimension (a face after its edges), then the other
    // ranks, highest first. Identical triangles collapse into one.
    std::sort(simplices.begin(), simplices.end(), [](const Simplex& x, const Simplex& y) {
        if (x.ranks[0] != y.ranks[0]) return x.ranks[0] < y.ranks[0];
        if (x.dimension != y.dimension) return x.dimension < y.dimension;
        return x.ranks < y.ranks;
    });
    simplices.erase(std::unique(simplices.begin(), simplices.end(), [](const Simplex& x, const Simplex& y) {
        return x.dimension == y.dimension && x.ranks == y.ranks;
    }), simplices.end());

    // Index of each vertex and edge in the filtration, for the boundaries.
    const auto m = static_cast<uint32_t>(simplices.size());
    std::vector<uint32_t> vertex_at(n);
    std::unordered_map<uint64_t, uint32_t> edge_at;
    edge_at.reserve(edges.size());
    const auto key = [](uint32_t hi, uint32_t lo) { return (uint64_t{hi} << 32) | lo; };
    for (uint32_t i = 0; i < m; ++i) {
        const auto& s = simplices[i];
        if (s.dimension == 0) vertex_at[s.ranks[0]] = i;
        else if (s.dimension == 1) edge_at.emplace(key(s.ranks[0], s.ranks[1]), i);
    }
    auto boundary = [&](const Simplex& s) {
        Column c;
        if (s.dimension == 1) {
            c = {vertex_at[s.ranks[0]], vertex_at[s.ranks[1]]};
        } else {
            const auto& r = s.ranks;
            c = {edge_at.at(key(r[0], r[1])), edge_at.at(key(r[0], r[2])), edge_at.at(key(r[1], r[2]))};
        }
        std::sort(c.begin(), c.end());
        return c;
    };

    // Reduction, faces first, with clearing. pivot[low] = the column whose lowest entry is `low`.
    std::vector<uint32_t> pivot(m, kNone);
    std::vector<Column> reduced(m);
    std::vector<bool> cleared(m, false), paired(m, false);
    Persistence out;
    auto vertex_of = [&](uint32_t simplex) { return order[simplices[simplex].ranks[0]]; };
    auto record = [&](int dimension, uint32_t birth, uint32_t death) {
        const uint32_t bv = vertex_of(birth), dv = death == kNone ? kNone : vertex_of(death);
        if (bv == dv) return;  // born and killed by the same vertex: not a feature
        out.pairs.push_back({dimension, bv, dv, height[bv], dv == kNone ? std::numeric_limits<double>::infinity() : height[dv]});
    };
    for (const int dimension : {2, 1}) {
        for (uint32_t j = 0; j < m; ++j) {
            if (simplices[j].dimension != dimension || cleared[j]) continue;
            Column c = boundary(simplices[j]);
            while (!c.empty() && pivot[c.back()] != kNone) add(c, reduced[pivot[c.back()]]);
            if (c.empty()) continue;  // a positive simplex: it creates a class
            const uint32_t low = c.back();
            pivot[low] = j;
            paired[low] = paired[j] = true;
            cleared[low] = true;  // the column of `low` would reduce to zero
            record(dimension - 1, low, j);
            reduced[j] = std::move(c);
        }
    }
    // Unpaired simplices are the essential classes. A cleared or zero column that kills nothing.
    for (uint32_t i = 0; i < m; ++i) {
        if (paired[i]) continue;
        const int dimension = simplices[i].dimension;
        out.betti[static_cast<std::size_t>(dimension)] += 1;
        record(dimension, i, kNone);
    }

    // A total order (the vertices break the last ties), so that the output does not depend on the sort.
    std::sort(out.pairs.begin(), out.pairs.end(), [](const PersistencePair& a, const PersistencePair& b) {
        if (a.essential() != b.essential()) return b.essential();
        if (a.essential() && a.dimension != b.dimension) return a.dimension < b.dimension;
        if (a.birth != b.birth) return a.birth < b.birth;
        if (a.death != b.death) return a.death < b.death;
        return std::tie(a.dimension, a.birth_vertex, a.death_vertex) < std::tie(b.dimension, b.birth_vertex, b.death_vertex);
    });
    return out;
}

}  // namespace topo
