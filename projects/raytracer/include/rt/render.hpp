// The camera, the path tracer and a progressive renderer that adds one sample per pixel at each pass.
#pragma once

#include "rt/rng.hpp"
#include "rt/scene.hpp"

#include <cstdint>
#include <vector>

namespace rt {

class Camera {
public:
    // Looks from `eye` at `target`, y up, with a vertical field of view in degrees.
    Camera(const Vec3& eye, const Vec3& target, double vfov_degrees, double aspect);
    // Orbits `target` at `distance`: yaw around the vertical axis, pitch above the horizon, in degrees.
    static Camera orbit(const Vec3& target, double yaw_degrees, double pitch_degrees, double distance,
                        double vfov_degrees, double aspect);

    // The ray through the point (s, t) of the image, s from left to right and t from top to bottom, in [0, 1].
    [[nodiscard]] Ray ray(double s, double t) const;
    [[nodiscard]] const Vec3& eye() const { return eye_; }

private:
    Vec3 eye_, corner_, horizontal_, vertical_;
};

// Radiance carried back along `ray`: diffuse surfaces sample the light sphere directly (soft shadows) and
// bounce in a cosine-weighted direction; metal reflects, glass reflects or refracts by Schlick's Fresnel.
Vec3 radiance(const Scene& scene, Ray ray, Rng& rng);

// Linear radiance to an 8-bit sRGB-like value: ACES filmic curve (Narkowicz's fit), then gamma 2.
uint8_t to_byte(double linear);

class Renderer {
public:
    static constexpr int kMaxDepth = 10;

    Renderer(uint32_t width, uint32_t height);

    // Adds the next sample to every pixel of rows [y0, y1). Rows keep their own sample count, so bands can
    // be rendered in any order; pixel (x, y) always draws sample k from the same random stream.
    void pass(const Scene& scene, const Camera& camera, uint32_t y0, uint32_t y1);
    // Forgets every sample (new scene, camera or size).
    void reset();

    [[nodiscard]] uint32_t width() const { return width_; }
    [[nodiscard]] uint32_t height() const { return height_; }
    // Fewest samples of any row.
    [[nodiscard]] uint32_t samples() const;
    // RGBA, 8 bits per channel, rows from top to bottom; up to date for the rows rendered.
    [[nodiscard]] const std::vector<uint8_t>& rgba() const { return rgba_; }
    // The mean radiance of a pixel so far.
    [[nodiscard]] Vec3 pixel(uint32_t x, uint32_t y) const;

private:
    uint32_t width_, height_;
    std::vector<Vec3> sum_;
    std::vector<uint32_t> row_samples_;
    std::vector<uint8_t> rgba_;
};

}  // namespace rt
