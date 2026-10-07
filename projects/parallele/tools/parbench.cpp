// parbench: times each version of the curvature on tori of growing size, and writes the results as JSON
// for the project page (data/bench.json).
//   parbench [--out FILE] [--runs N] [--threads N] [--sizes 50000,200000,...] [--machine "description"]
// --threads sets the OpenMP threads (default: OpenMP's own, every logical processor). One per physical
// core is usually faster: with hyperthreading, threads that spin while they wait slow the others.
#include "par/cuda.hpp"
#include "par/curvature.hpp"
#include "par/opencl.hpp"

#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <functional>
#include <sstream>
#include <string>
#include <thread>
#include <vector>

namespace {

struct Sample {
    double ms = 0;         // wall clock, median
    double upload_ms = -1; // CUDA only, medians of the same runs
    double kernels_ms = -1;
    double download_ms = -1;
};

double median(std::vector<double> v)
{
    std::sort(v.begin(), v.end());
    return v[v.size() / 2];
}

Sample measure(int runs, const std::function<void(par::CudaTiming*)>& run)
{
    run(nullptr); // warm-up: first allocations, kernel compilation (OpenCL), context creation (CUDA)
    std::vector<double> wall, up, kern, down;
    for (int i = 0; i < runs; ++i) {
        par::CudaTiming t;
        const auto start = std::chrono::steady_clock::now();
        run(&t);
        wall.push_back(std::chrono::duration<double, std::milli>(std::chrono::steady_clock::now() - start).count());
        up.push_back(t.upload_ms);
        kern.push_back(t.kernels_ms);
        down.push_back(t.download_ms);
    }
    return {median(wall), median(up), median(kern), median(down)};
}

// The largest difference with the sequential defects: 0 for the exact versions, measured for float.
double worst(const par::Curvature& a, const par::Curvature& b)
{
    double w = 0;
    for (std::size_t v = 0; v < a.defect.size(); ++v)
        w = std::max(w, std::abs(a.defect[v] - b.defect[v]));
    return w;
}

std::string cpu_name()
{
    std::ifstream in("/proc/cpuinfo");
    std::string line;
    while (std::getline(in, line))
        if (line.rfind("model name", 0) == 0)
            return line.substr(line.find(':') + 2);
    return "unknown processor";
}

std::string escape(const std::string& s)
{
    std::string out;
    for (char c : s) {
        if (c == '"' || c == '\\')
            out += '\\';
        out += c;
    }
    return out;
}

}  // namespace

int main(int argc, char** argv)
{
    std::string out = "-", machine;
    int runs = 7;
    int threads = 0;
    std::vector<uint32_t> sizes = {50000, 200000, 800000, 3200000};
    for (int i = 1; i + 1 < argc; i += 2) {
        const std::string key = argv[i], value = argv[i + 1];
        if (key == "--out")
            out = value;
        else if (key == "--runs")
            runs = std::max(1, std::atoi(value.c_str()));
        else if (key == "--threads")
            threads = std::max(0, std::atoi(value.c_str()));
        else if (key == "--machine")
            machine = value;
        else if (key == "--sizes") {
            sizes.clear();
            std::stringstream list(value);
            for (std::string item; std::getline(list, item, ',');)
                sizes.push_back(static_cast<uint32_t>(std::stoul(item)));
        } else {
            std::fputs("usage: parbench [--out FILE] [--runs N] [--threads N] [--sizes N,N,...] [--machine TEXT]\n", stderr);
            return 2;
        }
    }

    const auto opencl = par::opencl_devices();
    const auto cuda = par::cuda_devices();
    std::ostringstream json;
    json << "{\n  \"machine\": {\"cpu\": \"" << escape(cpu_name()) << "\", \"logical_processors\": " << std::thread::hardware_concurrency()
         << ", \"openmp_threads\": " << (threads > 0 ? threads : par::openmp_threads());
    json << ", \"opencl\": \"" << (opencl.empty() ? "" : escape(opencl[0].name)) << "\"";
    json << ", \"cuda\": \"" << (cuda.empty() ? "" : escape(cuda[0].name)) << "\"";
    json << ", \"note\": \"" << escape(machine) << "\"},\n  \"runs\": " << runs << ",\n  \"results\": [";
    bool first = true;
    for (uint32_t triangles : sizes) {
        // A torus of rings x segments quads with segments = rings / 2: 2 x rings x segments triangles.
        const auto rings = static_cast<uint32_t>(std::lround(std::sqrt(triangles)));
        const uint32_t segments = std::max<uint32_t>(3, triangles / (2 * rings));
        const par::Mesh mesh = par::torus(rings, segments);
        const auto t0 = std::chrono::steady_clock::now();
        const par::Adjacency adj = par::build_adjacency(mesh);
        const double adjacency_ms = std::chrono::duration<double, std::milli>(std::chrono::steady_clock::now() - t0).count();
        const par::Curvature reference = par::curvature_sequential(mesh, adj);
        std::fprintf(stderr, "%u triangles\n", mesh.face_count());

        struct Backend {
            const char* name;
            std::function<par::Curvature(par::CudaTiming*)> run;
            bool gpu = false; // times its transfers and kernels with CUDA events
        };
        std::vector<Backend> backends = {
            {"sequential", [&](par::CudaTiming*) { return par::curvature_sequential(mesh, adj); }},
            {"openmp", [&](par::CudaTiming*) { return par::curvature_openmp(mesh, adj, threads); }},
        };
        if (!opencl.empty())
            backends.push_back({"opencl", [&](par::CudaTiming*) { return par::curvature_opencl(mesh, adj); }});
        if (!cuda.empty()) {
            backends.push_back({"cuda-double", [&](par::CudaTiming* t) { return par::curvature_cuda(mesh, adj, 0, t); }, true});
            backends.push_back({"cuda-float", [&](par::CudaTiming* t) { return par::curvature_cuda_float(mesh, adj, 0, t); }, true});
        }
        for (const Backend& b : backends) {
            par::Curvature last;
            const Sample s = measure(runs, [&](par::CudaTiming* t) { last = b.run(t); });
            json << (first ? "\n" : ",\n") << "    {\"backend\": \"" << b.name << "\", \"triangles\": " << mesh.face_count()
                 << ", \"vertices\": " << mesh.vertex_count() << ", \"adjacency_ms\": " << adjacency_ms << ", \"ms\": " << s.ms;
            if (b.gpu)
                json << ", \"upload_ms\": " << s.upload_ms << ", \"kernels_ms\": " << s.kernels_ms << ", \"download_ms\": " << s.download_ms;
            json << ", \"max_defect_error\": " << worst(last, reference) << "}";
            first = false;
            std::fprintf(stderr, "  %-12s %9.2f ms\n", b.name, s.ms);
        }
    }
    json << "\n  ]\n}\n";
    if (out == "-")
        std::fputs(json.str().c_str(), stdout);
    else
        std::ofstream(out) << json.str();
    return 0;
}
