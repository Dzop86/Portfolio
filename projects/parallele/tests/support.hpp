// Shared by the tests: the reference meshes, read by lib-c, and topologie's own result on them.
#pragma once

#include "par/curvature.hpp"
#include "par/io.hpp"
#include "topo/curvature.hpp"
#include "topo/mesh.hpp"

#include <string>
#include <vector>

namespace test {

// The topology viewer's samples and lib-c's test meshes: closed, open, non-orientable, OBJ, PLY, STL.
inline std::vector<std::string> reference_files()
{
    return {TOPO_SAMPLES "/torus.obj", TOPO_SAMPLES "/sphere.obj", TOPO_SAMPLES "/mobius.obj", TOPO_SAMPLES "/saddle.obj",
            MESH_TEST_DATA "/cube.obj", MESH_TEST_DATA "/tetrahedron.ply", MESH_TEST_DATA "/cube.stl"};
}

inline topo::Mesh to_topo(const par::Mesh& m)
{
    std::vector<topo::Vec3> p;
    for (std::size_t i = 0; i < m.xyz.size(); i += 3)
        p.push_back({m.xyz[i], m.xyz[i + 1], m.xyz[i + 2]});
    std::vector<topo::Triangle> t;
    for (std::size_t i = 0; i < m.tri.size(); i += 3)
        t.push_back({m.tri[i], m.tri[i + 1], m.tri[i + 2]});
    return topo::Mesh(std::move(p), std::move(t));
}

}  // namespace test
