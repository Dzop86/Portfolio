/* Edges are encoded as 64-bit keys (smaller index times the vertex count, plus the larger one) and sorted by radix:
 * distinct keys are the edges, keys seen once are boundary edges. Compact keys keep the radix passes few: below
 * 2^40, four 11-bit passes (D51). */
#include "internal.h"

#include <stdlib.h>
#include <string.h>

#define DIGIT_BITS 11
#define DIGITS (1u << DIGIT_BITS)

mesh_status mesh__sort_keys(uint64_t *keys, size_t n) {
    if (n < 2) return MESH_OK;
    uint64_t all = 0;
    for (size_t i = 0; i < n; i++) all |= keys[i];
    unsigned passes = 0;
    while (passes * DIGIT_BITS < 64 && (all >> (passes * DIGIT_BITS)) != 0) passes++;
    if (n > SIZE_MAX / sizeof *keys) return MESH_ERR_MEMORY;
    /* The histograms of every pass in one reading of the keys; on the heap (a WebAssembly stack is small). */
    size_t *count = calloc((size_t)passes * DIGITS, sizeof *count);
    uint64_t *buffer = malloc(n * sizeof *buffer);
    if (!count || !buffer) {
        free(count);
        free(buffer);
        return MESH_ERR_MEMORY;
    }
    for (size_t i = 0; i < n; i++) {
        for (unsigned p = 0; p < passes; p++) count[p * DIGITS + (size_t)((keys[i] >> (p * DIGIT_BITS)) & (DIGITS - 1))]++;
    }
    uint64_t *src = keys, *dst = buffer;
    for (unsigned p = 0; p < passes; p++) {
        size_t *c = count + (size_t)p * DIGITS;
        const unsigned shift = p * DIGIT_BITS;
        /* Every key has the same digit here: the pass would copy them in the same order. */
        if (c[(size_t)((src[0] >> shift) & (DIGITS - 1))] == n) continue;
        size_t sum = 0;
        for (size_t d = 0; d < DIGITS; d++) {
            const size_t k = c[d];
            c[d] = sum;
            sum += k;
        }
        /* Stable: equal digits keep the order of the previous pass. */
        for (size_t i = 0; i < n; i++) dst[c[(size_t)((src[i] >> shift) & (DIGITS - 1))]++] = src[i];
        uint64_t *swap = src;
        src = dst;
        dst = swap;
    }
    if (src != keys) memcpy(keys, src, n * sizeof *keys);
    free(buffer);
    free(count);
    return MESH_OK;
}

static uint64_t edge_key(uint32_t a, uint32_t b, uint64_t base) {
    return a < b ? (uint64_t)a * base + b : (uint64_t)b * base + a;
}

mesh_status mesh_compute_topology(const mesh *m, mesh_topology *out) {
    mesh_topology t = {0, 0, 0, {0, 0, 0}, {0, 0, 0}};
    if (m->vertex_count > 0) t.min = t.max = m->vertices[0];
    for (size_t i = 1; i < m->vertex_count; i++) {
        const mesh_vec3 *v = &m->vertices[i];
        if (v->x < t.min.x) t.min.x = v->x;
        if (v->y < t.min.y) t.min.y = v->y;
        if (v->z < t.min.z) t.min.z = v->z;
        if (v->x > t.max.x) t.max.x = v->x;
        if (v->y > t.max.y) t.max.y = v->y;
        if (v->z > t.max.z) t.max.z = v->z;
    }

    size_t n = m->triangle_count;
    if (n > SIZE_MAX / 3 / sizeof(uint64_t)) return MESH_ERR_MEMORY;
    uint64_t *keys = n ? malloc(3 * n * sizeof *keys) : NULL;
    if (n && !keys) return MESH_ERR_MEMORY;
    /* Indices are 32-bit, so a base of at most 2^32 keeps every key below 2^64. Compared as 64 bits: a 32-bit size_t
     * (WebAssembly) can never exceed 2^32, and Clang refuses the comparison as always false. */
    const uint64_t vertices = (uint64_t)m->vertex_count;
    const uint64_t base = vertices > ((uint64_t)1 << 32) ? ((uint64_t)1 << 32) : vertices;
    for (size_t i = 0; i < n; i++) {
        const uint32_t *tri = m->triangles[i];
        keys[3 * i] = edge_key(tri[0], tri[1], base);
        keys[3 * i + 1] = edge_key(tri[1], tri[2], base);
        keys[3 * i + 2] = edge_key(tri[2], tri[0], base);
    }
    if (mesh__sort_keys(keys, 3 * n) != MESH_OK) {
        free(keys);
        return MESH_ERR_MEMORY;
    }
    for (size_t i = 0; i < 3 * n;) {
        size_t j = i + 1;
        while (j < 3 * n && keys[j] == keys[i]) j++;
        t.edge_count++;
        if (j - i == 1) t.boundary_edge_count++;
        i = j;
    }
    free(keys);

    t.euler_characteristic = (int64_t)m->vertex_count - (int64_t)t.edge_count + (int64_t)n;
    *out = t;
    return MESH_OK;
}
