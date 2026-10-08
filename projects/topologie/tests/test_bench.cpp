// The benchmark of the project page (tools/bench.hpp, sprint 40): it times the right mesh and the right results.
#include "../tools/bench.hpp"
#include "topo/invariants.hpp"

#include <gtest/gtest.h>

TEST(Bench, TorusHasTheRequestedSizeAndIsATorus) {
    const auto mesh = bench::torus(100, 50);
    EXPECT_EQ(mesh.face_count(), 10000u);
    EXPECT_EQ(mesh.vertex_count(), 5000u);
    const auto inv = topo::analyze(mesh);
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_EQ(inv.genus, 1);
    EXPECT_EQ(inv.boundary_loops, 0u);
}

TEST(Bench, MeasuresTheStepsOnTheirResults) {
    const auto t = bench::measure(bench::torus(40, 20), 2);
    // The standing torus: Betti numbers 1, 2, 1 and one loop in the Reeb graph.
    EXPECT_EQ(t.betti, (std::array<int, 3>{1, 2, 1}));
    EXPECT_EQ(t.loops, 1u);
    EXPECT_GE(t.elevation_ms, 0.0);
    EXPECT_GE(t.persistence_ms, 0.0);
    EXPECT_GE(t.reeb_ms, 0.0);
    EXPECT_LT(t.persistence_ms, 1e300);  // every step ran
}

TEST(Bench, SizesGoFromTenThousandToAMillionTriangles) {
    std::size_t last = 0;
    for (const auto [n, m] : bench::kSizes) {
        const std::size_t triangles = 2 * std::size_t{n} * m;
        EXPECT_GT(triangles, last);
        last = triangles;
    }
    EXPECT_EQ(2 * std::size_t{bench::kSizes.front()[0]} * bench::kSizes.front()[1], 10000u);
    EXPECT_EQ(last, 1000000u);
}
