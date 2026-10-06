/* maillec: parses a Maille program and prints its syntax tree as an S-expression.
 *   maillec program.maille      (or - for standard input)
 * On an error, prints "file:line:column: error: message" on standard error and exits with 1. */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "maille.h"

/* Programs above this size are refused (the demo and the tests stay far below). */
#define MAX_SOURCE (16u << 20)

static char *read_all(FILE *f, size_t *len) {
    size_t cap = 4096, n = 0;
    char *buf = malloc(cap);
    while (buf) {
        n += fread(buf + n, 1, cap - n, f);
        if (n < cap || n >= MAX_SOURCE) break;
        char *grown = realloc(buf, cap * 2);
        if (!grown) { free(buf); return NULL; }
        buf = grown;
        cap *= 2;
    }
    if (buf && (ferror(f) || n >= MAX_SOURCE)) { free(buf); return NULL; }
    *len = n;
    return buf;
}

int main(int argc, char **argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: maillec <file.maille | ->\n");
        return 2;
    }
    const char *name = argv[1];
    FILE *f = strcmp(name, "-") == 0 ? stdin : fopen(name, "rb");
    if (!f) {
        fprintf(stderr, "%s: cannot open the file\n", name);
        return 2;
    }
    size_t len = 0;
    char *source = read_all(f, &len);
    if (f != stdin) fclose(f);
    if (!source) {
        fprintf(stderr, "%s: cannot read the file (or larger than 16 MB)\n", name);
        return 2;
    }
    maille_result result;
    maille_status status = maille_parse(source, len, &result);
    free(source);
    if (status != MAILLE_OK) {
        fprintf(stderr, "%s:%d:%d: error: %s\n", name, result.line, result.column, result.message);
        return 1;
    }
    node_print(stdout, result.tree);
    fputc('\n', stdout);
    node_free(result.tree);
    return 0;
}
