/* Triangle meshes and their OBJ reader. */
#ifndef MESH_MESH_H
#define MESH_MESH_H

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    double x, y, z;
} mesh_vec3;

/* Polygons are fan-triangulated on reading; polygon_count keeps the number of faces in the file. */
typedef struct {
    mesh_vec3 *vertices;
    size_t vertex_count;
    uint32_t (*triangles)[3]; /* 0-based vertex indices */
    size_t triangle_count;
    size_t polygon_count;
} mesh;

typedef enum {
    MESH_OK = 0,
    MESH_ERR_IO,     /* file missing or unreadable */
    MESH_ERR_MEMORY, /* allocation failed or mesh too large */
    MESH_ERR_SYNTAX, /* malformed line */
    MESH_ERR_INDEX   /* face refers to a vertex that does not exist (yet) */
} mesh_status;

/* Leaves an empty mesh, safe to pass to mesh_free. */
void mesh_init(mesh *m);

/* Frees the arrays and leaves an empty mesh. */
void mesh_free(mesh *m);

/* Parses OBJ text into `out`, replacing its contents. Only `v` and `f` are used; texture and normal
 * references in faces are skipped, other statements ignored. On error `out` is left empty and, if
 * `error_line` is not NULL, it receives the 1-based line number (0 for I/O or memory errors). */
mesh_status mesh_read_obj_string(const char *text, mesh *out, size_t *error_line);

/* Same as mesh_read_obj_string, reading the whole file first. */
mesh_status mesh_read_obj_file(const char *path, mesh *out, size_t *error_line);

/* Parses a PLY mesh (ascii, binary_little_endian or binary_big_endian 1.0) of `size` bytes. Uses the
 * x, y, z properties of `vertex` and the `vertex_indices` (or `vertex_index`) list of `face`; other
 * properties and elements are skipped. `error_line` is the header or ASCII body line, 0 in a binary body. */
mesh_status mesh_read_ply(const char *data, size_t size, mesh *out, size_t *error_line);

/* Reads PLY if the data starts with the line "ply", OBJ otherwise. `data` need not be NUL-terminated. */
mesh_status mesh_read_buffer(const char *data, size_t size, mesh *out, size_t *error_line);

/* Reads a whole file and detects its format like mesh_read_buffer. */
mesh_status mesh_read_file(const char *path, mesh *out, size_t *error_line);

/* Short English description of a status, never NULL. */
const char *mesh_status_string(mesh_status status);

#ifdef __cplusplus
}
#endif

#endif
