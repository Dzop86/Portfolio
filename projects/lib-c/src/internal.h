/* Helpers shared by the readers; not part of the public API. */
#ifndef MESH_INTERNAL_H
#define MESH_INTERNAL_H

#include "mesh/mesh.h"

/* Returns `items` grown to hold at least `needed` elements of `size` bytes, or NULL on failure
 * (`items` is then left untouched and still owned by the caller). */
void *mesh__reserve(void *items, size_t *capacity, size_t needed, size_t size);

/* Appends triangle (a, b, c); `capacity` tracks the allocated size of m->triangles. */
mesh_status mesh__add_triangle(mesh *m, size_t *capacity, uint32_t a, uint32_t b, uint32_t c);

/* Reads a whole file into a malloc'ed buffer, NUL-terminated (the NUL is not counted in `size`). */
mesh_status mesh__load_file(const char *path, char **data, size_t *size);

/* OBJ reader over `len` bytes, which need not be NUL-terminated; a NUL byte is a syntax error. */
mesh_status mesh__read_obj(const char *text, size_t len, mesh *out, size_t *error_line);

#endif
