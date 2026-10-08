// Reeb graph of the height: known shapes, the loops against the genus, degrees, monotone arcs.
#include "shapes.hpp"
#include "topo/invariants.hpp"
#include "topo/morse.hpp"
#include "topo/reeb.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <map>
#include <random>
#include <stdexcept>
#include <string>

namespace {

using topo::Mesh;
using topo::Vec3;

const std::string kSamples = TOPO_SAMPLES;

double along(Vec3 p, Vec3 d) { return p.x * d.x + p.y * d.y + p.z * d.z; }

uint32_t degree(const topo::ReebNode& n) { return n.down + n.up; }

// Every arc goes up from its lower node to its upper node, through points whose heights increase strictly.
void expect_monotone_arcs(const Mesh& m, const topo::Elevation& e, const topo::ReebGraph& g, Vec3 d, const std::string& name) {
    for (const auto& a : g.arcs) {
        const uint32_t lo = g.nodes[a.lower].vertex, hi = g.nodes[a.upper].vertex;
        ASSERT_LT(e.rank[lo], e.rank[hi]) << name;
        double last = e.height[lo];
        for (const auto& p : a.path) {
            const double h = along(p, d);
            EXPECT_GT(h, last - 1e-9) << name;
            last = h;
        }
        EXPECT_LT(last, e.height[hi] + 1e-9) << name;
        (void)m;
    }
}

Vec3 unit(Vec3 d) {
    const double n = std::sqrt(along(d, d));
    return {d.x / n, d.y / n, d.z / n};
}

// A convex sphere: a segment, from the bottom to the top.
TEST(Reeb, ASphereIsOneArc) {
    const Mesh sphere = Mesh::load(kSamples + "/sphere.obj");
    const auto e = topo::elevation(sphere, {0, 1, 0});
    const auto g = topo::reeb_graph(sphere, e);
    ASSERT_EQ(g.nodes.size(), 2u);
    ASSERT_EQ(g.arcs.size(), 1u);
    EXPECT_EQ(g.loops(), 0u);
    EXPECT_EQ(g.nodes[0].vertex, e.order.front());
    EXPECT_EQ(g.nodes[1].vertex, e.order.back());
    EXPECT_EQ(g.arcs[0].lower, 0u);
    EXPECT_EQ(g.arcs[0].upper, 1u);
    // The sampled heights give the drawing its points: the arc runs up the sphere's axis.
    EXPECT_GE(g.arcs[0].path.size(), 20u);
    for (const auto& p : g.arcs[0].path) {
        EXPECT_NEAR(p.x, 0, 0.05);
        EXPECT_NEAR(p.z, 0, 0.05);
    }
    expect_monotone_arcs(sphere, e, g, {0, 1, 0}, "sphere");
}

// The standing torus: minimum, saddle, saddle, maximum; the two saddles joined by two arcs (the two arms of
// the ring): one loop, the genus.
TEST(Reeb, AStandingTorusHasOneLoop) {
    const Mesh torus = Mesh::load(kSamples + "/torus.obj");
    const auto e = topo::elevation(torus, {0, 1, 0});
    const auto g = topo::reeb_graph(torus, e);
    ASSERT_EQ(g.nodes.size(), 4u);
    ASSERT_EQ(g.arcs.size(), 4u);
    EXPECT_EQ(g.loops(), 1u);
    EXPECT_EQ(g.components, 1u);
    std::vector<uint32_t> degrees;
    for (const auto& n : g.nodes) degrees.push_back(degree(n));
    EXPECT_EQ(degrees, (std::vector<uint32_t>{1, 3, 3, 1}));
    // The lower saddle splits the level set in two (one in, two out), the upper one merges them back.
    EXPECT_EQ(g.nodes[1].down, 1u);
    EXPECT_EQ(g.nodes[1].up, 2u);
    EXPECT_EQ(g.nodes[2].down, 2u);
    EXPECT_EQ(g.nodes[2].up, 1u);
    // The two arms are on either side of the hole.
    std::vector<const topo::ReebArc*> arms;
    for (const auto& a : g.arcs) {
        if (a.lower == 1 && a.upper == 2) arms.push_back(&a);
    }
    ASSERT_EQ(arms.size(), 2u);
    ASSERT_FALSE(arms[0]->path.empty());
    ASSERT_FALSE(arms[1]->path.empty());
    const double x0 = arms[0]->path[arms[0]->path.size() / 2].x, x1 = arms[1]->path[arms[1]->path.size() / 2].x;
    EXPECT_LT(x0 * x1, 0) << "one arm on each side";
    expect_monotone_arcs(torus, e, g, {0, 1, 0}, "torus");
}

// Cole-McLaughlin et al.: on a closed orientable surface the Reeb graph of a generic function has exactly
// genus loops, whatever the function; on any surface, at most b1 loops; one graph component per mesh
// component.
TEST(Reeb, LoopsAreTheGenusOnClosedOrientableSurfaces) {
    struct Case {
        Mesh mesh;
        bool closed_orientable;
    };
    std::map<std::string, Case> cases;
    cases.emplace("torus", Case{Mesh::load(kSamples + "/torus.obj"), true});
    cases.emplace("sphere", Case{Mesh::load(kSamples + "/sphere.obj"), true});
    cases.emplace("two tori", Case{shapes::disjoint_union(shapes::grid(10, 6, shapes::Gluing::Torus), shapes::grid(8, 5, shapes::Gluing::Torus)), true});
    cases.emplace("mobius", Case{Mesh::load(kSamples + "/mobius.obj"), false});
    cases.emplace("saddle", Case{Mesh::load(kSamples + "/saddle.obj"), false});
    cases.emplace("cylinder", Case{shapes::grid(12, 4, shapes::Gluing::Cylinder), false});
    for (const auto& [name, c] : cases) {
        const auto inv = topo::analyze(c.mesh);
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0, 0, 1}, Vec3{1, 1, 1}, Vec3{-0.2, 0.9, 0.4}}) {
            const auto e = topo::elevation(c.mesh, d);
            const auto g = topo::reeb_graph(c.mesh, e, 12);
            EXPECT_EQ(g.components, inv.components) << name;
            if (c.closed_orientable) {
                EXPECT_EQ(static_cast<int64_t>(g.loops()), *inv.genus) << name;
            } else {
                // b1 over Z/2 of a surface with boundary: components - chi.
                EXPECT_LE(static_cast<int64_t>(g.loops()), static_cast<int64_t>(inv.components) - inv.euler_characteristic) << name;
            }
            expect_monotone_arcs(c.mesh, e, g, unit(d), name);
        }
    }
}

// On a closed orientable surface, extrema have degree 1 and simple saddles degree 3; a noisy height adds
// critical points but no loop.
TEST(Reeb, DegreesOnAClosedSurface) {
    const Mesh torus = shapes::grid(24, 12, shapes::Gluing::Torus);
    std::mt19937 rng(38);
    std::uniform_real_distribution<double> noise(-0.6, 0.6);
    auto e = topo::elevation(torus, {0.3, 1, 0.2});
    std::vector<topo::Vec3> moved = torus.positions();
    for (auto& p : moved) p.y += noise(rng);
    const Mesh rough(moved, torus.triangles());
    e = topo::elevation(rough, {0.3, 1, 0.2});
    const auto g = topo::reeb_graph(rough, e, 0);
    ASSERT_GT(g.nodes.size(), 10u) << "the noise must add critical points";
    EXPECT_EQ(g.loops(), 1u);
    std::map<uint32_t, int> index;
    for (const auto& c : e.critical) index[c.vertex] = c.index;
    for (const auto& n : g.nodes) {
        ASSERT_TRUE(index.contains(n.vertex)) << "a node of a closed surface is a critical point";
        const int i = index[n.vertex];
        EXPECT_EQ(static_cast<int>(degree(n)), i == 1 ? 1 : 2 - i) << "vertex " << n.vertex;
    }
}

TEST(Reeb, RefusesTooManyNodesAndAForeignElevation) {
    const Mesh torus = Mesh::load(kSamples + "/torus.obj");
    const auto e = topo::elevation(torus, {0, 1, 0});
    EXPECT_THROW((void)topo::reeb_graph(torus, e, 8, 3), std::length_error);
    EXPECT_EQ(topo::reeb_graph(torus, e, 8, 4).nodes.size(), 4u);
    const Mesh sphere = Mesh::load(kSamples + "/sphere.obj");
    EXPECT_THROW((void)topo::reeb_graph(sphere, e), std::invalid_argument);
}

// A disk (an open surface): its top is on the boundary and not critical, but the level set vanishes there:
// it is a node.
TEST(Reeb, ABoundaryTopIsANode) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}}, {{{0, 1, 3}}, {{0, 3, 2}}});
    const auto e = topo::elevation(m, {0.1, 1, 0});
    const auto g = topo::reeb_graph(m, e);
    ASSERT_EQ(g.nodes.size(), 2u);
    EXPECT_EQ(g.nodes[1].vertex, e.order.back());
    EXPECT_EQ(g.arcs.size(), 1u);
}

}  // namespace
