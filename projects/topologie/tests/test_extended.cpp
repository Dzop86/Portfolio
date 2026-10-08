// Extended persistence of the height (D50): the standing torus and the sphere, agreement with ordinary
// persistence, the dualities of Poincaré and Lefschetz as an oracle, and the Reeb graph's loops.
#include "shapes.hpp"
#include "topo/extended.hpp"
#include "topo/invariants.hpp"
#include "topo/morse.hpp"
#include "topo/persistence.hpp"
#include "topo/reeb.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <map>
#include <random>
#include <stdexcept>
#include <string>
#include <utility>

namespace {

using topo::ExtendedPair;
using topo::Mesh;
using topo::Vec3;

const std::string kSamples = TOPO_SAMPLES;

std::vector<ExtendedPair> of_dimension(const std::vector<ExtendedPair>& list, int dim) {
    std::vector<ExtendedPair> out;
    for (const auto& p : list) {
        if (p.dimension == dim) out.push_back(p);
    }
    return out;
}

// (birth, death) values, sorted; `flip` reflects them across the diagonal.
std::vector<std::pair<double, double>> points(const std::vector<ExtendedPair>& list, bool flip = false) {
    std::vector<std::pair<double, double>> out;
    for (const auto& p : list) out.push_back(flip ? std::pair{p.death, p.birth} : std::pair{p.birth, p.death});
    std::sort(out.begin(), out.end());
    return out;
}

std::vector<double> noisy(const Mesh& m, Vec3 d, unsigned seed, double amplitude) {
    auto h = topo::elevation(m, d).height;
    std::mt19937 rng(seed);
    std::uniform_real_distribution<double> noise(-amplitude, amplitude);
    for (auto& x : h) x += noise(rng);
    return h;
}

TEST(Extended, NeedsOneHeightPerVertex) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{{0, 1, 2}}});
    EXPECT_THROW((void)topo::extended_persistence(m, {0, 1}), std::invalid_argument);
}

// A sphere: its component lives from the bottom up to the top, then its cavity from the top down to the bottom.
TEST(Extended, ASpherePairsItsMinimumAndItsMaximum) {
    const Mesh sphere = Mesh::load(kSamples + "/sphere.obj");
    const auto e = topo::elevation(sphere, {0, 1, 0});
    const auto x = topo::extended_persistence(sphere, e.height);
    EXPECT_TRUE(x.ordinary.empty());
    EXPECT_TRUE(x.relative.empty());
    ASSERT_EQ(x.extended.size(), 2u);
    const uint32_t lo = e.order.front(), hi = e.order.back();
    EXPECT_EQ(x.extended[0].dimension, 0);
    EXPECT_EQ(std::pair(x.extended[0].birth_vertex, x.extended[0].death_vertex), std::pair(lo, hi));
    EXPECT_EQ(x.extended[1].dimension, 2);
    EXPECT_EQ(std::pair(x.extended[1].birth_vertex, x.extended[1].death_vertex), std::pair(hi, lo));
}

// The standing torus (Charles's review, D50): ordinary persistence keeps its two loops forever, extended
// persistence pairs them saddle to saddle, once each way; the pair going up is the Reeb graph's loop.
TEST(Extended, AStandingTorusPairsItsLoopsSaddleToSaddle) {
    const Mesh torus = Mesh::load(kSamples + "/torus.obj");
    const auto e = topo::elevation(torus, {0, 1, 0});
    const auto x = topo::extended_persistence(torus, e.height);
    EXPECT_TRUE(x.ordinary.empty());
    EXPECT_TRUE(x.relative.empty());
    const uint32_t lo = e.order.front(), hi = e.order.back();
    const auto g = topo::reeb_graph(torus, e);
    ASSERT_EQ(g.nodes.size(), 4u);
    const uint32_t s1 = g.nodes[1].vertex, s2 = g.nodes[2].vertex;  // the lower saddle, the upper one
    const auto h0 = of_dimension(x.extended, 0), h1 = of_dimension(x.extended, 1), h2 = of_dimension(x.extended, 2);
    ASSERT_EQ(h0.size(), 1u);
    ASSERT_EQ(h1.size(), 2u);
    ASSERT_EQ(h2.size(), 1u);
    EXPECT_EQ(std::pair(h0[0].birth_vertex, h0[0].death_vertex), std::pair(lo, hi));
    EXPECT_EQ(std::pair(h2[0].birth_vertex, h2[0].death_vertex), std::pair(hi, lo));
    std::vector<std::pair<uint32_t, uint32_t>> loops{{h1[0].birth_vertex, h1[0].death_vertex}, {h1[1].birth_vertex, h1[1].death_vertex}};
    std::sort(loops.begin(), loops.end());
    std::vector<std::pair<uint32_t, uint32_t>> expected{{s1, s2}, {s2, s1}};
    std::sort(expected.begin(), expected.end());
    EXPECT_EQ(loops, expected);
}

// Going up only, the same pairs as topo::persistence; the classes it leaves essential are the extended ones,
// as many per dimension as the Betti numbers.
TEST(Extended, AgreesWithOrdinaryPersistence) {
    const Mesh torus = shapes::grid(20, 10, shapes::Gluing::Torus);
    const auto h = noisy(torus, {0.3, 1, 0.2}, 7, 1.5);
    const auto x = topo::extended_persistence(torus, h);
    const auto p = topo::persistence(torus, h);
    std::vector<ExtendedPair> finite;
    for (const auto& q : p.pairs) {
        if (!q.essential()) finite.push_back({q.dimension, q.birth_vertex, q.death_vertex, q.birth, q.death});
    }
    ASSERT_GT(finite.size(), 10u);
    EXPECT_EQ(points(x.ordinary), points(finite));
    for (int d = 0; d < 3; ++d) EXPECT_EQ(static_cast<int>(of_dimension(x.extended, d).size()), p.betti[static_cast<std::size_t>(d)]) << d;
}

// Cohen-Steiner, Edelsbrunner and Harer (2009): on a closed surface, ordinary pairs of dimension p are the
// relative pairs of dimension 2 - p reflected across the diagonal, and extended pairs of dimension p those of
// dimension 2 - p. An oracle independent of the reduction, on rough heights with hundreds of pairs.
TEST(Extended, PoincareAndLefschetzDualities) {
    std::map<std::string, Mesh> meshes;
    meshes.emplace("torus", shapes::grid(24, 12, shapes::Gluing::Torus));
    meshes.emplace("two tori", shapes::disjoint_union(shapes::grid(12, 6, shapes::Gluing::Torus), shapes::grid(10, 8, shapes::Gluing::Torus)));
    meshes.emplace("sphere", Mesh::load(kSamples + "/sphere.obj"));
    unsigned seed = 50;
    for (const auto& [name, mesh] : meshes) {
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0.3, 1, 0.2}, Vec3{1, 0.2, -0.5}}) {
            const auto x = topo::extended_persistence(mesh, noisy(mesh, d, seed++, 0.6));
            EXPECT_GT(x.ordinary.size() + x.relative.size(), 0u) << name;
            for (int p = 0; p <= 2; ++p) {
                EXPECT_EQ(points(of_dimension(x.ordinary, p)), points(of_dimension(x.relative, 2 - p), true)) << name << " Ord" << p;
                EXPECT_EQ(points(of_dimension(x.extended, p)), points(of_dimension(x.extended, 2 - p), true)) << name << " Ext" << p;
            }
        }
    }
}

// On a closed orientable surface, extended pairs of dimension 1 come in mirror pairs, as many above the
// diagonal as the genus: as many as the Reeb graph's loops (Cole-McLaughlin et al.), whatever the direction.
TEST(Extended, LoopPairsGoingUpAreTheReebGraphsLoops) {
    std::map<std::string, Mesh> meshes;
    meshes.emplace("torus", Mesh::load(kSamples + "/torus.obj"));
    meshes.emplace("sphere", Mesh::load(kSamples + "/sphere.obj"));
    meshes.emplace("two tori", shapes::disjoint_union(shapes::grid(10, 6, shapes::Gluing::Torus), shapes::grid(8, 5, shapes::Gluing::Torus)));
    for (const auto& [name, mesh] : meshes) {
        const auto inv = topo::analyze(mesh);
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0, 0, 1}, Vec3{1, 1, 1}, Vec3{-0.2, 0.9, 0.4}}) {
            const auto e = topo::elevation(mesh, d);
            const auto x = topo::extended_persistence(mesh, e.height);
            const auto g = topo::reeb_graph(mesh, e, 12);
            const auto h1 = of_dimension(x.extended, 1);
            const auto up = std::count_if(h1.begin(), h1.end(), [](const ExtendedPair& p) { return p.death > p.birth; });
            EXPECT_EQ(static_cast<int64_t>(h1.size()), 2 * *inv.genus) << name;
            EXPECT_EQ(up, *inv.genus) << name;
            EXPECT_EQ(static_cast<std::size_t>(up), g.loops()) << name;
        }
    }
}

}  // namespace
