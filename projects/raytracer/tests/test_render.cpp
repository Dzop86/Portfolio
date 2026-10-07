// The path tracer against what physics says, and the images it produces.
#include "rt/image.hpp"
#include "rt/render.hpp"
#include "rt/scenes.hpp"

#include <gtest/gtest.h>

#include <cmath>
#include <cstdlib>
#include <string>

namespace {

using rt::MaterialKind;
using rt::Scene;
using rt::Vec3;

const std::string kSamples = RT_SAMPLES;
const std::string kReference = RT_REFERENCE;

// Mean radiance of `n` paths along one ray.
Vec3 mean_radiance(const Scene& s, const rt::Ray& r, int n) {
    Vec3 sum{};
    for (int i = 0; i < n; ++i) {
        rt::Rng rng = rt::Rng::for_sample(0, 0, 1, static_cast<uint32_t>(i));
        sum += rt::radiance(s, r, rng);
    }
    return sum / n;
}

// A sphere alone under a uniform white sky (the "white furnace"): whatever bounces off a convex object
// leaves it, so it must look exactly as bright as its albedo; clear glass must vanish.
Scene furnace(const rt::Material& m) {
    Scene s;
    s.sky_horizon = s.sky_zenith = {1, 1, 1};
    s.spheres.push_back({{0, 0, 0}, 1, s.add(m)});
    return s;
}

TEST(Physics, WhiteFurnaceConservesEnergy) {
    // Off the centre, but well inside the sphere's outline.
    const rt::Ray r{{0, 0, -5}, rt::normalize({0.05, 0.08, 1})};
    const Vec3 diffuse = mean_radiance(furnace({.kind = MaterialKind::Diffuse, .albedo = {0.5, 0.5, 0.5}}), r, 4000);
    EXPECT_NEAR(diffuse.x, 0.5, 1e-9);  // one bounce, then the sky: no variance at all
    const Vec3 metal = mean_radiance(furnace({.kind = MaterialKind::Metal, .albedo = {0.7, 0.7, 0.7}, .fuzz = 0.3}), r, 4000);
    EXPECT_NEAR(metal.x, 0.7, 1e-9);
    const Vec3 glass = mean_radiance(furnace({.kind = MaterialKind::Glass, .albedo = {1, 1, 1}, .ior = 1.5}), r, 4000);
    // Paths inside glass last long enough for Russian roulette: unbiased, but no longer exact.
    EXPECT_NEAR(glass.x, 1, 0.01);
}

// A diffuse floor right under a spherical light of radius r at height d, in a black sky: the irradiance
// is pi Le (r / d)^2 exactly, so the floor's radiance is albedo Le (r / d)^2.
Scene lit_floor() {
    Scene s;
    s.sky_horizon = s.sky_zenith = {};
    s.planes.push_back({{0, 0, 0}, {0, 1, 0}, s.add({.kind = MaterialKind::Diffuse, .albedo = {0.5, 0.5, 0.5}})});
    s.spheres.push_back({{0, 4, 0}, 1, s.add({.kind = MaterialKind::Light, .emission = {10, 10, 10}})});
    s.light = 0;
    return s;
}

TEST(Physics, DirectLightMatchesTheAnalyticIrradiance) {
    const Scene s = lit_floor();
    const rt::Ray r{{0, 1, -1}, rt::normalize({0, -1, 1})};  // looks at the origin
    const Vec3 l = mean_radiance(s, r, 20000);
    EXPECT_NEAR(l.x, 0.5 * 10 * (1.0 / 16), 0.01 * 0.3125);
}

TEST(Physics, ABlockerCastsAShadow) {
    Scene s = lit_floor();
    // Seen from the floor, the blocker (half-angle asin(0.8 / 2)) covers the light (asin(1 / 4)) entirely.
    s.spheres.push_back({{0, 2, 0}, 0.8, s.add({.kind = MaterialKind::Diffuse, .albedo = {}})});  // black: lit, it would light the floor
    const Vec3 l = mean_radiance(s, {{0, 1, -1}, rt::normalize({0, -1, 1})}, 2000);
    EXPECT_EQ(l.x, 0);
}

TEST(Physics, TheCameraSeesTheLightButDiffuseBouncesDoNotCountItTwice) {
    const Scene s = lit_floor();
    EXPECT_DOUBLE_EQ(mean_radiance(s, {{0, 0.5, 0}, {0, 1, 0}}, 1).x, 10);
}

TEST(Display, ToneCurveIsMonotonicAndBounded) {
    EXPECT_EQ(rt::to_byte(0), 0);
    EXPECT_EQ(rt::to_byte(-1), 0);
    EXPECT_EQ(rt::to_byte(std::nan("")), 0);
    EXPECT_EQ(rt::to_byte(1e9), 255);
    int previous = 0;
    for (double x = 0.001; x < 20; x *= 1.5) {
        const int b = rt::to_byte(x);
        EXPECT_GE(b, previous);
        previous = b;
    }
}

TEST(Camera, CentreRayAimsAtTheTarget) {
    const auto cam = rt::Camera::orbit({0, 1, 0}, 30, 20, 5, 40, 16.0 / 9);
    EXPECT_NEAR(rt::length(cam.eye() - Vec3{0, 1, 0}), 5, 1e-12);
    const rt::Ray r = cam.ray(0.5, 0.5);
    const Vec3 to_target = rt::normalize(Vec3{0, 1, 0} - cam.eye());
    EXPECT_NEAR(rt::dot(r.dir, to_target), 1, 1e-12);
    // Top of the image is up, left is left.
    EXPECT_GT(cam.ray(0.5, 0).dir.y, r.dir.y);
    EXPECT_LT(rt::dot(rt::cross(cam.ray(0, 0.5).dir, r.dir), Vec3{0, 1, 0}), 0);
}

TEST(Renderer, BandsInAnyOrderGiveTheSameImage) {
    const Scene s = rt::make_scene(rt::SceneKind::Spheres);
    const rt::View v = rt::default_view(rt::SceneKind::Spheres);
    const auto cam = rt::Camera::orbit(v.target, v.yaw, v.pitch, v.distance, v.vfov, 2.0);
    rt::Renderer whole(48, 24), bands(48, 24);
    for (int i = 0; i < 3; ++i) whole.pass(s, cam, 0, 24);
    for (int i = 0; i < 3; ++i) {
        bands.pass(s, cam, 16, 24);
        bands.pass(s, cam, 0, 8);
        bands.pass(s, cam, 8, 16);
    }
    EXPECT_EQ(whole.samples(), 3u);
    EXPECT_EQ(whole.rgba(), bands.rgba());
    bands.reset();
    EXPECT_EQ(bands.samples(), 0u);
}

TEST(Renderer, BvhAndBruteForceRenderTheSameMesh) {
    const auto torus = rt::TriangleMesh::load(kSamples + "/torus.obj");
    Scene s = rt::make_scene(rt::SceneKind::Mesh, &torus, MaterialKind::Glass);
    const rt::View v = rt::default_view(rt::SceneKind::Mesh);
    const auto cam = rt::Camera::orbit(v.target, v.yaw, v.pitch, v.distance, v.vfov, 1.5);
    rt::Renderer a(36, 24), b(36, 24);
    a.pass(s, cam, 0, 24);
    s.use_bvh = false;
    b.pass(s, cam, 0, 24);
    EXPECT_EQ(a.rgba(), b.rgba());
}

// The reference image, rendered once and committed: any change to the renderer that alters the picture
// shows here. A small tolerance absorbs the last bits of sin, cos and tan, which differ between the
// standard libraries of the three systems. RT_UPDATE_REFERENCE=1 rewrites it.
TEST(Renderer, MatchesTheReferenceImage) {
    const auto torus = rt::TriangleMesh::load(kSamples + "/torus.obj");
    const Scene spheres = rt::make_scene(rt::SceneKind::Spheres);
    const Scene mesh = rt::make_scene(rt::SceneKind::Mesh, &torus, MaterialKind::Metal);
    for (const auto& [name, scene, kind] : {std::tuple{"spheres", &spheres, rt::SceneKind::Spheres},
                                            std::tuple{"torus-metal", &mesh, rt::SceneKind::Mesh}}) {
        SCOPED_TRACE(name);
        const rt::View v = rt::default_view(kind);
        const auto cam = rt::Camera::orbit(v.target, v.yaw, v.pitch, v.distance, v.vfov, 16.0 / 9);
        rt::Renderer r(128, 72);
        for (int i = 0; i < 16; ++i) r.pass(*scene, cam, 0, 72);
        const rt::Image got = rt::from_rgba(128, 72, r.rgba());
        const std::string path = kReference + "/" + name + ".ppm";
        if (std::getenv("RT_UPDATE_REFERENCE") != nullptr) rt::write_ppm(path, got);
        EXPECT_GT(rt::psnr(got, rt::read_ppm(path)), 45.0);
    }
}

}  // namespace
