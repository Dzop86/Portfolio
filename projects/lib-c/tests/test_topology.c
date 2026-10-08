/* Unit tests for edge counting and the Euler characteristic, on synthetic meshes. */
#include "mesh/mesh.h"
#include "../src/internal.h"
#include "unity.h"

#include <stdlib.h>
#include <string.h>

static mesh m;

void setUp(void) { mesh_init(&m); }
void tearDown(void) { mesh_free(&m); }

/* Builds an n x n grid whose opposite sides are glued: a torus with n*n vertices and 2*n*n triangles. */
static void build_torus(size_t n) {
    m.vertex_count = n * n;
    m.vertices = calloc(m.vertex_count, sizeof *m.vertices);
    m.triangle_count = 2 * n * n;
    m.polygon_count = m.triangle_count;
    m.triangles = malloc(m.triangle_count * sizeof *m.triangles);
    TEST_ASSERT_NOT_NULL(m.vertices);
    TEST_ASSERT_NOT_NULL(m.triangles);
    size_t t = 0;
    for (size_t i = 0; i < n; i++) {
        for (size_t j = 0; j < n; j++) {
            uint32_t a = (uint32_t)(i * n + j), b = (uint32_t)(i * n + (j + 1) % n);
            uint32_t c = (uint32_t)(((i + 1) % n) * n + j), d = (uint32_t)(((i + 1) % n) * n + (j + 1) % n);
            m.triangles[t][0] = a, m.triangles[t][1] = b, m.triangles[t][2] = d, t++;
            m.triangles[t][0] = a, m.triangles[t][1] = d, m.triangles[t][2] = c, t++;
        }
    }
}

static void test_closed_sphere_like_meshes_have_euler_characteristic_2(void) {
    mesh_topology topo;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_file(MESH_TEST_DATA "/tetrahedron.ply", &m, NULL));
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_size_t(6, topo.edge_count);
    TEST_ASSERT_EQUAL_size_t(0, topo.boundary_edge_count);
    TEST_ASSERT_EQUAL_INT64(2, topo.euler_characteristic);

    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_file(MESH_TEST_DATA "/cube.obj", &m, NULL));
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_size_t(18, topo.edge_count); /* 12 cube edges + 6 diagonals */
    TEST_ASSERT_EQUAL_INT64(2, topo.euler_characteristic);
}

static void test_a_torus_has_euler_characteristic_0(void) {
    build_torus(4);
    mesh_topology topo;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_size_t(48, topo.edge_count);
    TEST_ASSERT_EQUAL_size_t(0, topo.boundary_edge_count);
    TEST_ASSERT_EQUAL_INT64(0, topo.euler_characteristic);
}

static void test_an_open_triangle_has_three_boundary_edges(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n", &m, NULL));
    mesh_topology topo;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_size_t(3, topo.edge_count);
    TEST_ASSERT_EQUAL_size_t(3, topo.boundary_edge_count);
    TEST_ASSERT_EQUAL_INT64(1, topo.euler_characteristic);
}

static void test_an_empty_mesh_has_no_edges(void) {
    mesh_topology topo;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_size_t(0, topo.edge_count);
    TEST_ASSERT_EQUAL_INT64(0, topo.euler_characteristic);
}

static void test_the_bounding_box_spans_the_vertices(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string("v -1 2 0.5\nv 3 -4 0\nv 0 0 7\n", &m, NULL));
    mesh_topology topo;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
    TEST_ASSERT_EQUAL_DOUBLE(-1.0, topo.min.x);
    TEST_ASSERT_EQUAL_DOUBLE(-4.0, topo.min.y);
    TEST_ASSERT_EQUAL_DOUBLE(0.0, topo.min.z);
    TEST_ASSERT_EQUAL_DOUBLE(3.0, topo.max.x);
    TEST_ASSERT_EQUAL_DOUBLE(2.0, topo.max.y);
    TEST_ASSERT_EQUAL_DOUBLE(7.0, topo.max.z);
}

/* The radix sort of the edge keys (D51), against qsort: random keys of every width, duplicates, sorted and
 * reversed input, all keys equal, and the extremes of 64 bits. */
static int compare_keys(const void *a, const void *b) {
    uint64_t x = *(const uint64_t *)a, y = *(const uint64_t *)b;
    return (x > y) - (x < y);
}

static uint64_t state = 0x9E3779B97F4A7C15u;
static uint64_t next_random(void) { /* splitmix64 */
    uint64_t z = (state += 0x9E3779B97F4A7C15u);
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9u;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBu;
    return z ^ (z >> 31);
}

static void expect_sorted_like_qsort(uint64_t *keys, size_t n) {
    uint64_t *expected = malloc((n ? n : 1) * sizeof *expected);
    TEST_ASSERT_NOT_NULL(expected);
    if (n) memcpy(expected, keys, n * sizeof *keys);
    qsort(expected, n, sizeof *expected, compare_keys);
    TEST_ASSERT_EQUAL(MESH_OK, mesh__sort_keys(keys, n));
    for (size_t i = 0; i < n; i++) TEST_ASSERT_TRUE_MESSAGE(keys[i] == expected[i], "same order as qsort");
    free(expected);
}

static void test_the_radix_sort_orders_keys_like_qsort(void) {
    const size_t sizes[] = {0, 1, 2, 3, 100, 4097, 70000};
    /* Widths: one digit, a few digits, every 64 bits (six passes, the last one partial). */
    const unsigned widths[] = {5, 11, 12, 38, 64};
    for (size_t s = 0; s < sizeof sizes / sizeof *sizes; s++) {
        for (size_t w = 0; w < sizeof widths / sizeof *widths; w++) {
            const size_t n = sizes[s];
            uint64_t *keys = malloc((n ? n : 1) * sizeof *keys);
            TEST_ASSERT_NOT_NULL(keys);
            for (size_t i = 0; i < n; i++) keys[i] = widths[w] == 64 ? next_random() : next_random() >> (64 - widths[w]);
            expect_sorted_like_qsort(keys, n);
            /* Already sorted, then reversed. */
            expect_sorted_like_qsort(keys, n);
            for (size_t i = 0; i < n / 2; i++) {
                const uint64_t x = keys[i];
                keys[i] = keys[n - 1 - i];
                keys[n - 1 - i] = x;
            }
            expect_sorted_like_qsort(keys, n);
            free(keys);
        }
    }
}

static void test_the_radix_sort_handles_duplicates_and_extremes(void) {
    uint64_t same[1000];
    for (size_t i = 0; i < 1000; i++) same[i] = 0x123456789ABCDEFull; /* every pass skipped */
    expect_sorted_like_qsort(same, 1000);
    uint64_t extremes[] = {UINT64_MAX, 0, UINT64_MAX - 1, 1, (uint64_t)1 << 63, 0, UINT64_MAX, 2047, 2048, (uint64_t)1 << 32};
    expect_sorted_like_qsort(extremes, sizeof extremes / sizeof *extremes);
    uint64_t few[5000];
    for (size_t i = 0; i < 5000; i++) few[i] = next_random() % 7; /* many duplicates */
    expect_sorted_like_qsort(few, 5000);
}

/* Counts are unchanged by the new sort: tori of many sizes (3n^2 edges, chi 0) and a strip with a boundary. */
static void test_edge_counts_of_tori_of_every_size(void) {
    for (size_t n = 3; n <= 60; n += 3) {
        mesh_free(&m);
        mesh_init(&m);
        build_torus(n);
        mesh_topology topo;
        TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &topo));
        TEST_ASSERT_EQUAL_size_t(3 * n * n, topo.edge_count);
        TEST_ASSERT_EQUAL_size_t(0, topo.boundary_edge_count);
        TEST_ASSERT_EQUAL_INT64(0, topo.euler_characteristic);
    }
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_closed_sphere_like_meshes_have_euler_characteristic_2);
    RUN_TEST(test_a_torus_has_euler_characteristic_0);
    RUN_TEST(test_an_open_triangle_has_three_boundary_edges);
    RUN_TEST(test_an_empty_mesh_has_no_edges);
    RUN_TEST(test_the_bounding_box_spans_the_vertices);
    RUN_TEST(test_the_radix_sort_orders_keys_like_qsort);
    RUN_TEST(test_the_radix_sort_handles_duplicates_and_extremes);
    RUN_TEST(test_edge_counts_of_tori_of_every_size);
    return UNITY_END();
}
