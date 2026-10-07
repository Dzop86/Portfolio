// The same two passes as OpenCL kernels, in double precision, on a graphics card or a processor.
#pragma once

#include "par/curvature.hpp"

#include <string>
#include <vector>

namespace par {

struct OpenCLDevice {
    std::string platform;
    std::string name;
    bool gpu = false;
};

// The devices that compute in double precision (cl_khr_fp64), graphics cards first; empty when the
// build has no OpenCL or the machine no OpenCL driver.
[[nodiscard]] std::vector<OpenCLDevice> opencl_devices();

// On the device of that index in opencl_devices(). Throws std::runtime_error when there is none, or
// when OpenCL reports an error (its code in the message).
[[nodiscard]] Curvature curvature_opencl(const Mesh& mesh, const Adjacency& adjacency, std::size_t device = 0);

}  // namespace par
