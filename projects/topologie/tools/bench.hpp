// Computing times of the height, its persistence and its Reeb graph on a standing torus of a given size, shared by
// tools/topo_bench.cpp and its test (sprint 40). The JavaScript twin is scripts/bench.mjs, for WebAssembly.
#pragma once

#include "topo/mesh.hpp"
#include "topo/morse.hpp"
#include "topo/persistence.hpp"
#include "topo/reeb.hpp"

#include <algorithm>
#include <array>
#include <chrono>
#include <cmath>
#include <numbers>
#include <vector>

namespace bench {

// A torus of n x m quads (2nm triangles), ring in the xy plane: along y it stands, with a minimum, two saddles
// and a maximum. The same vertices, in the same order, as torusObj in scripts/bench.mjs.
inline topo::Mesh torus(uint32_t n, uint32_t m) {
    std::vector<topo::Vec3> positions;
    positions.reserve(std::size_t{n} * m);
    for (uint32_t i = 0; i < n; ++i) {
        for (uint32_t j = 0; j < m; ++j) {
            const double u = 2 * std::numbers::pi * i / n, v = 2 * std::numbers::pi * j / m;
            positions.push_back({(2 + std::cos(v)) * std::cos(u), (2 + std::cos(v)) * std::sin(u), std::sin(v)});
        }
    }
    std::vector<topo::Triangle> triangles;
    triangles.reserve(2 * std::size_t{n} * m);
    for (uint32_t i = 0; i < n; ++i) {
        for (uint32_t j = 0; j < m; ++j) {
            const uint32_t a = i * m + j, b = (i + 1) % n * m + j, c = (i + 1) % n * m + (j + 1) % m, d = i * m + (j + 1) % m;
            triangles.push_back({a, b, c});
            triangles.push_back({a, c, d});
        }
    }
    return topo::Mesh(std::move(positions), std::move(triangles));
}

struct Timing {
    double elevation_ms = 0, persistence_ms = 0, reeb_ms = 0;  // best of the runs
    std::array<int, 3> betti{};
    std::size_t loops = 0;
};

// Best time of `runs` runs of each step, on one thread, with the height along y.
inline Timing measure(const topo::Mesh& mesh, int runs = 3) {
    using clock = std::chrono::steady_clock;
    const auto ms = [](clock::time_point a, clock::time_point b) { return std::chrono::duration<double, std::milli>(b - a).count(); };
    Timing t;
    t.elevation_ms = t.persistence_ms = t.reeb_ms = 1e300;
    for (int r = 0; r < runs; ++r) {
        const auto t0 = clock::now();
        const auto e = topo::elevation(mesh, {0, 1, 0});
        const auto t1 = clock::now();
        const auto p = topo::persistence(mesh, e.height);
        const auto t2 = clock::now();
        const auto g = topo::reeb_graph(mesh, e);
        const auto t3 = clock::now();
        t.elevation_ms = std::min(t.elevation_ms, ms(t0, t1));
        t.persistence_ms = std::min(t.persistence_ms, ms(t1, t2));
        t.reeb_ms = std::min(t.reeb_ms, ms(t2, t3));
        t.betti = p.betti;
        t.loops = g.loops();
    }
    return t;
}

// The sizes of the table, n x m quads: 10 000 to 1 000 000 triangles.
inline constexpr std::array<std::array<uint32_t, 2>, 5> kSizes{{{100, 50}, {200, 75}, {500, 100}, {750, 200}, {1000, 500}}};

}  // namespace bench
