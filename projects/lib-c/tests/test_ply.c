/* Unit tests for the PLY reader and format detection. Binary meshes are built byte by byte here. */
#include "mesh/mesh.h"
#include "unity.h"

#include <string.h>

static mesh m;
static size_t line;

void setUp(void) {
    mesh_init(&m);
    line = 0;
}

void tearDown(void) { mesh_free(&m); }

static mesh_status read_text(const char *text) { return mesh_read_ply(text, strlen(text), &m, &line); }

/* --- byte buffer helpers -------------------------------------------------------------------- */

static unsigned char buf[1024];
static size_t len;

static void put_text(const char *s) {
    memcpy(buf + len, s, strlen(s));
    len += strlen(s);
}

static void put_bytes(const void *p, size_t n, int big_endian) {
    const unsigned char *b = p;
    unsigned char probe[2] = {1, 0};
    uint16_t host;
    memcpy(&host, probe, 2);
    int host_little = host == 1;
    for (size_t i = 0; i < n; i++) buf[len + i] = (host_little != !big_endian) ? b[n - 1 - i] : b[i];
    len += n;
}

static void put_f32(float v, int be) { put_bytes(&v, 4, be); }
static void put_u8(uint8_t v) { put_bytes(&v, 1, 0); }
static void put_i32(int32_t v, int be) { put_bytes(&v, 4, be); }
static void put_u16(uint16_t v, int be) { put_bytes(&v, 2, be); }

/* Binary triangle: 3 vertices (float x y z + uchar red), one edge element to skip, one face. */
static void build_binary_triangle(int be) {
    len = 0;
    put_text("ply\n");
    put_text(be ? "format binary_big_endian 1.0\n" : "format binary_little_endian 1.0\n");
    put_text("comment synthetic triangle\n"
             "element vertex 3\n"
             "property float x\nproperty float y\nproperty float z\nproperty uchar red\n"
             "element edge 1\n"
             "property list uchar ushort ends\n"
             "element face 1\n"
             "property list uchar int vertex_indices\n"
             "end_header\n");
    const float xyz[3][3] = {{0, 0, 0}, {1.5f, 0, 0}, {0, -2, 0.25f}};
    for (int i = 0; i < 3; i++) {
        for (int k = 0; k < 3; k++) put_f32(xyz[i][k], be);
        put_u8(200);
    }
    put_u8(2);
    put_u16(0, be);
    put_u16(1, be);
    put_u8(3);
    put_i32(0, be);
    put_i32(1, be);
    put_i32(2, be);
}

/* --- ASCII ---------------------------------------------------------------------------------- */

static const char *SQUARE_PYRAMID =
    "ply\r\n"
    "format ascii 1.0\r\n"
    "comment square pyramid, written by hand\r\n"
    "obj_info synthetic\r\n"
    "element vertex 5\r\n"
    "property double x\r\nproperty double y\r\nproperty double z\r\n"
    "property float nx\r\n"
    "element face 5\r\n"
    "property list uchar uint vertex_index\r\n"
    "end_header\r\n"
    "0 0 0 0\r\n1 0 0 0\r\n1 1 0 0\r\n0 1 0 0\r\n0.5 0.5 1 0\r\n"
    "4 0 3 2 1\r\n"
    "3 0 1 4\r\n3 1 2 4\r\n3 2 3 4\r\n3 3 0 4\r\n";

static void test_reads_ascii_with_extra_properties_and_crlf(void) {
    TEST_ASSERT_EQUAL(MESH_OK, read_text(SQUARE_PYRAMID));
    TEST_ASSERT_EQUAL_size_t(5, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(5, m.polygon_count);
    TEST_ASSERT_EQUAL_size_t(6, m.triangle_count);
    TEST_ASSERT_EQUAL_DOUBLE(0.5, m.vertices[4].y);
    TEST_ASSERT_EQUAL_DOUBLE(1.0, m.vertices[4].z);
    TEST_ASSERT_EQUAL_UINT32(0, m.triangles[1][0]);
    TEST_ASSERT_EQUAL_UINT32(2, m.triangles[1][1]);
    TEST_ASSERT_EQUAL_UINT32(1, m.triangles[1][2]);
}

static void test_reports_an_ascii_index_out_of_range_with_its_line(void) {
    const char *text = "ply\nformat ascii 1.0\nelement vertex 3\nproperty float x\nproperty float y\n"
                       "property float z\nelement face 1\nproperty list uchar int vertex_indices\nend_header\n"
                       "0 0 0\n1 0 0\n0 1 0\n3 0 1 3\n";
    TEST_ASSERT_EQUAL(MESH_ERR_INDEX, read_text(text));
    TEST_ASSERT_EQUAL_size_t(13, line);
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
}

static void test_rejects_faces_with_fewer_than_three_vertices(void) {
    const char *text = "ply\nformat ascii 1.0\nelement vertex 3\nproperty float x\nproperty float y\n"
                       "property float z\nelement face 1\nproperty list uchar int vertex_indices\nend_header\n"
                       "0 0 0\n1 0 0\n0 1 0\n2 0 1\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
    TEST_ASSERT_EQUAL_size_t(13, line);
}

static void test_rejects_non_integer_and_negative_indices(void) {
    const char *head = "ply\nformat ascii 1.0\nelement vertex 3\nproperty float x\nproperty float y\n"
                       "property float z\nelement face 1\nproperty list uchar int vertex_indices\nend_header\n"
                       "0 0 0\n1 0 0\n0 1 0\n";
    char text[512];
    strcpy(text, head);
    strcat(text, "3 0 1 1.5\n");
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
    strcpy(text, head);
    strcat(text, "3 0 1 -1\n");
    TEST_ASSERT_EQUAL(MESH_ERR_INDEX, read_text(text));
}

static void test_rejects_a_truncated_ascii_body(void) {
    const char *text = "ply\nformat ascii 1.0\nelement vertex 2\nproperty float x\nproperty float y\n"
                       "property float z\nend_header\n0 0 0\n1 0\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
}

/* --- binary --------------------------------------------------------------------------------- */

static void test_reads_binary_little_and_big_endian_and_skips_other_elements(void) {
    for (int be = 0; be <= 1; be++) {
        build_binary_triangle(be);
        TEST_ASSERT_EQUAL(MESH_OK, mesh_read_ply((const char *)buf, len, &m, &line));
        TEST_ASSERT_EQUAL_size_t(3, m.vertex_count);
        TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
        TEST_ASSERT_EQUAL_DOUBLE(1.5, m.vertices[1].x);
        TEST_ASSERT_EQUAL_DOUBLE(-2.0, m.vertices[2].y);
        TEST_ASSERT_EQUAL_DOUBLE(0.25, m.vertices[2].z);
        TEST_ASSERT_EQUAL_UINT32(2, m.triangles[0][2]);
    }
}

static void test_rejects_a_truncated_binary_body(void) {
    build_binary_triangle(0);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_ply((const char *)buf, len - 1, &m, &line));
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
}

static void test_rejects_counts_larger_than_the_data_without_allocating(void) {
    const char *text = "ply\nformat binary_little_endian 1.0\nelement vertex 4000000000\nproperty float x\n"
                       "property float y\nproperty float z\nend_header\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
}

/* --- header --------------------------------------------------------------------------------- */

static void test_rejects_bad_headers_with_their_line(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("plx\nformat ascii 1.0\nend_header\n"));
    TEST_ASSERT_EQUAL_size_t(1, line);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nformat ascii 2.0\nend_header\n"));
    TEST_ASSERT_EQUAL_size_t(2, line);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nformat ascii 1.0\nelement vertex 1\nproperty quad x\n"));
    TEST_ASSERT_EQUAL_size_t(4, line);
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nformat ascii 1.0\nproperty float x\nend_header\n"));
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nformat ascii 1.0\nelement vertex 1\n"));
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nelement vertex 0\nend_header\n"));
}

/* Found by fuzzing: the buffer was sized from the last `vertex` element but filled from the first. */
static void test_uses_the_first_vertex_element_when_there_are_two(void) {
    const char *text = "ply\nformat ascii 1.0\n"
                       "element vertex 4\nproperty float x\nproperty float y\nproperty float z\n"
                       "element vertex 1\nproperty float x\nproperty float y\nproperty float z\n"
                       "element face 1\nproperty list uchar int vertex_indices\nend_header\n"
                       "0 0 0\n1 0 0\n0 1 0\n0 0 1\n"
                       "9 9 9\n"
                       "3 1 2 3\n";
    TEST_ASSERT_EQUAL(MESH_OK, read_text(text));
    TEST_ASSERT_EQUAL_size_t(4, m.vertex_count);
    TEST_ASSERT_EQUAL_DOUBLE(1.0, m.vertices[3].z);
    TEST_ASSERT_EQUAL_UINT32(3, m.triangles[0][2]);
}

/* Found by fuzzing: an element without properties takes no bytes, so a huge count looped for minutes. */
static void test_rejects_a_non_empty_element_without_properties(void) {
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text("ply\nformat ascii 1.0\nelement junk 9999999999\nend_header\n"));
    TEST_ASSERT_EQUAL(MESH_OK, read_text("ply\nformat ascii 1.0\nelement junk 0\nend_header\n"));
}

static void test_rejects_a_nul_byte_in_the_header(void) {
    const char text[] = "ply\nformat ascii 1.0\nelement vertex 1\0junk\nend_header\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_ply(text, sizeof text - 1, &m, &line));
    TEST_ASSERT_EQUAL_size_t(3, line);
}

/* Found by fuzzing (UBSan): a negative list count was converted to size_t. */
static void test_rejects_a_negative_list_count(void) {
    const char *text = "ply\nformat ascii 1.0\nelement vertex 3\nproperty float x\nproperty float y\n"
                       "property float z\nelement edge 1\nproperty list char int ends\nend_header\n"
                       "0 0 0\n1 0 0\n0 1 0\n-1 0\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
    TEST_ASSERT_EQUAL_size_t(13, line);
}

static void test_requires_x_y_and_z(void) {
    const char *text = "ply\nformat ascii 1.0\nelement vertex 1\nproperty float x\nproperty float y\n"
                       "end_header\n0 0\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, read_text(text));
}

static void test_a_header_without_elements_gives_an_empty_mesh(void) {
    TEST_ASSERT_EQUAL(MESH_OK, read_text("ply\nformat ascii 1.0\ncomment nothing\nend_header\n"));
    TEST_ASSERT_EQUAL_size_t(0, m.vertex_count);
}

/* --- detection and files -------------------------------------------------------------------- */

static void test_read_buffer_detects_ply_and_obj(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_buffer(SQUARE_PYRAMID, strlen(SQUARE_PYRAMID), &m, &line));
    TEST_ASSERT_EQUAL_size_t(5, m.vertex_count);
    const char obj[] = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_buffer(obj, sizeof obj - 1, &m, &line)); /* no NUL needed */
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
}

static void test_read_buffer_rejects_a_nul_byte_inside_obj_text(void) {
    const char obj[] = "v 0 0 0\nv 1\0 0 0\n";
    TEST_ASSERT_EQUAL(MESH_ERR_SYNTAX, mesh_read_buffer(obj, sizeof obj - 1, &m, &line));
    TEST_ASSERT_EQUAL_size_t(2, line);
}

/* Seen in a ZBrush export: the file ends with NUL padding after its last line. */
static void test_read_buffer_accepts_nul_padding_at_the_end_of_obj_text(void) {
    const char obj[] = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n\n\0\n\0\0";
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_buffer(obj, sizeof obj - 1, &m, &line));
    TEST_ASSERT_EQUAL_size_t(1, m.triangle_count);
}

static void test_read_file_detects_the_format_from_the_content(void) {
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_file(MESH_TEST_DATA "/tetrahedron.ply", &m, &line));
    TEST_ASSERT_EQUAL_size_t(4, m.vertex_count);
    TEST_ASSERT_EQUAL_size_t(4, m.triangle_count);
    TEST_ASSERT_EQUAL(MESH_OK, mesh_read_file(MESH_TEST_DATA "/cube.obj", &m, &line));
    TEST_ASSERT_EQUAL_size_t(12, m.triangle_count);
    TEST_ASSERT_EQUAL(MESH_ERR_IO, mesh_read_file(MESH_TEST_DATA "/missing.ply", &m, &line));
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_reads_ascii_with_extra_properties_and_crlf);
    RUN_TEST(test_reports_an_ascii_index_out_of_range_with_its_line);
    RUN_TEST(test_rejects_faces_with_fewer_than_three_vertices);
    RUN_TEST(test_rejects_non_integer_and_negative_indices);
    RUN_TEST(test_rejects_a_truncated_ascii_body);
    RUN_TEST(test_reads_binary_little_and_big_endian_and_skips_other_elements);
    RUN_TEST(test_rejects_a_truncated_binary_body);
    RUN_TEST(test_rejects_counts_larger_than_the_data_without_allocating);
    RUN_TEST(test_rejects_bad_headers_with_their_line);
    RUN_TEST(test_uses_the_first_vertex_element_when_there_are_two);
    RUN_TEST(test_rejects_a_non_empty_element_without_properties);
    RUN_TEST(test_rejects_a_nul_byte_in_the_header);
    RUN_TEST(test_rejects_a_negative_list_count);
    RUN_TEST(test_requires_x_y_and_z);
    RUN_TEST(test_a_header_without_elements_gives_an_empty_mesh);
    RUN_TEST(test_read_buffer_detects_ply_and_obj);
    RUN_TEST(test_read_buffer_rejects_a_nul_byte_inside_obj_text);
    RUN_TEST(test_read_buffer_accepts_nul_padding_at_the_end_of_obj_text);
    RUN_TEST(test_read_file_detects_the_format_from_the_content);
    return UNITY_END();
}
