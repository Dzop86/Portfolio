// Integration tests on the meshes shown by the portfolio viewer (samples/, from scripts/make_samples.py).
#include "topo/curvature.hpp"
#include "topo/invariants.hpp"

#include <gtest/gtest.h>

#include <algorithm>

namespace {

using topo::Mesh;

const std::string kSamples = TOPO_SAMPLES;

TEST(Samples, InvariantsMatchTheirDescription) {
    const auto sphere = topo::analyze(Mesh::load(kSamples + "/sphere.obj"));
    EXPECT_EQ(sphere.genus, 0);
    EXPECT_EQ(sphere.boundary_loops, 0u);

    const auto torus = topo::analyze(Mesh::load(kSamples + "/torus.obj"));
    EXPECT_EQ(torus.genus, 1);

    const auto mobius = topo::analyze(Mesh::load(kSamples + "/mobius.obj"));
    EXPECT_FALSE(mobius.orientable);
    EXPECT_EQ(mobius.boundary_loops, 1u);
    EXPECT_EQ(mobius.euler_characteristic, 0);

    const auto saddle = topo::analyze(Mesh::load(kSamples + "/saddle.obj"));
    EXPECT_EQ(saddle.boundary_loops, 1u);
    EXPECT_EQ(saddle.genus, 0);
}

TEST(Samples, CurvatureHasTheExpectedSign) {
    const auto sphere = topo::gaussian_curvature(Mesh::load(kSamples + "/sphere.obj"));
    EXPECT_TRUE(std::ranges::all_of(sphere.gaussian, [](double k) { return k > 0; }));
    // Unit sphere: K = 1 everywhere. Within 2 % with mixed Voronoi areas; barycentric areas gave 15 %
    // at the 12 valence-5 vertices (see DECISIONS.md, T5).
    for (double k : sphere.gaussian) EXPECT_NEAR(k, 1.0, 0.02);

    // Torus vertex (i, j) is at index i * 24 + j: j = 0 on the outer equator, j = 12 on the inner one.
    const auto torus = topo::gaussian_curvature(Mesh::load(kSamples + "/torus.obj"));
    EXPECT_GT(torus.gaussian[0], 0);
    EXPECT_LT(torus.gaussian[12], 0);

    // Saddle z = x^2 - y^2: negative at the centre vertex (12, 12) of the 25 x 25 grid.
    const auto saddle = topo::gaussian_curvature(Mesh::load(kSamples + "/saddle.obj"));
    EXPECT_LT(saddle.gaussian[12 * 25 + 12], 0);
}

}  // namespace
