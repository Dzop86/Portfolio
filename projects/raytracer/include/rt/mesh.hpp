// Triangle meshes read by lib-c (OBJ, PLY, STL), placed in the scene, with smooth vertex normals.
#pragma once

#include "rt/geometry.hpp"

#include <array>
#include <cstdint>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

namespace rt {

// Thrown when lib-c cannot read a mesh: status() is lib-c's mesh_status (1 I/O, 2 memory, 3 syntax,
// 4 index), line() is 0 when the error has no line.
class LoadError : public std::runtime_error {
public:
    LoadError(const std::string& message, int status, std::size_t line)
        : std::runtime_error(message), status_(status), line_(line) {}
    [[nodiscard]] int status() const noexcept { return status_; }
    [[nodiscard]] std::size_t line() const noexcept { return line_; }

private:
    int status_;
    std::size_t line_;
};

struct TriangleMesh {
    std::vector<Vec3> positions;
    std::vector<Vec3> normals;  // one per vertex, see compute_normals
    std::vector<std::array<uint32_t, 3>> triangles;

    // Reads a mesh in any format lib-c knows; throws LoadError. An empty mesh is an error too.
    static TriangleMesh read(std::string_view data);
    static TriangleMesh load(const std::string& path);

    // Area-weighted vertex normals. A face normal is added with the sign that agrees with what the vertex
    // has gathered so far, so that a non-orientable surface (a Möbius strip) or inconsistently oriented
    // faces still get smooth normals; the renderer turns each normal towards the side the ray comes from.
    void compute_normals();

    // Centres the bounding box on the vertical axis, scales the mesh to fit a sphere of radius `radius`,
    // and sets it on the plane y = 0.
    void fit(double radius);

    [[nodiscard]] Box bounds() const;
};

}  // namespace rt
