#include "par/io.hpp"

extern "C" {
#include "mesh/mesh.h"
}

#include <algorithm>

namespace par {

Mesh load(const std::string& path)
{
    ::mesh m;
    mesh_init(&m);
    size_t line = 0;
    const mesh_status status = mesh_read_file(path.c_str(), &m, &line);
    if (status != MESH_OK) {
        std::string message = path + ": " + mesh_status_string(status);
        if (line > 0)
            message += " (line " + std::to_string(line) + ")";
        mesh_free(&m);
        throw LoadError(message);
    }
    Mesh out;
    out.xyz.reserve(3 * m.vertex_count);
    for (size_t i = 0; i < m.vertex_count; ++i)
        out.xyz.insert(out.xyz.end(), {m.vertices[i].x, m.vertices[i].y, m.vertices[i].z});
    out.tri.reserve(3 * m.triangle_count);
    for (size_t i = 0; i < m.triangle_count; ++i)
        out.tri.insert(out.tri.end(), {m.triangles[i][0], m.triangles[i][1], m.triangles[i][2]});
    mesh_free(&m);
    return out;
}

}  // namespace par
