// Entry points for the WebAssembly build used by the portfolio viewer. One mesh at a time: read a
// buffer, then fetch a JSON summary and pointers to the arrays (valid until the next read).
#include "topo/curvature.hpp"
#include "topo/invariants.hpp"
#include "topo/mesh.hpp"

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <exception>
#include <optional>
#include <string>
#include <vector>

namespace {

struct State {
    std::vector<float> positions;  // centred on the bounding box, scaled into the unit sphere
    std::vector<uint32_t> indices;
    std::vector<float> curvature;  // Gaussian curvature density, in the scaled units
    std::vector<float> defect;     // angle defect (scale-free)
    std::string summary;           // JSON with the invariants
    std::string error;
    std::size_t line = 0;
};

State state;

constexpr int kInvalidMesh = 10;  // statuses 1..4 are lib-c's

std::string json(const topo::Invariants& inv, double total) {
    auto b = [](bool v) { return v ? "true" : "false"; };
    std::string s = "{";
    s += "\"components\":" + std::to_string(inv.components);
    s += ",\"isolatedVertices\":" + std::to_string(inv.isolated_vertices);
    s += ",\"boundaryLoops\":" + std::to_string(inv.boundary_loops);
    s += ",\"nonManifoldEdges\":" + std::to_string(inv.non_manifold_edges);
    s += ",\"nonManifoldVertices\":" + std::to_string(inv.non_manifold_vertices);
    s += std::string(",\"manifold\":") + b(inv.manifold);
    s += std::string(",\"orientable\":") + b(inv.orientable);
    s += std::string(",\"consistentlyOriented\":") + b(inv.consistently_oriented);
    s += ",\"euler\":" + std::to_string(inv.euler_characteristic);
    s += ",\"genus\":" + (inv.genus ? std::to_string(*inv.genus) : std::string("null"));
    char buf[64];
    std::snprintf(buf, sizeof buf, ",\"totalCurvature\":%.17g}", total);
    return s + buf;
}

void fill(const topo::Mesh& m) {
    const auto& p = m.positions();
    double lo[3] = {0, 0, 0}, hi[3] = {0, 0, 0};
    if (!p.empty()) {
        lo[0] = hi[0] = p[0].x, lo[1] = hi[1] = p[0].y, lo[2] = hi[2] = p[0].z;
    }
    for (const auto& v : p) {
        const double c[3] = {v.x, v.y, v.z};
        for (int k = 0; k < 3; ++k) lo[k] = std::min(lo[k], c[k]), hi[k] = std::max(hi[k], c[k]);
    }
    const double centre[3] = {(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2};
    double radius = 0;
    for (const auto& v : p) radius = std::max(radius, std::hypot(v.x - centre[0], v.y - centre[1], v.z - centre[2]));
    const double scale = radius > 0 ? 1 / radius : 1;

    state.positions.clear();
    for (const auto& v : p) {
        state.positions.push_back(static_cast<float>((v.x - centre[0]) * scale));
        state.positions.push_back(static_cast<float>((v.y - centre[1]) * scale));
        state.positions.push_back(static_cast<float>((v.z - centre[2]) * scale));
    }
    state.indices.clear();
    for (const auto& t : m.triangles()) state.indices.insert(state.indices.end(), t.begin(), t.end());

    const auto k = topo::gaussian_curvature(m);
    state.curvature.clear();
    state.defect.clear();
    for (std::size_t v = 0; v < k.angle_defect.size(); ++v) {
        state.curvature.push_back(static_cast<float>(k.gaussian[v] / (scale * scale)));  // density scales as 1/length^2
        state.defect.push_back(static_cast<float>(k.angle_defect[v]));
    }
    state.summary = json(topo::analyze(m), k.total);
}

}  // namespace

extern "C" {

int topojs_read(const char* data, std::size_t size) {
    state = State{};
    try {
        fill(topo::Mesh::parse(std::string_view(data, size)));
        return 0;
    } catch (const topo::LoadError& e) {
        state.error = e.what();
        state.line = e.line();
        return e.status();
    } catch (const std::exception& e) {
        state.error = e.what();
        return kInvalidMesh;
    }
}

const char* topojs_error() { return state.error.c_str(); }
double topojs_error_line() { return static_cast<double>(state.line); }
const char* topojs_summary() { return state.summary.c_str(); }
uint32_t topojs_vertex_count() { return static_cast<uint32_t>(state.defect.size()); }
uint32_t topojs_index_count() { return static_cast<uint32_t>(state.indices.size()); }
const float* topojs_positions() { return state.positions.data(); }
const uint32_t* topojs_indices() { return state.indices.data(); }
const float* topojs_curvature() { return state.curvature.data(); }
const float* topojs_defect() { return state.defect.data(); }

}  // extern "C"
