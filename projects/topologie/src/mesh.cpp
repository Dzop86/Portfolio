#include "topo/mesh.hpp"

#include "mesh/mesh.h"

#include <algorithm>

namespace topo {

namespace {

// Owns a lib-c mesh for the duration of a conversion.
struct CMesh {
    mesh m;
    CMesh() { mesh_init(&m); }
    ~CMesh() { mesh_free(&m); }
    CMesh(const CMesh&) = delete;
    CMesh& operator=(const CMesh&) = delete;
};

Mesh from_c(const mesh& m) {
    std::vector<Vec3> positions(m.vertex_count);
    for (std::size_t i = 0; i < m.vertex_count; ++i)
        positions[i] = {m.vertices[i].x, m.vertices[i].y, m.vertices[i].z};
    std::vector<Triangle> triangles(m.triangle_count);
    for (std::size_t i = 0; i < m.triangle_count; ++i)
        triangles[i] = {m.triangles[i][0], m.triangles[i][1], m.triangles[i][2]};
    return Mesh(std::move(positions), std::move(triangles));
}

void check(mesh_status status, std::size_t line) {
    if (status != MESH_OK) throw LoadError(mesh_status_string(status), static_cast<int>(status), line);
}

}  // namespace

Mesh::Mesh(std::vector<Vec3> positions, std::vector<Triangle> triangles)
    : positions_(std::move(positions)), triangles_(std::move(triangles)) {
    if (triangles_.size() > kNone / 3) throw std::invalid_argument("too many triangles");
    const auto n = static_cast<uint32_t>(triangles_.size());
    half_edges_.resize(3 * std::size_t{n});

    // Half-edges keyed by their undirected edge, then sorted so that each edge's half-edges are adjacent.
    struct Keyed {
        uint64_t key;
        uint32_t h;
    };
    std::vector<Keyed> keyed(half_edges_.size());
    for (uint32_t f = 0; f < n; ++f) {
        const Triangle& t = triangles_[f];
        for (uint32_t k = 0; k < 3; ++k) {
            if (t[k] >= positions_.size()) throw std::invalid_argument("triangle index out of range");
            if (t[k] == t[(k + 1) % 3]) throw std::invalid_argument("degenerate triangle");
            const uint32_t h = 3 * f + k;
            half_edges_[h] = {t[k], 3 * f + (k + 1) % 3, kNone, f, false};
            const uint32_t a = std::min(t[k], t[(k + 1) % 3]), b = std::max(t[k], t[(k + 1) % 3]);
            keyed[h] = {(uint64_t{a} << 32) | b, h};
        }
    }
    std::ranges::sort(keyed, {}, &Keyed::key);

    for (std::size_t i = 0; i < keyed.size();) {
        std::size_t j = i + 1;
        while (j < keyed.size() && keyed[j].key == keyed[i].key) ++j;
        ++edge_count_;
        if (j - i == 2) {
            HalfEdge& p = half_edges_[keyed[i].h];
            HalfEdge& q = half_edges_[keyed[i + 1].h];
            p.twin = keyed[i + 1].h;
            q.twin = keyed[i].h;
            p.flipped = q.flipped = p.origin == q.origin;
        } else if (j - i > 2) {
            const uint64_t key = keyed[i].key;
            non_manifold_edges_.push_back({static_cast<uint32_t>(key >> 32), static_cast<uint32_t>(key)});
        }
        i = j;
    }
}

Mesh Mesh::load(const std::filesystem::path& path) {
    CMesh c;
    std::size_t line = 0;
    // Two statements: argument evaluation order is unspecified, `line` must be read after the call.
    const mesh_status status = mesh_read_file(path.string().c_str(), &c.m, &line);
    check(status, line);
    return from_c(c.m);
}

Mesh Mesh::parse(std::string_view data) {
    CMesh c;
    std::size_t line = 0;
    // Two statements: argument evaluation order is unspecified, `line` must be read after the call.
    const mesh_status status = mesh_read_buffer(data.data(), data.size(), &c.m, &line);
    check(status, line);
    return from_c(c.m);
}

}  // namespace topo
