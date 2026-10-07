// Optics and random numbers.
#include "rt/math.hpp"
#include "rt/rng.hpp"

#include <gtest/gtest.h>

#include <cmath>

namespace {

using rt::Vec3;

TEST(Optics, ReflectionKeepsTheAngle) {
    const Vec3 d = rt::normalize({1, -1, 0});
    const Vec3 r = rt::reflect(d, {0, 1, 0});
    EXPECT_DOUBLE_EQ(r.x, d.x);
    EXPECT_DOUBLE_EQ(r.y, -d.y);
    EXPECT_DOUBLE_EQ(r.z, 0);
}

TEST(Optics, RefractionFollowsSnellsLaw) {
    const Vec3 n{0, 1, 0};
    for (const double angle : {0.0, 0.3, 0.7, 1.2}) {
        const Vec3 d{std::sin(angle), -std::cos(angle), 0};
        const double eta = 1 / 1.5;
        const auto t = rt::refract(d, n, eta);
        ASSERT_TRUE(t.has_value());
        EXPECT_NEAR(rt::length(*t), 1, 1e-12);
        // sin(transmitted) = eta sin(incident), on the same side of the normal.
        EXPECT_NEAR(t->x, eta * std::sin(angle), 1e-12);
        EXPECT_LT(t->y, 0);
    }
}

TEST(Optics, TotalInternalReflectionBeyondTheCriticalAngle) {
    const double critical = std::asin(1 / 1.5);
    const Vec3 n{0, 1, 0};
    EXPECT_TRUE(rt::refract({std::sin(critical - 0.01), -std::cos(critical - 0.01), 0}, n, 1.5).has_value());
    EXPECT_FALSE(rt::refract({std::sin(critical + 0.01), -std::cos(critical + 0.01), 0}, n, 1.5).has_value());
}

TEST(Optics, SchlickGoesFromR0ToOne) {
    EXPECT_NEAR(rt::schlick(1, 1 / 1.5), 0.04, 1e-12);  // glass seen head-on: 4 %
    EXPECT_DOUBLE_EQ(rt::schlick(0, 1 / 1.5), 1);         // grazing: a mirror
    double previous = 1;
    for (double c = 0.1; c <= 1; c += 0.1) {
        const double f = rt::schlick(c, 1 / 1.5);
        EXPECT_LT(f, previous);
        previous = f;
    }
}

TEST(Optics, BasisIsOrthonormal) {
    for (const Vec3 n : {Vec3{0, 0, 1}, Vec3{0, 0, -1}, rt::normalize({1, 2, 3}), rt::normalize({-0.3, 0.1, -0.9})}) {
        Vec3 t, b;
        rt::basis(n, t, b);
        EXPECT_NEAR(rt::length(t), 1, 1e-12);
        EXPECT_NEAR(rt::length(b), 1, 1e-12);
        EXPECT_NEAR(rt::dot(t, n), 0, 1e-12);
        EXPECT_NEAR(rt::dot(b, n), 0, 1e-12);
        EXPECT_NEAR(rt::dot(t, b), 0, 1e-12);
    }
}

TEST(Random, StreamsAreReproducibleAndDistinct) {
    auto a = rt::Rng::for_sample(3, 4, 100, 7), b = rt::Rng::for_sample(3, 4, 100, 7);
    auto c = rt::Rng::for_sample(4, 4, 100, 7), d = rt::Rng::for_sample(3, 4, 100, 8);
    const uint64_t first = a.next();
    EXPECT_EQ(first, b.next());
    EXPECT_NE(first, c.next());
    EXPECT_NE(first, d.next());
}

TEST(Random, UniformStaysInTheUnitIntervalWithTheRightMean) {
    rt::Rng r(42);
    double sum = 0;
    constexpr int n = 200000;
    for (int i = 0; i < n; ++i) {
        const double u = r.uniform();
        ASSERT_GE(u, 0);
        ASSERT_LT(u, 1);
        sum += u;
    }
    EXPECT_NEAR(sum / n, 0.5, 0.005);
}

}  // namespace
