/* meshinfo: prints the size of an OBJ mesh. Exit codes: 0 ok, 1 unreadable mesh, 2 usage. */
#include "mesh/mesh.h"

#include <stdio.h>

int main(int argc, char **argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: meshinfo FILE.obj\n");
        return 2;
    }
    mesh m;
    mesh_init(&m);
    size_t line = 0;
    mesh_status st = mesh_read_obj_file(argv[1], &m, &line);
    if (st != MESH_OK) {
        if (line)
            fprintf(stderr, "%s:%zu: %s\n", argv[1], line, mesh_status_string(st));
        else
            fprintf(stderr, "%s: %s\n", argv[1], mesh_status_string(st));
        return 1;
    }
    printf("vertices: %zu\npolygons: %zu\ntriangles: %zu\n", m.vertex_count, m.polygon_count, m.triangle_count);
    mesh_free(&m);
    return 0;
}
