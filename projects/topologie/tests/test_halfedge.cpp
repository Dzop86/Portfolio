// Unit and integration tests for the half-edge structure. Meshes are synthetic or lib-c's test data.
#include "topo/mesh.hpp"

#include <gtest/gtest.h>

#include <algorithm>

#include "mesh/mesh.h"

namespace {

using topo::kNone;
using topo::Mesh;

const std::string kData = MESH_TEST_DATA;

// Checks the invariants every half-edge structure must satisfy, whatever the mesh.
void expect_consistent(const Mesh& m) {
    const auto& he = m.half_edges();
    ASSERT_EQ(he.size(), 3 * m.face_count());
    for (uint32_t h = 0; h < he.size(); ++h) {
        EXPECT_EQ(he[he[he[h].next].next].next, h) << "next cycles in 3 steps";
        EXPECT_EQ(he[h].face, h / 3);
        if (he[h].twin == kNone) continue;
        EXPECT_NE(he[h].twin, h);
        EXPECT_EQ(he[he[h].twin].twin, h) << "twin is an involution";
        const bool same = he[he[h].twin].origin == he[h].origin;
        EXPECT_EQ(he[h].flipped, same) << "flipped iff both half-edges run the same way";
        if (!same) EXPECT_EQ(he[he[h].twin].origin, m.target(h));
    }
}

TEST(HalfEdge, ClosedTetrahedronHasSixTwinnedEdges) {
    const Mesh m = Mesh::load(kData + "/tetrahedron.ply");
    EXPECT_EQ(m.vertex_count(), 4u);
    EXPECT_EQ(m.face_count(), 4u);
    EXPECT_EQ(m.edge_count(), 6u);
    expect_consistent(m);
    for (const auto& h : m.half_edges()) {
        EXPECT_NE(h.twin, kNone);
        EXPECT_FALSE(h.flipped);
    }
    EXPECT_TRUE(m.non_manifold_edges().empty());
}

TEST(HalfEdge, SingleTriangleHasThreeBoundaryHalfEdges) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{0, 1, 2}});
    EXPECT_EQ(m.edge_count(), 3u);
    expect_consistent(m);
    EXPECT_EQ(std::ranges::count_if(m.half_edges(), [](auto& h) { return h.twin == kNone; }), 3);
    EXPECT_EQ(m.target(0), 1u);
    EXPECT_EQ(m.target(2), 0u);
}

TEST(HalfEdge, InconsistentOrientationIsPairedButFlagged) {
    // Two triangles sharing edge 1-2, both listing it as 1 -> 2.
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}}, {{0, 1, 2}, {1, 2, 3}});
    expect_consistent(m);
    EXPECT_EQ(m.edge_count(), 5u);
    EXPECT_EQ(std::ranges::count_if(m.half_edges(), [](auto& h) { return h.flipped; }), 2);
}

TEST(HalfEdge, EdgeSharedByThreeFacesIsNonManifold) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {0, -1, 0}, {0, 0, 1}}, {{0, 1, 2}, {1, 0, 3}, {0, 1, 4}});
    expect_consistent(m);
    ASSERT_EQ(m.non_manifold_edges().size(), 1u);
    EXPECT_EQ(m.non_manifold_edges()[0], (std::array<uint32_t, 2>{0, 1}));
    EXPECT_EQ(m.edge_count(), 7u);
    for (uint32_t h = 0; h < m.half_edges().size(); ++h) {
        const uint32_t a = m.half_edges()[h].origin, b = m.target(h);
        if (std::min(a, b) == 0 && std::max(a, b) == 1) EXPECT_EQ(m.half_edges()[h].twin, kNone);
    }
}

TEST(HalfEdge, RejectsBadIndicesAndDegenerateTriangles) {
    EXPECT_THROW(Mesh({{0, 0, 0}, {1, 0, 0}}, {{0, 1, 2}}), std::invalid_argument);
    EXPECT_THROW(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{0, 1, 1}}), std::invalid_argument);
}

TEST(HalfEdge, EmptyMeshIsValid) {
    const Mesh m({}, {});
    EXPECT_EQ(m.face_count(), 0u);
    EXPECT_EQ(m.edge_count(), 0u);
}

TEST(HalfEdgeLoad, ReportsLibCErrorsWithTheirLine) {
    try {
        (void)Mesh::load(kData + "/missing.obj");
        FAIL() << "expected LoadError";
    } catch (const topo::LoadError& e) {
        EXPECT_EQ(e.line(), 0u);
        EXPECT_EQ(e.status(), MESH_ERR_IO);
        EXPECT_STREQ(e.what(), "cannot read file");
    }
    const std::string broken = "v 0 0 0\nv 1 0 0\nf 1 2 3\n";
    try {
        (void)Mesh::parse(broken);
        FAIL() << "expected LoadError";
    } catch (const topo::LoadError& e) {
        EXPECT_EQ(e.line(), 3u);
        EXPECT_EQ(e.status(), MESH_ERR_INDEX);
    }
}

// Integration: same edge count as lib-c's own topology, on every sample.
TEST(HalfEdgeLoad, EdgeCountMatchesLibC) {
    for (const char* name : {"cube.obj", "cube.stl", "tetrahedron.ply", "torus.obj"}) {
        const std::string path = kData + "/" + name;
        const Mesh m = Mesh::load(path);
        expect_consistent(m);
        mesh c;
        mesh_init(&c);
        ASSERT_EQ(mesh_read_file(path.c_str(), &c, nullptr), MESH_OK);
        mesh_topology t;
        ASSERT_EQ(mesh_compute_topology(&c, &t), MESH_OK);
        EXPECT_EQ(m.edge_count(), t.edge_count) << name;
        EXPECT_EQ(m.vertex_count(), c.vertex_count) << name;
        mesh_free(&c);
    }
}

}  // namespace
