// CUDA version: the passes of src/kernels.hpp, templated on the floating-point type. --fmad=false (see
// CMakeLists.txt) keeps a * b + c as two roundings, as in the C++ build.
#include "par/cuda.hpp"

#include <cuda_runtime.h>

#include <stdexcept>
#include <string>
#include <type_traits>
#include <vector>

namespace par {

namespace {

void check(cudaError_t status, const char* what)
{
    if (status != cudaSuccess)
        throw std::runtime_error(std::string("CUDA: ") + what + ": " + cudaGetErrorString(status));
}

template <typename R>
struct V3 {
    R x, y, z;
};
template <typename R>
__device__ V3<R> point(const R* xyz, unsigned v) { return {xyz[3 * v], xyz[3 * v + 1], xyz[3 * v + 2]}; }
template <typename R>
__device__ V3<R> sub(V3<R> a, V3<R> b) { return {a.x - b.x, a.y - b.y, a.z - b.z}; }
template <typename R>
__device__ R dot3(V3<R> a, V3<R> b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
template <typename R>
__device__ V3<R> cross3(V3<R> a, V3<R> b) { return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x}; }
template <typename R>
__device__ R norm3(V3<R> a) { return sqrt(dot3(a, a)); }
template <typename R>
__device__ R angle3(V3<R> u, V3<R> v) { return atan2(norm3(cross3(u, v)), dot3(u, v)); }

constexpr double kPi = 3.141592653589793;

// pi as hi + lo: float(pi) is 8.7e-8 too large, and a defect 2 pi - sum of angles that started from it
// would carry the same bias on every vertex (0.02 over the 120 000 vertices of a torus, where Gauss-Bonnet
// wants 0). The remainder lo is put back after the sum; in double it is exactly 0, nothing changes.
template <typename R>
struct Pi {
    static constexpr R hi = R(kPi);
    static constexpr R lo = R(kPi - double(hi));
};

template <typename R>
__global__ void face_pass(const R* xyz, const unsigned* tri, unsigned nf, R* corner_angle, R* corner_area)
{
    const unsigned f = blockIdx.x * blockDim.x + threadIdx.x;
    if (f >= nf)
        return;
    const unsigned* t = tri + 3 * f;
    const R pi = R(kPi);
    const R area = norm3(cross3(sub(point(xyz, t[1]), point(xyz, t[0])), sub(point(xyz, t[2]), point(xyz, t[0])))) / 2;
    R theta[3];
    for (unsigned c = 0; c < 3; ++c) {
        const unsigned v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        theta[c] = angle3(sub(point(xyz, a), point(xyz, v)), sub(point(xyz, b), point(xyz, v)));
    }
    const unsigned obtuse = theta[0] > pi / 2 ? 0 : theta[1] > pi / 2 ? 1 : theta[2] > pi / 2 ? 2 : 3;
    for (unsigned c = 0; c < 3; ++c) {
        const unsigned v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        R share;
        if (obtuse < 3) {
            share = c == obtuse ? area / 2 : area / 4;
        } else {
            const V3<R> va = sub(point(xyz, a), point(xyz, v)), vb = sub(point(xyz, b), point(xyz, v));
            const R cot_b = 1 / tan(theta[(c + 2) % 3]), cot_a = 1 / tan(theta[(c + 1) % 3]);
            share = (dot3(va, va) * cot_b + dot3(vb, vb) * cot_a) / 8;
        }
        corner_angle[3 * f + c] = theta[c];
        corner_area[3 * f + c] = share;
    }
}

template <typename R>
__global__ void vertex_pass(const unsigned* offsets, const unsigned* corners, const unsigned char* boundary, unsigned nv,
                            const R* corner_angle, const R* corner_area, R* defect, R* area, R* gaussian)
{
    const unsigned v = blockIdx.x * blockDim.x + threadIdx.x;
    if (v >= nv)
        return;
    R d = 2 * Pi<R>::hi, s = 0;
    for (unsigned k = offsets[v]; k < offsets[v + 1]; ++k) {
        d -= corner_angle[corners[k]];
        s += corner_area[corners[k]];
    }
    if (boundary[v])
        d -= Pi<R>::hi;
    d += (boundary[v] ? 1 : 2) * Pi<R>::lo;
    defect[v] = d;
    area[v] = s;
    gaussian[v] = s > 0 ? d / s : R(0);
}

// Device memory freed on every path, exceptions included.
template <typename T>
struct DeviceArray {
    T* data = nullptr;
    explicit DeviceArray(std::size_t n)
    {
        if (n > 0)
            check(cudaMalloc(&data, n * sizeof(T)), "cudaMalloc");
    }
    DeviceArray(const DeviceArray&) = delete;
    DeviceArray& operator=(const DeviceArray&) = delete;
    ~DeviceArray() { cudaFree(data); }
};

struct Event {
    cudaEvent_t e{};
    Event() { check(cudaEventCreate(&e), "cudaEventCreate"); }
    Event(const Event&) = delete;
    Event& operator=(const Event&) = delete;
    ~Event() { cudaEventDestroy(e); }
};

float elapsed(const Event& a, const Event& b)
{
    float ms = 0;
    check(cudaEventElapsedTime(&ms, a.e, b.e), "cudaEventElapsedTime");
    return ms;
}

template <typename R>
Curvature run(const Mesh& mesh, const Adjacency& adj, int device, CudaTiming* timing)
{
    int count = 0;
    if (cudaGetDeviceCount(&count) != cudaSuccess || device < 0 || device >= count)
        throw std::runtime_error("no CUDA device at index " + std::to_string(device));
    check(cudaSetDevice(device), "cudaSetDevice");
    const unsigned nf = mesh.face_count(), nv = mesh.vertex_count();
    Curvature k;
    k.defect.resize(nv);
    k.area.resize(nv);
    k.gaussian.resize(nv);
    if (nv == 0)
        return k;

    // In double the arrays go to and from the card as they are; in float they are converted (each copy of
    // fresh memory costs, 20 ms for the positions of 1.6 million vertices under WSL).
    constexpr bool same = std::is_same_v<R, double>;
    std::vector<R> xyz;
    if constexpr (!same)
        xyz.assign(mesh.xyz.begin(), mesh.xyz.end());
    DeviceArray<R> d_xyz(mesh.xyz.size()), d_angle(mesh.tri.size()), d_share(mesh.tri.size());
    DeviceArray<R> d_defect(nv), d_area(nv), d_gaussian(nv);
    DeviceArray<unsigned> d_tri(mesh.tri.size()), d_offsets(adj.offsets.size()), d_corners(adj.corners.size());
    DeviceArray<unsigned char> d_boundary(adj.boundary.size());

    Event start, uploaded, computed, downloaded;
    check(cudaEventRecord(start.e), "cudaEventRecord");
    auto upload = [](auto& dst, const auto& src) {
        if (!src.empty())
            check(cudaMemcpy(dst.data, src.data(), src.size() * sizeof(src[0]), cudaMemcpyHostToDevice), "upload");
    };
    if constexpr (same)
        upload(d_xyz, mesh.xyz);
    else
        upload(d_xyz, xyz);
    upload(d_tri, mesh.tri);
    upload(d_offsets, adj.offsets);
    upload(d_corners, adj.corners);
    upload(d_boundary, adj.boundary);
    check(cudaEventRecord(uploaded.e), "cudaEventRecord");

    constexpr unsigned block = 256;
    if (nf > 0)
        face_pass<R><<<(nf + block - 1) / block, block>>>(d_xyz.data, d_tri.data, nf, d_angle.data, d_share.data);
    check(cudaGetLastError(), "face_pass");
    // Same stream: the vertex pass starts once every face is done.
    vertex_pass<R><<<(nv + block - 1) / block, block>>>(d_offsets.data, d_corners.data, d_boundary.data, nv, d_angle.data, d_share.data,
                                                        d_defect.data, d_area.data, d_gaussian.data);
    check(cudaGetLastError(), "vertex_pass");
    check(cudaEventRecord(computed.e), "cudaEventRecord");

    std::vector<R> defect, area, gaussian;
    auto download = [nv](std::vector<double>& result, std::vector<R>& narrow, const DeviceArray<R>& src) {
        R* dst = nullptr;
        if constexpr (same) {
            dst = result.data();
        } else {
            narrow.resize(nv);
            dst = narrow.data();
        }
        check(cudaMemcpy(dst, src.data, nv * sizeof(R), cudaMemcpyDeviceToHost), "download");
    };
    download(k.defect, defect, d_defect);
    download(k.area, area, d_area);
    download(k.gaussian, gaussian, d_gaussian);
    check(cudaEventRecord(downloaded.e), "cudaEventRecord");
    check(cudaEventSynchronize(downloaded.e), "cudaEventSynchronize");
    if (timing != nullptr)
        *timing = {elapsed(start, uploaded), elapsed(uploaded, computed), elapsed(computed, downloaded)};

    if constexpr (!same) {
        for (unsigned v = 0; v < nv; ++v) {
            k.defect[v] = defect[v];
            k.area[v] = area[v];
            k.gaussian[v] = gaussian[v];
        }
    }
    for (unsigned v = 0; v < nv; ++v)
        k.total += k.defect[v];
    return k;
}

}  // namespace

std::vector<CudaDevice> cuda_devices()
{
    std::vector<CudaDevice> out;
    int count = 0;
    if (cudaGetDeviceCount(&count) != cudaSuccess)
        return out;
    for (int i = 0; i < count; ++i) {
        cudaDeviceProp p{};
        if (cudaGetDeviceProperties(&p, i) == cudaSuccess)
            out.push_back({p.name, p.major, p.minor});
    }
    return out;
}

Curvature curvature_cuda(const Mesh& mesh, const Adjacency& adjacency, int device, CudaTiming* timing)
{
    return run<double>(mesh, adjacency, device, timing);
}

Curvature curvature_cuda_float(const Mesh& mesh, const Adjacency& adjacency, int device, CudaTiming* timing)
{
    return run<float>(mesh, adjacency, device, timing);
}

}  // namespace par
