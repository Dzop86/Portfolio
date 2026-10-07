#include "rt/render.hpp"

#include <algorithm>
#include <cmath>

namespace rt {

Camera::Camera(const Vec3& eye, const Vec3& target, double vfov_degrees, double aspect) : eye_(eye) {
    const double h = std::tan(vfov_degrees * kPi / 360);
    const Vec3 w = normalize(eye - target);
    const Vec3 u = normalize(cross(Vec3{0, 1, 0}, w));
    const Vec3 v = cross(w, u);
    horizontal_ = u * (2 * h * aspect);
    vertical_ = v * (-2 * h);  // t grows downwards
    corner_ = -w - horizontal_ * 0.5 - vertical_ * 0.5;
}

Camera Camera::orbit(const Vec3& target, double yaw_degrees, double pitch_degrees, double distance,
                     double vfov_degrees, double aspect) {
    const double yaw = yaw_degrees * kPi / 180, pitch = pitch_degrees * kPi / 180;
    const Vec3 offset{std::cos(pitch) * std::sin(yaw), std::sin(pitch), std::cos(pitch) * std::cos(yaw)};
    return Camera(target + offset * distance, target, vfov_degrees, aspect);
}

Ray Camera::ray(double s, double t) const {
    return {eye_, normalize(corner_ + horizontal_ * s + vertical_ * t)};
}

namespace {

// Cosine-weighted direction around the unit normal n.
Vec3 cosine_direction(const Vec3& n, Rng& rng) {
    const double r1 = rng.uniform(), r2 = rng.uniform();
    const double phi = 2 * kPi * r1;
    const double r = std::sqrt(r2);
    Vec3 t, b;
    basis(n, t, b);
    return normalize(t * (r * std::cos(phi)) + b * (r * std::sin(phi)) + n * std::sqrt(1 - r2));
}

// A point uniformly in the unit ball, by rejection.
Vec3 in_unit_ball(Rng& rng) {
    for (;;) {
        const Vec3 p{2 * rng.uniform() - 1, 2 * rng.uniform() - 1, 2 * rng.uniform() - 1};
        if (dot(p, p) < 1) return p;
    }
}

// Moves a point off its surface, to the side the new direction leaves towards.
Vec3 offset(const Vec3& p, const Vec3& normal, const Vec3& dir) {
    const double eps = 1e-6 * (1 + std::max({std::fabs(p.x), std::fabs(p.y), std::fabs(p.z)}));
    return p + normal * (dot(dir, normal) > 0 ? eps : -eps);
}

// Light reaching a diffuse point from the light sphere, by sampling the cone it subtends (soft shadows).
Vec3 direct_light(const Scene& scene, const SurfaceHit& hit, Rng& rng) {
    if (!scene.light) return {};
    const Sphere& light = scene.spheres[*scene.light];
    const Vec3 to_center = light.center - hit.point;
    const double d2 = dot(to_center, to_center);
    if (d2 <= light.radius * light.radius) return {};
    const double cos_max = std::sqrt(1 - light.radius * light.radius / d2);
    const double r1 = rng.uniform(), r2 = rng.uniform();
    const double cos_theta = 1 - r1 * (1 - cos_max);
    const double sin_theta = std::sqrt(std::max(0.0, 1 - cos_theta * cos_theta));
    const double phi = 2 * kPi * r2;
    const Vec3 w = normalize(to_center);
    Vec3 t, b;
    basis(w, t, b);
    const Vec3 dir = normalize(t * (sin_theta * std::cos(phi)) + b * (sin_theta * std::sin(phi)) + w * cos_theta);
    const double cos_surface = dot(dir, hit.shading);
    if (cos_surface <= 0 || dot(dir, hit.geometric) <= 0) return {};
    const Ray shadow{offset(hit.point, hit.geometric, dir), dir};
    const auto t_light = hit_sphere(shadow, light.center, light.radius, 0, kInfinity);
    if (!t_light || scene.occluded(shadow, kEpsilon, *t_light * (1 - 1e-9))) return {};
    // Uniform over the cone: pdf = 1 / (2 pi (1 - cos_max)); diffuse BRDF = albedo / pi.
    const double solid_angle = 2 * kPi * (1 - cos_max);
    return scene.materials[light.material].emission * (cos_surface * solid_angle / kPi);
}

}  // namespace

Vec3 radiance(const Scene& scene, Ray ray, Rng& rng) {
    Vec3 result{}, throughput{1, 1, 1};
    // The camera sees the light, and so do mirrors and glass, until a diffuse bounce: from then on the light
    // is sampled directly at each diffuse point, and paths that find it through glass or a mirror (caustics)
    // are dropped: rare and very bright, they would leave lone white pixels for hundreds of samples.
    bool count_light = true;
    for (int depth = 0; depth < Renderer::kMaxDepth; ++depth) {
        const auto hit = scene.intersect(ray, kEpsilon, kInfinity);
        if (!hit) {
            result += throughput * scene.sky(ray.dir);
            break;
        }
        const Material& mat = scene.materials[hit->material];
        if (mat.kind == MaterialKind::Light) {
            if (count_light) result += throughput * mat.emission;
            break;
        }
        const Vec3 albedo = scene.albedo(hit->material, hit->point);
        Vec3 dir;
        switch (mat.kind) {
            case MaterialKind::Diffuse:
                result += throughput * albedo * direct_light(scene, *hit, rng);
                dir = cosine_direction(hit->shading, rng);
                if (dot(dir, hit->geometric) <= 0) return result;
                throughput = throughput * albedo;
                count_light = false;
                break;
            case MaterialKind::Metal:
                dir = reflect(ray.dir, hit->shading);
                if (mat.fuzz > 0) dir = normalize(dir + in_unit_ball(rng) * mat.fuzz);
                if (dot(dir, hit->geometric) <= 0) return result;
                throughput = throughput * albedo;
                break;
            case MaterialKind::Glass: {
                const double eta = hit->front ? 1 / mat.ior : mat.ior;
                const double cos_i = std::min(-dot(ray.dir, hit->shading), 1.0);
                const auto refracted = refract(ray.dir, hit->shading, eta);
                if (!refracted || rng.uniform() < schlick(cos_i, eta)) {
                    dir = reflect(ray.dir, hit->shading);
                } else {
                    dir = normalize(*refracted);
                    throughput = throughput * albedo;
                }
                break;
            }
            case MaterialKind::Light:
                break;
        }
        ray = {offset(hit->point, hit->geometric, dir), dir};
        // Russian roulette after a few bounces: unbiased, and dim paths stop early.
        if (depth >= 3) {
            const double p = std::clamp(max_component(throughput), 0.05, 0.95);
            if (rng.uniform() >= p) break;
            throughput = throughput / p;
        }
    }
    return result;
}

uint8_t to_byte(double linear) {
    if (!(linear > 0)) return 0;  // NaN too
    const double x = linear;
    const double aces = std::clamp((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0);
    return static_cast<uint8_t>(std::lround(std::sqrt(aces) * 255));
}

Renderer::Renderer(uint32_t width, uint32_t height)
    : width_(width), height_(height),
      sum_(static_cast<std::size_t>(width) * height),
      row_samples_(height),
      rgba_(static_cast<std::size_t>(width) * height * 4, 0) {
    for (std::size_t i = 3; i < rgba_.size(); i += 4) rgba_[i] = 255;
}

void Renderer::reset() {
    std::fill(sum_.begin(), sum_.end(), Vec3{});
    std::fill(row_samples_.begin(), row_samples_.end(), 0u);
}

uint32_t Renderer::samples() const {
    return row_samples_.empty() ? 0 : *std::min_element(row_samples_.begin(), row_samples_.end());
}

Vec3 Renderer::pixel(uint32_t x, uint32_t y) const {
    const uint32_t n = row_samples_[y];
    return n == 0 ? Vec3{} : sum_[static_cast<std::size_t>(y) * width_ + x] / n;
}

void Renderer::pass(const Scene& scene, const Camera& camera, uint32_t y0, uint32_t y1) {
    y1 = std::min(y1, height_);
    for (uint32_t y = y0; y < y1; ++y) {
        const uint32_t sample = row_samples_[y]++;
        for (uint32_t x = 0; x < width_; ++x) {
            Rng rng = Rng::for_sample(x, y, width_, sample);
            // A random point of the pixel each time: the samples average into anti-aliased edges.
            const double s = (x + rng.uniform()) / width_;
            const double t = (y + rng.uniform()) / height_;
            Vec3 l = radiance(scene, camera.ray(s, t), rng);
            // Rare paths (caustics through glass) would leave lone bright pixels for a long time: clamp them.
            if (!(l.x == l.x && l.y == l.y && l.z == l.z)) l = {};
            l = min(l, Vec3{20, 20, 20});
            const std::size_t i = static_cast<std::size_t>(y) * width_ + x;
            sum_[i] += l;
            const Vec3 mean = sum_[i] / (sample + 1);
            rgba_[4 * i] = to_byte(mean.x);
            rgba_[4 * i + 1] = to_byte(mean.y);
            rgba_[4 * i + 2] = to_byte(mean.z);
        }
    }
}

}  // namespace rt
