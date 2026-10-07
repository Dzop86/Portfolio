#include "par/opencl.hpp"

#define CL_TARGET_OPENCL_VERSION 120
#ifdef __APPLE__
#include <OpenCL/opencl.h>
#else
#include <CL/cl.h>
#endif

#include <map>
#include <memory>
#include <mutex>
#include <stdexcept>
#include <string>

namespace par {

namespace {

// The kernels mirror src/kernels.hpp line for line. FP_CONTRACT OFF keeps a * b + c as two roundings,
// as in the C++ build, instead of one fused multiply-add.
constexpr const char* kSource = R"CL(
#pragma OPENCL EXTENSION cl_khr_fp64 : enable
#pragma OPENCL FP_CONTRACT OFF
#define PI 3.141592653589793

typedef struct { double x, y, z; } V3;
V3 point(__global const double* xyz, uint v) { V3 p = { xyz[3 * v], xyz[3 * v + 1], xyz[3 * v + 2] }; return p; }
V3 sub(V3 a, V3 b) { V3 r = { a.x - b.x, a.y - b.y, a.z - b.z }; return r; }
double dot3(V3 a, V3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
V3 cross3(V3 a, V3 b) { V3 r = { a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x }; return r; }
double norm3(V3 a) { return sqrt(dot3(a, a)); }
double angle3(V3 u, V3 v) { return atan2(norm3(cross3(u, v)), dot3(u, v)); }

__kernel void face_pass(__global const double* xyz, __global const uint* tri, uint nf,
                        __global double* corner_angle, __global double* corner_area) {
    uint f = get_global_id(0);
    if (f >= nf) return;
    __global const uint* t = tri + 3 * f;
    double area = norm3(cross3(sub(point(xyz, t[1]), point(xyz, t[0])), sub(point(xyz, t[2]), point(xyz, t[0])))) / 2;
    double theta[3];
    for (uint c = 0; c < 3; ++c) {
        uint v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        theta[c] = angle3(sub(point(xyz, a), point(xyz, v)), sub(point(xyz, b), point(xyz, v)));
    }
    uint obtuse = theta[0] > PI / 2 ? 0 : theta[1] > PI / 2 ? 1 : theta[2] > PI / 2 ? 2 : 3;
    for (uint c = 0; c < 3; ++c) {
        uint v = t[c], a = t[(c + 1) % 3], b = t[(c + 2) % 3];
        double share;
        if (obtuse < 3) {
            share = c == obtuse ? area / 2 : area / 4;
        } else {
            V3 va = sub(point(xyz, a), point(xyz, v)), vb = sub(point(xyz, b), point(xyz, v));
            double cot_b = 1 / tan(theta[(c + 2) % 3]), cot_a = 1 / tan(theta[(c + 1) % 3]);
            share = (dot3(va, va) * cot_b + dot3(vb, vb) * cot_a) / 8;
        }
        corner_angle[3 * f + c] = theta[c];
        corner_area[3 * f + c] = share;
    }
}

__kernel void vertex_pass(__global const uint* offsets, __global const uint* corners, __global const uchar* boundary, uint nv,
                          __global const double* corner_angle, __global const double* corner_area,
                          __global double* defect, __global double* area, __global double* gaussian) {
    uint v = get_global_id(0);
    if (v >= nv) return;
    double d = 2 * PI, s = 0;
    for (uint k = offsets[v]; k < offsets[v + 1]; ++k) {
        d -= corner_angle[corners[k]];
        s += corner_area[corners[k]];
    }
    if (boundary[v]) d -= PI;
    defect[v] = d;
    area[v] = s;
    gaussian[v] = s > 0 ? d / s : 0.0;
}
)CL";

void check(cl_int status, const char* what)
{
    if (status != CL_SUCCESS)
        throw std::runtime_error(std::string("OpenCL: ") + what + " failed with error " + std::to_string(status));
}

std::string info(cl_device_id device, cl_device_info what)
{
    size_t size = 0;
    clGetDeviceInfo(device, what, 0, nullptr, &size);
    std::string text(size, '\0');
    clGetDeviceInfo(device, what, size, text.data(), nullptr);
    while (!text.empty() && text.back() == '\0')
        text.pop_back();
    return text;
}

struct Found {
    cl_device_id id;
    OpenCLDevice description;
};

// Every device with double precision, graphics cards first, in platform order otherwise.
std::vector<Found> find_devices()
{
    std::vector<Found> gpus, others;
    cl_uint nplatforms = 0;
    if (clGetPlatformIDs(0, nullptr, &nplatforms) != CL_SUCCESS || nplatforms == 0)
        return {};
    std::vector<cl_platform_id> platforms(nplatforms);
    clGetPlatformIDs(nplatforms, platforms.data(), nullptr);
    for (cl_platform_id platform : platforms) {
        size_t size = 0;
        clGetPlatformInfo(platform, CL_PLATFORM_NAME, 0, nullptr, &size);
        std::string pname(size, '\0');
        clGetPlatformInfo(platform, CL_PLATFORM_NAME, size, pname.data(), nullptr);
        while (!pname.empty() && pname.back() == '\0')
            pname.pop_back();
        cl_uint ndevices = 0;
        if (clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, 0, nullptr, &ndevices) != CL_SUCCESS || ndevices == 0)
            continue;
        std::vector<cl_device_id> devices(ndevices);
        clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, ndevices, devices.data(), nullptr);
        for (cl_device_id d : devices) {
            cl_device_fp_config fp64 = 0;
            clGetDeviceInfo(d, CL_DEVICE_DOUBLE_FP_CONFIG, sizeof fp64, &fp64, nullptr);
            if (fp64 == 0)
                continue;
            cl_device_type type = 0;
            clGetDeviceInfo(d, CL_DEVICE_TYPE, sizeof type, &type, nullptr);
            const bool gpu = (type & CL_DEVICE_TYPE_GPU) != 0;
            (gpu ? gpus : others).push_back({d, {pname, info(d, CL_DEVICE_NAME), gpu}});
        }
    }
    gpus.insert(gpus.end(), others.begin(), others.end());
    return gpus;
}

// RAII for the OpenCL handles.
template <typename T, cl_int (*Release)(T)>
struct Handle {
    T value = nullptr;
    Handle() = default;
    explicit Handle(T v) : value(v) {}
    Handle(const Handle&) = delete;
    Handle& operator=(const Handle&) = delete;
    Handle(Handle&& other) noexcept : value(other.value) { other.value = nullptr; }
    Handle& operator=(Handle&&) = delete;
    ~Handle()
    {
        if (value != nullptr)
            Release(value);
    }
};
using Context = Handle<cl_context, clReleaseContext>;
using Queue = Handle<cl_command_queue, clReleaseCommandQueue>;
using Program = Handle<cl_program, clReleaseProgram>;
using Kernel = Handle<cl_kernel, clReleaseKernel>;
using Buffer = Handle<cl_mem, clReleaseMemObject>;

// The context and the compiled kernels of a device, built on first use and kept: PoCL takes some 45 ms
// to compile them, which every call paid before, and which the benchmarks then measured instead of the
// computation. Never freed: at exit the OpenCL library may already be unloaded.
struct Compiled {
    Context context;
    Program program;
};

const Compiled& compiled(cl_device_id device)
{
    static std::mutex mutex;
    static auto* cache = new std::map<cl_device_id, std::unique_ptr<Compiled>>;
    const std::lock_guard lock(mutex);
    std::unique_ptr<Compiled>& slot = (*cache)[device];
    if (slot)
        return *slot;
    cl_int status = CL_SUCCESS;
    Context context(clCreateContext(nullptr, 1, &device, nullptr, nullptr, &status));
    check(status, "clCreateContext");
    const char* source = kSource;
    Program program(clCreateProgramWithSource(context.value, 1, &source, nullptr, &status));
    check(status, "clCreateProgramWithSource");
    if (clBuildProgram(program.value, 1, &device, "", nullptr, nullptr) != CL_SUCCESS) {
        size_t size = 0;
        clGetProgramBuildInfo(program.value, device, CL_PROGRAM_BUILD_LOG, 0, nullptr, &size);
        std::string log(size, '\0');
        clGetProgramBuildInfo(program.value, device, CL_PROGRAM_BUILD_LOG, size, log.data(), nullptr);
        throw std::runtime_error("OpenCL: the kernels do not build:\n" + log);
    }
    slot = std::make_unique<Compiled>(Compiled{std::move(context), std::move(program)});
    return *slot;
}

}  // namespace

std::vector<OpenCLDevice> opencl_devices()
{
    std::vector<OpenCLDevice> out;
    for (const Found& f : find_devices())
        out.push_back(f.description);
    return out;
}

Curvature curvature_opencl(const Mesh& mesh, const Adjacency& adjacency, std::size_t index)
{
    const std::vector<Found> devices = find_devices();
    if (index >= devices.size())
        throw std::runtime_error("no OpenCL device with double precision at index " + std::to_string(index));
    cl_device_id device = devices[index].id;
    const uint32_t nf = mesh.face_count(), nv = mesh.vertex_count();
    Curvature k;
    k.defect.resize(nv);
    k.area.resize(nv);
    k.gaussian.resize(nv);
    if (nv == 0 || nf == 0) {
        for (uint32_t v = 0; v < nv; ++v)
            k.defect[v] = adjacency.boundary[v] ? 3.141592653589793 : 2 * 3.141592653589793;
        for (double d : k.defect)
            k.total += d;
        return k;
    }

    const Compiled& c = compiled(device);
    const Context& context = c.context;
    const Program& program = c.program;
    cl_int status = CL_SUCCESS;
    Queue queue(clCreateCommandQueue(context.value, device, 0, &status));
    check(status, "clCreateCommandQueue");

    auto input = [&](const auto& v) {
        Buffer b(clCreateBuffer(context.value, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR, v.size() * sizeof(v[0]),
                                const_cast<void*>(static_cast<const void*>(v.data())), &status));
        check(status, "clCreateBuffer");
        return b;
    };
    auto output = [&](std::size_t bytes) {
        Buffer b(clCreateBuffer(context.value, CL_MEM_READ_WRITE, bytes, nullptr, &status));
        check(status, "clCreateBuffer");
        return b;
    };
    Buffer xyz = input(mesh.xyz), tri = input(mesh.tri);
    Buffer offsets = input(adjacency.offsets), corners = input(adjacency.corners), boundary = input(adjacency.boundary);
    Buffer angle = output(mesh.tri.size() * sizeof(double)), share = output(mesh.tri.size() * sizeof(double));
    Buffer defect = output(nv * sizeof(double)), area = output(nv * sizeof(double)), gaussian = output(nv * sizeof(double));

    Kernel faces(clCreateKernel(program.value, "face_pass", &status));
    check(status, "clCreateKernel(face_pass)");
    Kernel vertices(clCreateKernel(program.value, "vertex_pass", &status));
    check(status, "clCreateKernel(vertex_pass)");
    auto arg = [](cl_kernel kernel, cl_uint i, const auto& value) {
        check(clSetKernelArg(kernel, i, sizeof value, &value), "clSetKernelArg");
    };
    arg(faces.value, 0, xyz.value);
    arg(faces.value, 1, tri.value);
    arg(faces.value, 2, cl_uint{nf});
    arg(faces.value, 3, angle.value);
    arg(faces.value, 4, share.value);
    arg(vertices.value, 0, offsets.value);
    arg(vertices.value, 1, corners.value);
    arg(vertices.value, 2, boundary.value);
    arg(vertices.value, 3, cl_uint{nv});
    arg(vertices.value, 4, angle.value);
    arg(vertices.value, 5, share.value);
    arg(vertices.value, 6, defect.value);
    arg(vertices.value, 7, area.value);
    arg(vertices.value, 8, gaussian.value);

    // Work sizes rounded up to a multiple of 64; the kernels ignore the extra items. The queue is in
    // order: the vertex pass starts once every face is done.
    auto round_up = [](std::size_t n) { return (n + 63) / 64 * 64; };
    const std::size_t face_items = round_up(nf), vertex_items = round_up(nv);
    check(clEnqueueNDRangeKernel(queue.value, faces.value, 1, nullptr, &face_items, nullptr, 0, nullptr, nullptr), "face_pass");
    check(clEnqueueNDRangeKernel(queue.value, vertices.value, 1, nullptr, &vertex_items, nullptr, 0, nullptr, nullptr), "vertex_pass");
    check(clEnqueueReadBuffer(queue.value, defect.value, CL_TRUE, 0, nv * sizeof(double), k.defect.data(), 0, nullptr, nullptr), "read");
    check(clEnqueueReadBuffer(queue.value, area.value, CL_TRUE, 0, nv * sizeof(double), k.area.data(), 0, nullptr, nullptr), "read");
    check(clEnqueueReadBuffer(queue.value, gaussian.value, CL_TRUE, 0, nv * sizeof(double), k.gaussian.data(), 0, nullptr, nullptr), "read");
    for (double d : k.defect)
        k.total += d;
    return k;
}

}  // namespace par
