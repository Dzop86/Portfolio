#include "rt/bvh.hpp"

#include <algorithm>
#include <array>
#include <numeric>

namespace rt {

Bvh::Bvh(const TriangleMesh& mesh) {
    const std::size_t n = mesh.triangles.size();
    std::vector<Box> boxes(n);
    std::vector<Vec3> centroids(n);
    for (std::size_t i = 0; i < n; ++i) {
        for (const uint32_t v : mesh.triangles[i]) boxes[i].grow(mesh.positions[v]);
        centroids[i] = boxes[i].center();
    }
    order_.resize(n);
    std::iota(order_.begin(), order_.end(), 0u);
    nodes_.reserve(2 * n);
    nodes_.push_back({{}, 0, static_cast<uint32_t>(n)});
    build(mesh, 0, boxes, centroids, 1);
}

void Bvh::build(const TriangleMesh& mesh, uint32_t index, std::vector<Box>& boxes, std::vector<Vec3>& centroids,
                std::size_t level) {
    // nodes_ may grow below: work on copies of the fields, write the node back at the end.
    const uint32_t first = nodes_[index].first, count = nodes_[index].count;
    Box box, cbox;
    for (uint32_t i = first; i < first + count; ++i) {
        box.grow(boxes[order_[i]]);
        cbox.grow(centroids[order_[i]]);
    }
    nodes_[index].box = box;
    // The traversal stack holds at most one entry per level, plus one.
    if (count <= kMaxLeaf || level >= kMaxDepth) return;

    // Widest axis of the centroids; all centroids at one point: no split can separate them.
    const Vec3 extent = cbox.hi - cbox.lo;
    const int axis = extent.x >= extent.y && extent.x >= extent.z ? 0 : extent.y >= extent.z ? 1 : 2;
    if (extent[axis] <= 0) return;

    struct Bin {
        Box box;
        uint32_t count = 0;
    };
    std::array<Bin, kBins> bins{};
    const double scale = kBins / extent[axis];
    auto bin_of = [&](uint32_t tri) {
        const int b = static_cast<int>((centroids[tri][axis] - cbox.lo[axis]) * scale);
        return std::min(b, kBins - 1);
    };
    for (uint32_t i = first; i < first + count; ++i) {
        Bin& b = bins[static_cast<std::size_t>(bin_of(order_[i]))];
        b.box.grow(boxes[order_[i]]);
        ++b.count;
    }
    // Cost of each split between bin s - 1 and bin s: area times count on each side (traversal cost 1,
    // intersection cost 1, relative to the parent's area).
    std::array<double, kBins> left_cost{};
    Box acc;
    uint32_t acc_count = 0;
    for (int s = 1; s < kBins; ++s) {
        acc.grow(bins[static_cast<std::size_t>(s - 1)].box);
        acc_count += bins[static_cast<std::size_t>(s - 1)].count;
        left_cost[static_cast<std::size_t>(s)] = acc.area() * acc_count;
    }
    acc = Box{};
    acc_count = 0;
    int best = -1;
    double best_cost = kInfinity;
    for (int s = kBins - 1; s >= 1; --s) {
        acc.grow(bins[static_cast<std::size_t>(s)].box);
        acc_count += bins[static_cast<std::size_t>(s)].count;
        const double cost = left_cost[static_cast<std::size_t>(s)] + acc.area() * acc_count;
        if (cost < best_cost) best_cost = cost, best = s;
    }
    const double leaf_cost = box.area() * count;
    if (best < 0 || best_cost >= leaf_cost) return;

    const auto begin = order_.begin() + static_cast<std::ptrdiff_t>(first);
    const auto mid = std::partition(begin, begin + static_cast<std::ptrdiff_t>(count),
                                    [&](uint32_t tri) { return bin_of(tri) < best; });
    const auto left_count = static_cast<uint32_t>(mid - begin);
    if (left_count == 0 || left_count == count) return;

    const auto left = static_cast<uint32_t>(nodes_.size());
    nodes_.push_back({{}, first, left_count});
    nodes_.push_back({{}, first + left_count, count - left_count});
    nodes_[index].first = left;
    nodes_[index].count = 0;
    build(mesh, left, boxes, centroids, level + 1);
    build(mesh, left + 1, boxes, centroids, level + 1);
}

template <bool AnyHit>
std::optional<MeshHit> Bvh::traverse(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const {
    if (nodes_.empty()) return std::nullopt;
    const Vec3 inv{1 / r.dir.x, 1 / r.dir.y, 1 / r.dir.z};
    std::optional<MeshHit> best;
    std::array<uint32_t, kMaxDepth + 1> stack{};
    std::size_t top = 0;
    if (hit_box(nodes_[0].box, r, inv, tmin, tmax) == kInfinity) return std::nullopt;
    stack[top++] = 0;
    while (top > 0) {
        const Node& node = nodes_[stack[--top]];
        if (node.count > 0) {
            for (uint32_t i = node.first; i < node.first + node.count; ++i) {
                const auto& tri = mesh.triangles[order_[i]];
                const auto h = hit_triangle(r, mesh.positions[tri[0]], mesh.positions[tri[1]], mesh.positions[tri[2]], tmin, tmax);
                if (h) {
                    best = MeshHit{h->t, h->u, h->v, order_[i]};
                    if constexpr (AnyHit) return best;
                    tmax = h->t;
                }
            }
            continue;
        }
        // Nearer child last on the stack, so it is visited first and shrinks tmax for the other.
        const double d0 = hit_box(nodes_[node.first].box, r, inv, tmin, tmax);
        const double d1 = hit_box(nodes_[node.first + 1].box, r, inv, tmin, tmax);
        const uint32_t near = d0 <= d1 ? node.first : node.first + 1;
        const uint32_t far = near == node.first ? node.first + 1 : node.first;
        const double dnear = std::min(d0, d1), dfar = std::max(d0, d1);
        if (dfar != kInfinity) stack[top++] = far;
        if (dnear != kInfinity) stack[top++] = near;
    }
    return best;
}

std::optional<MeshHit> Bvh::intersect(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const {
    return traverse<false>(mesh, r, tmin, tmax);
}

bool Bvh::occluded(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) const {
    return traverse<true>(mesh, r, tmin, tmax).has_value();
}

std::size_t Bvh::depth() const {
    if (nodes_.empty()) return 0;
    std::size_t deepest = 0;
    std::vector<std::pair<uint32_t, std::size_t>> stack{{0u, 1u}};
    while (!stack.empty()) {
        const auto [i, d] = stack.back();
        stack.pop_back();
        deepest = std::max(deepest, d);
        if (nodes_[i].count == 0) {
            stack.push_back({nodes_[i].first, d + 1});
            stack.push_back({nodes_[i].first + 1, d + 1});
        }
    }
    return deepest;
}

std::optional<MeshHit> intersect_brute_force(const TriangleMesh& mesh, const Ray& r, double tmin, double tmax) {
    std::optional<MeshHit> best;
    for (std::size_t i = 0; i < mesh.triangles.size(); ++i) {
        const auto& tri = mesh.triangles[i];
        const auto h = hit_triangle(r, mesh.positions[tri[0]], mesh.positions[tri[1]], mesh.positions[tri[2]], tmin, tmax);
        if (h) {
            best = MeshHit{h->t, h->u, h->v, static_cast<uint32_t>(i)};
            tmax = h->t;
        }
    }
    return best;
}

}  // namespace rt
