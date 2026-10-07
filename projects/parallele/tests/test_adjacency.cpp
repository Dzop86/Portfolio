#include "support.hpp"

#include <gtest/gtest.h>

namespace {

TEST(Adjacency, ListsEachCornerUnderItsVertexInFaceOrder)
{
    const par::Mesh m = par::load(MESH_TEST_DATA "/cube.obj");
    const par::Adjacency a = par::build_adjacency(m);
    ASSERT_EQ(a.offsets.size(), m.vertex_count() + 1u);
    EXPECT_EQ(a.offsets.back(), m.tri.size());
    for (uint32_t v = 0; v < m.vertex_count(); ++v)
        for (uint32_t k = a.offsets[v]; k < a.offsets[v + 1]; ++k) {
            EXPECT_EQ(m.tri[a.corners[k]], v);
            if (k > a.offsets[v]) {
                EXPECT_LT(a.corners[k - 1], a.corners[k]);
            }
        }
}

TEST(Adjacency, BoundaryVerticesMatchTopologie)
{
    for (const std::string& file : test::reference_files()) {
        const par::Mesh m = par::load(file);
        const par::Adjacency a = par::build_adjacency(m);
        const topo::GaussianCurvature reference = topo::gaussian_curvature(test::to_topo(m));
        for (uint32_t v = 0; v < m.vertex_count(); ++v)
            ASSERT_EQ(a.boundary[v] != 0, reference.boundary[v]) << file << " vertex " << v;
    }
}

TEST(Adjacency, AnEdgeSharedByThreeFacesIsNotABoundary)
{
    // Three triangles around the edge 0-1, a "book" of three pages: 0 and 1 lie on the outer edges.
    const par::Mesh book{{0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, -1, 0, 0, 0, -1, 0}, {0, 1, 2, 0, 1, 3, 0, 1, 4}};
    const par::Adjacency a = par::build_adjacency(book);
    const topo::GaussianCurvature reference = topo::gaussian_curvature(test::to_topo(book));
    for (uint32_t v = 0; v < book.vertex_count(); ++v)
        EXPECT_EQ(a.boundary[v] != 0, reference.boundary[v]) << v;
}

TEST(Torus, IsAClosedSurfaceOfGenusOne)
{
    const par::Mesh t = par::torus(30, 20);
    EXPECT_EQ(t.vertex_count(), 600u);
    EXPECT_EQ(t.face_count(), 1200u);
    const topo::Mesh mesh = test::to_topo(t);
    EXPECT_EQ(static_cast<int64_t>(mesh.vertex_count()) - static_cast<int64_t>(mesh.edge_count()) + mesh.face_count(), 0);
    const par::Adjacency a = par::build_adjacency(t);
    for (uint8_t b : a.boundary)
        EXPECT_EQ(b, 0);
}

}  // namespace
