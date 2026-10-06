/* meshinfo: prints the size of an OBJ or PLY mesh. Exit codes: 0 ok, 1 unreadable mesh, 2 usage. */
#include "mesh/mesh.h"

#include <stdio.h>

int main(int argc, char **argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: meshinfo FILE.obj|FILE.ply\n");
        return 2;
    }
    mesh m;
    mesh_init(&m);
    size_t line = 0;
    mesh_status st = mesh_read_file(argv[1], &m, &line);
    if (st != MESH_OK) {
        if (line)
            fprintf(stderr, "%s:%zu: %s\n", argv[1], line, mesh_status_string(st));
        else
            fprintf(stderr, "%s: %s\n", argv[1], mesh_status_string(st));
        return 1;
    }
    printf("vertices: %zu\npolygons: %zu\ntriangles: %zu\n", m.vertex_count, m.polygon_count, m.triangle_count);
    mesh_topology topo;
    if (mesh_compute_topology(&m, &topo) == MESH_OK)
        printf("edges: %zu\nboundary edges: %zu\neuler characteristic: %lld\n", topo.edge_count,
               topo.boundary_edge_count, (long long)topo.euler_characteristic);
    mesh_free(&m);
    return 0;
}
