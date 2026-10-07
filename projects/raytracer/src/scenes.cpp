#include "rt/scenes.hpp"

#include <stdexcept>

namespace rt {
namespace {

// The site's colours (tokens.css), in linear RGB: pistachio #bef374, chocolate #5a3a22.
constexpr Vec3 kPistachio{0.51, 0.90, 0.17};
constexpr Vec3 kChocolate{0.10, 0.042, 0.016};
constexpr Vec3 kSteel{0.90, 0.86, 0.80};
constexpr Vec3 kCream{0.72, 0.68, 0.60};

uint32_t finish_material(Scene& s, MaterialKind finish, const Vec3& diffuse) {
    switch (finish) {
        case MaterialKind::Metal: return s.add({.kind = MaterialKind::Metal, .albedo = kSteel, .fuzz = 0.06});
        case MaterialKind::Glass: return s.add({.kind = MaterialKind::Glass, .albedo = {0.98, 1.0, 0.96}, .ior = 1.5});
        case MaterialKind::Diffuse: return s.add({.kind = MaterialKind::Diffuse, .albedo = diffuse});
        case MaterialKind::Light: break;
    }
    throw std::invalid_argument("a mesh cannot be the light");
}

void add_floor_and_light(Scene& s) {
    const uint32_t floor = s.add({.kind = MaterialKind::Diffuse, .albedo = kCream, .albedo2 = kChocolate, .checker = 1.0});
    s.planes.push_back({{0, 0, 0}, {0, 1, 0}, floor});
    const uint32_t lamp = s.add({.kind = MaterialKind::Light, .emission = {52, 48, 42}});
    s.spheres.push_back({{-3, 6, 3}, 1, lamp});
    s.light = static_cast<uint32_t>(s.spheres.size() - 1);
}

}  // namespace

Scene make_scene(SceneKind kind, const TriangleMesh* mesh, MaterialKind finish) {
    Scene s;
    add_floor_and_light(s);
    if (kind == SceneKind::Spheres) {
        const uint32_t glass = finish_material(s, MaterialKind::Glass, {});
        const uint32_t matte = finish_material(s, MaterialKind::Diffuse, kPistachio);
        const uint32_t metal = finish_material(s, MaterialKind::Metal, {});
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
    s.add_mesh(std::move(m), finish_material(s, finish, kPistachio));
    s.spheres.push_back({{1.7, 0.3, 1.2}, 0.3, s.add({.kind = MaterialKind::Diffuse, .albedo = kChocolate})});
    return s;
}

View default_view(SceneKind kind) {
    if (kind == SceneKind::Spheres) return {{0, 0.55, 0.3}, 20, 14, 6.2, 40};
    return {{0, 0.9, 0}, 25, 18, 5.2, 40};
}

}  // namespace rt
