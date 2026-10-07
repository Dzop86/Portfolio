// Built when OpenCL is not found: no device, and a clear error if the version is asked for anyway.
#include "par/opencl.hpp"

#include <stdexcept>

namespace par {

std::vector<OpenCLDevice> opencl_devices() { return {}; }

Curvature curvature_opencl(const Mesh&, const Adjacency&, std::size_t)
{
    throw std::runtime_error("this build has no OpenCL");
}

}  // namespace par
