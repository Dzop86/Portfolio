// The two passes, written once and called by the sequential and the OpenMP versions: the same
// operations in the same order give the same doubles, bit for bit. The formulas follow
// topologie/src/curvature.cpp (Meyer et al. 2003 for the mixed Voronoi area).
#pragma once

#include "par/mesh.hpp"

#include <cmath>
#include <numbers>

namespace par::detail {

struct V3 {
    double x, y, z;
};
inline V3 point(const Mesh& m, uint32_t v) { return {m.xyz[3 * v], m.xyz[3 * v + 1], m.xyz[3 * v + 2]}; }
inline V3 operator-(V3 a, V3 b) { return {a.x - b.x, a.y - b.y, a.z - b.z}; }
inline double dot(V3 a, V3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
inline V3 cross(V3 a, V3 b) { return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x}; }
inline double norm(V3 a) { return std::sqrt(dot(a, a)); }
// atan2 stays accurate near 0 and pi, unlike acos.
inline double angle(V3 u, V3 v) { return std::atan2(norm(cross(u, v)), dot(u, v)); }

// Face f: the angle at each of its three corners, and the share of its area each corner receives
// (the Voronoi cell inside a non-obtuse triangle; otherwise half to the obtuse corner, a quarter to the others).
inline void face_pass(const Mesh& m, uint32_t f, double* corner_angle, double* corner_area)
{
    constexpr double pi = std::numbers::pi;
    const uint32_t* t = &m.tri[3 * static_cast<std::size_t>(f)];
    const double area = norm(cross(point(m, t[1]) - point(m, t[0]), point(m, t[2]) - point(m, t[0]))) / 2;
    double theta[3];
    for (uint32_t c = 0; c < 3; ++c) {
        const uint32_t v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        theta[c] = angle(point(m, a) - point(m, v), point(m, b) - point(m, v));
    }
    const uint32_t obtuse = theta[0] > pi / 2 ? 0 : theta[1] > pi / 2 ? 1 : theta[2] > pi / 2 ? 2 : 3;
    for (uint32_t c = 0; c < 3; ++c) {
        const uint32_t v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        double share;
        if (obtuse < 3) {
            share = c == obtuse ? area / 2 : area / 4;
        } else {
            const V3 va = point(m, a) - point(m, v), vb = point(m, b) - point(m, v);
            const double cot_b = 1 / std::tan(theta[(c + 2) % 3]), cot_a = 1 / std::tan(theta[(c + 1) % 3]);
            share = (dot(va, va) * cot_b + dot(vb, vb) * cot_a) / 8;
        }
        corner_angle[3 * static_cast<std::size_t>(f) + c] = theta[c];
        corner_area[3 * static_cast<std::size_t>(f) + c] = share;
    }
}

// Vertex v: the sums over its corners, in the order of the faces.
inline void vertex_pass(const Adjacency& adj, uint32_t v, const double* corner_angle, const double* corner_area,
                        double* defect, double* area, double* gaussian)
{
    constexpr double pi = std::numbers::pi;
    double d = 2 * pi, s = 0;
    for (uint32_t k = adj.offsets[v]; k < adj.offsets[v + 1]; ++k) {
        d -= corner_angle[adj.corners[k]];
        s += corner_area[adj.corners[k]];
    }
    // On the boundary the reference is a half turn.
    if (adj.boundary[v])
        d -= pi;
    defect[v] = d;
    area[v] = s;
    gaussian[v] = s > 0 ? d / s : 0.0;
}

}  // namespace par::detail
