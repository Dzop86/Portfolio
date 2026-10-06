/* Unit tests for the OBJ reader. Meshes are synthetic, written by hand. */
#include "mesh/mesh.h"
#include "unity.h"

static mesh m;
static size_t line;

void setUp(void) {
    mesh_init(&m);
    line = 0;
}

void tearDown(void) { mesh_free(&m); }

static const char *TETRAHEDRON =
    "# regular tetrahedron\n"
    "v 1 1 1\n"
    "v -1 -1 1\n"
    "v -1 1 -1\n"
    "v 1 -1 -1\n"
    "f 1 2 3\n"
    "f 1 4 2\n"
    "f 1 3 4\n"
    "f 2 4 3\n";

static void test_reads_vertices_and_triangles(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string(TETRAHEDRON, &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(4, m.polygon_count);
    TEST_ASSERT_EQUAL_size_t(4, m.triangle_count);
    TEST_ASSERT_EQUAL_DOUBLE(-1.0, m.vertices[1].x);
    TEST_ASSERT_EQUAL_DOUBLE(-1.0, m.vertices[3].z);
    TEST_ASSERT_EQUAL_UINT32(0, m.triangles[1][0]);
    TEST_ASSERT_EQUAL_UINT32(3, m.triangles[1][1]);
    TEST_ASSERT_EQUAL_UINT32(1, m.triangles[1][2]);
}

static void test_fan_triangulates_polygons(void) {
    const char *quad_and_pentagon =
        "v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nv -1 0.5 0\n"
        "f 1 2 3 4\n"
        "f 1 2 3 4 5\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string(quad_and_pentagon, &m, &line));
    TEST_ASSERT_EQUAL_size_t(2, m.polygon_count);
    TEST_ASSERT_EQUAL_size_t(2 + 3, m.triangle_count);
    TEST_ASSERT_EQUAL_UINT32(0, m.triangles[1][0]);
    TEST_ASSERT_EQUAL_UINT32(2, m.triangles[1][1]);
    TEST_ASSERT_EQUAL_UINT32(3, m.triangles[1][2]);
}

static void test_accepts_texture_and_normal_references(void) {
    const char *text =
        "v 0 0 0\nv 1 0 0\nv 0 1 0\n"
        "vt 0 0\nvt 1 0\nvt 0 1\nvn 0 0 1\n"
        "f 1/1/1 2/2/1 3/3/1\n"
        "f 1//1 2//1 3//1\n"
        "f 1/1 2/2 3/3\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string(text, &m, &line));
    TEST_ASSERT_EQUAL_size_t(3, m.triangle_count);
    TEST_ASSERT_EQUAL_UINT32(2, m.triangles[2][2]);
}

static void test_resolves_negative_indices(void) {
    const char *text = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf -3 -2 -1\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string(text, &m, &line));
    TEST_ASSERT_EQUAL_UINT32(0, m.triangles[0][0]);
    TEST_ASSERT_EQUAL_UINT32(2, m.triangles[0][2]);
}

static void test_ignores_comments_groups_materials_and_crlf(void) {
    const char *text =
        "mtllib scene.mtl\r\n"
        "o triangle\r\n"
        "g part\r\n"
        "  # indented comment\r\n"
        "\r\n"
        "v 0 0 0\r\nv 1 0 0\r\nv 0 1 0\r\n"
        "usemtl red\r\n"
        "s off\r\n"
        "f 1 2 3\r\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string(text, &m, &line));
    TEST_ASSERT_EQUAL_size_t(3, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
}

static void test_reports_an_index_out_of_range_with_its_line(void) {
    const char *text = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 4\n";
    TEST_ASSERT_EQUAL(MESH_ERR_INDEX, mesh_read_obj_string(text, &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, line);
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
}

static void test_rejects_index_zero_and_too_negative_indices(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_INDEX, mesh_read_obj_string("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 0 1 2\n", &m, &line));
    TEST_ASSERT_EQUAL(MESH_ERR_INDEX, mesh_read_obj_string("v 0 0 0\nv 1 0 0\nv 0 1 0\nf -4 1 2\n", &m, &line));
}

static void test_reports_syntax_errors_with_their_line(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 0 0\nv 1 zero 0\n", &m, &line));
    TEST_ASSERT_EQUAL_size_t(2, line);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 0\n", &m, &line));
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 0 0\nv 1 0 0\nf 1 2\n", &m, &line));
    TEST_ASSERT_EQUAL_size_t(3, line);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 x\n", &m, &line));
}

static void test_does_not_read_numbers_from_the_next_line(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 1 2\n3\n", &m, &line));
    TEST_ASSERT_EQUAL_size_t(1, line);
}

static void test_rejects_infinite_coordinates_but_accepts_tiny_ones(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v inf 0 0\n", &m, &line));
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 nan 0\n", &m, &line));
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("v 0 0 1e999\n", &m, &line));
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string("v 1e-320 0 0\n", &m, &line));
}

static void test_accepts_an_optional_w_and_a_missing_final_newline(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string("v 0 0 0 1\nv 1 0 0\nv 0 1 0\nf 1 2 3", &m, &line));
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
}

static void test_empty_input_gives_an_empty_mesh(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_string("", &m, &line));
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
    TEST_ASSERT_NULL(m.vertices);
}

static void test_error_line_pointer_is_optional(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_obj_string("bogus\n", &m, NULL));
}

static void test_reads_a_file_and_reports_missing_files(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_obj_file(MESH_TEST_DATA "/cube.obj", &m, &line));
    TEST_ASSERT_EQUAL_size_t(8, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(6, m.polygon_count);
    TEST_ASSERT_EQUAL_size_t(12, m.triangle_count);
    mesh_free(&m);
    TEST_ASSERT_EQUAL(MESH_ERR_IO, mesh_read_obj_file(MESH_TEST_DATA "/missing.obj", &m, &line));
}

static void test_status_strings_are_readable(void) {
    TEST_ASSERT_EQUAL_STRING("ok", mesh_status_string(MESH_OK));
    TEST_ASSERT_EQUAL_STRING("index out of range", mesh_status_string(MESH_ERR_INDEX));
    TEST_ASSERT_EQUAL_STRING("unknown error", mesh_status_string((mesh_status)99));
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_reads_vertices_and_triangles);
    RUN_TEST(test_fan_triangulates_polygons);
    RUN_TEST(test_accepts_texture_and_normal_references);
    RUN_TEST(test_resolves_negative_indices);
    RUN_TEST(test_ignores_comments_groups_materials_and_crlf);
    RUN_TEST(test_reports_an_index_out_of_range_with_its_line);
    RUN_TEST(test_rejects_index_zero_and_too_negative_indices);
    RUN_TEST(test_reports_syntax_errors_with_their_line);
    RUN_TEST(test_does_not_read_numbers_from_the_next_line);
    RUN_TEST(test_rejects_infinite_coordinates_but_accepts_tiny_ones);
    RUN_TEST(test_accepts_an_optional_w_and_a_missing_final_newline);
    RUN_TEST(test_empty_input_gives_an_empty_mesh);
    RUN_TEST(test_error_line_pointer_is_optional);
    RUN_TEST(test_reads_a_file_and_reports_missing_files);
    RUN_TEST(test_status_strings_are_readable);
    return UNITY_END();
}
