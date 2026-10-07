#include "rt/mesh.hpp"

extern "C" {
#include "mesh/mesh.h"
}

#include <algorithm>

namespace rt {
namespace {

// lib-c's mesh, freed on every path.
struct CMesh {
    mesh m;
    CMesh() { mesh_init(&m); }
    ~CMesh() { mesh_free(&m); }
    CMesh(const CMesh&) = delete;
    CMesh& operator=(const CMesh&) = delete;
};

TriangleMesh convert(const mesh& m) {
    if (m.triangle_count == 0) throw LoadError("mesh has no triangle", 0, 0);
    TriangleMesh out;
    out.positions.reserve(m.vertex_count);
    for (std::size_t i = 0; i < m.vertex_count; ++i) {
        out.positions.push_back({m.vertices[i].x, m.vertices[i].y, m.vertices[i].z});
    }
    out.triangles.reserve(m.triangle_count);
    for (std::size_t i = 0; i < m.triangle_count; ++i) {
        out.triangles.push_back({m.triangles[i][0], m.triangles[i][1], m.triangles[i][2]});
    }
    out.compute_normals();
    return out;
}

void check(mesh_status status, std::size_t line) {
    if (status != MESH_OK) throw LoadError(mesh_status_string(status), static_cast<int>(status), line);
}

}  // namespace

TriangleMesh TriangleMesh::read(std::string_view data) {
    CMesh c;
    std::size_t line = 0;
    // Two statements: in check(read(&line), line) the second argument may be read before the call runs.
    const mesh_status status = mesh_read_buffer(data.data(), data.size(), &c.m, &line);
    check(status, line);
    return convert(c.m);
}

TriangleMesh TriangleMesh::load(const std::string& path) {
    CMesh c;
    std::size_t line = 0;
    const mesh_status status = mesh_read_file(path.c_str(), &c.m, &line);
    check(status, line);
    return convert(c.m);
}

void TriangleMesh::compute_normals() {
    normals.assign(positions.size(), Vec3{});
    for (const auto& tri : triangles) {
        const Vec3& a = positions[tri[0]];
        // Twice the area times the unit normal: larger faces weigh more.
        const Vec3 n = cross(positions[tri[1]] - a, positions[tri[2]] - a);
        for (const uint32_t v : tri) normals[v] += dot(normals[v], n) < 0 ? -n : n;
    }
    for (auto& n : normals) {
        const double len = length(n);
        // Vertices of degenerate faces only, or none: no direction to give, the renderer uses the face's.
        n = len > 0 ? n / len : Vec3{};
    }
}

Box TriangleMesh::bounds() const {
    Box b;
    for (const auto& p : positions) b.grow(p);
    return b;
}

void TriangleMesh::fit(double radius) {
    const Box b = bounds();
    const Vec3 c = b.center();
    double r2 = 0;
    for (const auto& p : positions) {
        const Vec3 d = p - c;
        r2 = std::max(r2, dot(d, d));
    }
    const double scale = r2 > 0 ? radius / std::sqrt(r2) : 1;
    // Centre on x and z, rest the lowest point on y = 0.
    const Vec3 shift{-c.x, -b.lo.y, -c.z};
    for (auto& p : positions) p = (p + shift) * scale;
}

}  // namespace rt
