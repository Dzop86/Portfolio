// Discrete Gaussian curvature (angle defect over the mixed Voronoi area, as in the topologie project),
// computed in two passes: per face (angles and areas of its corners), then per vertex (sums over its
// corners). Each pass has independent iterations: they run in parallel.
#pragma once

#include "par/mesh.hpp"

#include <string>
#include <vector>

namespace par {

struct Curvature {
    std::vector<double> defect;   // 2 pi - sum of the angles (pi - sum on the boundary)
    std::vector<double> area;     // mixed Voronoi area
    std::vector<double> gaussian; // defect / area, 0 where the area is 0
    double total = 0;             // sum of the defects: 2 pi chi (discrete Gauss-Bonnet)
};

// The reference: plain loops.
[[nodiscard]] Curvature curvature_sequential(const Mesh& mesh, const Adjacency& adjacency);

// OpenMP on `threads` threads (0: all of them); identical, bit for bit, to the sequential result.
[[nodiscard]] Curvature curvature_openmp(const Mesh& mesh, const Adjacency& adjacency, int threads = 0);

// Whether this build has OpenMP (a compiler without it runs the OpenMP version on one thread).
[[nodiscard]] bool openmp_available();

// The threads the OpenMP version uses by default (1 without OpenMP).
[[nodiscard]] int openmp_threads();

}  // namespace par
