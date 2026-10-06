/* Unit tests for edge counting and the Euler characteristic, on synthetic meshes. */
#include "mesh/mesh.h"
#include "unity.h"

#include <stdlib.h>

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

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_closed_sphere_like_meshes_have_euler_characteristic_2);
    RUN_TEST(test_a_torus_has_euler_characteristic_0);
    RUN_TEST(test_an_open_triangle_has_three_boundary_edges);
    RUN_TEST(test_an_empty_mesh_has_no_edges);
    RUN_TEST(test_the_bounding_box_spans_the_vertices);
    return UNITY_END();
}
