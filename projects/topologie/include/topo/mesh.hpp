// Triangle mesh as a half-edge structure, built from lib-c's reader or from arrays.
#pragma once

#include <array>
#include <cstdint>
#include <filesystem>
#include <limits>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

namespace topo {

inline constexpr uint32_t kNone = std::numeric_limits<uint32_t>::max();

struct Vec3 {
    double x, y, z;
};

using Triangle = std::array<uint32_t, 3>;

// Half-edge h belongs to face h / 3 and runs from `origin` to the origin of `next`.
// `twin` is kNone on a boundary edge and on an edge shared by more than two faces.
// `flipped` marks twins running the same way: the two faces disagree on orientation.
struct HalfEdge {
    uint32_t origin;
    uint32_t next;
    uint32_t twin;
    uint32_t face;
    bool flipped;
};

// Thrown when lib-c cannot read a mesh; line() is 0 when the error has no line (I/O, binary PLY).
class LoadError : public std::runtime_error {
public:
    LoadError(const std::string& message, std::size_t line) : std::runtime_error(message), line_(line) {}
    [[nodiscard]] std::size_t line() const noexcept { return line_; }

private:
    std::size_t line_;
};

class Mesh {
public:
    // Throws std::invalid_argument on an out-of-range index or a triangle repeating a vertex.
    Mesh(std::vector<Vec3> positions, std::vector<Triangle> triangles);

    // Reads an OBJ or PLY file (format detected by lib-c). Throws LoadError.
    [[nodiscard]] static Mesh load(const std::filesystem::path& path);
    // Same from bytes in memory.
    [[nodiscard]] static Mesh parse(std::string_view data);

    [[nodiscard]] uint32_t vertex_count() const noexcept { return static_cast<uint32_t>(positions_.size()); }
    [[nodiscard]] uint32_t face_count() const noexcept { return static_cast<uint32_t>(triangles_.size()); }
    [[nodiscard]] std::size_t edge_count() const noexcept { return edge_count_; }

    [[nodiscard]] const std::vector<Vec3>& positions() const noexcept { return positions_; }
    [[nodiscard]] const std::vector<Triangle>& triangles() const noexcept { return triangles_; }
    [[nodiscard]] const std::vector<HalfEdge>& half_edges() const noexcept { return half_edges_; }
    // Edges shared by three faces or more, as sorted vertex pairs.
    [[nodiscard]] const std::vector<std::array<uint32_t, 2>>& non_manifold_edges() const noexcept {
        return non_manifold_edges_;
    }

    [[nodiscard]] uint32_t target(uint32_t h) const { return half_edges_[half_edges_[h].next].origin; }

private:
    std::vector<Vec3> positions_;
    std::vector<Triangle> triangles_;
    std::vector<HalfEdge> half_edges_;
    std::vector<std::array<uint32_t, 2>> non_manifold_edges_;
    std::size_t edge_count_ = 0;
};

}  // namespace topo
