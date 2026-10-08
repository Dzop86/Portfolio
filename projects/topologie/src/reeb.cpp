#include "topo/reeb.hpp"

#include <algorithm>
#include <array>
#include <cmath>
#include <numeric>
#include <stdexcept>
#include <utility>

namespace topo {

namespace {

// Union-find with path halving.
struct Sets {
    std::vector<uint32_t> parent;
    explicit Sets(std::size_t n) : parent(n) { std::iota(parent.begin(), parent.end(), 0u); }
    uint32_t find(uint32_t x) {
        while (parent[x] != x) x = parent[x] = parent[parent[x]];
        return x;
    }
    void join(uint32_t a, uint32_t b) { parent[find(a)] = find(b); }
};

uint64_t key(uint32_t a, uint32_t b) { return (uint64_t{std::min(a, b)} << 32) | std::max(a, b); }

// Pieces of the link of `v` among its neighbours of rank below (lower) or above (upper) it.
std::pair<int, int> link_pieces(uint32_t v, const std::vector<uint32_t>& neighbours, const std::vector<std::array<uint32_t, 2>>& link,
                                const std::vector<uint32_t>& rank) {
    Sets sets(neighbours.size());
    auto at = [&](uint32_t w) {
        return static_cast<uint32_t>(std::lower_bound(neighbours.begin(), neighbours.end(), w) - neighbours.begin());
    };
    for (const auto& [a, b] : link) {
        if ((rank[a] < rank[v]) == (rank[b] < rank[v])) sets.join(at(a), at(b));
    }
    int lower = 0, upper = 0;
    for (uint32_t k = 0; k < neighbours.size(); ++k) {
        if (sets.find(k) != k) continue;
        (rank[neighbours[k]] < rank[v] ? lower : upper) += 1;
    }
    return {lower, upper};
}

}  // namespace

ReebGraph reeb_graph(const Mesh& mesh, const Elevation& e, uint32_t samples, uint32_t max_nodes) {
    const uint32_t n = mesh.vertex_count(), t_count = mesh.face_count();
    if (e.rank.size() != n) throw std::invalid_argument("reeb_graph: the elevation is not of this mesh");
    const auto& tris = mesh.triangles();
    const auto& pos = mesh.positions();
    const auto& rank = e.rank;

    // Links, as in topo::elevation.
    std::vector<std::vector<uint32_t>> neighbours(n), around(n);
    std::vector<std::vector<std::array<uint32_t, 2>>> link(n);
    for (uint32_t t = 0; t < t_count; ++t) {
        for (std::size_t k = 0; k < 3; ++k) {
            const uint32_t v = tris[t][k], a = tris[t][(k + 1) % 3], b = tris[t][(k + 2) % 3];
            neighbours[v].insert(neighbours[v].end(), {a, b});
            link[v].push_back({a, b});
            around[v].push_back(t);
        }
    }

    // Cuts: the nodes (link not in one piece below or above), then regular heights for the drawing.
    ReebGraph g;
    std::vector<uint32_t> cut;          // ranks, increasing
    std::vector<uint32_t> cut_node;     // node index at each cut, or kNone for a sampled height
    std::vector<uint32_t> node_at(n, kNone);
    for (uint32_t r = 0; r < n; ++r) {
        const uint32_t v = e.order[r];
        auto& nb = neighbours[v];
        std::sort(nb.begin(), nb.end());
        nb.erase(std::unique(nb.begin(), nb.end()), nb.end());
        const auto [lower, upper] = link_pieces(v, nb, link[v], rank);
        if (lower != 1 || upper != 1) {
            node_at[r] = static_cast<uint32_t>(g.nodes.size());
            g.nodes.push_back({v});
        }
    }
    if (g.nodes.size() > max_nodes) throw std::length_error("reeb_graph: more nodes than asked for");
    for (uint32_t s = 1; s <= samples && n > 1; ++s) {
        const auto r = static_cast<uint32_t>(std::lround(double(s) * (n - 1) / (samples + 1)));
        if (node_at[r] == kNone) node_at[r] = kNone - 1;  // marks a sampled height
    }
    for (uint32_t r = 0; r < n; ++r) {
        if (node_at[r] == kNone) continue;
        cut.push_back(r);
        cut_node.push_back(node_at[r] == kNone - 1 ? kNone : node_at[r]);
    }

    // Slabs: between cut i - 1 and cut i (i = 0..K). A triangle with ranks a < b < c meets slab i when
    // cut[i - 1] < c and cut[i] > a; one element per (triangle, slab).
    std::vector<std::array<uint32_t, 3>> tr(t_count);  // ranks, increasing
    std::vector<uint32_t> first(t_count), offset(t_count + 1, 0);
    for (uint32_t t = 0; t < t_count; ++t) {
        tr[t] = {rank[tris[t][0]], rank[tris[t][1]], rank[tris[t][2]]};
        std::sort(tr[t].begin(), tr[t].end());
        first[t] = static_cast<uint32_t>(std::upper_bound(cut.begin(), cut.end(), tr[t][0]) - cut.begin());
        const auto last = static_cast<uint32_t>(std::lower_bound(cut.begin(), cut.end(), tr[t][2]) - cut.begin());
        offset[t + 1] = offset[t] + (last - first[t] + 1);
    }
    auto element = [&](uint32_t t, uint32_t slab) { return offset[t] + (slab - first[t]); };
    auto has = [&](uint32_t t, uint32_t slab) { return slab >= first[t] && element(t, slab) < offset[t + 1]; };

    // Pieces: elements of one slab glued across the edges that cross the slab. Edges get an id on the way
    // (edge_id[3t + k] for the edge from corner k of triangle t), for the level sets below.
    Sets elements(offset[t_count]);
    std::vector<uint32_t> edge_id(3 * std::size_t{t_count});
    std::vector<std::array<uint32_t, 2>> edge_ends;
    {
        std::vector<std::pair<uint64_t, uint32_t>> edges;
        edges.reserve(3 * std::size_t{t_count});
        for (uint32_t t = 0; t < t_count; ++t) {
            for (uint32_t k = 0; k < 3; ++k) edges.emplace_back(key(tris[t][k], tris[t][(k + 1) % 3]), 3 * t + k);
        }
        std::sort(edges.begin(), edges.end());
        for (std::size_t i = 0; i < edges.size();) {
            std::size_t j = i;
            while (j < edges.size() && edges[j].first == edges[i].first) ++j;
            for (std::size_t k = i; k < j; ++k) edge_id[edges[k].second] = static_cast<uint32_t>(edge_ends.size());
            edge_ends.push_back({static_cast<uint32_t>(edges[i].first >> 32), static_cast<uint32_t>(edges[i].first & 0xffffffffu)});
            if (j - i > 1) {
                const auto a = static_cast<uint32_t>(edges[i].first >> 32), b = static_cast<uint32_t>(edges[i].first & 0xffffffffu);
                const uint32_t lo = std::min(rank[a], rank[b]), hi = std::max(rank[a], rank[b]);
                const auto s0 = static_cast<uint32_t>(std::upper_bound(cut.begin(), cut.end(), lo) - cut.begin());
                const auto s1 = static_cast<uint32_t>(std::lower_bound(cut.begin(), cut.end(), hi) - cut.begin());
                for (uint32_t s = s0; s <= s1; ++s) {
                    for (std::size_t k = i + 1; k < j; ++k) elements.join(element(edges[k].second / 3, s), element(edges[i].second / 3, s));
                }
            }
            i = j;
        }
    }
    std::vector<uint32_t> piece_of(offset[t_count], kNone);
    uint32_t pieces = 0;
    for (uint32_t x = 0; x < offset[t_count]; ++x) {
        const uint32_t root = elements.find(x);
        if (piece_of[root] == kNone) piece_of[root] = pieces++;
        piece_of[x] = piece_of[root];
    }
    std::vector<uint32_t> bottom(pieces, kNone), top(pieces, kNone), next(pieces, kNone);
    std::vector<Vec3> point(pieces);  // where a piece meets the next one: a level set centroid

    // Level sets at each cut, swept upwards: the triangles strictly across the cut, and those of its vertex.
    std::vector<uint32_t> by_bottom(t_count);
    std::iota(by_bottom.begin(), by_bottom.end(), 0u);
    std::sort(by_bottom.begin(), by_bottom.end(), [&](uint32_t a, uint32_t b) { return tr[a][0] < tr[b][0]; });
    std::vector<uint32_t> active, local(t_count, kNone), first_at(edge_ends.size(), kNone);
    // Buffers reused from one cut to the next: a cut costs the size of its level set, not of the mesh.
    std::vector<uint32_t> members, touched, count, lo_piece, hi_piece, node_below, node_above;
    std::vector<uint8_t> lo_many, hi_many;
    std::vector<Vec3> sum;
    Sets level(0);
    std::size_t entered = 0;
    for (uint32_t j = 0; j < cut.size(); ++j) {
        const uint32_t r = cut[j], v = e.order[r];
        const double h = e.height[v];
        while (entered < by_bottom.size() && tr[by_bottom[entered]][0] < r) active.push_back(by_bottom[entered++]);
        std::erase_if(active, [&](uint32_t t) { return tr[t][2] <= r; });

        // Local elements: 0 is the vertex, then the triangles.
        members.assign(active.begin(), active.end());
        for (const uint32_t t : around[v]) {
            if (tr[t][1] != r) members.push_back(t);  // the middle ones are already active
        }
        const std::size_t m = members.size() + 1;
        for (uint32_t k = 0; k < members.size(); ++k) local[members[k]] = k + 1;
        level.parent.resize(m);
        std::iota(level.parent.begin(), level.parent.end(), 0u);
        for (const uint32_t t : around[v]) level.join(local[t], 0);
        // Triangles holding the same edge across the cut are in the same level set component.
        touched.clear();
        for (const uint32_t t : active) {
            for (uint32_t k = 0; k < 3; ++k) {
                const uint32_t id = edge_id[3 * t + k];
                const auto [a, b] = edge_ends[id];
                if ((rank[a] < r) == (rank[b] < r) || rank[a] == r || rank[b] == r) continue;
                if (first_at[id] == kNone) {
                    first_at[id] = local[t];
                    touched.push_back(id);
                } else {
                    level.join(local[t], first_at[id]);
                }
            }
        }
        // Centroid of each component: where its edges cross the cut, and the vertex.
        sum.assign(m, Vec3{0, 0, 0});
        count.assign(m, 0);
        auto add_point = [&](uint32_t root, Vec3 p) {
            sum[root] = {sum[root].x + p.x, sum[root].y + p.y, sum[root].z + p.z};
            ++count[root];
        };
        add_point(level.find(0), pos[v]);
        for (const uint32_t id : touched) {
            const auto [a, b] = edge_ends[id];
            const double ha = e.height[a], hb = e.height[b];
            const double s = hb != ha ? (h - ha) / (hb - ha) : 0.5;
            add_point(level.find(first_at[id]),
                      {pos[a].x + s * (pos[b].x - pos[a].x), pos[a].y + s * (pos[b].y - pos[a].y), pos[a].z + s * (pos[b].z - pos[a].z)});
            first_at[id] = kNone;
        }

        // The pieces each component touches, below (slab j) and above (slab j + 1): one each for a regular
        // component (kept with a flag if there are more), all of them for the node's.
        const uint32_t vertex_root = level.find(0);
        const bool node = cut_node[j] != kNone;
        lo_piece.assign(m, kNone);
        hi_piece.assign(m, kNone);
        lo_many.assign(m, 0);
        hi_many.assign(m, 0);
        node_below.clear();
        node_above.clear();
        auto touch = [&](std::vector<uint32_t>& piece, std::vector<uint8_t>& many, std::vector<uint32_t>& at_node, uint32_t root, uint32_t p) {
            if (node && root == vertex_root) at_node.push_back(p);
            else if (piece[root] == kNone) piece[root] = p;
            else if (piece[root] != p) many[root] = 1;
        };
        for (const uint32_t t : members) {
            const uint32_t root = level.find(local[t]);
            if (tr[t][0] < r && has(t, j)) touch(lo_piece, lo_many, node_below, root, piece_of[element(t, j)]);
            if (tr[t][2] > r && has(t, j + 1)) touch(hi_piece, hi_many, node_above, root, piece_of[element(t, j + 1)]);
        }
        for (const uint32_t p : node_below) top[p] = cut_node[j];
        for (const uint32_t p : node_above) bottom[p] = cut_node[j];
        for (uint32_t root = 0; root < m; ++root) {
            if (level.find(root) != root || (node && root == vertex_root)) continue;
            // A regular level set component: one piece below goes on as one piece above.
            if (lo_piece[root] == kNone || hi_piece[root] == kNone || lo_many[root] || hi_many[root]) {
                throw std::logic_error("reeb_graph: a level set changed away from a node");
            }
            next[lo_piece[root]] = hi_piece[root];
            point[lo_piece[root]] = {sum[root].x / count[root], sum[root].y / count[root], sum[root].z / count[root]};
        }
        for (const uint32_t t : members) local[t] = kNone;
    }

    // Arcs: from a piece that starts at a node, follow the pieces up to the node where it ends.
    for (uint32_t p = 0; p < pieces; ++p) {
        if (bottom[p] == kNone) continue;
        ReebArc arc{bottom[p], kNone, {}};
        uint32_t q = p;
        while (top[q] == kNone) {
            if (next[q] == kNone) throw std::logic_error("reeb_graph: an arc ends away from a node");
            arc.path.push_back(point[q]);
            q = next[q];
        }
        arc.upper = top[q];
        g.nodes[arc.lower].up += 1;
        g.nodes[arc.upper].down += 1;
        g.arcs.push_back(std::move(arc));
    }
    Sets graph(g.nodes.size());
    for (const auto& a : g.arcs) graph.join(a.lower, a.upper);
    for (uint32_t k = 0; k < g.nodes.size(); ++k) g.components += graph.find(k) == k ? 1 : 0;
    return g;
}

}  // namespace topo
