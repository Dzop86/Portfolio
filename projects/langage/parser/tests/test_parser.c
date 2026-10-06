/* Unit tests of the Maille parser through its C API: trees, precedence, desugaring, located errors,
 * limits. Run under ASan and UBSan in CI, so leaks on error paths fail the test too. */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "maille.h"

static int failures = 0;

#define CHECK(cond, ...)                                              \
    do {                                                              \
        if (!(cond)) {                                                \
            failures++;                                               \
            fprintf(stderr, "%s:%d: ", __FILE__, __LINE__);           \
            fprintf(stderr, __VA_ARGS__);                             \
            fputc('\n', stderr);                                      \
        }                                                             \
    } while (0)

/* Parses `src` and compares the printed tree with `expected`. */
static void tree(const char *src, const char *expected) {
    maille_result r;
    maille_status st = maille_parse(src, strlen(src), &r);
    CHECK(st == MAILLE_OK, "\"%s\": status %d (%d:%d %s)", src, st, r.line, r.column, r.message);
    if (st != MAILLE_OK) return;
    char *got = node_to_string(r.tree);
    CHECK(got && strcmp(got, expected) == 0, "\"%s\"\n  expected %s\n  got      %s", src, expected, got);
    free(got);
    node_free(r.tree);
}

/* Parses `src` and expects an error at line:column whose message contains `part`. */
static void error_at(const char *src, size_t len, int line, int column, const char *part) {
    maille_result r;
    maille_status st = maille_parse(src, len, &r);
    CHECK(st == MAILLE_SYNTAX_ERROR && r.tree == NULL, "\"%s\": expected an error, status %d", src, st);
    CHECK(r.line == line && r.column == column, "\"%s\": error at %d:%d, expected %d:%d (%s)",
          src, r.line, r.column, line, column, r.message);
    CHECK(strstr(r.message, part) != NULL, "\"%s\": message \"%s\" lacks \"%s\"", src, r.message, part);
}
#define ERROR_AT(src, line, column, part) error_at(src, strlen(src), line, column, part)

static void literals(void) {
    tree("42", "(int 1:1 42)");
    tree("2.5", "(float 1:1 2.5)");
    tree("2.", "(float 1:1 2.0)");
    tree("1e3", "(float 1:1 1000.0)");
    tree("0.1", "(float 1:1 0.1)");
    tree("true", "(bool 1:1 true)");
    tree("\"a\\\"b\\n\\\\\"", "(str 1:1 \"a\\\"b\\n\\\\\")");
    tree("x_1'", "(var 1:1 x_1')");
    tree("4611686018427387903", "(int 1:1 4611686018427387903)");
}

static void precedence(void) {
    tree("1 + 2 * 3", "(binop 1:3 + (int 1:1 1) (binop 1:7 * (int 1:5 2) (int 1:9 3)))");
    tree("1 - 2 - 3", "(binop 1:7 - (binop 1:3 - (int 1:1 1) (int 1:5 2)) (int 1:9 3))");
    tree("a ^ b ^ c", "(binop 1:3 ^ (var 1:1 a) (binop 1:7 ^ (var 1:5 b) (var 1:9 c)))");
    tree("f x y", "(app 1:1 (app 1:1 (var 1:1 f) (var 1:3 x)) (var 1:5 y))");
    tree("-f x", "(unop 1:1 - (app 1:2 (var 1:2 f) (var 1:4 x)))");
    tree("not a && b", "(binop 1:7 && (unop 1:1 not (var 1:5 a)) (var 1:10 b))");
    tree("a || b && c", "(binop 1:3 || (var 1:1 a) (binop 1:8 && (var 1:6 b) (var 1:11 c)))");
    tree("1 + 2 == 3", "(binop 1:7 == (binop 1:3 + (int 1:1 1) (int 1:5 2)) (int 1:10 3))");
    tree("(1 + 2) * 3", "(binop 1:9 * (binop 1:4 + (int 1:2 1) (int 1:6 2)) (int 1:11 3))");
    tree("x *. 2.0 +. y", "(binop 1:10 +. (binop 1:3 *. (var 1:1 x) (float 1:6 2.0)) (var 1:13 y))");
    tree("f (g x)", "(app 1:1 (var 1:1 f) (app 1:4 (var 1:4 g) (var 1:6 x)))");
}

static void binders(void) {
    tree("let x = 1 in x", "(let 1:1 x (int 1:9 1) (var 1:14 x))");
    tree("let f x y = x in f",
         "(let 1:1 f (fun 1:7 x (fun 1:9 y (var 1:13 x))) (var 1:18 f))");
    tree("let rec f n = f n in f",
         "(letrec 1:1 f (fun 1:11 n (app 1:15 (var 1:15 f) (var 1:17 n))) (var 1:22 f))");
    tree("fun x y -> x", "(fun 1:5 x (fun 1:7 y (var 1:12 x)))");
    tree("if a then 1 else 2", "(if 1:1 (var 1:4 a) (int 1:11 1) (int 1:18 2))");
    /* let extends as far right as possible. */
    tree("let x = 1 in x + 1", "(let 1:1 x (int 1:9 1) (binop 1:16 + (var 1:14 x) (int 1:18 1)))");
    tree("# comment\nlet x = 1 # trailing\nin x", "(let 2:1 x (int 2:9 1) (var 3:4 x))");
}

static void errors(void) {
    ERROR_AT("let x = in 3", 1, 9, "unexpected in");
    ERROR_AT("1 +", 1, 4, "unexpected end of file");
    ERROR_AT("1 < 2 < 3", 1, 7, "unexpected <");
    ERROR_AT("let rec f = 1 in f", 1, 11, "unexpected =");
    ERROR_AT("(1 + 2", 1, 7, "expecting )");
    ERROR_AT("1 $ 2", 1, 3, "unexpected character '$'");
    ERROR_AT("let X = 1 in X", 1, 5, "lower-case");
    ERROR_AT("\"abc", 1, 1, "unterminated string");
    ERROR_AT("\"a\nb\"", 1, 1, "unterminated string");
    ERROR_AT("\"a\\qb\"", 1, 3, "unknown escape \\q");
    ERROR_AT("4611686018427387904", 1, 1, "too large");
    ERROR_AT("1e999", 1, 1, "out of range");
    ERROR_AT("1 +\n\n  )", 3, 3, "unexpected )");
    /* Columns count characters: "é" is two bytes but one column. */
    ERROR_AT("\"é\" ^ $", 1, 7, "unexpected character");
    error_at("1 +\0 2", 6, 1, 1, "NUL byte");
    ERROR_AT("", 1, 1, "unexpected end of file");
}

static char *repeat(const char *head, const char *unit, int times, const char *tail) {
    size_t len = strlen(head) + strlen(unit) * (size_t)times + strlen(tail) + 1;
    char *s = malloc(len);
    if (!s) return NULL;
    char *p = s;
    p += sprintf(p, "%s", head);
    for (int i = 0; i < times; i++) p += sprintf(p, "%s", unit);
    sprintf(p, "%s", tail);
    return s;
}

static void limits(void) {
    /* 900 additions nest 900 deep: accepted; 2 000 are refused, not a crash. */
    char *ok = repeat("1", " + 1", 900, "");
    char *deep = repeat("1", " + 1", 2000, "");
    char *parens = repeat("", "(", 20000, "1");
    CHECK(ok && deep && parens, "out of memory in the test");
    if (ok && deep && parens) {
        maille_result r;
        CHECK(maille_parse(ok, strlen(ok), &r) == MAILLE_OK, "900 additions: %s", r.message);
        node_free(r.tree);
        /* The 1 000th "+" (column 3 + 4 x 999) would make the tree 1 001 deep. */
        error_at(deep, strlen(deep), 1, 3 + 4 * 999, "nested too deeply");
        error_at(parens, strlen(parens), 1, 1, "nested too deeply");
    }
    free(ok);
    free(deep);
    free(parens);
}

int main(void) {
    literals();
    precedence();
    binders();
    errors();
    limits();
    if (failures) fprintf(stderr, "%d failure(s)\n", failures);
    else printf("all parser tests passed\n");
    return failures ? 1 : 0;
}
