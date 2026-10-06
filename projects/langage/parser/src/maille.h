/* Public API of the Maille parser: source text in, syntax tree (or a located error) out. */
#ifndef MAILLE_H
#define MAILLE_H

#include <stddef.h>

#include "ast.h"

typedef enum { MAILLE_OK = 0, MAILLE_SYNTAX_ERROR = 1, MAILLE_OUT_OF_MEMORY = 2 } maille_status;

typedef struct {
    node *tree;          /* set on success; free with node_free */
    int line, column;    /* position of the error */
    char message[256];   /* error message, empty on success */
} maille_result;

/* Parses `len` bytes of source (no terminating zero needed). */
maille_status maille_parse(const char *source, size_t len, maille_result *result);

#endif
