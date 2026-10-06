/* libFuzzer target: any input must give MESH_OK or an error, never a crash, leak or invalid access. */
#include "mesh/mesh.h"

#include <stddef.h>
#include <stdint.h>

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t size);

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t size) {
    mesh m;
    mesh_init(&m);
    size_t line;
    if (mesh_read_buffer((const char *)data, size, &m, &line) == MESH_OK) {
        for (size_t i = 0; i < m.triangle_count; i++)
            for (int k = 0; k < 3; k++)
                if (m.triangles[i][k] >= m.vertex_count) __builtin_trap(); /* index invariant */
        mesh_topology topo;
        if (mesh_compute_topology(&m, &topo) == MESH_OK && topo.boundary_edge_count > topo.edge_count)
            __builtin_trap();
    }
    mesh_free(&m);
    return 0;
}
