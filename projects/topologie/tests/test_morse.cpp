// Height and critical points (piecewise-linear Morse theory): known shapes, and the theorems that must
// hold on any mesh.
#include "shapes.hpp"
#include "topo/invariants.hpp"
#include "topo/morse.hpp"

#include <gtest/gtest.h>

#include <cmath>
#include <map>
#include <numbers>
#include <set>
#include <stdexcept>
#include <string>

namespace {

using topo::CriticalKind;
using topo::Mesh;
using topo::Vec3;

const std::string kSamples = TOPO_SAMPLES;

struct Count {
    int minima = 0, saddles = 0, maxima = 0, other = 0;  // saddles with their multiplicity
};

Count count(const topo::Elevation& e) {
    Count c;
    for (const auto& p : e.critical) {
        switch (p.kind) {
            case CriticalKind::Minimum: ++c.minima; break;
            case CriticalKind::Saddle: c.saddles += -p.index; break;
            case CriticalKind::Maximum: ++c.maxima; break;
            case CriticalKind::Other: ++c.other; break;
        }
    }
    return c;
}

int index_sum(const topo::Elevation& e) {
    int s = 0;
    for (const auto& p : e.critical) s += p.index;
    return s;
}

// Euler characteristic of the sublevel set up to rank r, counted from scratch: the vertices of rank <= r,
// the edges and the faces all of whose vertices have rank <= r.
int sublevel_euler(const Mesh& m, const topo::Elevation& e, uint32_t r) {
    std::set<std::pair<uint32_t, uint32_t>> edges;
    int vertices = 0, faces = 0;
    for (uint32_t v = 0; v < m.vertex_count(); ++v) vertices += e.rank[v] <= r ? 1 : 0;
    for (const auto& t : m.triangles()) {
        const bool in[3] = {e.rank[t[0]] <= r, e.rank[t[1]] <= r, e.rank[t[2]] <= r};
        faces += in[0] && in[1] && in[2] ? 1 : 0;
        for (std::size_t k = 0; k < 3; ++k) {
            if (in[k] && in[(k + 1) % 3]) edges.insert({std::min(t[k], t[(k + 1) % 3]), std::max(t[k], t[(k + 1) % 3])});
        }
    }
    return vertices - static_cast<int>(edges.size()) + faces;
}

TEST(Morse, HeightIsAlongTheDirectionAndTheOrderSortsIt) {
    const Mesh m({{0, 0, 0}, {1, 2, 3}, {-1, 2, 0}}, {{{0, 1, 2}}});
    const auto e = topo::elevation(m, {0, 2, 0});  // not unit: normalised
    EXPECT_EQ(e.height, (std::vector<double>{0, 2, 2}));
    // Equal heights: the smaller index comes first.
    EXPECT_EQ(e.order, (std::vector<uint32_t>{0, 1, 2}));
    EXPECT_EQ(e.rank, (std::vector<uint32_t>{0, 1, 2}));
    EXPECT_THROW((void)topo::elevation(m, {0, 0, 0}), std::invalid_argument);
}

// A convex surface has no saddle: one minimum and one maximum, whatever the direction.
TEST(Morse, AConvexSphereHasOneMinimumAndOneMaximum) {
    const Mesh sphere = Mesh::load(kSamples + "/sphere.obj");
    for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{1, 0, 0}, Vec3{0.3, -0.8, 0.52}}) {
        const auto c = count(topo::elevation(sphere, d));
        EXPECT_EQ(c.minima, 1);
        EXPECT_EQ(c.maxima, 1);
        EXPECT_EQ(c.saddles, 0);
        EXPECT_EQ(c.other, 0);
    }
}

// The textbook example: a torus standing on its edge has a minimum, two saddles (where the hole begins
// and where it closes) and a maximum. The sample's axis is z; y runs across it.
TEST(Morse, AStandingTorusHasAMinimumTwoSaddlesAndAMaximum) {
    const Mesh torus = Mesh::load(kSamples + "/torus.obj");
    const auto e = topo::elevation(torus, {0, 1, 0});
    const auto c = count(e);
    EXPECT_EQ(c.minima, 1);
    EXPECT_EQ(c.saddles, 2);
    EXPECT_EQ(c.maxima, 1);
    EXPECT_EQ(c.other, 0);
    // In the order of the filtration: minimum, the two saddles, maximum.
    ASSERT_EQ(e.critical.size(), 4u);
    EXPECT_EQ(e.critical.front().kind, CriticalKind::Minimum);
    EXPECT_EQ(e.critical[1].kind, CriticalKind::Saddle);
    EXPECT_EQ(e.critical[2].kind, CriticalKind::Saddle);
    EXPECT_EQ(e.critical.back().kind, CriticalKind::Maximum);
    EXPECT_EQ(e.critical.back().vertex, e.order.back());
}

// A monkey saddle: the centre's six neighbours go up, down, up, down, up, down. Its lower link has
// three pieces: one saddle point of multiplicity 2, index -2.
TEST(Morse, AMonkeySaddleCountsTwice) {
    std::vector<Vec3> p{{0, 0, 0}};
    for (int k = 0; k < 6; ++k) {
        const double a = k * std::numbers::pi / 3;
        p.push_back({std::cos(a), k % 2 == 0 ? 1.0 : -1.0, std::sin(a)});
    }
    std::vector<topo::Triangle> t;
    for (uint32_t k = 0; k < 6; ++k) t.push_back({0, 1 + k, 1 + (k + 1) % 6});
    const auto e = topo::elevation(Mesh(p, t));
    const auto centre = std::find_if(e.critical.begin(), e.critical.end(), [](const auto& c) { return c.vertex == 0; });
    ASSERT_NE(centre, e.critical.end());
    EXPECT_EQ(centre->kind, CriticalKind::Saddle);
    EXPECT_EQ(centre->index, -2);
}

// Morse's theorem, discrete: the indices sum to the Euler characteristic, on every mesh and direction,
// boundaries and non-orientable surfaces included.
TEST(Morse, IndicesSumToTheEulerCharacteristic) {
    std::map<std::string, Mesh> meshes;
    for (const char* name : {"torus", "sphere", "mobius", "saddle"}) meshes.emplace(name, Mesh::load(kSamples + "/" + name + ".obj"));
    meshes.emplace("cylinder", shapes::grid(12, 4, shapes::Gluing::Cylinder));
    meshes.emplace("two tori", shapes::disjoint_union(shapes::grid(10, 6, shapes::Gluing::Torus), shapes::grid(8, 5, shapes::Gluing::Torus)));
    for (const auto& [name, m] : meshes) {
        const auto chi = topo::analyze(m).euler_characteristic;
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0, 0, 1}, Vec3{1, 1, 1}, Vec3{-0.2, 0.9, 0.4}}) {
            const auto e = topo::elevation(m, d);
            EXPECT_EQ(index_sum(e), chi) << name;
            EXPECT_EQ(e.euler.back(), chi) << name;
            EXPECT_GE(count(e).minima, static_cast<int>(topo::analyze(m).components)) << name;
        }
    }
}

// The sublevel Euler characteristic, kept vertex by vertex, against a count from scratch.
TEST(Morse, SublevelEulerCharacteristicMatchesACountFromScratch) {
    for (const char* name : {"torus", "mobius", "saddle"}) {
        const Mesh m = Mesh::load(kSamples + "/" + name + ".obj");
        const auto e = topo::elevation(m, {0.1, 1, 0.2});
        for (uint32_t r = 0; r < m.vertex_count(); r += 37) EXPECT_EQ(e.euler[r], sublevel_euler(m, e, r)) << name << " rank " << r;
        // Between two critical vertices, chi does not change.
        std::set<uint32_t> critical;
        for (const auto& c : e.critical) critical.insert(c.vertex);
        for (uint32_t r = 1; r < m.vertex_count(); ++r) {
            if (!critical.contains(e.order[r])) {
                EXPECT_EQ(e.euler[r], e.euler[r - 1]) << name;
            }
        }
    }
}

// The highest point of an open surface is on its boundary and changes nothing: not critical.
TEST(Morse, ABoundaryTopIsNotCritical) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{{0, 1, 2}}});
    const auto e = topo::elevation(m, {0, 1, 0});
    ASSERT_EQ(e.critical.size(), 1u);
    EXPECT_EQ(e.critical[0].kind, CriticalKind::Minimum);
    EXPECT_EQ(e.critical[0].vertex, 0u);
}

}  // namespace
