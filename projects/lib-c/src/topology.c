/* Edges are encoded as 64-bit keys (smaller index in the high half) and sorted: distinct keys are
 * the edges, keys seen once are boundary edges. */
#include "internal.h"

#include <stdlib.h>

static int compare_keys(const void *a, const void *b) {
    uint64_t x = *(const uint64_t *)a, y = *(const uint64_t *)b;
    return (x > y) - (x < y);
}

static uint64_t edge_key(uint32_t a, uint32_t b) {
    return a < b ? ((uint64_t)a << 32) | b : ((uint64_t)b << 32) | a;
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
    for (size_t i = 0; i < n; i++) {
        const uint32_t *tri = m->triangles[i];
        keys[3 * i] = edge_key(tri[0], tri[1]);
        keys[3 * i + 1] = edge_key(tri[1], tri[2]);
        keys[3 * i + 2] = edge_key(tri[2], tri[0]);
    }
    if (n) qsort(keys, 3 * n, sizeof *keys, compare_keys);
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
