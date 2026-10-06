// Unit tests for the topological invariants, on surfaces whose answer is known.
#include "shapes.hpp"
#include "topo/invariants.hpp"

#include <gtest/gtest.h>

namespace {

using shapes::Gluing;
using topo::analyze;
using topo::Mesh;

const std::string kData = MESH_TEST_DATA;

TEST(Invariants, TetrahedronIsAClosedOrientableSphere) {
    const auto inv = analyze(Mesh::load(kData + "/tetrahedron.ply"));
    EXPECT_EQ(inv.components, 1u);
    EXPECT_EQ(inv.boundary_loops, 0u);
    EXPECT_TRUE(inv.manifold);
    EXPECT_TRUE(inv.orientable);
    EXPECT_TRUE(inv.consistently_oriented);
    EXPECT_EQ(inv.euler_characteristic, 2);
    EXPECT_EQ(inv.genus, 0);
}

TEST(Invariants, TorusFromLibCHasGenusOne) {
    const auto inv = analyze(Mesh::load(kData + "/torus.obj"));
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_EQ(inv.boundary_loops, 0u);
    EXPECT_EQ(inv.genus, 1);
}

TEST(Invariants, TwoDisjointToriHaveTotalGenusTwo) {
    const auto inv = analyze(shapes::disjoint_union(shapes::grid(6, 4, Gluing::Torus), shapes::grid(5, 3, Gluing::Torus)));
    EXPECT_EQ(inv.components, 2u);
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_EQ(inv.genus, 2);
}

TEST(Invariants, CylinderHasTwoBoundaryLoopsAndGenusZero) {
    const auto inv = analyze(shapes::grid(8, 3, Gluing::Cylinder));
    EXPECT_EQ(inv.components, 1u);
    EXPECT_EQ(inv.boundary_loops, 2u);
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_TRUE(inv.orientable);
    EXPECT_EQ(inv.genus, 0);
}

TEST(Invariants, MobiusStripIsNotOrientable) {
    const auto inv = analyze(shapes::grid(8, 2, Gluing::Mobius));
    EXPECT_TRUE(inv.manifold);
    EXPECT_FALSE(inv.orientable);
    EXPECT_FALSE(inv.consistently_oriented);
    EXPECT_EQ(inv.boundary_loops, 1u);
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_FALSE(inv.genus.has_value()) << "genus is only defined here for orientable surfaces";
}

TEST(Invariants, SingleTriangleIsADisk) {
    const auto inv = analyze(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{0, 1, 2}}));
    EXPECT_EQ(inv.boundary_loops, 1u);
    EXPECT_EQ(inv.euler_characteristic, 1);
    EXPECT_EQ(inv.genus, 0);
}

TEST(Invariants, FlippedFaceIsOrientableButNotConsistentlyOriented) {
    const auto inv = analyze(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}}, {{0, 1, 2}, {1, 2, 3}}));
    EXPECT_TRUE(inv.orientable);
    EXPECT_FALSE(inv.consistently_oriented);
    EXPECT_EQ(inv.boundary_loops, 1u);
}

TEST(Invariants, BowtieVertexIsNotManifold) {
    // Two triangles touching at vertex 0 only.
    const auto inv = analyze(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {-1, 0, 0}, {0, -1, 0}}, {{0, 1, 2}, {0, 3, 4}}));
    EXPECT_EQ(inv.components, 1u);
    EXPECT_EQ(inv.non_manifold_vertices, 1u);
    EXPECT_FALSE(inv.manifold);
    EXPECT_FALSE(inv.genus.has_value());
}

TEST(Invariants, EdgeSharedByThreeFacesIsNotManifold) {
    const auto inv = analyze(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {0, -1, 0}, {0, 0, 1}}, {{0, 1, 2}, {1, 0, 3}, {0, 1, 4}}));
    EXPECT_EQ(inv.non_manifold_edges, 1u);
    EXPECT_FALSE(inv.manifold);
    EXPECT_FALSE(inv.genus.has_value());
}

TEST(Invariants, IsolatedVerticesCountAsComponents) {
    const auto inv = analyze(Mesh({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {5, 5, 5}}, {{0, 1, 2}}));
    EXPECT_EQ(inv.components, 2u);
    EXPECT_EQ(inv.isolated_vertices, 1u);
    EXPECT_EQ(inv.euler_characteristic, 2);
    EXPECT_EQ(inv.genus, 0);
}

TEST(Invariants, EmptyMesh) {
    const auto inv = analyze(Mesh({}, {}));
    EXPECT_EQ(inv.components, 0u);
    EXPECT_EQ(inv.euler_characteristic, 0);
    EXPECT_EQ(inv.genus, 0);
}

}  // namespace
