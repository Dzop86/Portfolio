// Discrete Gaussian curvature by angle defect.
#pragma once

#include "topo/mesh.hpp"

#include <vector>

namespace topo {

struct GaussianCurvature {
    // Per vertex: 2 pi - sum of the angles around it, or pi - sum on the boundary (concentrated curvature).
    std::vector<double> angle_defect;
    // Per vertex: one third of the area of its triangles (barycentric area).
    std::vector<double> area;
    // Per vertex: angle_defect / area, an estimate of K; 0 where the area is 0.
    std::vector<double> gaussian;
    // Sum of the defects: 2 pi chi on any triangulated surface (discrete Gauss-Bonnet).
    double total = 0;
};

// O(V + T). Boundary vertices are those on an edge used by a single face.
[[nodiscard]] GaussianCurvature gaussian_curvature(const Mesh& mesh);

}  // namespace topo
