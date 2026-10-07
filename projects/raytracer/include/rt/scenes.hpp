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

// What the visitor adjusts on the project page, for every scene: where the light is and its colour, how
// rough the metal is and how strongly the glass bends light.
struct Settings {
    double light_azimuth = 135;    // degrees around the vertical axis, from +x towards +z
    double light_elevation = 55;   // degrees above the floor
    double light_kelvin = 5800;    // colour temperature; 5800 K is the warm white the lamp always had
    double fuzz = 0.06;            // metal: 0 is a perfect mirror
    double ior = 1.5;              // glass: refractive index (1 is air, no bending)

    // The accepted ranges, also those of the page's sliders; NaN is out of range.
    [[nodiscard]] bool valid() const;
};

// The light's centre at `kLightDistance` from the origin, at the given angles in degrees.
inline constexpr double kLightDistance = 7.35;
Vec3 light_position(double azimuth, double elevation);

// Linear RGB of a black body at `kelvin` (approximation by Tanner Helland, 1000 K to 40000 K), scaled so
// that every temperature gives the light the same luminance: the colour changes, not the brightness.
Vec3 blackbody(double kelvin);
Vec3 light_emission(double kelvin);

// The mesh (copied, fitted into a sphere of radius 1 on the floor) is used by SceneKind::Mesh only, with
// `finish` as its material (Diffuse, Metal or Glass). Throws std::invalid_argument if `settings` is not valid.
Scene make_scene(SceneKind kind, const TriangleMesh* mesh = nullptr, MaterialKind finish = MaterialKind::Diffuse,
                 const Settings& settings = {});
View default_view(SceneKind kind);

// Moves and colours the light, and sets every metal's fuzz and every glass's index, in place: cheaper than
// building the scene again (and its BVH) at each move of a slider. Throws std::invalid_argument if `settings`
// is not valid or the scene has no light.
void apply_settings(Scene& scene, const Settings& settings);

}  // namespace rt
