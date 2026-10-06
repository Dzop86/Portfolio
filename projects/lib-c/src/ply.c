/* PLY reader: header parsed line by line, then the body read value by value, in ASCII or binary.
 * Every read is bounds-checked against the buffer, and element counts are checked against the
 * remaining bytes before any allocation, so a forged header cannot trigger a huge malloc. */
#include "internal.h"

#include <math.h>
#include <stdlib.h>
#include <string.h>

#define PLY_MAX_ELEMENTS 16
#define PLY_MAX_PROPERTIES 32
#define PLY_NAME_MAX 32
#define PLY_LINE_MAX 256
#define PLY_TOKEN_MAX 64

typedef enum { PLY_INT8, PLY_UINT8, PLY_INT16, PLY_UINT16, PLY_INT32, PLY_UINT32, PLY_FLOAT32, PLY_FLOAT64 } ply_type;

static const struct {
    const char *name, *alias;
    size_t size;
    double min, max; /* integer range; ignored for floats */
} TYPES[] = {
    {"char", "int8", 1, -128.0, 127.0},
    {"uchar", "uint8", 1, 0.0, 255.0},
    {"short", "int16", 2, -32768.0, 32767.0},
    {"ushort", "uint16", 2, 0.0, 65535.0},
    {"int", "int32", 4, -2147483648.0, 2147483647.0},
    {"uint", "uint32", 4, 0.0, 4294967295.0},
    {"float", "float32", 4, 0.0, 0.0},
    {"double", "float64", 8, 0.0, 0.0},
};

static int is_integer_type(ply_type t) { return t <= PLY_UINT32; }

typedef struct {
    char name[PLY_NAME_MAX];
    int is_list;
    ply_type count_type; /* lists only */
    ply_type type;
} ply_property;

typedef struct {
    char name[PLY_NAME_MAX];
    size_t count;
    ply_property props[PLY_MAX_PROPERTIES];
    size_t prop_count;
} ply_element;

typedef enum { PLY_ASCII, PLY_BINARY_LE, PLY_BINARY_BE } ply_format;

typedef struct {
    ply_format format;
    ply_element elements[PLY_MAX_ELEMENTS];
    size_t element_count;
    size_t lines; /* header lines, end_header included */
    size_t body;  /* offset of the first body byte */
} ply_header;

typedef struct {
    const unsigned char *p, *end;
    ply_format format;
    int swap; /* binary byte order differs from the host */
    size_t line;
} ply_cursor;

/* --- header --------------------------------------------------------------------------------- */

static int parse_type(const char *s, ply_type *t) {
    for (size_t i = 0; i < sizeof TYPES / sizeof *TYPES; i++) {
        if (strcmp(s, TYPES[i].name) == 0 || strcmp(s, TYPES[i].alias) == 0) {
            *t = (ply_type)i;
            return 1;
        }
    }
    return 0;
}

static int copy_name(char dst[PLY_NAME_MAX], const char *src) {
    size_t n = strlen(src);
    if (n == 0 || n >= PLY_NAME_MAX) return 0;
    memcpy(dst, src, n + 1);
    return 1;
}

static int parse_count(const char *s, size_t *count) {
    if (*s == '\0') return 0;
    size_t value = 0;
    for (; *s; s++) {
        if (*s < '0' || *s > '9') return 0;
        size_t digit = (size_t)(*s - '0');
        if (value > (SIZE_MAX - digit) / 10) return 0;
        value = value * 10 + digit;
    }
    *count = value;
    return 1;
}

/* Splits `line` in place on spaces and tabs; returns the number of tokens, at most `max`. */
static size_t tokenize(char *line, char **tokens, size_t max) {
    size_t n = 0;
    char *s = line;
    while (*s) {
        while (*s == ' ' || *s == '\t') *s++ = '\0';
        if (!*s) break;
        if (n == max) return max + 1;
        tokens[n++] = s;
        while (*s && *s != ' ' && *s != '\t') s++;
    }
    return n;
}

static mesh_status parse_header(const char *data, size_t size, ply_header *h, size_t *line_out) {
    int format_seen = 0;
    size_t line = 0;
    const char *p = data, *end = data + size;
    h->element_count = 0;

    while (p < end) {
        const char *eol = memchr(p, '\n', (size_t)(end - p));
        if (!eol) break;
        size_t n = (size_t)(eol - p);
        if (n > 0 && p[n - 1] == '\r') n--;
        line++;
        *line_out = line;
        const char *next = eol + 1;
        if (memchr(p, '\0', n)) return MESH_ERR_SYNTAX; /* would silently cut the line short */

        if (line == 1) {
            if (n != 3 || memcmp(p, "ply", 3) != 0) return MESH_ERR_SYNTAX;
            p = next;
            continue;
        }
        if ((n >= 7 && memcmp(p, "comment", 7) == 0 && (n == 7 || p[7] == ' ' || p[7] == '\t')) ||
            (n >= 8 && memcmp(p, "obj_info", 8) == 0 && (n == 8 || p[8] == ' ' || p[8] == '\t'))) {
            p = next;
            continue;
        }
        if (n >= PLY_LINE_MAX) return MESH_ERR_SYNTAX;
        char buf[PLY_LINE_MAX];
        memcpy(buf, p, n);
        buf[n] = '\0';
        char *tok[6];
        size_t count = tokenize(buf, tok, 5);
        p = next;
        if (count == 0) return MESH_ERR_SYNTAX;

        if (strcmp(tok[0], "format") == 0) {
            if (format_seen || count != 3 || strcmp(tok[2], "1.0") != 0) return MESH_ERR_SYNTAX;
            if (strcmp(tok[1], "ascii") == 0)
                h->format = PLY_ASCII;
            else if (strcmp(tok[1], "binary_little_endian") == 0)
                h->format = PLY_BINARY_LE;
            else if (strcmp(tok[1], "binary_big_endian") == 0)
                h->format = PLY_BINARY_BE;
            else
                return MESH_ERR_SYNTAX;
            format_seen = 1;
        } else if (!format_seen) {
            return MESH_ERR_SYNTAX;
        } else if (strcmp(tok[0], "element") == 0) {
            if (count != 3 || h->element_count == PLY_MAX_ELEMENTS) return MESH_ERR_SYNTAX;
            ply_element *e = &h->elements[h->element_count];
            if (!copy_name(e->name, tok[1]) || !parse_count(tok[2], &e->count)) return MESH_ERR_SYNTAX;
            e->prop_count = 0;
            h->element_count++;
        } else if (strcmp(tok[0], "property") == 0) {
            if (h->element_count == 0) return MESH_ERR_SYNTAX;
            ply_element *e = &h->elements[h->element_count - 1];
            if (e->prop_count == PLY_MAX_PROPERTIES) return MESH_ERR_SYNTAX;
            ply_property *prop = &e->props[e->prop_count];
            if (count == 3) {
                prop->is_list = 0;
                if (!parse_type(tok[1], &prop->type) || !copy_name(prop->name, tok[2])) return MESH_ERR_SYNTAX;
            } else if (count == 5 && strcmp(tok[1], "list") == 0) {
                prop->is_list = 1;
                if (!parse_type(tok[2], &prop->count_type) || !is_integer_type(prop->count_type) ||
                    !parse_type(tok[3], &prop->type) || !copy_name(prop->name, tok[4]))
                    return MESH_ERR_SYNTAX;
            } else {
                return MESH_ERR_SYNTAX;
            }
            e->prop_count++;
        } else if (strcmp(tok[0], "end_header") == 0 && count == 1) {
            h->lines = line;
            h->body = (size_t)(next - data);
            return MESH_OK;
        } else {
            return MESH_ERR_SYNTAX;
        }
    }
    return MESH_ERR_SYNTAX; /* no end_header */
}

/* --- body ----------------------------------------------------------------------------------- */

static int is_ascii_space(unsigned char c) { return c == ' ' || c == '\t' || c == '\r' || c == '\n'; }

static mesh_status read_value(ply_cursor *c, ply_type t, double *out) {
    if (c->format == PLY_ASCII) {
        while (c->p < c->end && is_ascii_space(*c->p)) {
            if (*c->p == '\n') c->line++;
            c->p++;
        }
        const unsigned char *start = c->p;
        while (c->p < c->end && !is_ascii_space(*c->p)) c->p++;
        size_t n = (size_t)(c->p - start);
        if (n == 0 || n >= PLY_TOKEN_MAX) return MESH_ERR_SYNTAX;
        char tok[PLY_TOKEN_MAX];
        memcpy(tok, start, n);
        tok[n] = '\0';
        char *end;
        double v = strtod(tok, &end);
        if (end != tok + n || !isfinite(v)) return MESH_ERR_SYNTAX;
        if (is_integer_type(t) && (v != floor(v) || v < TYPES[t].min || v > TYPES[t].max)) return MESH_ERR_SYNTAX;
        *out = v;
        return MESH_OK;
    }

    size_t size = TYPES[t].size;
    if ((size_t)(c->end - c->p) < size) return MESH_ERR_SYNTAX;
    unsigned char b[8];
    for (size_t i = 0; i < size; i++) b[i] = c->swap ? c->p[size - 1 - i] : c->p[i];
    c->p += size;
    switch (t) {
    case PLY_INT8: { int8_t v; memcpy(&v, b, 1); *out = v; break; }
    case PLY_UINT8: { uint8_t v; memcpy(&v, b, 1); *out = v; break; }
    case PLY_INT16: { int16_t v; memcpy(&v, b, 2); *out = v; break; }
    case PLY_UINT16: { uint16_t v; memcpy(&v, b, 2); *out = v; break; }
    case PLY_INT32: { int32_t v; memcpy(&v, b, 4); *out = v; break; }
    case PLY_UINT32: { uint32_t v; memcpy(&v, b, 4); *out = v; break; }
    case PLY_FLOAT32: { float v; memcpy(&v, b, 4); *out = v; break; }
    case PLY_FLOAT64: { double v; memcpy(&v, b, 8); *out = v; break; }
    }
    return isfinite(*out) ? MESH_OK : MESH_ERR_SYNTAX;
}

/* Smallest number of bytes one item of `e` can take: lets us reject counts the data cannot hold. */
static size_t min_item_size(const ply_element *e, ply_format format) {
    size_t n = 0;
    for (size_t i = 0; i < e->prop_count; i++) {
        const ply_property *p = &e->props[i];
        if (format == PLY_ASCII)
            n += 1; /* one digit; separators are not required for the last value */
        else
            n += TYPES[p->is_list ? p->count_type : p->type].size;
    }
    return n;
}

static int find_property(const ply_element *e, const char *name, int list) {
    for (size_t i = 0; i < e->prop_count; i++)
        if (e->props[i].is_list == list && strcmp(e->props[i].name, name) == 0) return (int)i;
    return -1;
}

static int host_is_little_endian(void) {
    const uint16_t one = 1;
    unsigned char first;
    memcpy(&first, &one, 1);
    return first == 1;
}

mesh_status mesh_read_ply(const char *data, size_t size, mesh *out, size_t *error_line) {
    mesh_free(out);
    if (error_line) *error_line = 0;

    ply_header *h = malloc(sizeof *h);
    if (!h) return MESH_ERR_MEMORY;
    size_t line = 0;
    mesh_status st = parse_header(data, size, h, &line);
    if (st != MESH_OK) {
        free(h);
        if (error_line) *error_line = line;
        return st;
    }

    ply_cursor c;
    c.p = (const unsigned char *)data + h->body;
    c.end = (const unsigned char *)data + size;
    c.format = h->format;
    c.swap = h->format != PLY_ASCII && (h->format == PLY_BINARY_LE) != host_is_little_endian();
    c.line = h->lines + 1;

    /* Locate x, y, z and the face list before reading anything. Only the first `vertex` element
     * holds the vertices: the allocation and the reading loop below must agree on it. */
    size_t vertex_count = 0;
    int vertex_seen = 0;
    int ix = -1, iy = -1, iz = -1;
    for (size_t i = 0; i < h->element_count && st == MESH_OK; i++) {
        const ply_element *e = &h->elements[i];
        size_t min = min_item_size(e, h->format);
        /* Items without properties take no bytes: any count would loop for nothing. */
        if (e->count > 0 && (min == 0 || e->count > (size_t)(c.end - c.p) / min)) st = MESH_ERR_SYNTAX;
        if (!vertex_seen && strcmp(e->name, "vertex") == 0) {
            vertex_seen = 1;
            vertex_count = e->count;
            ix = find_property(e, "x", 0);
            iy = find_property(e, "y", 0);
            iz = find_property(e, "z", 0);
            if (ix < 0 || iy < 0 || iz < 0) st = MESH_ERR_SYNTAX;
            if (vertex_count > UINT32_MAX) st = MESH_ERR_MEMORY;
        } else if (strcmp(e->name, "face") == 0) {
            int list = find_property(e, "vertex_indices", 1);
            if (list < 0) list = find_property(e, "vertex_index", 1);
            if (list < 0 || !is_integer_type(e->props[list].type)) st = MESH_ERR_SYNTAX;
        }
    }

    mesh m;
    mesh_init(&m);
    size_t triangle_capacity = 0;
    if (st == MESH_OK && vertex_count > 0) {
        m.vertices = malloc(vertex_count * sizeof *m.vertices);
        if (!m.vertices) st = MESH_ERR_MEMORY;
    }

    int vertices_done = 0;
    for (size_t i = 0; i < h->element_count && st == MESH_OK; i++) {
        const ply_element *e = &h->elements[i];
        int is_vertex = !vertices_done && strcmp(e->name, "vertex") == 0;
        int is_face = strcmp(e->name, "face") == 0;
        for (size_t item = 0; item < e->count && st == MESH_OK; item++) {
            double xyz[3] = {0, 0, 0};
            for (size_t k = 0; k < e->prop_count && st == MESH_OK; k++) {
                const ply_property *p = &e->props[k];
                double v;
                if (!p->is_list) {
                    st = read_value(&c, p->type, &v);
                    if (is_vertex && (int)k == ix) xyz[0] = v;
                    if (is_vertex && (int)k == iy) xyz[1] = v;
                    if (is_vertex && (int)k == iz) xyz[2] = v;
                    continue;
                }
                double count;
                st = read_value(&c, p->count_type, &count);
                if (st != MESH_OK) break;
                if (count < 0) { /* signed count types are allowed by the format */
                    st = MESH_ERR_SYNTAX;
                    break;
                }
                int is_indices = is_face && (strcmp(p->name, "vertex_indices") == 0 || strcmp(p->name, "vertex_index") == 0);
                if (is_indices && count < 3) {
                    st = MESH_ERR_SYNTAX;
                    break;
                }
                uint32_t first = 0, previous = 0;
                size_t items = (size_t)count; /* integral and within the count type's range */
                for (size_t j = 0; j < items && st == MESH_OK; j++) {
                    st = read_value(&c, p->type, &v);
                    if (st != MESH_OK || !is_indices) continue;
                    if (v < 0 || v >= (double)vertex_count) {
                        st = MESH_ERR_INDEX;
                        break;
                    }
                    uint32_t index = (uint32_t)v;
                    if (j == 0)
                        first = index;
                    else if (j >= 2)
                        st = mesh__add_triangle(&m, &triangle_capacity, first, previous, index);
                    previous = index;
                }
                if (st == MESH_OK && is_indices) m.polygon_count++;
            }
            if (st == MESH_OK && is_vertex) m.vertices[m.vertex_count++] = (mesh_vec3){xyz[0], xyz[1], xyz[2]};
        }
        if (is_vertex) vertices_done = 1;
    }

    if (st != MESH_OK) {
        mesh_free(&m);
        if (error_line) *error_line = h->format == PLY_ASCII ? c.line : 0;
        free(h);
        return st;
    }
    free(h);
    *out = m;
    return MESH_OK;
}
