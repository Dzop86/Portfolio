// Unit tests for discrete Gaussian curvature. The discrete Gauss-Bonnet theorem gives an exact oracle:
// the angle defects of any triangulated surface sum to 2 pi chi, whatever its geometry.
#include "shapes.hpp"
#include "topo/curvature.hpp"
#include "topo/invariants.hpp"

#include <gtest/gtest.h>

#include <numbers>
#include <random>

namespace {

using shapes::Gluing;
using topo::Mesh;

constexpr double kPi = std::numbers::pi;
const std::string kData = MESH_TEST_DATA;

void expect_gauss_bonnet(const Mesh& m, const char* name) {
    const auto k = topo::gaussian_curvature(m);
    EXPECT_NEAR(k.total, 2 * kPi * static_cast<double>(topo::analyze(m).euler_characteristic), 1e-9) << name;
}

TEST(Curvature, GaussBonnetHoldsOnEverySurface) {
    expect_gauss_bonnet(Mesh::load(kData + "/tetrahedron.ply"), "tetrahedron");
    expect_gauss_bonnet(Mesh::load(kData + "/cube.obj"), "cube");
    expect_gauss_bonnet(Mesh::load(kData + "/torus.obj"), "torus");
    expect_gauss_bonnet(shapes::grid(12, 8, Gluing::Torus), "grid torus");
    expect_gauss_bonnet(shapes::grid(12, 4, Gluing::Cylinder), "cylinder");
    expect_gauss_bonnet(shapes::grid(12, 3, Gluing::Mobius), "Moebius strip");
    expect_gauss_bonnet(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{0, 1, 2}}), "triangle");
    expect_gauss_bonnet(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {9, 9, 9}}, {{0, 1, 2}}), "triangle and isolated vertex");
}

// The total depends only on topology: moving the vertices of a torus at random keeps it at 0.
TEST(Curvature, GaussBonnetSurvivesRandomDeformation) {
    const Mesh torus = shapes::grid(16, 10, Gluing::Torus);
    std::mt19937 rng(42);
    std::uniform_real_distribution<double> noise(-0.2, 0.2);
    for (int trial = 0; trial < 20; ++trial) {
        auto positions = torus.positions();
        for (auto& p : positions) p = {p.x + noise(rng), p.y + noise(rng), p.z + noise(rng)};
        const Mesh bumpy(std::move(positions), torus.triangles());
        EXPECT_NEAR(topo::gaussian_curvature(bumpy).total, 0.0, 1e-9) << "trial " << trial;
    }
}

TEST(Curvature, CubeCornersEachCarryAQuarterTurn) {
    const auto k = topo::gaussian_curvature(Mesh::load(kData + "/cube.obj"));
    ASSERT_EQ(k.angle_defect.size(), 8u);
    for (double d : k.angle_defect) EXPECT_NEAR(d, kPi / 2, 1e-12);
    double area = 0;
    for (double a : k.area) area += a;
    EXPECT_NEAR(area, 6.0, 1e-12) << "vertex areas add up to the surface area";
}

TEST(Curvature, RegularOctahedronVerticesHaveDefectTwoThirdsPi) {
    const Mesh octa({{1, 0, 0}, {-1, 0, 0}, {0, 1, 0}, {0, -1, 0}, {0, 0, 1}, {0, 0, -1}},
                    {{0, 2, 4}, {2, 1, 4}, {1, 3, 4}, {3, 0, 4}, {2, 0, 5}, {1, 2, 5}, {3, 1, 5}, {0, 3, 5}});
    const auto k = topo::gaussian_curvature(octa);
    for (double d : k.angle_defect) EXPECT_NEAR(d, 2 * kPi / 3, 1e-12);
    // Density = defect / (one third of the incident area); each face has area sqrt(3)/2.
    for (double g : k.gaussian) EXPECT_NEAR(g, (2 * kPi / 3) / (4 * (std::sqrt(3.0) / 2) / 3), 1e-12);
}

TEST(Curvature, FlatInteriorVertexHasNoCurvature) {
    // A planar fan of 6 triangles around vertex 0.
    std::vector<topo::Vec3> p{{0, 0, 0}};
    std::vector<topo::Triangle> t;
    for (uint32_t i = 0; i < 6; ++i) {
        p.push_back({std::cos(i * kPi / 3), std::sin(i * kPi / 3), 0});
        t.push_back({0, 1 + i, 1 + (i + 1) % 6});
    }
    const auto k = topo::gaussian_curvature(Mesh(std::move(p), std::move(t)));
    EXPECT_NEAR(k.angle_defect[0], 0.0, 1e-12);
    EXPECT_NEAR(k.gaussian[0], 0.0, 1e-12);
    // Boundary vertices: pi minus their two 60 degree angles.
    EXPECT_NEAR(k.angle_defect[1], kPi / 3, 1e-12);
    EXPECT_FALSE(k.boundary[0]);
    for (std::size_t v = 1; v < 7; ++v) EXPECT_TRUE(k.boundary[v]) << v;
}

TEST(Curvature, IsolatedVertexCarriesAFullTurnAndNoDensity) {
    const auto k = topo::gaussian_curvature(Mesh({{0, 0, 0}}, {}));
    EXPECT_NEAR(k.angle_defect[0], 2 * kPi, 1e-12);
    EXPECT_EQ(k.gaussian[0], 0.0) << "no area, so the density is reported as 0";
}

}  // namespace
