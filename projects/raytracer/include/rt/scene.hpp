// What a scene holds: materials, spheres, planes, meshes with their BVH, one spherical light and a sky.
#pragma once

#include "rt/bvh.hpp"
#include "rt/geometry.hpp"
#include "rt/mesh.hpp"

#include <cstdint>
#include <optional>
#include <vector>

namespace rt {

enum class MaterialKind { Diffuse, Metal, Glass, Light };

struct Material {
    MaterialKind kind = MaterialKind::Diffuse;
    Vec3 albedo{0.8, 0.8, 0.8};  // linear RGB; glass: transmitted tint
    Vec3 albedo2{};              // second colour of a checkerboard (`checker` > 0)
    double checker = 0;          // size of a checkerboard square on planes, 0 for none
    double fuzz = 0;             // metal: radius of the random perturbation of the mirror direction
    double ior = 1.5;            // glass: refractive index
    Vec3 emission{};             // light
};

struct Sphere {
    Vec3 center;
    double radius;
    uint32_t material;
};

struct Plane {
    Vec3 point;
    Vec3 normal;  // unit
    uint32_t material;
};

struct MeshObject {
    TriangleMesh mesh;
    Bvh bvh;
    uint32_t material;
};

struct SurfaceHit {
    double t;
    Vec3 point;
    Vec3 geometric;  // unit, turned towards the incoming ray
    Vec3 shading;    // unit, smooth on meshes, on the same side as `geometric`
    bool front;      // the ray came from the side the surface's own normal points to
    uint32_t material;
    bool light;      // the light sphere
};

struct Scene {
    std::vector<Material> materials;
    std::vector<Sphere> spheres;
    std::vector<Plane> planes;
    std::vector<MeshObject> meshes;
    std::optional<uint32_t> light;  // index of the sphere that emits light, sampled at every diffuse bounce
    Vec3 sky_horizon{0.6, 0.65, 0.7};
    Vec3 sky_zenith{0.15, 0.25, 0.45};
    bool use_bvh = true;  // false: meshes are tested triangle by triangle (measures, tests)

    uint32_t add(const Material& m);
    void add_mesh(TriangleMesh mesh, uint32_t material);

    [[nodiscard]] std::optional<SurfaceHit> intersect(const Ray& r, double tmin, double tmax) const;
    // Shadow rays: any surface but the light in (tmin, tmax).
    [[nodiscard]] bool occluded(const Ray& r, double tmin, double tmax) const;
    [[nodiscard]] Vec3 sky(const Vec3& dir) const;
    // The albedo of material `m` at `p`, checkerboards included.
    [[nodiscard]] Vec3 albedo(uint32_t m, const Vec3& p) const;
};

}  // namespace rt
