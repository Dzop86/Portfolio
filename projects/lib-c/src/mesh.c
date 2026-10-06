#include "internal.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

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

void *mesh__reserve(void *items, size_t *capacity, size_t needed, size_t size) {
    if (needed <= *capacity) return items;
    size_t next = *capacity ? *capacity : 64;
    while (next < needed) {
        if (next > SIZE_MAX / 2) return NULL;
        next *= 2;
    }
    if (next > SIZE_MAX / size) return NULL;
    void *grown = realloc(items, next * size);
    if (grown) *capacity = next;
    return grown;
}

mesh_status mesh__add_triangle(mesh *m, size_t *capacity, uint32_t a, uint32_t b, uint32_t c) {
    uint32_t(*grown)[3] = mesh__reserve(m->triangles, capacity, m->triangle_count + 1, sizeof *grown);
    if (!grown) return MESH_ERR_MEMORY;
    m->triangles = grown;
    uint32_t *t = m->triangles[m->triangle_count++];
    t[0] = a;
    t[1] = b;
    t[2] = c;
    return MESH_OK;
}

mesh_status mesh__load_file(const char *path, char **data, size_t *size) {
    *data = NULL;
    *size = 0;
    FILE *f = fopen(path, "rb");
    if (!f) return MESH_ERR_IO;
    char *text = NULL;
    size_t used = 0, capacity = 0;
    mesh_status st = MESH_OK;
    for (;;) {
        char *grown = mesh__reserve(text, &capacity, used + 4096 + 1, 1);
        if (!grown) {
            st = MESH_ERR_MEMORY;
            break;
        }
        text = grown;
        size_t n = fread(text + used, 1, capacity - used - 1, f);
        used += n;
        if (n == 0) {
            if (ferror(f)) st = MESH_ERR_IO;
            break;
        }
    }
    fclose(f);
    if (st != MESH_OK) {
        free(text);
        return st;
    }
    text[used] = '\0';
    *data = text;
    *size = used;
    return MESH_OK;
}

/* PLY files start with the line "ply"; anything else is read as OBJ. */
static int looks_like_ply(const char *data, size_t size) {
    return size >= 4 && memcmp(data, "ply", 3) == 0 && (data[3] == '\n' || data[3] == '\r');
}

mesh_format mesh_detect_format(const char *data, size_t size) {
    if (looks_like_ply(data, size)) return MESH_FORMAT_PLY;
    if (mesh__looks_like_stl(data, size)) return MESH_FORMAT_STL;
    return MESH_FORMAT_OBJ;
}

mesh_status mesh_read_buffer(const char *data, size_t size, mesh *out, size_t *error_line) {
    switch (mesh_detect_format(data, size)) {
    case MESH_FORMAT_PLY: return mesh_read_ply(data, size, out, error_line);
    case MESH_FORMAT_STL: return mesh_read_stl(data, size, out, error_line);
    case MESH_FORMAT_OBJ: break;
    }
    return mesh__read_obj(data, size, out, error_line);
}

mesh_status mesh_read_obj_string(const char *text, mesh *out, size_t *error_line) {
    return mesh__read_obj(text, strlen(text), out, error_line);
}

static mesh_status read_file_with(mesh_status (*reader)(const char *, size_t, mesh *, size_t *), const char *path,
                                  mesh *out, size_t *error_line) {
    mesh_free(out);
    if (error_line) *error_line = 0;
    char *data;
    size_t size;
    mesh_status st = mesh__load_file(path, &data, &size);
    if (st != MESH_OK) return st;
    st = reader(data, size, out, error_line);
    free(data);
    return st;
}

mesh_status mesh_read_obj_file(const char *path, mesh *out, size_t *error_line) {
    return read_file_with(mesh__read_obj, path, out, error_line);
}

mesh_status mesh_read_file(const char *path, mesh *out, size_t *error_line) {
    return read_file_with(mesh_read_buffer, path, out, error_line);
}
