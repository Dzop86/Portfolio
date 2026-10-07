// Persistence diagrams of the height: known shapes, the Betti numbers, the critical points, and the
// stability theorem.
#include "shapes.hpp"
#include "topo/invariants.hpp"
#include "topo/morse.hpp"
#include "topo/persistence.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <cmath>
#include <functional>
#include <limits>
#include <map>
#include <random>
#include <stdexcept>
#include <string>

namespace {

using topo::Mesh;
using topo::PersistencePair;
using topo::Vec3;

const std::string kSamples = TOPO_SAMPLES;

std::vector<double> heights(const Mesh& m, Vec3 d) { return topo::elevation(m, d).height; }

std::vector<PersistencePair> of_dimension(const topo::Persistence& p, int dim, bool essential) {
    std::vector<PersistencePair> out;
    for (const auto& x : p.pairs) {
        if (x.dimension == dim && x.essential() == essential) out.push_back(x);
    }
    return out;
}

// Betti numbers over Z/2 of a surface, from its invariants: b0 the components, b2 the closed ones,
// b1 from the Euler characteristic.
std::array<int, 3> expected_betti(const Mesh& m, int closed_components) {
    const auto inv = topo::analyze(m);
    const int b0 = static_cast<int>(inv.components), b2 = closed_components;
    return {b0, b0 + b2 - static_cast<int>(inv.euler_characteristic), b2};
}

TEST(Persistence, NeedsOneHeightPerVertex) {
    const Mesh m({{0, 0, 0}, {1, 0, 0}, {0, 1, 0}}, {{{0, 1, 2}}});
    EXPECT_THROW((void)topo::persistence(m, {0, 1}), std::invalid_argument);
}

// A convex sphere: one component born at the bottom, one cavity closed at the top, nothing else.
TEST(Persistence, ASphereHasOneComponentAndOneCavity) {
    const Mesh sphere = Mesh::load(kSamples + "/sphere.obj");
    const auto e = topo::elevation(sphere, {0, 1, 0});
    const auto p = topo::persistence(sphere, e.height);
    EXPECT_EQ(p.betti, (std::array<int, 3>{1, 0, 1}));
    const auto h0 = of_dimension(p, 0, true), h2 = of_dimension(p, 2, true);
    ASSERT_EQ(h0.size(), 1u);
    ASSERT_EQ(h2.size(), 1u);
    EXPECT_EQ(h0[0].birth_vertex, e.order.front());
    EXPECT_EQ(h2[0].birth_vertex, e.order.back());
    EXPECT_TRUE(std::isinf(h0[0].death));
    // A convex surface has no other critical point: every finite pair is a plateau tie, of zero length.
    for (const auto& x : p.pairs) {
        if (!x.essential()) {
            EXPECT_EQ(x.persistence(), 0.0);
        }
    }
}

// The standing torus: a component at the bottom, two loops born at the two saddles (the hole opens,
// then the ring closes), a cavity at the top. All four are essential.
TEST(Persistence, AStandingTorusHasTwoLoopsBornAtItsSaddles) {
    const Mesh torus = Mesh::load(kSamples + "/torus.obj");
    const auto e = topo::elevation(torus, {0, 1, 0});
    const auto p = topo::persistence(torus, e.height);
    EXPECT_EQ(p.betti, (std::array<int, 3>{1, 2, 1}));
    const auto loops = of_dimension(p, 1, true);
    ASSERT_EQ(loops.size(), 2u);
    ASSERT_EQ(e.critical.size(), 4u);
    EXPECT_EQ(loops[0].birth_vertex, e.critical[1].vertex);
    EXPECT_EQ(loops[1].birth_vertex, e.critical[2].vertex);
    EXPECT_EQ(of_dimension(p, 2, true).at(0).birth_vertex, e.critical[3].vertex);
    for (const auto& x : p.pairs) {
        if (!x.essential()) {
            EXPECT_EQ(x.persistence(), 0.0);
        }
    }
}

// A square terrain, flat at 0 but for pits of known depth (cones): each pit is a component born at its
// bottom that dies when the water reaches the plain, at 0, merged into the deepest one, which never dies.
// Persistence is exactly the depth.
TEST(Persistence, EachPitOfATerrainLivesAsLongAsItIsDeep) {
    const uint32_t n = 40;
    std::vector<Vec3> positions;
    for (uint32_t i = 0; i <= n; ++i) {
        for (uint32_t j = 0; j <= n; ++j) positions.push_back({double(i), 0, double(j)});
    }
    std::vector<topo::Triangle> triangles;
    for (uint32_t i = 0; i < n; ++i) {
        for (uint32_t j = 0; j < n; ++j) {
            const uint32_t a = i * (n + 1) + j, b = a + n + 1;
            triangles.push_back({a, b, b + 1});
            triangles.push_back({a, b + 1, a + 1});
        }
    }
    const Mesh m(positions, triangles);
    struct Pit {
        double x, z, depth;
    };
    const std::vector<Pit> pits{{8, 8, 3.0}, {30, 10, 1.5}, {12, 30, 0.75}, {30, 30, 2.25}};
    std::vector<double> h(m.vertex_count(), 0.0);
    for (uint32_t v = 0; v < m.vertex_count(); ++v) {
        for (const auto& pit : pits) {
            const double r = std::hypot(positions[v].x - pit.x, positions[v].z - pit.z);
            h[v] = std::min(h[v], -pit.depth * std::max(0.0, 1 - r / 5));
        }
    }
    const auto p = topo::persistence(m, h);
    EXPECT_EQ(p.betti, (std::array<int, 3>{1, 0, 0}));
    std::vector<double> lives;
    for (const auto& x : of_dimension(p, 0, false)) {
        if (x.persistence() > 0) lives.push_back(x.persistence());
    }
    std::sort(lives.begin(), lives.end());
    EXPECT_EQ(lives, (std::vector<double>{0.75, 1.5, 2.25}));
    EXPECT_EQ(of_dimension(p, 0, true).at(0).birth, -3.0);
    EXPECT_TRUE(of_dimension(p, 1, false).empty() || std::all_of(p.pairs.begin(), p.pairs.end(), [](const auto& x) {
        return x.dimension != 1 || x.persistence() == 0;
    }));
}

// Essential classes are the Betti numbers over Z/2, on every mesh and direction: b0 the components, b2
// the closed components (orientable or not), b1 = b0 + b2 - chi.
TEST(Persistence, EssentialClassesAreTheBettiNumbers) {
    struct Case {
        Mesh mesh;
        int closed;
    };
    std::map<std::string, Case> cases;
    cases.emplace("torus", Case{Mesh::load(kSamples + "/torus.obj"), 1});
    cases.emplace("sphere", Case{Mesh::load(kSamples + "/sphere.obj"), 1});
    cases.emplace("mobius", Case{Mesh::load(kSamples + "/mobius.obj"), 0});
    cases.emplace("saddle", Case{Mesh::load(kSamples + "/saddle.obj"), 0});
    cases.emplace("cylinder", Case{shapes::grid(12, 4, shapes::Gluing::Cylinder), 0});
    cases.emplace("two tori", Case{shapes::disjoint_union(shapes::grid(10, 6, shapes::Gluing::Torus), shapes::grid(8, 5, shapes::Gluing::Torus)), 2});
    for (const auto& [name, c] : cases) {
        const auto betti = expected_betti(c.mesh, c.closed);
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0, 0, 1}, Vec3{1, 1, 1}, Vec3{-0.2, 0.9, 0.4}}) {
            const auto p = topo::persistence(c.mesh, heights(c.mesh, d));
            EXPECT_EQ(p.betti, betti) << name;
            for (int dim = 0; dim < 3; ++dim) EXPECT_EQ(static_cast<int>(of_dimension(p, dim, true).size()), betti[static_cast<std::size_t>(dim)]) << name;
        }
    }
}

// Every pair starts and ends at critical points: a vertex is the birth or death of as many pairs as its
// Morse multiplicity |index| (a minimum one, a simple saddle one, a monkey saddle two), and a regular
// vertex of none.
TEST(Persistence, PairsStartAndEndAtCriticalPoints) {
    for (const char* name : {"torus", "mobius", "saddle", "sphere"}) {
        const Mesh m = Mesh::load(kSamples + "/" + name + ".obj");
        for (const Vec3 d : {Vec3{0, 1, 0}, Vec3{0.1, 1, 0.2}, Vec3{1, -0.3, 0.5}}) {
            const auto e = topo::elevation(m, d);
            const auto p = topo::persistence(m, e.height);
            std::vector<int> events(m.vertex_count(), 0);
            for (const auto& x : p.pairs) {
                ++events[x.birth_vertex];
                if (!x.essential()) ++events[x.death_vertex];
            }
            std::vector<int> expected(m.vertex_count(), 0);
            for (const auto& c : e.critical) expected[c.vertex] = std::abs(c.index);
            EXPECT_EQ(events, expected) << name;
            for (const auto& x : p.pairs) {
                if (!x.essential()) {
                    EXPECT_LT(e.rank[x.birth_vertex], e.rank[x.death_vertex]) << name;
                }
            }
        }
    }
}

// The documented order: finite pairs by birth then death, then the essential classes by dimension.
TEST(Persistence, PairsComeInTheDocumentedOrder) {
    // A rough height, so that there are finite pairs to order (a smooth torus only has its 4 essential ones).
    const Mesh torus = shapes::grid(20, 10, shapes::Gluing::Torus);
    std::mt19937 rng(7);
    std::uniform_real_distribution<double> noise(-1.5, 1.5);
    auto h = heights(torus, {0.3, 1, 0.2});
    for (auto& x : h) x += noise(rng);
    const auto p = topo::persistence(torus, h);
    ASSERT_GT(p.pairs.size(), 10u);
    const auto first_essential = std::find_if(p.pairs.begin(), p.pairs.end(), [](const auto& x) { return x.essential(); });
    EXPECT_TRUE(std::all_of(first_essential, p.pairs.end(), [](const auto& x) { return x.essential(); }));
    EXPECT_TRUE(std::is_sorted(p.pairs.begin(), first_essential, [](const auto& a, const auto& b) {
        return a.birth != b.birth ? a.birth < b.birth : a.death < b.death;
    }));
    EXPECT_TRUE(std::is_sorted(first_essential, p.pairs.end(), [](const auto& a, const auto& b) { return a.dimension < b.dimension; }));
    EXPECT_EQ(p.pairs.end() - first_essential, 4);
}

// Bottleneck distance between two diagrams of one dimension (finite pairs; a point may also be matched
// to the diagonal, at half its persistence), by binary search on the candidate values and a perfect
// matching (augmenting paths). Small diagrams only: this is a test oracle.
double bottleneck(const std::vector<PersistencePair>& a, const std::vector<PersistencePair>& b) {
    const std::size_t n = a.size(), m = b.size(), size = n + m;
    // Left: a's points then b's diagonal copies; right: b's points then a's diagonal copies.
    auto cost = [&](std::size_t i, std::size_t j) -> double {
        const bool ia = i < n, jb = j < m;
        if (ia && jb) return std::max(std::abs(a[i].birth - b[j].birth), std::abs(a[i].death - b[j].death));
        if (ia) return j - m == i ? a[i].persistence() / 2 : std::numeric_limits<double>::infinity();
        if (jb) return i - n == j ? b[j].persistence() / 2 : std::numeric_limits<double>::infinity();
        return 0;  // diagonal to diagonal
    };
    std::vector<double> candidates{0};
    for (std::size_t i = 0; i < size; ++i) {
        for (std::size_t j = 0; j < size; ++j) {
            const double c = cost(i, j);
            if (std::isfinite(c)) candidates.push_back(c);
        }
    }
    std::sort(candidates.begin(), candidates.end());
    candidates.erase(std::unique(candidates.begin(), candidates.end()), candidates.end());
    auto perfect = [&](double eps) {
        std::vector<std::size_t> match(size, size);
        for (std::size_t i = 0; i < size; ++i) {
            std::vector<bool> seen(size, false);
            std::function<bool(std::size_t)> augment = [&](std::size_t u) {
                for (std::size_t v = 0; v < size; ++v) {
                    if (seen[v] || cost(u, v) > eps) continue;
                    seen[v] = true;
                    if (match[v] == size || augment(match[v])) {
                        match[v] = u;
                        return true;
                    }
                }
                return false;
            };
            if (!augment(i)) return false;
        }
        return true;
    };
    std::size_t lo = 0, hi = candidates.size() - 1;
    while (lo < hi) {
        const std::size_t mid = (lo + hi) / 2;
        if (perfect(candidates[mid])) hi = mid;
        else lo = mid + 1;
    }
    return candidates[lo];
}

// The stability theorem (Cohen-Steiner, Edelsbrunner and Harer 2007): moving every height by at most eps
// moves the diagram by at most eps in bottleneck distance; the essential classes keep their count and
// their births move by at most eps.
TEST(Persistence, ASmallPerturbationMovesTheDiagramALittle) {
    const Mesh torus = shapes::grid(20, 10, shapes::Gluing::Torus);
    std::mt19937 rng(20261008);
    std::uniform_real_distribution<double> noise(-1, 1);
    auto base = heights(torus, {0.3, 1, 0.2});
    for (auto& h : base) h += 1.5 * noise(rng);  // a rough function (neighbours are about 0.6 apart): many pairs
    const auto p = topo::persistence(torus, base);
    const auto features = std::count_if(p.pairs.begin(), p.pairs.end(), [](const auto& x) { return !x.essential() && x.persistence() > 0.01; });
    ASSERT_GE(features, 10) << "the test needs a diagram with many points";
    for (const double eps : {0.001, 0.01, 0.05}) {
        auto moved = base;
        for (auto& h : moved) h += eps * noise(rng);
        const auto q = topo::persistence(torus, moved);
        EXPECT_EQ(q.betti, p.betti);
        for (int dim = 0; dim < 3; ++dim) {
            EXPECT_LE(bottleneck(of_dimension(p, dim, false), of_dimension(q, dim, false)), eps + 1e-12) << "dimension " << dim;
            auto births = [&](const topo::Persistence& r) {
                std::vector<double> out;
                for (const auto& x : of_dimension(r, dim, true)) out.push_back(x.birth);
                std::sort(out.begin(), out.end());
                return out;
            };
            const auto bp = births(p), bq = births(q);
            ASSERT_EQ(bp.size(), bq.size());
            for (std::size_t k = 0; k < bp.size(); ++k) EXPECT_LE(std::abs(bp[k] - bq[k]), eps + 1e-12);
        }
    }
}

// The oracle itself, on a hand-made case.
TEST(Persistence, BottleneckOracle) {
    const std::vector<PersistencePair> a{{0, 0, 1, 0.0, 4.0}, {0, 2, 3, 1.0, 1.4}};
    const std::vector<PersistencePair> b{{0, 0, 1, 0.5, 4.0}};
    // (0,4) -> (0.5,4) costs 0.5; (1,1.4) -> diagonal costs 0.2.
    EXPECT_DOUBLE_EQ(bottleneck(a, b), 0.5);
    EXPECT_DOUBLE_EQ(bottleneck(a, a), 0.0);
}

}  // namespace
