#include "support.hpp"

#include <gtest/gtest.h>

#include <cmath>
#include <cstring>
#include <fstream>
#include <numbers>

namespace {

// Bit for bit: the same doubles, not just close ones (EXPECT_EQ on doubles compares values, and
// would call 0.0 and -0.0 equal).
bool same_bits(double a, double b) { return std::memcmp(&a, &b, sizeof a) == 0; }

TEST(Sequential, GivesTopologiesResultExactly)
{
    for (const std::string& file : test::reference_files()) {
        const par::Mesh m = par::load(file);
        const par::Curvature k = par::curvature_sequential(m, par::build_adjacency(m));
        const topo::GaussianCurvature reference = topo::gaussian_curvature(test::to_topo(m));
        for (uint32_t v = 0; v < m.vertex_count(); ++v) {
            ASSERT_TRUE(same_bits(k.defect[v], reference.angle_defect[v])) << file << " defect of " << v;
            ASSERT_TRUE(same_bits(k.area[v], reference.area[v])) << file << " area of " << v;
            ASSERT_TRUE(same_bits(k.gaussian[v], reference.gaussian[v])) << file << " K of " << v;
        }
        EXPECT_TRUE(same_bits(k.total, reference.total)) << file;
    }
}

TEST(Sequential, FollowsGaussBonnet)
{
    constexpr double tau = 2 * std::numbers::pi;
    const par::Mesh torus = par::torus(120, 80);
    EXPECT_NEAR(par::curvature_sequential(torus, par::build_adjacency(torus)).total, 0.0, 1e-9);
    const par::Mesh sphere = par::load(TOPO_SAMPLES "/sphere.obj");
    EXPECT_NEAR(par::curvature_sequential(sphere, par::build_adjacency(sphere)).total, 2 * tau, 1e-9);
    // A Möbius strip: chi = 0, one boundary.
    const par::Mesh mobius = par::load(TOPO_SAMPLES "/mobius.obj");
    EXPECT_NEAR(par::curvature_sequential(mobius, par::build_adjacency(mobius)).total, 0.0, 1e-9);
}

TEST(OpenMP, IsIdenticalToSequentialBitForBit_WhateverTheThreads)
{
    const par::Mesh m = par::torus(300, 200); // 120,000 triangles
    const par::Adjacency a = par::build_adjacency(m);
    const par::Curvature reference = par::curvature_sequential(m, a);
    for (int threads : {1, 2, 3, 5, 8, 16}) {
        const par::Curvature k = par::curvature_openmp(m, a, threads);
        for (uint32_t v = 0; v < m.vertex_count(); ++v) {
            ASSERT_TRUE(same_bits(k.defect[v], reference.defect[v])) << threads << " threads, vertex " << v;
            ASSERT_TRUE(same_bits(k.area[v], reference.area[v])) << threads << " threads, vertex " << v;
            ASSERT_TRUE(same_bits(k.gaussian[v], reference.gaussian[v])) << threads << " threads, vertex " << v;
        }
        EXPECT_TRUE(same_bits(k.total, reference.total)) << threads << " threads";
    }
}

TEST(OpenMP, IsBuiltIn)
{
    // The CI builds with OpenMP on the three systems; without it the "OpenMP" version is sequential.
    EXPECT_TRUE(par::openmp_available());
}

TEST(Load, ReportsLibCErrorsWithTheirLine)
{
    const std::string path = ::testing::TempDir() + "par_bad.obj";
    std::ofstream(path) << "v 0 0 0\nv 1 0 0\nf 1 2 9\n";
    try {
        (void)par::load(path);
        FAIL() << "no error";
    } catch (const par::LoadError& e) {
        EXPECT_NE(std::string(e.what()).find("(line 3)"), std::string::npos) << e.what();
    }
}

TEST(Curvature, HandlesAMeshWithoutFaces)
{
    const par::Mesh points{{0, 0, 0, 1, 1, 1}, {}};
    const par::Curvature k = par::curvature_openmp(points, par::build_adjacency(points));
    EXPECT_EQ(k.gaussian, std::vector<double>(2, 0.0));
    EXPECT_DOUBLE_EQ(k.total, 4 * std::numbers::pi);
}

}  // namespace
