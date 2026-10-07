#include "par/opencl.hpp"
#include "support.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <cmath>
#include <cstdlib>

namespace {

// Without an OpenCL device the tests are skipped, unless PAR_REQUIRE_OPENCL is set: the Linux CI sets
// it (PoCL), so that a missing device there is a failure, not a silent skip.
bool device_or_skip()
{
    if (!par::opencl_devices().empty())
        return true;
    if (std::getenv("PAR_REQUIRE_OPENCL") != nullptr)
        ADD_FAILURE() << "PAR_REQUIRE_OPENCL is set but no OpenCL device computes in double precision";
    return false;
}

// atan2, tan and sqrt of OpenCL may differ from the C library in the last bits: defects and areas
// within 1e-12 (relative above 1). K = defect / area divides that error by the area, which is tiny on a
// fine mesh (1e-4): K is checked as K x area, the defect it stands for. Measured on a 240,000-triangle
// torus with PoCL: 1.9e-15 at most on the defects, 94 % of them identical to the bit.
void expect_close(const par::Curvature& k, const par::Curvature& reference, const std::string& what)
{
    auto close = [](double a, double b) { return std::abs(a - b) <= 1e-12 * std::max(1.0, std::abs(b)); };
    for (std::size_t v = 0; v < reference.defect.size(); ++v) {
        ASSERT_TRUE(close(k.defect[v], reference.defect[v])) << what << " defect of " << v << ": " << k.defect[v] << " vs " << reference.defect[v];
        ASSERT_TRUE(close(k.area[v], reference.area[v])) << what << " area of " << v;
        ASSERT_TRUE(close(k.gaussian[v] * reference.area[v], reference.gaussian[v] * reference.area[v])) << what << " K of " << v;
    }
    EXPECT_NEAR(k.total, reference.total, 1e-8) << what;
}

TEST(OpenCL, ListsDevicesWithDoublePrecision)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no OpenCL device with double precision";
    for (const par::OpenCLDevice& d : par::opencl_devices())
        std::cout << "OpenCL device: " << d.platform << " / " << d.name << (d.gpu ? " (GPU)" : " (CPU)") << "\n";
}

TEST(OpenCL, AgreesWithSequentialOnTheReferenceMeshes)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no OpenCL device with double precision";
    for (const std::string& file : test::reference_files()) {
        const par::Mesh m = par::load(file);
        const par::Adjacency a = par::build_adjacency(m);
        expect_close(par::curvature_opencl(m, a), par::curvature_sequential(m, a), file);
    }
}

TEST(OpenCL, AgreesWithSequentialOnALargeMesh)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no OpenCL device with double precision";
    const par::Mesh m = par::torus(400, 300); // 240,000 triangles, not a multiple of the work-group size
    const par::Adjacency a = par::build_adjacency(m);
    expect_close(par::curvature_opencl(m, a), par::curvature_sequential(m, a), "torus 400 x 300");
}

TEST(OpenCL, RefusesAMissingDevice)
{
    const par::Mesh m = par::torus(4, 3);
    EXPECT_THROW((void)par::curvature_opencl(m, par::build_adjacency(m), 1000), std::runtime_error);
}

}  // namespace
