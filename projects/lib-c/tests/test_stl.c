/* Unit tests for the STL reader (binary and ASCII), vertex welding and format detection. */
#include "mesh/mesh.h"
#include "unity.h"

#include <math.h>
#include <string.h>

static mesh m;
static size_t line;

void setUp(void) {
    mesh_init(&m);
    line = 0;
}

void tearDown(void) { mesh_free(&m); }

/* --- binary STL built byte by byte (little-endian, as the format requires) --------------------- */

static unsigned char buf[2048];
static size_t len;

static void put_le(const void *p, size_t n) {
    unsigned char probe[2] = {1, 0};
    uint16_t host;
    memcpy(&host, probe, 2);
    const unsigned char *b = p;
    for (size_t i = 0; i < n; i++) buf[len + i] = host == 1 ? b[i] : b[n - 1 - i];
    len += n;
}

static void put_f32(float v) { put_le(&v, 4); }

/* Header deliberately starts with "solid", as some exporters do: detection must rely on the size. */
static void begin_binary(uint32_t count) {
    len = 0;
    memset(buf, ' ', 80);
    memcpy(buf, "solid exported as binary", 24);
    len = 80;
    put_le(&count, 4);
}

/* Three corners as 9 floats (x, y, z each); a flat pointer avoids qualifier rules on 2D arrays before C23. */
static void put_facet(const float *v) {
    for (int k = 0; k < 3; k++) put_f32(0.0f); /* normal, ignored */
    for (int i = 0; i < 9; i++) put_f32(v[i]);
    const uint16_t attributes = 0;
    put_le(&attributes, 2);
}

static const float TETRA[4][3] = {{1, 1, 1}, {-1, -1, 1}, {-1, 1, -1}, {1, -1, -1}};
static const int TETRA_FACES[4][3] = {{0, 1, 2}, {0, 3, 1}, {0, 2, 3}, {1, 3, 2}};

static void build_binary_tetrahedron(void) {
    begin_binary(4);
    for (int f = 0; f < 4; f++) {
        float v[3][3];
        for (int i = 0; i < 3; i++) memcpy(v[i], TETRA[TETRA_FACES[f][i]], sizeof v[i]);
        put_facet(&v[0][0]);
    }
}

static void test_reads_binary_stl_and_welds_shared_corners(void) {
    build_binary_tetrahedron();
    TEST_ASSERT_EQUAL(MESH_FORMAT_STL, mesh_detect_format((const char *)buf, len));
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_buffer((const char *)buf, len, &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, m.vertex_count); /* 12 corners welded into 4 vertices */
    TEST_ASSERT_EQUAL_size_t(4, m.triangle_count);
    mesh_topology t;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &t));
    TEST_ASSERT_EQUAL_INT64(2, t.euler_characteristic);
    TEST_ASSERT_EQUAL_size_t(0, t.boundary_edge_count);
}

static void test_welding_treats_minus_zero_as_zero(void) {
    begin_binary(2);
    const float a[3][3] = {{0, 0, 0}, {1, 0, 0}, {0, 1, 0}};
    const float b[3][3] = {{-0.0f, -0.0f, 0}, {0, 1, 0}, {-1, 0, 0}};
    put_facet(&a[0][0]);
    put_facet(&b[0][0]);
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_stl((const char *)buf, len, &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, m.vertex_count);
}

static void test_drops_degenerate_facets(void) {
    begin_binary(2);
    const float good[3][3] = {{0, 0, 0}, {1, 0, 0}, {0, 1, 0}};
    const float flat[3][3] = {{0, 0, 0}, {1, 0, 0}, {1, 0, 0}}; /* two identical corners */
    put_facet(&good[0][0]);
    put_facet(&flat[0][0]);
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_stl((const char *)buf, len, &m, &line));
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
    TEST_ASSERT_EQUAL_size_t(1, m.polygon_count);
    TEST_ASSERT_EQUAL_size_t(3, m.vertex_count);
}

static void test_rejects_non_finite_binary_coordinates(void) {
    begin_binary(1);
    const float bad[3][3] = {{0, 0, 0}, {INFINITY, 0, 0}, {0, 1, 0}};
    put_facet(&bad[0][0]);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl((const char *)buf, len, &m, &line));
}

static void test_a_truncated_binary_file_is_not_taken_for_binary(void) {
    build_binary_tetrahedron();
    TEST_ASSERT_EQUAL(MESH_FORMAT_STL, mesh_detect_format((const char *)buf, len - 1)); /* still "solid..." */
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl((const char *)buf, len - 1, &m, &line));
}

static void test_a_count_larger_than_the_data_is_rejected_without_allocating(void) {
    begin_binary(4000000000u);
    memcpy(buf, "binary, no solid prefix  ", 25);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl((const char *)buf, len, &m, &line));
}

/* --- ASCII STL ------------------------------------------------------------------------------ */

static void test_reads_an_ascii_cube_file(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_file(MESH_TEST_DATA "/cube.stl", &m, &line));
    TEST_ASSERT_EQUAL_size_t(8, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(12, m.triangle_count);
    mesh_topology t;
    TEST_ASSERT_EQUAL(MESH_OK, mesh_compute_topology(&m, &t));
    TEST_ASSERT_EQUAL_size_t(18, t.edge_count);
    TEST_ASSERT_EQUAL_INT64(2, t.euler_characteristic);
}

static void test_ascii_accepts_crlf_tabs_and_scientific_notation(void) {
    const char *text = "solid t\r\n\tfacet normal 0 0 1\r\n\t\touter loop\r\n"
                       "\t\t\tvertex 0e0 0 0\r\n\t\t\tvertex 1.0E+0 0 0\r\n\t\t\tvertex 0 1 0\r\n"
                       "\t\tendloop\r\n\tendfacet\r\nendsolid t\r\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_buffer(text, strlen(text), &m, &line));
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
    TEST_ASSERT_EQUAL_DOUBLE(1.0, m.vertices[1].x); /* vertices keep their order in the file */
}

static void test_ascii_errors_come_with_their_line(void) {
    const char *missing_vertex = "solid t\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nendloop\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl(missing_vertex, strlen(missing_vertex), &m, &line));
    TEST_ASSERT_EQUAL_size_t(6, line);
    const char *bad_number = "solid t\nfacet normal 0 0 1\nouter loop\nvertex 0 0 zero\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl(bad_number, strlen(bad_number), &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, line);
    const char *unterminated = "solid t\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_stl(unterminated, strlen(unterminated), &m, &line));
}

static void test_an_empty_solid_is_an_empty_mesh(void) {
    const char *text = "solid nothing\nendsolid nothing\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_stl(text, strlen(text), &m, &line));
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
}

/* --- detection ------------------------------------------------------------------------------ */

static void test_detects_each_format(void) {
    TEST_ASSERT_EQUAL(MESH_FORMAT_PLY, mesh_detect_format("ply\nformat ascii 1.0\n", 20));
    TEST_ASSERT_EQUAL(MESH_FORMAT_STL, mesh_detect_format("solid x\n", 8));
    TEST_ASSERT_EQUAL(MESH_FORMAT_OBJ, mesh_detect_format("v 0 0 0\n", 8));
    TEST_ASSERT_EQUAL(MESH_FORMAT_OBJ, mesh_detect_format("", 0));
    TEST_ASSERT_EQUAL(MESH_FORMAT_OBJ, mesh_detect_format("solidify\n", 9)); /* not the keyword */
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_reads_binary_stl_and_welds_shared_corners);
    RUN_TEST(test_welding_treats_minus_zero_as_zero);
    RUN_TEST(test_drops_degenerate_facets);
    RUN_TEST(test_rejects_non_finite_binary_coordinates);
    RUN_TEST(test_a_truncated_binary_file_is_not_taken_for_binary);
    RUN_TEST(test_a_count_larger_than_the_data_is_rejected_without_allocating);
    RUN_TEST(test_reads_an_ascii_cube_file);
    RUN_TEST(test_ascii_accepts_crlf_tabs_and_scientific_notation);
    RUN_TEST(test_ascii_errors_come_with_their_line);
    RUN_TEST(test_an_empty_solid_is_an_empty_mesh);
    RUN_TEST(test_detects_each_format);
    return UNITY_END();
}
