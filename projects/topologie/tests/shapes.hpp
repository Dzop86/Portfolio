// Synthetic surfaces for the tests, built from an n x m grid of quads (two triangles each).
#pragma once

#include "topo/mesh.hpp"

#include <cmath>
#include <numbers>

namespace shapes {

enum class Gluing {
    Cylinder,  // rows glued, columns open: 2 boundary loops, chi = 0
    Torus,     // rows and columns glued: chi = 0, genus 1
    Mobius,    // rows glued with a half twist, columns open: non-orientable, 1 boundary loop
};

// n >= 3 quads around, m >= 1 quads across (m >= 3 for the torus).
inline topo::Mesh grid(uint32_t n, uint32_t m, Gluing gluing) {
    const bool wrap_v = gluing == Gluing::Torus;
    const uint32_t rows = wrap_v ? m : m + 1;
    std::vector<topo::Vec3> positions;
    for (uint32_t i = 0; i < n; ++i) {
        for (uint32_t j = 0; j < rows; ++j) {
            // Embedded on a torus-like shape so that curvature tests get real geometry.
            const double u = 2 * std::numbers::pi * i / n, v = 2 * std::numbers::pi * j / (wrap_v ? m : 2 * m);
            positions.push_back({(2 + std::cos(v)) * std::cos(u), (2 + std::cos(v)) * std::sin(u), std::sin(v)});
        }
    }
    auto id = [&](uint32_t i, uint32_t j) {
        if (i == n) {
            i = 0;
            if (gluing == Gluing::Mobius) j = m - j;
        }
        return i * rows + (wrap_v ? j % m : j);
    };
    std::vector<topo::Triangle> triangles;
    for (uint32_t i = 0; i < n; ++i) {
        for (uint32_t j = 0; j < m; ++j) {
            const uint32_t a = id(i, j), b = id(i + 1, j), c = id(i + 1, j + 1), d = id(i, j + 1);
            triangles.push_back({a, b, c});
            triangles.push_back({a, c, d});
        }
    }
    return topo::Mesh(std::move(positions), std::move(triangles));
}

// Appends `b` to `a` as a separate component.
inline topo::Mesh disjoint_union(const topo::Mesh& a, const topo::Mesh& b) {
    auto positions = a.positions();
    auto triangles = a.triangles();
    positions.insert(positions.end(), b.positions().begin(), b.positions().end());
    for (auto t : b.triangles()) {
        for (auto& v : t) v += a.vertex_count();
        triangles.push_back(t);
    }
    return topo::Mesh(std::move(positions), std::move(triangles));
}

}  // namespace shapes
