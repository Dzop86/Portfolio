#include "mesh/mesh.h"

#include <stdlib.h>

void mesh_init(mesh *m) {
    m->vertices = NULL;
    m->vertex_count = 0;
    m->triangles = NULL;
    m->triangle_count = 0;
    m->polygon_count = 0;
}

void mesh_free(mesh *m) {
    free(m->vertices);
    free(m->triangles);
    mesh_init(m);
}

const char *mesh_status_string(mesh_status status) {
    switch (status) {
    case MESH_OK: return "ok";
    case MESH_ERR_IO: return "cannot read file";
    case MESH_ERR_MEMORY: return "out of memory";
    case MESH_ERR_SYNTAX: return "syntax error";
    case MESH_ERR_INDEX: return "index out of range";
    }
    return "unknown error";
}
