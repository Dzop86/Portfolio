// parcurv: Gaussian curvature of a mesh with each version, timed.
//   parcurv FILE|torus:RINGS:SEGMENTS [--backend seq|omp|opencl|cuda|cuda-float|all] [--threads N] [--runs N]
//   parcurv --devices
#include "par/cuda.hpp"
#include "par/curvature.hpp"
#include "par/io.hpp"
#include "par/opencl.hpp"

#include <algorithm>
#include <cmath>
#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <functional>
#include <numbers>
#include <stdexcept>
#include <string>
#include <vector>

namespace {

int usage()
{
    std::fputs("usage: parcurv FILE|torus:RINGS:SEGMENTS [--backend seq|omp|opencl|cuda|cuda-float|all] [--threads N] [--runs N]\n"
               "       parcurv --devices\n",
               stderr);
    return 2;
}

par::Mesh mesh_from(const std::string& spec)
{
    if (spec.rfind("torus:", 0) == 0) {
        const auto colon = spec.find(':', 6);
        if (colon == std::string::npos)
            throw std::invalid_argument("torus:RINGS:SEGMENTS");
        return par::torus(static_cast<uint32_t>(std::stoul(spec.substr(6, colon - 6))), static_cast<uint32_t>(std::stoul(spec.substr(colon + 1))));
    }
    return par::load(spec);
}

// The median of `runs` timings, in milliseconds, and the last result.
double time(int runs, const std::function<par::Curvature()>& run, par::Curvature& result)
{
    std::vector<double> ms;
    for (int i = 0; i < runs; ++i) {
        const auto start = std::chrono::steady_clock::now();
        result = run();
        ms.push_back(std::chrono::duration<double, std::milli>(std::chrono::steady_clock::now() - start).count());
    }
    std::sort(ms.begin(), ms.end());
    return ms[ms.size() / 2];
}

}  // namespace

int main(int argc, char** argv)
{
    std::vector<std::string> args(argv + 1, argv + argc);
    if (args.size() == 1 && args[0] == "--devices") {
        const auto devices = par::opencl_devices();
        if (devices.empty())
            std::puts("OpenCL: no device with double precision");
        for (std::size_t i = 0; i < devices.size(); ++i)
            std::printf("OpenCL %zu: %s / %s (%s)\n", i, devices[i].platform.c_str(), devices[i].name.c_str(), devices[i].gpu ? "GPU" : "CPU");
#ifdef PAR_HAVE_CUDA
        const auto cuda = par::cuda_devices();
        if (cuda.empty())
            std::puts("CUDA: built in, no device");
        for (std::size_t i = 0; i < cuda.size(); ++i)
            std::printf("CUDA %zu: %s (compute %d.%d)\n", i, cuda[i].name.c_str(), cuda[i].major, cuda[i].minor);
#else
        std::puts("CUDA: not built in");
#endif
        return 0;
    }
    if (args.empty() || args[0].rfind("--", 0) == 0)
        return usage();
    std::string backend = "all";
    int threads = 0, runs = 1;
    for (std::size_t i = 1; i < args.size(); ++i) {
        if (i + 1 >= args.size())
            return usage();
        if (args[i] == "--backend")
            backend = args[++i];
        else if (args[i] == "--threads")
            threads = std::atoi(args[++i].c_str());
        else if (args[i] == "--runs")
            runs = std::max(1, std::atoi(args[++i].c_str()));
        else
            return usage();
    }
    if (backend != "all" && backend != "seq" && backend != "omp" && backend != "opencl" && backend != "cuda" && backend != "cuda-float")
        return usage();

    try {
        const par::Mesh mesh = mesh_from(args[0]);
        const par::Adjacency adjacency = par::build_adjacency(mesh);
        std::printf("vertices %u, triangles %u\n", mesh.vertex_count(), mesh.face_count());
        auto report = [](const char* name, double ms, const par::Curvature& k) {
            // Below the printed precision, a rounding residue such as -3e-12 would print as -0.000000000.
            double chi = k.total / (2 * std::numbers::pi);
            if (std::abs(chi) < 5e-10)
                chi = 0.0;
            std::printf("%-7s %10.3f ms   sum of defects / 2 pi = %.9f\n", name, ms, chi);
        };
        par::Curvature k;
        if (backend == "all" || backend == "seq")
            report("seq", time(runs, [&] { return par::curvature_sequential(mesh, adjacency); }, k), k);
        if (backend == "all" || backend == "omp")
            report("omp", time(runs, [&] { return par::curvature_openmp(mesh, adjacency, threads); }, k), k);
        if (backend == "opencl" || (backend == "all" && !par::opencl_devices().empty()))
            report("opencl", time(runs, [&] { return par::curvature_opencl(mesh, adjacency); }, k), k);
        const bool cuda = backend == "all" && !par::cuda_devices().empty();
        if (backend == "cuda" || cuda)
            report("cuda", time(runs, [&] { return par::curvature_cuda(mesh, adjacency); }, k), k);
        if (backend == "cuda-float" || cuda)
            report("cuda-f", time(runs, [&] { return par::curvature_cuda_float(mesh, adjacency); }, k), k);
    } catch (const std::exception& e) {
        std::fprintf(stderr, "parcurv: %s\n", e.what());
        return 1;
    }
    return 0;
}
