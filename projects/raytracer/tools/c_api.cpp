// C API of the ray tracer, compiled to WebAssembly for the project page. One renderer at a time: load a
// mesh (optional), choose the scene and the view, then call rtc_render for bands of rows; rtc_pixels points
// to the RGBA image (valid until the next rtc_resize). Not thread-safe: each web worker has its own module.
#include "rt/render.hpp"
#include "rt/scenes.hpp"

#include <cstdint>
#include <exception>
#include <memory>
#include <optional>
#include <string>
#include <string_view>

namespace {

struct State {
    std::optional<rt::TriangleMesh> mesh;
    rt::Scene scene = rt::make_scene(rt::SceneKind::Spheres);
    rt::SceneKind kind = rt::SceneKind::Spheres;
    double yaw = 0, pitch = 0, distance = 0;
    std::unique_ptr<rt::Renderer> renderer = std::make_unique<rt::Renderer>(320, 180);
    std::optional<rt::Camera> camera;
    std::string error;
    std::size_t line = 0;
};

State state;

constexpr int kNoMesh = 11;      // statuses 1..4 are lib-c's, 10 an empty mesh
constexpr int kBadArgument = 12;

void update_camera() {
    const rt::View v = rt::default_view(state.kind);
    const double aspect = double(state.renderer->width()) / state.renderer->height();
    state.camera = rt::Camera::orbit(v.target, state.yaw, state.pitch, state.distance, v.vfov, aspect);
    state.renderer->reset();
}

}  // namespace

extern "C" {

// Reads a mesh (OBJ, PLY or STL) for the mesh scene. 0 on success, else lib-c's status or 10.
int rtc_load_mesh(const char* data, std::size_t size) {
    try {
        state.mesh = rt::TriangleMesh::read(std::string_view(data, size));
        state.error.clear();
        state.line = 0;
        return 0;
    } catch (const rt::LoadError& e) {
        state.error = e.what();
        state.line = e.line();
        return e.status() == 0 ? 10 : e.status();
    } catch (const std::exception& e) {
        state.error = e.what();
        state.line = 0;
        return 2;
    }
}

const char* rtc_error() { return state.error.c_str(); }
std::size_t rtc_error_line() { return state.line; }

// kind: 0 spheres, 1 the loaded mesh; finish: 0 diffuse, 1 metal, 2 glass. Resets the view to the scene's.
int rtc_set_scene(int kind, int finish) {
    if (kind < 0 || kind > 1 || finish < 0 || finish > 2) return kBadArgument;
    const auto k = static_cast<rt::SceneKind>(kind);
    if (k == rt::SceneKind::Mesh && !state.mesh) return kNoMesh;
    const rt::MaterialKind f = finish == 0 ? rt::MaterialKind::Diffuse : finish == 1 ? rt::MaterialKind::Metal : rt::MaterialKind::Glass;
    state.scene = rt::make_scene(k, state.mesh ? &*state.mesh : nullptr, f);
    state.kind = k;
    const rt::View v = rt::default_view(k);
    state.yaw = v.yaw, state.pitch = v.pitch, state.distance = v.distance;
    update_camera();
    return 0;
}

// Default view of the current scene, for the page's controls: yaw, pitch, distance.
double rtc_default_yaw() { return rt::default_view(state.kind).yaw; }
double rtc_default_pitch() { return rt::default_view(state.kind).pitch; }
double rtc_default_distance() { return rt::default_view(state.kind).distance; }

void rtc_set_view(double yaw, double pitch, double distance) {
    state.yaw = yaw, state.pitch = pitch, state.distance = distance;
    update_camera();
}

int rtc_resize(uint32_t width, uint32_t height) {
    if (width == 0 || height == 0 || width > 2048 || height > 2048) return kBadArgument;
    state.renderer = std::make_unique<rt::Renderer>(width, height);
    update_camera();
    return 0;
}

// Adds one sample to rows [y0, y1); returns the fewest samples of any row.
uint32_t rtc_render(uint32_t y0, uint32_t y1) {
    if (!state.camera) update_camera();
    state.renderer->pass(state.scene, *state.camera, y0, y1);
    return state.renderer->samples();
}

const uint8_t* rtc_pixels() { return state.renderer->rgba().data(); }
uint32_t rtc_samples() { return state.renderer->samples(); }
uint32_t rtc_width() { return state.renderer->width(); }
uint32_t rtc_height() { return state.renderer->height(); }
std::size_t rtc_triangles() { return state.mesh ? state.mesh->triangles.size() : 0; }

}  // extern "C"
