// Bounding volume hierarchy over the triangles of a mesh, split by the surface area heuristic (SAH)
// evaluated on bins along the widest axis of the centroids.
#pragma once

#include "rt/geometry.hpp"
#include "rt/mesh.hpp"

#include <cstdint>
#include <optional>
#include <vector>

namespace rt {

struct MeshHit {
    double t, u, v;
    uint32_t triangle;
};

class Bvh {
public:
    struct Node {
        Box box;
        // Inner node: children at `first` and `first + 1`, count == 0. Leaf: `count` triangles of order()
        // starting at `first`.
        uint32_t first = 0;
        uint32_t count = 0;
    };

    static constexpr uint32_t kMaxLeaf = 4;
    static constexpr int kBins = 12;
    // Deeper nodes become leaves whatever their size: bounds the traversal stack.
    static constexpr std::size_t kMaxDepth = 60;

    Bvh() = default;
    explicit Bvh(const TriangleMesh& mesh);

    // Nearest triangle hit in (tmin, tmax).
    [[nodiscard]] std::optional<MeshHit> intersect(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const;
    // Whether any triangle is hit in (tmin, tmax): shadow rays stop at the first one.
    [[nodiscard]] bool occluded(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const;

    [[nodiscard]] const std::vector<Node>& nodes() const { return nodes_; }
    [[nodiscard]] const std::vector<uint32_t>& order() const { return order_; }
    [[nodiscard]] std::size_t depth() const;

private:
    void build(const TriangleMesh& mesh, uint32_t node, std::vector<Box>& boxes, std::vector<Vec3>& centroids,
               std::size_t level);
    template <bool AnyHit>
    std::optional<MeshHit> traverse(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const;

    std::vector<Node> nodes_;
    std::vector<uint32_t> order_;  // triangle indices, leaves refer to ranges of it
};

// Every triangle tested in turn: the reference the BVH is checked and measured against.
std::optional<MeshHit> intersect_brute_force(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax);

}  // namespace rt
