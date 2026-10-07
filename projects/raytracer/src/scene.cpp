#include "rt/scene.hpp"

#include <cmath>

namespace rt {

uint32_t Scene::add(const Material& m) {
    materials.push_back(m);
    return static_cast<uint32_t>(materials.size() - 1);
}

void Scene::add_mesh(TriangleMesh mesh, uint32_t material) {
    Bvh bvh(mesh);
    meshes.push_back({std::move(mesh), std::move(bvh), material});
}

namespace {

SurfaceHit make_hit(const Ray& r, double t, const Vec3& outward, const Vec3& smooth, uint32_t material, bool light) {
    const bool front = dot(r.dir, outward) < 0;
    const Vec3 g = front ? outward : -outward;
    Vec3 s = dot(smooth, g) < 0 ? -smooth : smooth;
    // A smooth normal that faces away from the ray would send light through the surface: use the face's.
    if (dot(s, r.dir) >= 0) s = g;
    return {t, at(r, t), g, s, front, material, light};
}

}  // namespace

std::optional<SurfaceHit> Scene::intersect(const Ray& r, double tmin, double tmax) const {
    std::optional<SurfaceHit> best;
    for (std::size_t i = 0; i < spheres.size(); ++i) {
        const Sphere& s = spheres[i];
        if (const auto t = hit_sphere(r, s.center, s.radius, tmin, tmax)) {
            tmax = *t;
            const Vec3 n = (at(r, *t) - s.center) / s.radius;
            best = make_hit(r, *t, n, n, s.material, light && *light == i);
        }
    }
    for (const Plane& p : planes) {
        if (const auto t = hit_plane(r, p.point, p.normal, tmin, tmax)) {
            tmax = *t;
            best = make_hit(r, *t, p.normal, p.normal, p.material, false);
        }
    }
    for (const MeshObject& m : meshes) {
        const auto h = use_bvh ? m.bvh.intersect(m.mesh, r, tmin, tmax) : intersect_brute_force(m.mesh, r, tmin, tmax);
        if (!h) continue;
        tmax = h->t;
        const auto& tri = m.mesh.triangles[h->triangle];
        const Vec3& a = m.mesh.positions[tri[0]];
        const Vec3 face = normalize(cross(m.mesh.positions[tri[1]] - a, m.mesh.positions[tri[2]] - a));
        // Each vertex normal is turned to the face's side before blending: on a Möbius strip, or across
        // inconsistently oriented faces, neighbours may disagree.
        Vec3 smooth{};
        const double w[3] = {1 - h->u - h->v, h->u, h->v};
        for (int k = 0; k < 3; ++k) {
            const Vec3& n = m.mesh.normals[tri[static_cast<std::size_t>(k)]];
            smooth += (dot(n, face) < 0 ? -n : n) * w[k];
        }
        const double len = length(smooth);
        best = make_hit(r, h->t, face, len > 0 ? smooth / len : face, m.material, false);
    }
    return best;
}

bool Scene::occluded(const Ray& r, double tmin, double tmax) const {
    for (std::size_t i = 0; i < spheres.size(); ++i) {
        if (light && *light == i) continue;
        if (hit_sphere(r, spheres[i].center, spheres[i].radius, tmin, tmax)) return true;
    }
    for (const Plane& p : planes) {
        if (hit_plane(r, p.point, p.normal, tmin, tmax)) return true;
    }
    for (const MeshObject& m : meshes) {
        if (use_bvh ? m.bvh.occluded(m.mesh, r, tmin, tmax) : intersect_brute_force(m.mesh, r, tmin, tmax).has_value()) {
            return true;
        }
    }
    return false;
}

Vec3 Scene::sky(const Vec3& dir) const {
    // Below the horizon (seen only where no floor is), the horizon colour.
    const double t = dir.y > 0 ? dir.y : 0;
    return sky_horizon * (1 - t) + sky_zenith * t;
}

Vec3 Scene::albedo(uint32_t m, const Vec3& p) const {
    const Material& mat = materials[m];
    if (mat.checker <= 0) return mat.albedo;
    const auto cell = static_cast<long long>(std::floor(p.x / mat.checker)) + static_cast<long long>(std::floor(p.z / mat.checker));
    return cell % 2 == 0 ? mat.albedo : mat.albedo2;
}

}  // namespace rt
