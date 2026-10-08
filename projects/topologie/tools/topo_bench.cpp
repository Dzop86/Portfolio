// Native computing times for the table of the project page (sprint 40): prints one JSON object per size, read by
// scripts/bench.mjs. Usage: topo_bench [runs]   (build in Release: cmake -DCMAKE_BUILD_TYPE=Release)
#include "bench.hpp"

#include <cstdio>
#include <cstdlib>

int main(int argc, char** argv) {
    const int runs = argc > 1 ? std::atoi(argv[1]) : 3;
    std::printf("[\n");
    for (std::size_t k = 0; k < bench::kSizes.size(); ++k) {
        const auto [n, m] = bench::kSizes[k];
        const auto mesh = bench::torus(n, m);
        const auto t = bench::measure(mesh, runs);
        std::printf("  {\"triangles\": %zu, \"elevation\": %.2f, \"persistence\": %.2f, \"reeb\": %.2f, \"betti\": [%d, %d, %d], \"loops\": %zu}%s\n",
                    std::size_t{mesh.face_count()}, t.elevation_ms, t.persistence_ms, t.reeb_ms, t.betti[0], t.betti[1], t.betti[2], t.loops,
                    k + 1 < bench::kSizes.size() ? "," : "");
        std::fflush(stdout);
    }
    std::printf("]\n");
    return 0;
}
