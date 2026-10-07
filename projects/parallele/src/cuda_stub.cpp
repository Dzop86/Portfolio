// Built when no CUDA compiler is found: no device, and a clear error if the version is asked for anyway.
#include "par/cuda.hpp"

#include <stdexcept>

namespace par {

std::vector<CudaDevice> cuda_devices() { return {}; }

Curvature curvature_cuda(const Mesh&, const Adjacency&, int, CudaTiming*) { throw std::runtime_error("this build has no CUDA"); }

Curvature curvature_cuda_float(const Mesh&, const Adjacency&, int, CudaTiming*)
{
    throw std::runtime_error("this build has no CUDA");
}

}  // namespace par
