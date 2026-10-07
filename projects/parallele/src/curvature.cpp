#include "par/curvature.hpp"

#include "kernels.hpp"

#include <memory>

#ifdef _OPENMP
#include <omp.h>
#endif

namespace par {

namespace {

Curvature allocate(const Mesh& mesh)
{
    Curvature k;
    k.defect.resize(mesh.vertex_count());
    k.area.resize(mesh.vertex_count());
    k.gaussian.resize(mesh.vertex_count());
    return k;
}

// The total in vertex order, whatever computed the vertices: a parallel reduction would add in
// another order and change the last bits.
double total(const std::vector<double>& defect)
{
    double sum = 0;
    for (double d : defect)
        sum += d;
    return sum;
}

}  // namespace

Curvature curvature_sequential(const Mesh& mesh, const Adjacency& adjacency)
{
    // Not zero-filled, as in the OpenMP version: both are timed on the same work.
    const auto angle = std::make_unique_for_overwrite<double[]>(mesh.tri.size());
    const auto share = std::make_unique_for_overwrite<double[]>(mesh.tri.size());
    for (uint32_t f = 0; f < mesh.face_count(); ++f)
        detail::face_pass(mesh, f, angle.get(), share.get());
    Curvature k = allocate(mesh);
    for (uint32_t v = 0; v < mesh.vertex_count(); ++v)
        detail::vertex_pass(adjacency, v, angle.get(), share.get(), k.defect.data(), k.area.data(), k.gaussian.data());
    k.total = total(k.defect);
    return k;
}

Curvature curvature_openmp(const Mesh& mesh, const Adjacency& adjacency, [[maybe_unused]] int threads)
{
    // Not zero-filled: a std::vector would clear 48 bytes per triangle on one thread before the parallel
    // loops (77 MB for 1.6 million triangles), and that serial part capped the speed-up near 2. The
    // pages are touched first by the threads that compute them.
    const auto angle = std::make_unique_for_overwrite<double[]>(mesh.tri.size());
    const auto share = std::make_unique_for_overwrite<double[]>(mesh.tri.size());
    Curvature k = allocate(mesh);
    // OpenMP 2.0 (MSVC) wants a signed loop variable.
    const auto nf = static_cast<long long>(mesh.face_count());
    const auto nv = static_cast<long long>(mesh.vertex_count());
#ifdef _OPENMP
    const int n = threads > 0 ? threads : omp_get_max_threads();
#pragma omp parallel num_threads(n)
#endif
    {
        // Each face writes only its own three corners; each vertex only its own results: no locks.
#ifdef _OPENMP
#pragma omp for schedule(static)
#endif
        for (long long f = 0; f < nf; ++f)
            detail::face_pass(mesh, static_cast<uint32_t>(f), angle.get(), share.get());
        // The implicit barrier of the first loop: every corner is ready before any vertex sums them.
#ifdef _OPENMP
#pragma omp for schedule(static)
#endif
        for (long long v = 0; v < nv; ++v)
            detail::vertex_pass(adjacency, static_cast<uint32_t>(v), angle.get(), share.get(), k.defect.data(), k.area.data(),
                                k.gaussian.data());
    }
    k.total = total(k.defect);
    return k;
}

bool openmp_available()
{
#ifdef _OPENMP
    return true;
#else
    return false;
#endif
}

int openmp_threads()
{
#ifdef _OPENMP
    return omp_get_max_threads();
#else
    return 1;
#endif
}

}  // namespace par
