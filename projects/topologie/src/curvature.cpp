#include "topo/curvature.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>

namespace topo {

namespace {

struct V3 {
    double x, y, z;
};
V3 operator-(const Vec3& a, const Vec3& b) { return {a.x - b.x, a.y - b.y, a.z - b.z}; }
double dot(V3 a, V3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
V3 cross(V3 a, V3 b) { return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x}; }
double norm(V3 a) { return std::sqrt(dot(a, a)); }

// Angle between u and v; atan2 stays accurate near 0 and pi, unlike acos, and gives 0 for a null vector.
double angle(V3 u, V3 v) { return std::atan2(norm(cross(u, v)), dot(u, v)); }

}  // namespace

GaussianCurvature gaussian_curvature(const Mesh& m) {
    constexpr double pi = std::numbers::pi;
    const auto& p = m.positions();
    const auto& he = m.half_edges();
    const std::size_t nv = m.vertex_count();

    GaussianCurvature k;
    k.angle_defect.assign(nv, 2 * pi);
    k.area.assign(nv, 0.0);
    k.gaussian.assign(nv, 0.0);

    for (const Triangle& t : m.triangles()) {
        const double area = norm(cross(p[t[1]] - p[t[0]], p[t[2]] - p[t[0]])) / 2;
        double theta[3];
        for (std::size_t c = 0; c < 3; ++c) {
            const uint32_t v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
            theta[c] = angle(p[a] - p[v], p[b] - p[v]);
            k.angle_defect[v] -= theta[c];
        }
        // Mixed Voronoi area (Meyer et al. 2003): Voronoi cell inside a non-obtuse triangle, otherwise half
        // of the triangle to its obtuse corner and a quarter to each other corner. Sums to the triangle area.
        const std::size_t obtuse = theta[0] > pi / 2 ? 0 : theta[1] > pi / 2 ? 1 : theta[2] > pi / 2 ? 2 : 3;
        for (std::size_t c = 0; c < 3; ++c) {
            const uint32_t v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
            if (obtuse < 3) {
                k.area[v] += c == obtuse ? area / 2 : area / 4;
            } else {
                // |va|^2 cot(angle at b) + |vb|^2 cot(angle at a), over 8.
                const V3 va = p[a] - p[v], vb = p[b] - p[v];
                const double cot_b = 1 / std::tan(theta[(c + 2) % 3]), cot_a = 1 / std::tan(theta[(c + 1) % 3]);
                k.area[v] += (dot(va, va) * cot_b + dot(vb, vb) * cot_a) / 8;
            }
        }
    }
    // On the boundary the reference is a half turn: remove the other half once per boundary vertex.
    std::vector<bool> boundary(nv, false);
    for (uint32_t h = 0; h < he.size(); ++h) {
        if (he[h].twin != kNone) continue;
        const uint32_t a = std::min(he[h].origin, m.target(h)), b = std::max(he[h].origin, m.target(h));
        if (std::ranges::binary_search(m.non_manifold_edges(), std::array<uint32_t, 2>{a, b})) continue;
        boundary[he[h].origin] = boundary[m.target(h)] = true;
    }
    k.boundary = boundary;
    for (std::size_t v = 0; v < nv; ++v) {
        if (boundary[v]) k.angle_defect[v] -= pi;
        if (k.area[v] > 0) k.gaussian[v] = k.angle_defect[v] / k.area[v];
        k.total += k.angle_defect[v];
    }
    return k;
}

}  // namespace topo
