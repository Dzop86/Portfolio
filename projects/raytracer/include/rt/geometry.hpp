// Rays and their intersections with spheres, planes, triangles and boxes.
#pragma once

#include "rt/math.hpp"

#include <limits>
#include <optional>
#include <utility>

namespace rt {

inline constexpr double kInfinity = std::numeric_limits<double>::infinity();
// Secondary rays start this far from their surface so that they do not hit it again.
inline constexpr double kEpsilon = 1e-7;

struct Ray {
    Vec3 origin;
    Vec3 dir;  // unit length for every ray the renderer traces
};

constexpr Vec3 at(const Ray& r, double t) { return r.origin + t * r.dir; }

// Nearest t in (tmin, tmax) where the ray meets the sphere, if any (the far root when the origin is inside).
inline std::optional<double> hit_sphere(const Ray& r, const Vec3& center, double radius, double tmin, double tmax) {
    const Vec3 oc = r.origin - center;
    const double b = dot(oc, r.dir);
    const double c = dot(oc, oc) - radius * radius;
    // Written as the distance from the centre to the line, which keeps its precision far from the sphere.
    const Vec3 perp = oc - b * r.dir;
    const double h = radius * radius - dot(perp, perp);
    if (h < 0) return std::nullopt;
    const double sq = std::sqrt(h);
    // Two roots without cancellation: q = -b - sign(b) sq, t0 = c / q, t1 = q.
    const double q = b > 0 ? -b - sq : -b + sq;
    double t0 = c / q, t1 = q;
    if (q == 0) t0 = t1 = 0;
    if (t0 > t1) std::swap(t0, t1);
    if (t0 > tmin && t0 < tmax) return t0;
    if (t1 > tmin && t1 < tmax) return t1;
    return std::nullopt;
}

// The plane through `point` with unit normal `n`.
inline std::optional<double> hit_plane(const Ray& r, const Vec3& point, const Vec3& n, double tmin, double tmax) {
    const double denom = dot(r.dir, n);
    if (denom == 0) return std::nullopt;
    const double t = dot(point - r.origin, n) / denom;
    if (t > tmin && t < tmax) return t;
    return std::nullopt;
}

struct TriangleHit {
    double t, u, v;  // the point is (1 - u - v) a + u b + v c
};

// Möller-Trumbore, two-sided.
inline std::optional<TriangleHit> hit_triangle(const Ray& r, const Vec3& a, const Vec3& b, const Vec3& c,
                                               double tmin, double tmax) {
    const Vec3 e1 = b - a, e2 = c - a;
    const Vec3 p = cross(r.dir, e2);
    const double det = dot(e1, p);
    if (det == 0) return std::nullopt;
    const double inv = 1 / det;
    const Vec3 s = r.origin - a;
    const double u = dot(s, p) * inv;
    if (u < 0 || u > 1) return std::nullopt;
    const Vec3 q = cross(s, e1);
    const double v = dot(r.dir, q) * inv;
    if (v < 0 || u + v > 1) return std::nullopt;
    const double t = dot(e2, q) * inv;
    if (t <= tmin || t >= tmax) return std::nullopt;
    return TriangleHit{t, u, v};
}

struct Box {
    Vec3 lo{kInfinity, kInfinity, kInfinity};
    Vec3 hi{-kInfinity, -kInfinity, -kInfinity};

    void grow(const Vec3& p) { lo = min(lo, p), hi = max(hi, p); }
    void grow(const Box& b) { lo = min(lo, b.lo), hi = max(hi, b.hi); }
    [[nodiscard]] bool empty() const { return lo.x > hi.x; }
    [[nodiscard]] double area() const {
        if (empty()) return 0;
        const Vec3 d = hi - lo;
        return 2 * (d.x * d.y + d.y * d.z + d.z * d.x);
    }
    [[nodiscard]] Vec3 center() const { return (lo + hi) * 0.5; }
};

// Slab test: the entry distance into the box if the ray meets it within (tmin, tmax), else infinity.
// `inv` is 1 / dir per component (infinite components are fine).
inline double hit_box(const Box& b, const Ray& r, const Vec3& inv, double tmin, double tmax) {
    for (int k = 0; k < 3; ++k) {
        double t0 = (b.lo[k] - r.origin[k]) * inv[k];
        double t1 = (b.hi[k] - r.origin[k]) * inv[k];
        if (t0 > t1) std::swap(t0, t1);
        // NaN (0 * infinity, an axis-parallel ray on a face) leaves the interval unchanged.
        if (t0 > tmin) tmin = t0;
        if (t1 < tmax) tmax = t1;
        if (tmax < tmin) return kInfinity;
    }
    return tmin;
}

}  // namespace rt
