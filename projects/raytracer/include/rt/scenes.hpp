// The scenes shown on the project page: three spheres, or one mesh of the common thread, on a floor.
#pragma once

#include "rt/mesh.hpp"
#include "rt/scene.hpp"

namespace rt {

enum class SceneKind { Spheres = 0, Mesh = 1 };

// Where the camera orbits by default.
struct View {
    Vec3 target;
    double yaw, pitch, distance, vfov;
};

// The mesh (copied, fitted into a sphere of radius 1 on the floor) is used by SceneKind::Mesh only, with
// `finish` as its material (Diffuse, Metal or Glass).
Scene make_scene(SceneKind kind, const TriangleMesh* mesh = nullptr, MaterialKind finish = MaterialKind::Diffuse);
View default_view(SceneKind kind);

}  // namespace rt
