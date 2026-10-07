// Ray intersections with spheres, planes, triangles and boxes.
#include "rt/geometry.hpp"

#include <gtest/gtest.h>

namespace {

using rt::Ray;
using rt::Vec3;
constexpr double kInf = rt::kInfinity;

TEST(Sphere, NearestRootFromOutsideFarRootFromInside) {
    const Ray r{{0, 0, -5}, {0, 0, 1}};
    EXPECT_DOUBLE_EQ(*rt::hit_sphere(r, {0, 0, 0}, 1, 0, kInf), 4);
    const Ray inside{{0, 0, 0}, {0, 0, 1}};
    EXPECT_DOUBLE_EQ(*rt::hit_sphere(inside, {0, 0, 0}, 1, 0, kInf), 1);
}

TEST(Sphere, MissesBehindBeyondAndBeside) {
    EXPECT_FALSE(rt::hit_sphere({{0, 0, 5}, {0, 0, 1}}, {0, 0, 0}, 1, 0, kInf));   // behind
    EXPECT_FALSE(rt::hit_sphere({{0, 0, -5}, {0, 0, 1}}, {0, 0, 0}, 1, 0, 3.9));  // beyond tmax
    EXPECT_FALSE(rt::hit_sphere({{0, 1.001, -5}, {0, 0, 1}}, {0, 0, 0}, 1, 0, kInf));
}

TEST(Sphere, StaysPreciseFarAway) {
    // A small sphere a million units away: the naive quadratic loses the hit to cancellation.
    const Ray r{{0, 0, -1e6}, {0, 0, 1}};
    const auto t = rt::hit_sphere(r, {0, 0, 0}, 0.01, 0, kInf);
    ASSERT_TRUE(t.has_value());
    EXPECT_NEAR(*t, 1e6 - 0.01, 1e-6);
}

TEST(Plane, HitsOnlyInFrontAndNotWhenParallel) {
    EXPECT_DOUBLE_EQ(*rt::hit_plane({{0, 2, 0}, {0, -1, 0}}, {0, 0, 0}, {0, 1, 0}, 0, kInf), 2);
    EXPECT_FALSE(rt::hit_plane({{0, 2, 0}, {0, 1, 0}}, {0, 0, 0}, {0, 1, 0}, 0, kInf));
    EXPECT_FALSE(rt::hit_plane({{0, 2, 0}, {1, 0, 0}}, {0, 0, 0}, {0, 1, 0}, 0, kInf));
}

TEST(Triangle, BarycentricsAndBothSides) {
    const Vec3 a{0, 0, 0}, b{1, 0, 0}, c{0, 1, 0};
    const auto h = rt::hit_triangle({{0.25, 0.5, 1}, {0, 0, -1}}, a, b, c, 0, kInf);
    ASSERT_TRUE(h.has_value());
    EXPECT_DOUBLE_EQ(h->t, 1);
    EXPECT_DOUBLE_EQ(h->u, 0.25);
    EXPECT_DOUBLE_EQ(h->v, 0.5);
    EXPECT_TRUE(rt::hit_triangle({{0.25, 0.5, -1}, {0, 0, 1}}, a, b, c, 0, kInf));  // from behind
}

TEST(Triangle, MissesOutsideParallelAndBehind) {
    const Vec3 a{0, 0, 0}, b{1, 0, 0}, c{0, 1, 0};
    EXPECT_FALSE(rt::hit_triangle({{0.6, 0.6, 1}, {0, 0, -1}}, a, b, c, 0, kInf));  // u + v > 1
    EXPECT_FALSE(rt::hit_triangle({{-0.1, 0.5, 1}, {0, 0, -1}}, a, b, c, 0, kInf));
    EXPECT_FALSE(rt::hit_triangle({{0.2, 0.2, 1}, {1, 0, 0}}, a, b, c, 0, kInf));   // parallel
    EXPECT_FALSE(rt::hit_triangle({{0.2, 0.2, 1}, {0, 0, 1}}, a, b, c, 0, kInf));   // behind
}

TEST(Box, SlabTestIncludingAxisParallelRays) {
    rt::Box b;
    b.grow(Vec3{-1, -1, -1});
    b.grow(Vec3{1, 1, 1});
    EXPECT_DOUBLE_EQ(b.area(), 24);
    const auto inv = [](const Vec3& d) { return Vec3{1 / d.x, 1 / d.y, 1 / d.z}; };
    const Ray r{{0, 0, -5}, {0, 0, 1}};
    EXPECT_DOUBLE_EQ(rt::hit_box(b, r, inv(r.dir), 0, kInf), 4);
    const Ray beside{{2, 0, -5}, {0, 0, 1}};
    EXPECT_EQ(rt::hit_box(b, beside, inv(beside.dir), 0, kInf), kInf);
    const Ray inside{{0, 0, 0}, rt::normalize({1, 2, 3})};
    EXPECT_DOUBLE_EQ(rt::hit_box(b, inside, inv(inside.dir), 0, kInf), 0);
    // Exactly on a face, parallel to it: 0 * infinity is NaN, which must not break the test.
    const Ray grazing{{1, 0, -5}, {0, 0, 1}};
    EXPECT_NE(rt::hit_box(b, grazing, inv(grazing.dir), 0, kInf), kInf);
}

}  // namespace
