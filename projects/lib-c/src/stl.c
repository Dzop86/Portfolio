/* STL reader, binary and ASCII. STL stores every facet with its own three corners: identical corners are
 * welded into shared vertices (exact match, -0 equal to 0), and facets left with a repeated vertex are
 * dropped, so that topology can be computed on the result. */
#include "internal.h"

#include <math.h>
#include <stdlib.h>
#include <string.h>

typedef struct {
    double x, y, z;
    uint32_t corner;
} corner_pos;

static int compare_corners(const void *pa, const void *pb) {
    const corner_pos *a = pa, *b = pb;
    if (a->x != b->x) return a->x < b->x ? -1 : 1;
    if (a->y != b->y) return a->y < b->y ? -1 : 1;
    if (a->z != b->z) return a->z < b->z ? -1 : 1;
    return 0;
}

/* Builds the mesh from 3 * facets corners (consumed: sorted in place). Vertices are numbered in the order
 * of their first appearance in the file. */
static mesh_status weld(corner_pos *corners, size_t facets, mesh *out) {
    const size_t n = 3 * facets;
    mesh m;
    mesh_init(&m);
    if (n == 0) {
        *out = m;
        return MESH_OK;
    }
    uint32_t *group = malloc(n * sizeof *group);    /* corner -> group of identical corners */
    uint32_t *vertex = malloc(n * sizeof *vertex);  /* group -> vertex id, by first appearance */
    m.vertices = malloc(n * sizeof *m.vertices);     /* upper bound, shrunk below */
    if (!group || !vertex || !m.vertices) {
        free(group);
        free(vertex);
        mesh_free(&m);
        return MESH_ERR_MEMORY;
    }
    qsort(corners, n, sizeof *corners, compare_corners);
    uint32_t groups = 0;
    for (size_t i = 0; i < n; i++) {
        if (i > 0 && compare_corners(&corners[i - 1], &corners[i]) != 0) groups++;
        group[corners[i].corner] = groups;
    }
    for (size_t g = 0; g <= groups; g++) vertex[g] = UINT32_MAX;
    /* Positions by group, then vertex ids in file order. */
    mesh_vec3 *position = malloc(((size_t)groups + 1) * sizeof *position);
    if (!position) {
        free(group);
        free(vertex);
        mesh_free(&m);
        return MESH_ERR_MEMORY;
    }
    for (size_t i = 0; i < n; i++) position[group[corners[i].corner]] = (mesh_vec3){corners[i].x, corners[i].y, corners[i].z};
    for (size_t c = 0; c < n; c++) {
        const uint32_t g = group[c];
        if (vertex[g] == UINT32_MAX) {
            vertex[g] = (uint32_t)m.vertex_count;
            m.vertices[m.vertex_count++] = position[g];
        }
    }
    free(position);
    mesh_vec3 *shrunk = realloc(m.vertices, m.vertex_count * sizeof *shrunk);
    if (shrunk) m.vertices = shrunk;

    size_t capacity = 0;
    mesh_status st = MESH_OK;
    for (size_t f = 0; f < facets && st == MESH_OK; f++) {
        const uint32_t a = vertex[group[3 * f]], b = vertex[group[3 * f + 1]], c = vertex[group[3 * f + 2]];
        if (a == b || b == c || c == a) continue; /* degenerate once welded */
        st = mesh__add_triangle(&m, &capacity, a, b, c);
        m.polygon_count++;
    }
    free(group);
    free(vertex);
    if (st != MESH_OK) {
        mesh_free(&m);
        return st;
    }
    *out = m;
    return MESH_OK;
}

/* Adding 0.0 turns -0.0 into +0.0, so that both weld together. */
static corner_pos make_corner(double x, double y, double z, size_t index) {
    return (corner_pos){x + 0.0, y + 0.0, z + 0.0, (uint32_t)index};
}

/* --- binary --------------------------------------------------------------------------------- */

static uint32_t read_u32_le(const unsigned char *p) {
    return (uint32_t)p[0] | (uint32_t)p[1] << 8 | (uint32_t)p[2] << 16 | (uint32_t)p[3] << 24;
}

static float read_f32_le(const unsigned char *p) {
    const uint32_t bits = read_u32_le(p);
    float f;
    memcpy(&f, &bits, sizeof f);
    return f;
}

/* Binary STL is recognised by its exact size: 80-byte header, facet count, 50 bytes per facet. */
static int is_binary(const unsigned char *data, size_t size) {
    if (size < 84) return 0;
    const uint64_t facets = read_u32_le(data + 80);
    return (uint64_t)84 + 50 * facets == (uint64_t)size;
}

static mesh_status read_binary(const unsigned char *data, mesh *out) {
    const size_t facets = read_u32_le(data + 80);
    if (facets > UINT32_MAX / 3) return MESH_ERR_MEMORY;
    corner_pos *corners = facets ? malloc(3 * facets * sizeof *corners) : NULL;
    if (facets && !corners) return MESH_ERR_MEMORY;
    for (size_t f = 0; f < facets; f++) {
        const unsigned char *p = data + 84 + 50 * f + 12; /* skip the normal */
        for (size_t k = 0; k < 3; k++, p += 12) {
            const double x = read_f32_le(p), y = read_f32_le(p + 4), z = read_f32_le(p + 8);
            if (!isfinite(x) || !isfinite(y) || !isfinite(z)) {
                free(corners);
                return MESH_ERR_SYNTAX;
            }
            corners[3 * f + k] = make_corner(x, y, z, 3 * f + k);
        }
    }
    mesh_status st = weld(corners, facets, out);
    free(corners);
    return st;
}

/* --- ASCII ---------------------------------------------------------------------------------- */

typedef enum { EXPECT_SOLID, IN_SOLID, IN_FACET, IN_LOOP, LOOP_DONE, AFTER_SOLID } ascii_state;

static int is_space(char c) { return c == ' ' || c == '\t' || c == '\r' || c == '\v' || c == '\f'; }

/* Splits `line` in place; returns the number of tokens (at most `max` + 1 when there are more). */
static size_t tokenize(char *line, char **tokens, size_t max) {
    size_t n = 0;
    char *s = line;
    while (*s) {
        while (is_space(*s)) *s++ = '\0';
        if (!*s) break;
        if (n == max) return max + 1;
        tokens[n++] = s;
        while (*s && !is_space(*s)) s++;
    }
    return n;
}

static int parse_number(const char *s, double *out) {
    char *end;
    *out = strtod(s, &end);
    return end != s && *end == '\0' && isfinite(*out);
}

static mesh_status read_ascii(const char *text, size_t len, mesh *out, size_t *error_line) {
    ascii_state state = EXPECT_SOLID;
    corner_pos *corners = NULL;
    size_t corner_count = 0, corner_capacity = 0, vertices_in_loop = 0;
    char *line = NULL;
    size_t line_capacity = 0, line_number = 0;
    mesh_status st = MESH_OK;

    for (const char *p = text, *end = text + len; p < end && st == MESH_OK;) {
        const char *eol = memchr(p, '\n', (size_t)(end - p));
        const size_t n = eol ? (size_t)(eol - p) : (size_t)(end - p);
        line_number++;
        char *grown = mesh__reserve(line, &line_capacity, n + 1, 1);
        if (!grown) {
            st = MESH_ERR_MEMORY;
            break;
        }
        line = grown; /* kept before any other exit, so that free(line) below releases it */
        if (memchr(p, '\0', n)) {
            st = MESH_ERR_SYNTAX;
            break;
        }
        memcpy(line, p, n);
        line[n] = '\0';
        p = eol ? eol + 1 : end;

        char *tok[6];
        const size_t count = tokenize(line, tok, 5);
        if (count == 0) continue;
        const char *kw = tok[0];
        switch (state) {
        case EXPECT_SOLID:
        case AFTER_SOLID:
            if (strcmp(kw, "solid") != 0) st = MESH_ERR_SYNTAX;
            state = IN_SOLID;
            break;
        case IN_SOLID:
            if (strcmp(kw, "facet") == 0)
                state = IN_FACET;
            else if (strcmp(kw, "endsolid") == 0)
                state = AFTER_SOLID;
            else
                st = MESH_ERR_SYNTAX;
            break;
        case IN_FACET:
            if (count == 2 && strcmp(kw, "outer") == 0 && strcmp(tok[1], "loop") == 0) {
                state = IN_LOOP;
                vertices_in_loop = 0;
            } else {
                st = MESH_ERR_SYNTAX;
            }
            break;
        case IN_LOOP:
            if (strcmp(kw, "endloop") == 0) {
                if (vertices_in_loop != 3) st = MESH_ERR_SYNTAX;
                state = LOOP_DONE;
            } else if (strcmp(kw, "vertex") == 0 && count == 4 && vertices_in_loop < 3) {
                double xyz[3];
                if (!parse_number(tok[1], &xyz[0]) || !parse_number(tok[2], &xyz[1]) || !parse_number(tok[3], &xyz[2])) {
                    st = MESH_ERR_SYNTAX;
                    break;
                }
                if (corner_count >= UINT32_MAX) {
                    st = MESH_ERR_MEMORY;
                    break;
                }
                corner_pos *more = mesh__reserve(corners, &corner_capacity, corner_count + 1, sizeof *more);
                if (!more) {
                    st = MESH_ERR_MEMORY;
                    break;
                }
                corners = more;
                corners[corner_count] = make_corner(xyz[0], xyz[1], xyz[2], corner_count);
                corner_count++;
                vertices_in_loop++;
            } else {
                st = MESH_ERR_SYNTAX;
            }
            break;
        case LOOP_DONE:
            if (strcmp(kw, "endfacet") != 0) st = MESH_ERR_SYNTAX;
            state = IN_SOLID;
            break;
        }
    }
    free(line);
    /* A file may stop after its last facet without "endsolid"; it may not stop inside a facet. */
    if (st == MESH_OK && state != IN_SOLID && state != AFTER_SOLID) st = MESH_ERR_SYNTAX;
    if (st == MESH_OK) st = weld(corners, corner_count / 3, out);
    else if (error_line && st == MESH_ERR_SYNTAX) *error_line = line_number;
    free(corners);
    return st;
}

mesh_status mesh_read_stl(const char *data, size_t size, mesh *out, size_t *error_line) {
    mesh_free(out);
    if (error_line) *error_line = 0;
    if (is_binary((const unsigned char *)data, size)) return read_binary((const unsigned char *)data, out);
    return read_ascii(data, size, out, error_line);
}

int mesh__looks_like_stl(const char *data, size_t size) {
    if (is_binary((const unsigned char *)data, size)) return 1;
    return size >= 6 && memcmp(data, "solid", 5) == 0 && (is_space(data[5]) || data[5] == '\n');
}
