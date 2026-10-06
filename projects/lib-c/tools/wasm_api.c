/* Entry points for the WebAssembly build: read a buffer, then query the figures one by one.
 * Only one mesh is kept at a time; the JavaScript side copies the file into memory from _malloc. */
#include "mesh/mesh.h"

static mesh current;
static mesh_topology topo;
static size_t error_line;

/* Returns a mesh_status; on success the getters below describe the mesh. */
int meshjs_read(const char *data, size_t size) {
    mesh_free(&current);
    mesh_status st = mesh_read_buffer(data, size, &current, &error_line);
    if (st == MESH_OK) st = mesh_compute_topology(&current, &topo);
    if (st != MESH_OK) mesh_free(&current);
    return (int)st;
}

const char *meshjs_status_string(int status) { return mesh_status_string((mesh_status)status); }
double meshjs_error_line(void) { return (double)error_line; }
double meshjs_vertex_count(void) { return (double)current.vertex_count; }
double meshjs_polygon_count(void) { return (double)current.polygon_count; }
double meshjs_triangle_count(void) { return (double)current.triangle_count; }
double meshjs_edge_count(void) { return (double)topo.edge_count; }
double meshjs_boundary_edge_count(void) { return (double)topo.boundary_edge_count; }
double meshjs_euler_characteristic(void) { return (double)topo.euler_characteristic; }

/* Bounding box coordinate: axis 0..2 for x, y, z; corner 0 for min, 1 for max. */
double meshjs_bbox(int corner, int axis) {
    const mesh_vec3 *v = corner ? &topo.max : &topo.min;
    return axis == 0 ? v->x : axis == 1 ? v->y : v->z;
}
