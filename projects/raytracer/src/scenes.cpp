#include "rt/scenes.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>
#include <stdexcept>

namespace rt {
namespace {

// The site's colours (tokens.css), in linear RGB: pistachio #bef374, chocolate #5a3a22.
constexpr Vec3 kPistachio{0.51, 0.90, 0.17};
constexpr Vec3 kChocolate{0.10, 0.042, 0.016};
constexpr Vec3 kSteel{0.90, 0.86, 0.80};
constexpr Vec3 kCream{0.72, 0.68, 0.60};

// The luminance of the lamp (Rec. 709 weights), whatever its colour.
constexpr double kLightLuminance = 48.4;

double luminance(const Vec3& c) { return 0.2126 * c.x + 0.7152 * c.y + 0.0722 * c.z; }

uint32_t finish_material(Scene& s, MaterialKind finish, const Vec3& diffuse, const Settings& set) {
    switch (finish) {
        case MaterialKind::Metal: return s.add({.kind = MaterialKind::Metal, .albedo = kSteel, .fuzz = set.fuzz});
        case MaterialKind::Glass: return s.add({.kind = MaterialKind::Glass, .albedo = {0.98, 1.0, 0.96}, .ior = set.ior});
        case MaterialKind::Diffuse: return s.add({.kind = MaterialKind::Diffuse, .albedo = diffuse});
        case MaterialKind::Light: break;
    }
    throw std::invalid_argument("a mesh cannot be the light");
}

void add_floor_and_light(Scene& s, const Settings& set) {
    const uint32_t floor = s.add({.kind = MaterialKind::Diffuse, .albedo = kCream, .albedo2 = kChocolate, .checker = 1.0});
    s.planes.push_back({{0, 0, 0}, {0, 1, 0}, floor});
    const uint32_t lamp = s.add({.kind = MaterialKind::Light, .emission = light_emission(set.light_kelvin)});
    s.spheres.push_back({light_position(set.light_azimuth, set.light_elevation), 1, lamp});
    s.light = static_cast<uint32_t>(s.spheres.size() - 1);
}

double radians(double degrees) { return degrees * std::numbers::pi / 180; }

}  // namespace

bool Settings::valid() const {
    const auto in = [](double x, double lo, double hi) { return x >= lo && x <= hi; };  // false for NaN
    return in(light_azimuth, -180, 180) && in(light_elevation, 10, 85) && in(light_kelvin, 2000, 10000) &&
           in(fuzz, 0, 0.5) && in(ior, 1, 2.5);
}

Vec3 light_position(double azimuth, double elevation) {
    const double a = radians(azimuth), e = radians(elevation);
    return Vec3{std::cos(e) * std::cos(a), std::sin(e), std::cos(e) * std::sin(a)} * kLightDistance;
}

Vec3 blackbody(double kelvin) {
    const double t = std::clamp(kelvin, 1000.0, 40000.0) / 100;
    // sRGB values from 0 to 255, fitted to the CIE 1964 colour matching functions by Tanner Helland.
    const double r = t <= 66 ? 255 : 329.698727446 * std::pow(t - 60, -0.1332047592);
    const double g = t <= 66 ? 99.4708025861 * std::log(t) - 161.1195681661 : 288.1221695283 * std::pow(t - 60, -0.0755148492);
    const double b = t >= 66 ? 255 : t <= 19 ? 0 : 138.5177312231 * std::log(t - 10) - 305.0447927307;
    const auto linear = [](double c) { return std::pow(std::clamp(c, 0.0, 255.0) / 255, 2.2); };
    return {linear(r), linear(g), linear(b)};
}

Vec3 light_emission(double kelvin) {
    const Vec3 c = blackbody(kelvin);
    return c * (kLightLuminance / luminance(c));
}

Scene make_scene(SceneKind kind, const TriangleMesh* mesh, MaterialKind finish, const Settings& settings) {
    if (!settings.valid()) throw std::invalid_argument("settings out of range");
    Scene s;
    add_floor_and_light(s, settings);
    if (kind == SceneKind::Spheres) {
        const uint32_t glass = finish_material(s, MaterialKind::Glass, {}, settings);
        const uint32_t matte = finish_material(s, MaterialKind::Diffuse, kPistachio, settings);
        const uint32_t metal = finish_material(s, MaterialKind::Metal, {}, settings);
        const uint32_t choc = s.add({.kind = MaterialKind::Diffuse, .albedo = kChocolate});
        s.spheres.push_back({{-1.6, 0.7, 0}, 0.7, glass});
        s.spheres.push_back({{0, 0.7, 0}, 0.7, matte});
        s.spheres.push_back({{1.6, 0.7, 0}, 0.7, metal});
        s.spheres.push_back({{0.8, 0.25, 1.4}, 0.25, choc});
        s.spheres.push_back({{-0.75, 0.3, 1.5}, 0.3, metal});
        return s;
    }
    if (mesh == nullptr) throw std::invalid_argument("the mesh scene needs a mesh");
    TriangleMesh m = *mesh;
    m.fit(1.0);
    s.add_mesh(std::move(m), finish_material(s, finish, kPistachio, settings));
    s.spheres.push_back({{1.7, 0.3, 1.2}, 0.3, s.add({.kind = MaterialKind::Diffuse, .albedo = kChocolate})});
    return s;
}

void apply_settings(Scene& scene, const Settings& settings) {
    if (!settings.valid()) throw std::invalid_argument("settings out of range");
    if (!scene.light) throw std::invalid_argument("the scene has no light");
    Sphere& lamp = scene.spheres[*scene.light];
    lamp.center = light_position(settings.light_azimuth, settings.light_elevation);
    scene.materials[lamp.material].emission = light_emission(settings.light_kelvin);
    for (Material& m : scene.materials) {
        if (m.kind == MaterialKind::Metal) m.fuzz = settings.fuzz;
        if (m.kind == MaterialKind::Glass) m.ior = settings.ior;
    }
}

View default_view(SceneKind kind) {
    if (kind == SceneKind::Spheres) return {{0, 0.55, 0.3}, 20, 14, 6.2, 40};
    return {{0, 0.9, 0}, 25, 18, 5.2, 40};
}

}  // namespace rt
