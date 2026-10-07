// Meshes read by lib-c, their normals, and the BVH checked against every triangle tested in turn.
#include "rt/bvh.hpp"
#include "rt/mesh.hpp"
#include "rt/rng.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <numeric>
#include <string>

namespace {

using rt::TriangleMesh;
using rt::Vec3;

const std::string kSamples = RT_SAMPLES;

TEST(Mesh, ReadsAnObjStringAndComputesUnitNormals) {
    const auto m = TriangleMesh::read("v 0 0 0\nv 1 0 0\nv 0 1 0\nv 0 0 1\nf 1 3 2\nf 1 2 4\nf 1 4 3\nf 2 3 4\n");
    EXPECT_EQ(m.positions.size(), 4u);
    EXPECT_EQ(m.triangles.size(), 4u);
    for (const auto& n : m.normals) EXPECT_NEAR(rt::length(n), 1, 1e-12);
}

TEST(Mesh, ReportsLibCErrorsWithTheirLine) {
    try {
        (void)TriangleMesh::read("v 0 0 0\nv 1 0 0\nf 1 2 9\n");
        FAIL() << "an index past the vertices must be refused";
    } catch (const rt::LoadError& e) {
        EXPECT_EQ(e.status(), 4);
        EXPECT_EQ(e.line(), 3u);
    }
    EXPECT_THROW((void)TriangleMesh::read("v 0 0 0\n"), rt::LoadError);  // no triangle
    EXPECT_THROW((void)TriangleMesh::load(kSamples + "/missing.obj"), rt::LoadError);
}

TEST(Mesh, SphereNormalsPointOutwards) {
    auto m = TriangleMesh::load(kSamples + "/sphere.obj");
    const Vec3 c = m.bounds().center();
    for (std::size_t i = 0; i < m.positions.size(); ++i) {
        EXPECT_GT(rt::dot(m.normals[i], rt::normalize(m.positions[i] - c)), 0.95) << i;
    }
}

TEST(Mesh, MobiusStripStillGetsSmoothNormals) {
    // Non-orientable: summing face normals as they are would cancel them along the seam.
    const auto m = TriangleMesh::load(kSamples + "/mobius.obj");
    for (std::size_t i = 0; i < m.normals.size(); ++i) EXPECT_NEAR(rt::length(m.normals[i]), 1, 1e-9) << i;
}

TEST(Mesh, FitRestsTheMeshOnTheFloorInsideTheSphere) {
    auto m = TriangleMesh::load(kSamples + "/torus.obj");
    m.fit(1.0);
    const rt::Box b = m.bounds();
    EXPECT_NEAR(b.lo.y, 0, 1e-12);
    EXPECT_NEAR(b.center().x, 0, 1e-12);
    EXPECT_NEAR(b.center().z, 0, 1e-12);
    double far = 0;
    const Vec3 c = b.center();
    for (const auto& p : m.positions) far = std::max(far, rt::length(p - c));
    EXPECT_NEAR(far, 1, 1e-9);
}

void expect_same_hits(const TriangleMesh& m, const rt::Bvh& bvh, int rays, uint64_t seed) {
    rt::Rng rng(seed);
    const rt::Box b = m.bounds();
    const Vec3 size = b.hi - b.lo;
    int hits = 0;
    for (int i = 0; i < rays; ++i) {
        // From a point around the mesh towards a point inside its box: most rays hit, some graze or miss.
        const Vec3 from = b.center() + rt::normalize(Vec3{rng.uniform() - 0.5, rng.uniform() - 0.5, rng.uniform() - 0.5}) *
                                           (2 * rt::length(size));
        const Vec3 to = b.lo + Vec3{size.x * rng.uniform(), size.y * rng.uniform(), size.z * rng.uniform()};
        const rt::Ray r{from, rt::normalize(to - from)};
        const auto expected = rt::intersect_brute_force(m, r, 0, rt::kInfinity);
        const auto got = bvh.intersect(m, r, 0, rt::kInfinity);
        ASSERT_EQ(expected.has_value(), got.has_value()) << "ray " << i;
        ASSERT_EQ(bvh.occluded(m, r, 0, rt::kInfinity), expected.has_value()) << "ray " << i;
        if (!expected) continue;
        ++hits;
        // Same distance; the triangle may differ only when two share the hit point (an edge).
        ASSERT_EQ(expected->t, got->t) << "ray " << i;
    }
    EXPECT_GT(hits, rays / 4);
}

TEST(Bvh, FindsTheSameHitsAsBruteForceOnEverySample) {
    for (const char* name : {"torus", "sphere", "mobius", "saddle"}) {
        SCOPED_TRACE(name);
        const auto m = TriangleMesh::load(kSamples + "/" + name + ".obj");
        expect_same_hits(m, rt::Bvh(m), 3000, 7);
    }
}

TEST(Bvh, FindsTheSameHitsOnARandomTriangleSoup) {
    TriangleMesh m;
    rt::Rng rng(99);
    for (uint32_t i = 0; i < 2000; ++i) {
        const Vec3 c{rng.uniform() * 10, rng.uniform() * 10, rng.uniform() * 10};
        for (int k = 0; k < 3; ++k) m.positions.push_back(c + Vec3{rng.uniform(), rng.uniform(), rng.uniform()} * 0.8);
        m.triangles.push_back({3 * i, 3 * i + 1, 3 * i + 2});
    }
    m.compute_normals();
    expect_same_hits(m, rt::Bvh(m), 3000, 11);
}

TEST(Bvh, IsAValidTree) {
    const auto m = TriangleMesh::load(kSamples + "/torus.obj");
    const rt::Bvh bvh(m);
    // Every triangle exactly once, leaves small, every box containing its triangles.
    std::vector<uint32_t> order = bvh.order();
    std::sort(order.begin(), order.end());
    std::vector<uint32_t> all(m.triangles.size());
    std::iota(all.begin(), all.end(), 0u);
    EXPECT_EQ(order, all);
    std::size_t leaves = 0;
    for (const auto& node : bvh.nodes()) {
        if (node.count == 0) continue;
        ++leaves;
        EXPECT_LE(node.count, rt::Bvh::kMaxLeaf);
        for (uint32_t i = node.first; i < node.first + node.count; ++i) {
            for (const uint32_t v : m.triangles[bvh.order()[i]]) {
                const Vec3& p = m.positions[v];
                EXPECT_TRUE(p.x >= node.box.lo.x && p.x <= node.box.hi.x && p.y >= node.box.lo.y && p.y <= node.box.hi.y &&
                            p.z >= node.box.lo.z && p.z <= node.box.hi.z);
            }
        }
    }
    EXPECT_GT(leaves, m.triangles.size() / rt::Bvh::kMaxLeaf - 1);
    EXPECT_LE(bvh.depth(), 30u);
}

TEST(Bvh, DegenerateInputsStayCorrect) {
    // Every triangle at the same place: no split separates them, one leaf holds them all.
    TriangleMesh m;
    for (uint32_t i = 0; i < 50; ++i) {
        m.positions.insert(m.positions.end(), {{0, 0, 0}, {1, 0, 0}, {0, 1, 0}});
        m.triangles.push_back({3 * i, 3 * i + 1, 3 * i + 2});
    }
    m.compute_normals();
    const rt::Bvh bvh(m);
    EXPECT_EQ(bvh.nodes().size(), 1u);
    const auto h = bvh.intersect(m, {{0.2, 0.2, 1}, {0, 0, -1}}, 0, rt::kInfinity);
    ASSERT_TRUE(h.has_value());
    EXPECT_DOUBLE_EQ(h->t, 1);
}

}  // namespace
