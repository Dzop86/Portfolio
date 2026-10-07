// The same two passes as CUDA kernels, on an NVIDIA graphics card, in double or single precision.
#pragma once

#include "par/curvature.hpp"

#include <string>
#include <vector>

namespace par {

struct CudaDevice {
    std::string name;
    int major = 0; // compute capability
    int minor = 0;
};

// Empty when the build has no CUDA or the machine no NVIDIA card (or driver).
[[nodiscard]] std::vector<CudaDevice> cuda_devices();

// Milliseconds spent on the card: copies to it, the two kernels, copies back (CUDA events).
struct CudaTiming {
    double upload_ms = 0;
    double kernels_ms = 0;
    double download_ms = 0;
};

// In double precision: within 1e-12 of the sequential result. Throws std::runtime_error without a card.
[[nodiscard]] Curvature curvature_cuda(const Mesh& mesh, const Adjacency& adjacency, int device = 0, CudaTiming* timing = nullptr);

// In single precision (positions, angles and sums in float, results widened to double): much faster on
// consumer cards, whose double-precision units are few; how far it strays is measured by the tests.
[[nodiscard]] Curvature curvature_cuda_float(const Mesh& mesh, const Adjacency& adjacency, int device = 0, CudaTiming* timing = nullptr);

}  // namespace par
