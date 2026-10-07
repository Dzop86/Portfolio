#include "par/cuda.hpp"
#include "support.hpp"

#include <gtest/gtest.h>

#include <algorithm>
#include <cmath>
#include <cstdlib>

namespace {

// Like OpenCL: skipped without an NVIDIA card, unless PAR_REQUIRE_CUDA is set (a failure then).
bool device_or_skip()
{
    if (!par::cuda_devices().empty())
        return true;
    if (std::getenv("PAR_REQUIRE_CUDA") != nullptr)
        ADD_FAILURE() << "PAR_REQUIRE_CUDA is set but no CUDA device is available";
    return false;
}

// The same tolerance as OpenCL (see test_opencl.cpp): defects and areas within 1e-12, K x area too.
void expect_close(const par::Curvature& k, const par::Curvature& reference, const std::string& what)
{
    auto close = [](double a, double b) { return std::abs(a - b) <= 1e-12 * std::max(1.0, std::abs(b)); };
    for (std::size_t v = 0; v < reference.defect.size(); ++v) {
        ASSERT_TRUE(close(k.defect[v], reference.defect[v])) << what << " defect of " << v;
        ASSERT_TRUE(close(k.area[v], reference.area[v])) << what << " area of " << v;
        ASSERT_TRUE(close(k.gaussian[v] * reference.area[v], reference.gaussian[v] * reference.area[v])) << what << " K of " << v;
    }
    EXPECT_NEAR(k.total, reference.total, 1e-8) << what;
}

TEST(CUDA, AgreesWithSequentialOnTheReferenceMeshes)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no CUDA device";
    for (const par::CudaDevice& d : par::cuda_devices())
        std::cout << "CUDA device: " << d.name << " (compute " << d.major << "." << d.minor << ")\n";
    for (const std::string& file : test::reference_files()) {
        const par::Mesh m = par::load(file);
        const par::Adjacency a = par::build_adjacency(m);
        expect_close(par::curvature_cuda(m, a), par::curvature_sequential(m, a), file);
    }
}

TEST(CUDA, AgreesWithSequentialOnALargeMesh_AndTimesItself)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no CUDA device";
    const par::Mesh m = par::torus(400, 300);
    const par::Adjacency a = par::build_adjacency(m);
    par::CudaTiming timing;
    expect_close(par::curvature_cuda(m, a, 0, &timing), par::curvature_sequential(m, a), "torus 400 x 300");
    EXPECT_GT(timing.kernels_ms, 0.0);
    EXPECT_GT(timing.upload_ms, 0.0);
}

TEST(CUDA, SinglePrecisionStaysClose_AndKeepsGaussBonnet)
{
    if (!device_or_skip())
        GTEST_SKIP() << "no CUDA device";
    const par::Mesh m = par::torus(400, 300);
    const par::Adjacency a = par::build_adjacency(m);
    const par::Curvature single = par::curvature_cuda_float(m, a);
    const par::Curvature reference = par::curvature_sequential(m, a);
    double worst = 0;
    for (std::size_t v = 0; v < reference.defect.size(); ++v)
        worst = std::max(worst, std::abs(single.defect[v] - reference.defect[v]));
    std::cout << "single precision: largest error on a defect " << worst << "\n";
    // A float has 7 significant digits: the defects of a fine mesh (1e-4) keep a few of them.
    EXPECT_LT(worst, 1e-5);
    // Summed over 120 000 vertices: 0.0017 on the GTX 1660, 0.023 before pi was split into hi + lo (see
    // cuda.cu); the bound catches that bias coming back.
    std::cout << "single precision: Gauss-Bonnet total " << single.total << "\n";
    EXPECT_NEAR(single.total, 0.0, 5e-3);
}

TEST(CUDA, RefusesAMissingDevice)
{
    const par::Mesh m = par::torus(4, 3);
    EXPECT_THROW((void)par::curvature_cuda(m, par::build_adjacency(m), 99), std::runtime_error);
}

}  // namespace
