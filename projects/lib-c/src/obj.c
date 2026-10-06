/* OBJ reader: `v x y z [w]` and `f i[/t][/n] ...`, everything else ignored.
 * Each line is copied into a NUL-terminated buffer so that strtod and strtol never read past it. */
#include "mesh/mesh.h"

#include <errno.h>
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct {
    mesh m;
    size_t vertex_capacity;
    size_t triangle_capacity;
} builder;

/* Returns `items` grown to hold at least `needed` elements of `size` bytes, or NULL on failure
 * (`items` is then left untouched and still owned by the caller). */
static void *reserve(void *items, size_t *capacity, size_t needed, size_t size) {
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

static int is_space(char c) { return c == ' ' || c == '\t' || c == '\r' || c == '\v' || c == '\f'; }

static char *skip_spaces(char *s) {
    while (is_space(*s)) s++;
    return s;
}

/* True if the statement keyword at `s` is exactly `kw`. */
static int keyword(const char *s, const char *kw) {
    size_t n = strlen(kw);
    return strncmp(s, kw, n) == 0 && (s[n] == '\0' || is_space(s[n]));
}

static mesh_status parse_vertex(builder *b, char *s) {
    double xyz[3];
    for (int i = 0; i < 3; i++) {
        char *end;
        xyz[i] = strtod(s, &end);
        if (end == s || !isfinite(xyz[i]) || (*end != '\0' && !is_space(*end))) return MESH_ERR_SYNTAX;
        s = end;
    }
    s = skip_spaces(s);
    if (*s != '\0') { /* optional weight */
        char *end;
        (void)strtod(s, &end);
        if (end == s || *skip_spaces(end) != '\0') return MESH_ERR_SYNTAX;
    }
    if (b->m.vertex_count >= UINT32_MAX) return MESH_ERR_MEMORY;
    mesh_vec3 *grown = reserve(b->m.vertices, &b->vertex_capacity, b->m.vertex_count + 1, sizeof *grown);
    if (!grown) return MESH_ERR_MEMORY;
    b->m.vertices = grown;
    b->m.vertices[b->m.vertex_count++] = (mesh_vec3){xyz[0], xyz[1], xyz[2]};
    return MESH_OK;
}

/* Reads one face corner ("7", "7/2", "7//3", "-1/2/3") and resolves it to a 0-based index. */
static mesh_status parse_corner(const builder *b, char **cursor, uint32_t *index) {
    char *s = *cursor;
    char *end;
    errno = 0;
    long long value = strtoll(s, &end, 10);
    if (end == s || errno == ERANGE) return MESH_ERR_SYNTAX;
    while (*end == '/' || (*end >= '0' && *end <= '9') || *end == '-') end++; /* texture/normal refs */
    if (*end != '\0' && !is_space(*end)) return MESH_ERR_SYNTAX;
    *cursor = end;

    long long count = (long long)b->m.vertex_count;
    long long resolved = value > 0 ? value - 1 : count + value;
    if (value == 0 || resolved < 0 || resolved >= count) return MESH_ERR_INDEX;
    *index = (uint32_t)resolved;
    return MESH_OK;
}

static mesh_status parse_face(builder *b, char *s) {
    uint32_t first = 0, previous = 0;
    size_t corners = 0;
    size_t start = b->m.triangle_count;
    for (s = skip_spaces(s); *s != '\0'; s = skip_spaces(s)) {
        uint32_t index;
        mesh_status st = parse_corner(b, &s, &index);
        if (st != MESH_OK) {
            b->m.triangle_count = start;
            return st;
        }
        if (corners == 0) {
            first = index;
        } else if (corners >= 2) { /* fan: (first, previous, current) */
            uint32_t(*grown)[3] = reserve(b->m.triangles, &b->triangle_capacity, b->m.triangle_count + 1,
                                          sizeof *grown);
            if (!grown) return MESH_ERR_MEMORY;
            b->m.triangles = grown;
            uint32_t *t = b->m.triangles[b->m.triangle_count++];
            t[0] = first;
            t[1] = previous;
            t[2] = index;
        }
        previous = index;
        corners++;
    }
    if (corners < 3) {
        b->m.triangle_count = start;
        return MESH_ERR_SYNTAX;
    }
    b->m.polygon_count++;
    return MESH_OK;
}

static mesh_status parse_line(builder *b, char *line) {
    char *s = skip_spaces(line);
    if (*s == '\0' || *s == '#') return MESH_OK;
    if (keyword(s, "v")) return parse_vertex(b, s + 1);
    if (keyword(s, "f")) return parse_face(b, s + 1);
    static const char *const ignored[] = {"vt", "vn", "vp", "o", "g", "s", "usemtl", "mtllib", "l", "p"};
    for (size_t i = 0; i < sizeof ignored / sizeof *ignored; i++)
        if (keyword(s, ignored[i])) return MESH_OK;
    return MESH_ERR_SYNTAX;
}

mesh_status mesh_read_obj_string(const char *text, mesh *out, size_t *error_line) {
    mesh_free(out);
    if (error_line) *error_line = 0;

    builder b;
    mesh_init(&b.m);
    b.vertex_capacity = 0;
    b.triangle_capacity = 0;
    char *line = NULL;
    size_t line_capacity = 0;
    size_t line_number = 0;
    mesh_status st = MESH_OK;

    for (const char *p = text; *p != '\0' && st == MESH_OK;) {
        const char *eol = strchr(p, '\n');
        size_t len = eol ? (size_t)(eol - p) : strlen(p);
        line_number++;
        char *grown = reserve(line, &line_capacity, len + 1, 1);
        if (!grown) {
            st = MESH_ERR_MEMORY;
            line_number = 0;
            break;
        }
        line = grown;
        memcpy(line, p, len);
        line[len] = '\0';
        st = parse_line(&b, line);
        p = eol ? eol + 1 : p + len;
    }
    free(line);

    if (st != MESH_OK) {
        mesh_free(&b.m);
        if (error_line) *error_line = line_number;
        return st;
    }
    *out = b.m;
    return MESH_OK;
}

mesh_status mesh_read_obj_file(const char *path, mesh *out, size_t *error_line) {
    mesh_free(out);
    if (error_line) *error_line = 0;

    FILE *f = fopen(path, "rb");
    if (!f) return MESH_ERR_IO;
    char *text = NULL;
    size_t size = 0, capacity = 0;
    mesh_status st = MESH_OK;
    for (;;) {
        char *grown = reserve(text, &capacity, size + 4096 + 1, 1);
        if (!grown) {
            st = MESH_ERR_MEMORY;
            break;
        }
        text = grown;
        size_t n = fread(text + size, 1, capacity - size - 1, f);
        size += n;
        if (n == 0) {
            if (ferror(f)) st = MESH_ERR_IO;
            break;
        }
    }
    fclose(f);
    if (st == MESH_OK) {
        text[size] = '\0';
        st = mesh_read_obj_string(text, out, error_line);
    }
    free(text);
    return st;
}
