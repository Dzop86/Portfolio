/* Syntax tree of a Maille program, built by the Bison parser and printed as an S-expression for the
 * OCaml interpreter. Every node carries the line and column where it starts (1-based). */
#ifndef MAILLE_AST_H
#define MAILLE_AST_H

#include <stdio.h>

typedef enum {
    N_INT, N_FLOAT, N_STRING, N_BOOL, N_VAR,
    N_LET, N_LETREC, N_FUN, N_APP, N_IF, N_BINOP, N_UNOP
} node_kind;

/* Deepest tree accepted: every recursive walk (printing, freeing, the OCaml interpreter) stays bounded,
 * within the 64 KB stack of the WebAssembly build and the larger frames of an ASan build. */
#define MAILLE_MAX_DEPTH 1000

typedef struct node {
    node_kind kind;
    int line, column;
    int depth;             /* 1 for a leaf, 1 + the deepest child otherwise */
    long long int_value;   /* N_INT, N_BOOL (0 or 1) */
    double float_value;    /* N_FLOAT */
    char *text;            /* N_STRING contents, N_VAR / N_LET / N_LETREC / N_FUN name, operator */
    struct node *a, *b, *c; /* children: let = value, body; fun = body; app = function, argument;
                               if = condition, then, else; binop = left, right; unop = operand */
} node;

node *node_new(node_kind kind, int line, int column);
/* Takes ownership of `text` (may be NULL). */
node *node_text(node_kind kind, int line, int column, char *text);
/* Sets the children of `n` and its depth; returns NULL (freeing everything) if out of memory or too deep. */
node *node_with(node *n, node *a, node *b, node *c);
void node_free(node *n);

/* Writes the tree as one S-expression, e.g. (binop 1:3 + (int 1:1 1) (int 1:5 2)). */
void node_print(FILE *out, const node *n);
/* Same, into a newly allocated string (caller frees); NULL if out of memory. */
char *node_to_string(const node *n);

#endif
