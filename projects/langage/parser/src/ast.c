#include "ast.h"

#include <stdlib.h>
#include <string.h>

node *node_new(node_kind kind, int line, int column) {
    node *n = calloc(1, sizeof *n);
    if (!n) return NULL;
    n->kind = kind;
    n->line = line;
    n->column = column;
    n->depth = 1;
    return n;
}

node *node_text(node_kind kind, int line, int column, char *text) {
    node *n = node_new(kind, line, column);
    if (!n) {
        free(text);
        return NULL;
    }
    n->text = text;
    return n;
}

node *node_with(node *n, node *a, node *b, node *c) {
    int depth = 0;
    const node *children[] = { a, b, c };
    for (int i = 0; i < 3; i++) {
        if (children[i] && children[i]->depth > depth) depth = children[i]->depth;
    }
    if (!n || depth >= MAILLE_MAX_DEPTH) {
        node_free(n);
        node_free(a);
        node_free(b);
        node_free(c);
        return NULL;
    }
    n->a = a;
    n->b = b;
    n->c = c;
    n->depth = depth + 1;
    return n;
}

void node_free(node *n) {
    if (!n) return;
    node_free(n->a);
    node_free(n->b);
    node_free(n->c);
    free(n->text);
    free(n);
}

static const char *KIND_NAMES[] = {
    "int", "float", "str", "bool", "var", "let", "letrec", "fun", "app", "if", "binop", "unop"
};

static void print_string(FILE *out, const char *s) {
    fputc('"', out);
    for (; *s; s++) {
        switch (*s) {
        case '"': fputs("\\\"", out); break;
        case '\\': fputs("\\\\", out); break;
        case '\n': fputs("\\n", out); break;
        case '\t': fputs("\\t", out); break;
        default: fputc(*s, out);
        }
    }
    fputc('"', out);
}

void node_print(FILE *out, const node *n) {
    fprintf(out, "(%s %d:%d", KIND_NAMES[n->kind], n->line, n->column);
    switch (n->kind) {
    case N_INT: fprintf(out, " %lld", n->int_value); break;
    /* Shortest of %.15g, %.16g, %.17g that reads back as the same double (%.17g always does);
     * a value like 2 gets a ".0" so it still reads as a float. */
    case N_FLOAT: {
        char buf[40];
        for (int digits = 15; digits <= 17; digits++) {
            snprintf(buf, sizeof buf, "%.*g", digits, n->float_value);
            if (strtod(buf, NULL) == n->float_value) break;
        }
        /* Some C libraries on Windows write three exponent digits (1e+020): keep two, as elsewhere. */
        char *e = strchr(buf, 'e');
        if (e && (e[1] == '+' || e[1] == '-')) {
            char *d = e + 2;
            while (d[0] == '0' && strlen(d) > 2) memmove(d, d + 1, strlen(d));
        }
        fprintf(out, " %s%s", buf, strpbrk(buf, ".eEn") ? "" : ".0");
        break;
    }
    case N_STRING: fputc(' ', out); print_string(out, n->text); break;
    case N_BOOL: fputs(n->int_value ? " true" : " false", out); break;
    case N_VAR: fprintf(out, " %s", n->text); break;
    case N_LET: case N_LETREC: case N_FUN: case N_BINOP: case N_UNOP:
        fprintf(out, " %s", n->text);
        break;
    default: break;
    }
    const node *children[] = { n->a, n->b, n->c };
    for (int i = 0; i < 3; i++) {
        if (children[i]) {
            fputc(' ', out);
            node_print(out, children[i]);
        }
    }
    fputc(')', out);
}

char *node_to_string(const node *n) {
    char *buf = NULL;
    size_t len = 0;
#if defined(_WIN32)
    /* No open_memstream on Windows: print to a temporary file and read it back. */
    FILE *f = tmpfile();
    if (!f) return NULL;
    node_print(f, n);
    long size = ftell(f);
    rewind(f);
    buf = malloc((size_t)size + 1);
    if (buf) {
        len = fread(buf, 1, (size_t)size, f);
        buf[len] = '\0';
    }
    fclose(f);
#else
    FILE *f = open_memstream(&buf, &len);
    if (!f) return NULL;
    node_print(f, n);
    fclose(f);
#endif
    return buf;
}
