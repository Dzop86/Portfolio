// A triangle mesh in flat arrays, and the vertex-to-corner adjacency the parallel kernels need.
#pragma once

#include <cstdint>
#include <vector>

namespace par {

// Positions as x0 y0 z0 x1 y1 z1...; triangles as a0 b0 c0 a1 b1 c1... (indices from 0).
// Flat arrays copy as they are to a graphics card.
struct Mesh {
    std::vector<double> xyz;
    std::vector<uint32_t> tri;

    [[nodiscard]] uint32_t vertex_count() const { return static_cast<uint32_t>(xyz.size() / 3); }
    [[nodiscard]] uint32_t face_count() const { return static_cast<uint32_t>(tri.size() / 3); }
};

// For each vertex, the corners of the triangles around it (corner c of face f is 3f + c), in increasing
// order: summing over them adds the faces' contributions in the order of the faces, whatever the thread
// that computes the vertex. Compressed rows: the corners of vertex v are corners[offsets[v]..offsets[v+1]).
// `boundary` marks the vertices on an edge used by a single face (an edge shared by three faces or more
// is not a boundary).
struct Adjacency {
    std::vector<uint32_t> offsets;
    std::vector<uint32_t> corners;
    std::vector<uint8_t> boundary;
};

[[nodiscard]] Adjacency build_adjacency(const Mesh& mesh);

// A torus of (rings x segments) quads, each split into two triangles: a closed surface of genus 1 with
// curvature of both signs, at any size, for tests and benchmarks.
[[nodiscard]] Mesh torus(uint32_t rings, uint32_t segments, double major = 1.0, double minor = 0.4);

}  // namespace par
