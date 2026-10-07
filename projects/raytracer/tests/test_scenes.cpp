// The settings the visitor adjusts on the project page: the light (where, what colour) and the materials.
#include "rt/render.hpp"
#include "rt/scenes.hpp"

#include <gtest/gtest.h>

#include <cmath>
#include <limits>
#include <numbers>
#include <stdexcept>
#include <string>

namespace {

using rt::MaterialKind;
using rt::Scene;
using rt::Settings;
using rt::Vec3;

const std::string kSamples = RT_SAMPLES;

double luminance(const Vec3& c) { return 0.2126 * c.x + 0.7152 * c.y + 0.0722 * c.z; }

TEST(Settings, TheLightSitsAtItsDistanceAndAngles) {
    for (const double azimuth : {-180.0, -45.0, 0.0, 135.0, 180.0}) {
        for (const double elevation : {10.0, 55.0, 85.0}) {
            const Vec3 p = rt::light_position(azimuth, elevation);
            EXPECT_NEAR(rt::length(p), rt::kLightDistance, 1e-12);
            EXPECT_NEAR(std::asin(p.y / rt::kLightDistance) * 180 / std::numbers::pi, elevation, 1e-9);
            if (elevation < 85) {
                EXPECT_NEAR(std::remainder(std::atan2(p.z, p.x) * 180 / std::numbers::pi - azimuth, 360), 0, 1e-9);
            }
        }
    }
    // The default is where the lamp has always been: behind the scene, on the left, high up.
    const Vec3 p = rt::light_position(Settings{}.light_azimuth, Settings{}.light_elevation);
    EXPECT_NEAR(p.x, -3, 0.05);
    EXPECT_NEAR(p.y, 6, 0.05);
    EXPECT_NEAR(p.z, 3, 0.05);
}

TEST(Settings, ABlackBodyTurnsFromRedToBlueAsItHeatsUp) {
    double last = 0;
    for (double k = 2000; k <= 10000; k += 500) {
        const Vec3 c = rt::blackbody(k);
        SCOPED_TRACE(k);
        EXPECT_GT(c.z / c.x, last);  // more blue against red at every step
        last = c.z / c.x;
    }
    const Vec3 candle = rt::blackbody(2000);
    EXPECT_GT(candle.x, 2 * candle.y);
    EXPECT_LT(candle.z, 0.1);
    // Around 6500 K, the white of screens.
    const Vec3 white = rt::blackbody(6500);
    EXPECT_NEAR(white.x, 1, 0.05);
    EXPECT_NEAR(white.y, 1, 0.05);
    EXPECT_NEAR(white.z, 1, 0.05);
}

TEST(Settings, TheColourOfTheLightLeavesItsBrightnessAlone) {
    for (const double k : {2000.0, 3500.0, 5500.0, 10000.0}) {
        EXPECT_NEAR(luminance(rt::light_emission(k)), 48.4, 1e-9) << k;
    }
    // The default is close to the warm white the lamp had before it could be changed.
    const Vec3 e = rt::light_emission(Settings{}.light_kelvin);
    EXPECT_NEAR(e.x, 52, 3);
    EXPECT_NEAR(e.y, 48, 3);
    EXPECT_NEAR(e.z, 42, 3);
}

TEST(Settings, OutOfRangeOrNotANumberIsRefused) {
    EXPECT_TRUE(Settings{}.valid());
    const double nan = std::numeric_limits<double>::quiet_NaN();
    for (const Settings s : {Settings{.light_azimuth = 181}, Settings{.light_elevation = 9}, Settings{.light_elevation = 86},
                             Settings{.light_kelvin = 1999}, Settings{.light_kelvin = 10001}, Settings{.fuzz = -0.01},
                             Settings{.fuzz = 0.51}, Settings{.ior = 0.99}, Settings{.ior = 2.51}, Settings{.light_azimuth = nan},
                             Settings{.ior = nan}}) {
        EXPECT_FALSE(s.valid());
        EXPECT_THROW((void)rt::make_scene(rt::SceneKind::Spheres, nullptr, MaterialKind::Diffuse, s), std::invalid_argument);
    }
}

TEST(Settings, EveryMetalAndGlassOfTheSceneTakesThem) {
    const auto torus = rt::TriangleMesh::load(kSamples + "/torus.obj");
    const Settings set{.light_azimuth = -30, .light_elevation = 40, .light_kelvin = 3000, .fuzz = 0.3, .ior = 2.2};
    for (const Scene& s : {rt::make_scene(rt::SceneKind::Spheres, nullptr, MaterialKind::Diffuse, set),
                           rt::make_scene(rt::SceneKind::Mesh, &torus, MaterialKind::Metal, set),
                           rt::make_scene(rt::SceneKind::Mesh, &torus, MaterialKind::Glass, set)}) {
        for (const auto& m : s.materials) {
            if (m.kind == MaterialKind::Metal) {
                EXPECT_EQ(m.fuzz, 0.3);
            }
            if (m.kind == MaterialKind::Glass) {
                EXPECT_EQ(m.ior, 2.2);
            }
        }
        ASSERT_TRUE(s.light);
        const auto& lamp = s.spheres[*s.light];
        EXPECT_EQ(lamp.center, rt::light_position(-30, 40));
        EXPECT_EQ(s.materials[lamp.material].emission, rt::light_emission(3000));
    }
}

TEST(Settings, AppliedInPlaceTheyGiveTheSceneBuiltWithThem) {
    const auto torus = rt::TriangleMesh::load(kSamples + "/torus.obj");
    const Settings set{.light_azimuth = 80, .light_elevation = 30, .light_kelvin = 8000, .fuzz = 0.2, .ior = 1.33};
    for (const MaterialKind finish : {MaterialKind::Metal, MaterialKind::Glass}) {
        Scene changed = rt::make_scene(rt::SceneKind::Mesh, &torus, finish);
        rt::apply_settings(changed, set);
        const Scene built = rt::make_scene(rt::SceneKind::Mesh, &torus, finish, set);
        ASSERT_EQ(changed.materials.size(), built.materials.size());
        for (std::size_t i = 0; i < built.materials.size(); ++i) {
            EXPECT_EQ(changed.materials[i].emission, built.materials[i].emission);
            EXPECT_EQ(changed.materials[i].fuzz, built.materials[i].fuzz);
            EXPECT_EQ(changed.materials[i].ior, built.materials[i].ior);
        }
        EXPECT_EQ(changed.spheres[*changed.light].center, built.spheres[*built.light].center);
    }
    Scene s = rt::make_scene(rt::SceneKind::Spheres);
    EXPECT_THROW(rt::apply_settings(s, Settings{.fuzz = 1}), std::invalid_argument);
    s.light.reset();
    EXPECT_THROW(rt::apply_settings(s, Settings{}), std::invalid_argument);
}

TEST(Settings, MovingTheLightMovesTheShadows) {
    const rt::View v = rt::default_view(rt::SceneKind::Spheres);
    const auto cam = rt::Camera::orbit(v.target, v.yaw, v.pitch, v.distance, v.vfov, 2.0);
    rt::Renderer before(32, 16), after(32, 16), again(32, 16);
    before.pass(rt::make_scene(rt::SceneKind::Spheres), cam, 0, 16);
    after.pass(rt::make_scene(rt::SceneKind::Spheres, nullptr, MaterialKind::Diffuse, Settings{.light_azimuth = -45}), cam, 0, 16);
    again.pass(rt::make_scene(rt::SceneKind::Spheres, nullptr, MaterialKind::Diffuse, Settings{}), cam, 0, 16);
    EXPECT_NE(before.rgba(), after.rgba());
    EXPECT_EQ(before.rgba(), again.rgba());  // the defaults are the scene without settings
}

}  // namespace
