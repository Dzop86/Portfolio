// Renders a scene to a PPM file and prints how long it took.
//   rt_render [--scene spheres|mesh] [--mesh FILE] [--finish diffuse|metal|glass] [--size WxH] [--spp N]
//             [--yaw DEG] [--pitch DEG] [--distance D] [--brute-force] [-o FILE]
#include "rt/image.hpp"
#include "rt/render.hpp"
#include "rt/scenes.hpp"

#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <exception>
#include <optional>
#include <string>

namespace {

[[noreturn]] void usage() {
    std::fputs("usage: rt_render [--scene spheres|mesh] [--mesh FILE] [--finish diffuse|metal|glass]\n"
               "                 [--size WxH] [--spp N] [--yaw DEG] [--pitch DEG] [--distance D]\n"
               "                 [--brute-force] [-o FILE]\n",
               stderr);
    std::exit(2);
}

}  // namespace

int main(int argc, char** argv) {
    try {
        rt::SceneKind kind = rt::SceneKind::Spheres;
        rt::MaterialKind finish = rt::MaterialKind::Diffuse;
        std::optional<rt::TriangleMesh> mesh;
        unsigned width = 480, height = 270, spp = 64;
        std::optional<double> yaw, pitch, distance;
        bool brute = false;
        std::string out = "render.ppm";
        for (int i = 1; i < argc; ++i) {
            const std::string a = argv[i];
            auto value = [&]() -> std::string {
                if (i + 1 >= argc) usage();
                return argv[++i];
            };
            if (a == "--scene") {
                const std::string v = value();
                if (v == "spheres") kind = rt::SceneKind::Spheres;
                else if (v == "mesh") kind = rt::SceneKind::Mesh;
                else usage();
            } else if (a == "--mesh") {
                mesh = rt::TriangleMesh::load(value());
            } else if (a == "--finish") {
                const std::string v = value();
                if (v == "diffuse") finish = rt::MaterialKind::Diffuse;
                else if (v == "metal") finish = rt::MaterialKind::Metal;
                else if (v == "glass") finish = rt::MaterialKind::Glass;
                else usage();
            } else if (a == "--size") {
                // WxH, read with strtoul (MSVC refuses sscanf without _CRT_SECURE_NO_WARNINGS).
                const std::string v = value();
                char* end = nullptr;
                width = static_cast<unsigned>(std::strtoul(v.c_str(), &end, 10));
                if (*end != 'x') usage();
                height = static_cast<unsigned>(std::strtoul(end + 1, &end, 10));
                if (*end != '\0' || width == 0 || height == 0) usage();
            } else if (a == "--spp") {
                spp = static_cast<unsigned>(std::strtoul(value().c_str(), nullptr, 10));
            } else if (a == "--yaw") {
                yaw = std::strtod(value().c_str(), nullptr);
            } else if (a == "--pitch") {
                pitch = std::strtod(value().c_str(), nullptr);
            } else if (a == "--distance") {
                distance = std::strtod(value().c_str(), nullptr);
            } else if (a == "--brute-force") {
                brute = true;
            } else if (a == "-o") {
                out = value();
            } else {
                usage();
            }
        }
        rt::Scene scene = rt::make_scene(kind, mesh ? &*mesh : nullptr, finish);
        scene.use_bvh = !brute;
        const rt::View v = rt::default_view(kind);
        const auto camera = rt::Camera::orbit(v.target, yaw.value_or(v.yaw), pitch.value_or(v.pitch),
                                              distance.value_or(v.distance), v.vfov, double(width) / height);
        rt::Renderer renderer(width, height);
        const auto start = std::chrono::steady_clock::now();
        for (unsigned s = 0; s < spp; ++s) renderer.pass(scene, camera, 0, height);
        const std::chrono::duration<double> elapsed = std::chrono::steady_clock::now() - start;
        rt::write_ppm(out, rt::from_rgba(width, height, renderer.rgba()));
        std::printf("%ux%u, %u samples per pixel, %.3f s, written to %s\n", width, height, spp, elapsed.count(), out.c_str());
        return 0;
    } catch (const rt::LoadError& e) {
        std::fprintf(stderr, "rt_render: cannot read the mesh: %s (line %zu)\n", e.what(), e.line());
    } catch (const std::exception& e) {
        std::fprintf(stderr, "rt_render: %s\n", e.what());
    }
    return 1;
}
