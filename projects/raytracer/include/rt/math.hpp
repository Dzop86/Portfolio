// Vectors and the optics of a ray tracer: reflection, refraction (Snell) and Schlick's Fresnel.
#pragma once

#include <cmath>
#include <optional>

namespace rt {

struct Vec3 {
    double x = 0, y = 0, z = 0;

    constexpr Vec3 operator-() const { return {-x, -y, -z}; }
    constexpr Vec3& operator+=(const Vec3& o) { x += o.x, y += o.y, z += o.z; return *this; }
    constexpr Vec3& operator*=(double s) { x *= s, y *= s, z *= s; return *this; }
    constexpr bool operator==(const Vec3&) const = default;  // exact, for tests
    [[nodiscard]] constexpr double operator[](int i) const { return i == 0 ? x : i == 1 ? y : z; }
};

constexpr Vec3 operator+(Vec3 a, const Vec3& b) { return a += b; }
constexpr Vec3 operator-(const Vec3& a, const Vec3& b) { return {a.x - b.x, a.y - b.y, a.z - b.z}; }
constexpr Vec3 operator*(Vec3 a, double s) { return a *= s; }
constexpr Vec3 operator*(double s, Vec3 a) { return a *= s; }
// Component-wise product: colours times colours.
constexpr Vec3 operator*(const Vec3& a, const Vec3& b) { return {a.x * b.x, a.y * b.y, a.z * b.z}; }
constexpr Vec3 operator/(const Vec3& a, double s) { return {a.x / s, a.y / s, a.z / s}; }

constexpr double dot(const Vec3& a, const Vec3& b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
constexpr Vec3 cross(const Vec3& a, const Vec3& b) {
    return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x};
}
inline double length(const Vec3& a) { return std::sqrt(dot(a, a)); }
inline Vec3 normalize(const Vec3& a) { return a / length(a); }
constexpr double max_component(const Vec3& a) { return a.x > a.y ? (a.x > a.z ? a.x : a.z) : (a.y > a.z ? a.y : a.z); }

constexpr Vec3 min(const Vec3& a, const Vec3& b) {
    return {a.x < b.x ? a.x : b.x, a.y < b.y ? a.y : b.y, a.z < b.z ? a.z : b.z};
}
constexpr Vec3 max(const Vec3& a, const Vec3& b) {
    return {a.x > b.x ? a.x : b.x, a.y > b.y ? a.y : b.y, a.z > b.z ? a.z : b.z};
}

inline constexpr double kPi = 3.14159265358979323846;

// Mirror of `d` about the normal `n` (unit).
constexpr Vec3 reflect(const Vec3& d, const Vec3& n) { return d - 2 * dot(d, n) * n; }

// Refraction of the unit direction `d` through a surface of unit normal `n` facing `d` (dot(d, n) < 0),
// `eta` being the ratio of refractive indices (incident over transmitted). Empty on total internal
// reflection.
inline std::optional<Vec3> refract(const Vec3& d, const Vec3& n, double eta) {
    const double cos_i = -dot(d, n);
    const double k = 1 - eta * eta * (1 - cos_i * cos_i);
    if (k < 0) return std::nullopt;
    return eta * d + (eta * cos_i - std::sqrt(k)) * n;
}

// Schlick's approximation of the Fresnel reflectance, for the cosine of the incident angle.
constexpr double schlick(double cos_i, double eta) {
    double r0 = (1 - eta) / (1 + eta);
    r0 *= r0;
    const double m = 1 - cos_i;
    return r0 + (1 - r0) * m * m * m * m * m;
}

// An orthonormal basis around the unit vector n (Duff et al., 2017), without branches on the sign of z
// beyond copysign.
inline void basis(const Vec3& n, Vec3& t, Vec3& b) {
    const double s = std::copysign(1.0, n.z);
    const double a = -1 / (s + n.z);
    const double c = n.x * n.y * a;
    t = {1 + s * n.x * n.x * a, s * c, -s * n.x};
    b = {c, s + n.y * n.y * a, -n.y};
}

}  // namespace rt
